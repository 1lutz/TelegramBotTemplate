using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TelegramBotTemplate.Models;
using TelegramBotTemplate.Models.Replies;

namespace TelegramBotTemplate.Services
{
    public abstract class AbstractDispatchingDialog : AbstractDialog
    {
        class BotCommandDescriptor
        {
            private static readonly Dictionary<Type, TypeConverter> _converters = new Dictionary<Type, TypeConverter>();
            private readonly AbstractDispatchingDialog _dialog;
            private readonly MethodInfo _method;
            private readonly ParameterInfo[] _paramInfos;
            private readonly string[] _paramNames;
            private readonly Func<User, string[], Tuple<Task<IMessengerResponse>, bool>> _executor;

            public string Name { get; }

            public bool IsCallback { get; }

            public string HelpText
            {
                get
                {
                    string s = "/" + Name.Replace("_", "\\_");

                    for (int x = 1; x < _paramInfos.Length; ++x)
                    {
                        s += " <" + _paramNames[x] + ">";
                    }
                    DescriptionAttribute description = _method.GetCustomAttribute<DescriptionAttribute>();
                    if (description != null) s += " - " + description.Description;
                    return s;
                }
            }

            private static bool TryAddConverter(Type destinationType)
            {
                if (_converters.ContainsKey(destinationType)) return true;
                TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
                if (!converter.CanConvertFrom(typeof(string))) return false;
                _converters.Add(destinationType, converter);
                return true;
            }

            public static string NormalizeName(string name, string separator = "_")
            {
                name = Regex.Replace(name, "(^|[a-z])[A-Z]", m => m.Length == 1 ? char.ToLower(m.Value[0]).ToString() : m.Value[0] + separator + char.ToLower(m.Value[1]));
                if (name.EndsWith("async")) name = name.Substring(0, name.Length - 6);
                return name;
            }

            private Func<User, string[], Tuple<Task<IMessengerResponse>, bool>> GenerateExecutor()
            {
                ParameterExpression paramUser = Expression.Parameter(typeof(User), "user");
                ParameterExpression paramArgs = Expression.Parameter(typeof(string[]), "args");
                Expression[] parsedArgs = new Expression[_paramInfos.Length];
                parsedArgs[0] = paramUser;

                for (int x = 1; x < _paramInfos.Length; ++x)
                {
                    Expression getter = Expression.ArrayIndex(paramArgs, Expression.Constant(x - 1));

                    if (_paramInfos[x].ParameterType != typeof(string))
                    {
                        getter = Expression.Call(
                            _paramInfos[x].ParameterType.GetMethod("Parse", new[] { typeof(string) }),
                            getter
                        );
                    }
                    parsedArgs[x] = getter;
                }
                Expression caller = Expression.Call(
                    Expression.Constant(_dialog),
                    _method,
                    parsedArgs
                );

                if (_method.ReturnType != typeof(Task<IMessengerResponse>))
                {
                    caller = Expression.Call(
                        typeof(Task).GetMethod("FromResult").MakeGenericMethod(typeof(IMessengerResponse)),
                        caller
                    );
                }
                Expression wrappedInTuple = Expression.Call(
                    typeof(Tuple), "Create", new[] { typeof(Task<IMessengerResponse>), typeof(bool) },
                    new[] { caller, Expression.Constant(false) }
                );
                int paramCount = _paramInfos.Length - 1;
                Expression validatedArgCount;

                if (paramCount == 0)
                {
                    validatedArgCount = wrappedInTuple;
                }
                else
                {
                    string errorMessage = "This command requires _" + (paramCount == 1 ? "one parameter" : paramCount + " parameters") + "_. Use /help to learn more.";
                    validatedArgCount = Expression.Condition(
                        Expression.LessThan(Expression.ArrayLength(paramArgs), Expression.Constant(paramCount)),
                        Expression.Call(
                            typeof(Tuple), "Create", new[] { typeof(Task<IMessengerResponse>), typeof(bool) },
                            new Expression[] {
                                    Expression.Call(
                                        typeof(Task).GetMethod("FromResult").MakeGenericMethod(typeof(IMessengerResponse)),
                                        Expression.New(
                                            typeof(TextMessageResponse).GetConstructor(new[] { typeof(string), typeof(Keyboard), typeof(bool) }),
                                            Expression.Constant(errorMessage),
                                            Expression.Constant(null, typeof(Keyboard)),
                                            Expression.Constant(false)
                                        )
                                    ),
                                    Expression.Constant(true)
                            }
                        ),
                        wrappedInTuple
                    );
                }
                var lambda = Expression.Lambda<Func<User, string[], Tuple<Task<IMessengerResponse>, bool>>>(
                    validatedArgCount,
                    paramUser,
                    paramArgs
                );
                return lambda.Compile();
            }

            private BotCommandDescriptor(AbstractDispatchingDialog dialog, MethodInfo method, ParameterInfo[] paramInfos)
            {
                _dialog = dialog;
                _method = method;
                _paramInfos = paramInfos;
                Name = NormalizeName(method.Name);

                if (Name.EndsWith("callback"))
                {
                    IsCallback = true;
                    Name = Name.Substring(0, Name.Length - 9);
                }
                _paramNames = new string[paramInfos.Length];

                for (int x = 0; x < paramInfos.Length; ++x)
                {
                    _paramNames[x] = NormalizeName(paramInfos[x].Name, " ");
                }
                _executor = GenerateExecutor();
            }

