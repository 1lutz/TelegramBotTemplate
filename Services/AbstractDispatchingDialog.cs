using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private readonly ILogger _logger;
        private readonly Dictionary<Type, TypeConverter> _converters;
        private readonly Dictionary<string, MethodInfo> _commands;
        private readonly Dictionary<string, MethodInfo> _callbacks;
        private readonly Task<IMessengerResponse> _helpText;

        private bool TryAddConverter(Type destinationType)
        {
            if (_converters.ContainsKey(destinationType)) return true;
            TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
            if (!converter.CanConvertFrom(typeof(string))) return false;
            _converters.Add(destinationType, converter);
            return true;
        }

        private string NormalizeName(string name, string separator = "_")
        {
            name = Regex.Replace(name, "(^|[a-z])[A-Z]", m => m.Length == 1 ? char.ToLower(m.Value[0]).ToString() : m.Value[0] + separator + char.ToLower(m.Value[1]));
            if (name.EndsWith("async")) name = name.Substring(0, name.Length - 6);
            return name;
        }

        private Task<IMessengerResponse> Pack(object response)
        {
            return response is Task<IMessengerResponse> t ? t : Task.FromResult((IMessengerResponse)response);
        }

        private Task<IMessengerResponse> InvokeMethodAsync(MethodInfo method, User user, string[] args, out bool isError)
        {
            isError = true;
            ParameterInfo[] paramInfos = method.GetParameters();
            int paramCount = paramInfos.Length - 1;

            if (args.Length < paramCount)
            {
                switch (paramCount)
                {
                    case 1:
                        return Task.FromResult(Text("This command requires _one parameter_. Use /help to learn more."));

                    default:
                        return Task.FromResult(Text("This command requires _" + paramCount + " parameters_. Use /help to learn more."));
                }
            }
            object[] methodArgs = new object[paramInfos.Length];
            methodArgs[0] = user;

            for (int x = 1; x < paramInfos.Length; ++x)
            {
                try
                {
                    methodArgs[x] = _converters[paramInfos[x].ParameterType].ConvertFromString(args[0]);
                }
                catch (ArgumentException)
                {
                    return Task.FromResult(Text("The parameter `" + NormalizeName(paramInfos[x].Name, "_") + "` has invalid data. Use /help to learn more."));
                }
            }
            isError = false;
            object res = method.Invoke(this, methodArgs);
            return res is Task<IMessengerResponse> t ? t : Task.FromResult((IMessengerResponse)res);
        }

        public AbstractDispatchingDialog(ILogger<AbstractDispatchingDialog> logger)
        {
            _logger = logger;
            _converters = new Dictionary<Type, TypeConverter>();
            _commands = new Dictionary<string, MethodInfo>();
            _callbacks = new Dictionary<string, MethodInfo>();
            StringBuilder helpTextBuilder = new StringBuilder();
            helpTextBuilder.AppendLine("These are all available commands:");
            //Public instance methods, which are directly declared in a subclass
            MethodInfo[] allMethodInfos = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in allMethodInfos)
            {
                //Has return type IMessengerResponse or Task<IMessengerResponse> and does not override something
                if (method.IsVirtual || (method.ReturnType != typeof(IMessengerResponse) && method.ReturnType != typeof(Task<IMessengerResponse>))) continue;
                //First parameter must have type User
                ParameterInfo[] paramInfos = method.GetParameters();
                bool paramValidationError = false;

                if (paramInfos.Length == 0 || paramInfos[0].ParameterType != typeof(User))
                {
                    paramValidationError = true;
                    logger.LogError("Failed to register command \"{Command}\": The first parameter must be of type User.", method.Name);
                }
                //Ensure other parameters can be parsed
                for (int x = 1; x < paramInfos.Length; ++x)
                {
                    if (!TryAddConverter(paramInfos[x].ParameterType))
                    {
                        paramValidationError = true;
                        logger.LogError("Failed to register command \"{Command}\": Parameters of type {ParamType} are not allowed.", method.Name, paramInfos[x].ParameterType.Name);
                    }
                }
                if (paramValidationError) continue;

                //Normalize method name
                string name = NormalizeName(method.Name);

                if (name.EndsWith("callback"))
                {
                    //Add callback to dispatcher
                    name = name.Substring(0, name.Length - 9);
                    _callbacks.Add(name, method);
                }
                else
                {
                    //Add command to dispatcher
                    _commands.Add(name, method);

                    if (name != "start")
                    {
                        //Generate help text for command
                        helpTextBuilder.Append('/').Append(name.Replace("_", "\\_"));

                        for (int x = 1; x < paramInfos.Length; ++x)
                        {
                            helpTextBuilder.Append(" <").Append(NormalizeName(paramInfos[x].Name, " ")).Append('>');
                        }
                        DescriptionAttribute description = method.GetCustomAttribute<DescriptionAttribute>();
                        if (description != null) helpTextBuilder.Append(" - ").Append(description.Description);
                        helpTextBuilder.AppendLine();
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
            if (_commands.TryGetValue(command, out MethodInfo method))
            {
                return InvokeMethodAsync(method, user, args, out _);
            }
            return Task.FromResult(Text("Unrecognized command. You can use /help to show all available commands."));
        }

        public override Task<IMessengerResponse> HandleCallbackAsync(User user, string command, string[] args)
        {
            command = NormalizeName(command);
            if (command.EndsWith("callback")) command = command.Substring(0, command.Length - 9);

            if (_callbacks.TryGetValue(command, out MethodInfo method))
            {
                bool isError;
                var res = InvokeMethodAsync(method, user, args, out isError);

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