            public static BotCommandDescriptor TryCreateCommandAdapter(AbstractDispatchingDialog dialog, ILogger logger, MethodInfo method)
            {
                //Has return type IMessengerResponse or Task<IMessengerResponse> and does not override something
                if (method.IsVirtual || (method.ReturnType != typeof(IMessengerResponse) && method.ReturnType != typeof(Task<IMessengerResponse>))) return null;
                //First parameter must have type User
                ParameterInfo[] paramInfos = method.GetParameters();

                if (paramInfos.Length == 0 || paramInfos[0].ParameterType != typeof(User))
                {
                    logger.LogError("Failed to register command \"{Command}\": The first parameter must be of type User.", method.Name);
                    return null;
                }
                //Ensure other parameters can be parsed
                for (int x = 1; x < paramInfos.Length; ++x)
                {
                    if (!TryAddConverter(paramInfos[x].ParameterType))
                    {
                        logger.LogError("Failed to register command \"{Command}\": Parameters of type {ParamType} are not allowed.", method.Name, paramInfos[x].ParameterType.Name);
                        return null;
                    }
                }
                return new BotCommandDescriptor(dialog, method, paramInfos);
            }

            public Task<IMessengerResponse> InvokeAsync(User user, string[] args, out bool isError)
            {
                Tuple<Task<IMessengerResponse>, bool> res = _executor(user, args);
                isError = res.Item2;
                return res.Item1;
            }
        }

        private readonly ILogger _logger;
        private readonly Dictionary<string, BotCommandDescriptor> _commands;
        private readonly Dictionary<string, BotCommandDescriptor> _callbacks;
        private readonly Task<IMessengerResponse> _helpText;

        public AbstractDispatchingDialog(ILogger<AbstractDispatchingDialog> logger)
        {
            _logger = logger;
            _commands = new Dictionary<string, BotCommandDescriptor>();
            _callbacks = new Dictionary<string, BotCommandDescriptor>();
            StringBuilder helpTextBuilder = new StringBuilder();
            helpTextBuilder.AppendLine("These are all available commands:");
            //Public instance methods, which are directly declared in a subclass
            MethodInfo[] allMethodInfos = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in allMethodInfos)
            {
                BotCommandDescriptor adapter = BotCommandDescriptor.TryCreateCommandAdapter(this, logger, method);
                if (adapter == null) continue;

                if (adapter.IsCallback)
                {
                    //Add callback to dispatcher
                    _callbacks.Add(adapter.Name, adapter);
                }
                else
                {
                    //Add command to dispatcher
                    _commands.Add(adapter.Name, adapter);

                    if (adapter.Name != "start")
                    {
                        helpTextBuilder.AppendLine(adapter.HelpText);
                    }
                }
            }
            helpTextBuilder.Append("/help - Shows this page");
            _helpText = Task.FromResult(Text(helpTextBuilder.ToString()));
        }

        public override Task<IMessengerResponse> HandleCommandAsync(User user, string command, string[] args)
        {
            if (command == "help")
            {
                return _helpText;
            }
            if (_commands.TryGetValue(command, out BotCommandDescriptor method))
            {
                return method.InvokeAsync(user, args, out _);
            }
            return Task.FromResult(Text("Unrecognized command. You can use /help to show all available commands."));
        }

        public override Task<IMessengerResponse> HandleCallbackAsync(User user, string command, string[] args)
        {
            command = BotCommandDescriptor.NormalizeName(command);
            if (command.EndsWith("callback")) command = command.Substring(0, command.Length - 9);

            if (_callbacks.TryGetValue(command, out BotCommandDescriptor method))
            {
                bool isError;
                var res = method.InvokeAsync(user, args, out isError);

                if (isError)
                    _logger.LogWarning("Failed to invoke callback \"{Callback}\": {Message} Args: {Args}", method.Name, res.Result, string.Join(';', args));
                else
                    return res;
            }
            else
            {
                _logger.LogWarning("Tried to invoke the non-existent callback \"{Callback}\".", command);
            }
            return Task.FromResult(Nothing());
        }
    }

    public static class KeyboardExtensions
    {
        public static Keyboard Append<T>(this Keyboard keyboard, string text, Func<User, T, IMessengerResponse> callback, T arg)
        {
            return keyboard.Append(text, callback.Method.Name + ";" + arg);
        }

        public static Keyboard Append<T1, T2>(this Keyboard keyboard, string text, Func<User, T1, T2, IMessengerResponse> callback, T1 arg1, T2 arg2)
        {
            return keyboard.Append(text, callback.Method.Name + ";" + arg1 + ";" + arg2);
        }

        public static Keyboard Append<T1, T2, T3>(this Keyboard keyboard, string text, Func<User, T1, T2, T3, IMessengerResponse> callback, T1 arg1, T2 arg2, T3 arg3)
        {
            return keyboard.Append(text, callback.Method.Name + ";" + arg1 + ";" + arg2 + ";" + arg3);
        }
    }
}
