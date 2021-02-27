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
        private readonly Task<IMessengerResponse> UNKNOWN_COMMAND;
        private readonly Task<IMessengerResponse> UNKNOWN_CALLBACK;
        private readonly Dictionary<string, MethodInfo> _commands;
        private readonly Dictionary<string, MethodInfo> _callbacks;
        private readonly Task<IMessengerResponse> _helpText;

        private string NormalizeMethodName(string name)
        {
            name = Regex.Replace(name, "(^|[a-z])[A-Z]", m => m.Length == 1 ? char.ToLower(m.Value[0]).ToString() : m.Value[0] + "_" + char.ToLower(m.Value[1]));
            if (name.EndsWith("async")) name = name.Substring(0, name.Length - 6);
            return name;
        }

        private object[] BuildMethodArgs(User user, string[] args, int length)
        {
            object[] methodArgs = new object[length + 1];
            methodArgs[0] = user;
            Array.Copy(args, 0, methodArgs, 1, length);
            return methodArgs;
        }

        private Task<IMessengerResponse> Pack(object response)
        {
            return response is Task<IMessengerResponse> t ? t : Task.FromResult((IMessengerResponse)response);
        }

        public AbstractDispatchingDialog()
        {
            UNKNOWN_COMMAND = Task.FromResult(Text("Unrecognized command. You can use /help to show all available commands."));
            UNKNOWN_CALLBACK = Task.FromResult(Nothing());
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
                //First parameter must have type User and the other string
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(User) || parameters.Skip(1).Any(o => o.ParameterType != typeof(string))) continue;

                //Normalize method name
                string name = NormalizeMethodName(method.Name);

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

                        for (int x = 1; x < parameters.Length; ++x)
                        {
                            helpTextBuilder.Append(" <").Append(parameters[x].Name).Append('>');
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
                int paramCount = method.GetParameters().Length - 1;

                if (args.Length < paramCount)
                {
                    switch (paramCount)
                    {
                        case 1:
                            return Task.FromResult(Text("This command requires one parameter. Use /help to learn more."));

                        default:
                            return Task.FromResult(Text("This command requires " + paramCount + " parameters. Use /help to learn more."));
                    }
                }
                object[] methodArgs = BuildMethodArgs(user, args, paramCount);
                return Pack(method.Invoke(this, methodArgs));
            }
            else
            {
                return UNKNOWN_COMMAND;
            }
        }

        public override Task<IMessengerResponse> HandleCallbackAsync(User user, string command, string[] args)
        {
            command = NormalizeMethodName(command);
            if (command.EndsWith("callback")) command = command.Substring(0, command.Length - 9);

            if (_callbacks.TryGetValue(command, out MethodInfo method))
            {
                object[] methodArgs = BuildMethodArgs(user, args, args.Length);
                return Pack(method.Invoke(this, methodArgs));
            }
            else
            {
                return UNKNOWN_CALLBACK;
            }
        }
    }

    public static class KeyboardExtensions
    {
        public static Keyboard Append(this Keyboard keyboard, string text, Func<User, string, IMessengerResponse> callback, string arg)
        {
            return keyboard.Append(text, callback.Method.Name + ";" + arg);
        }

        public static Keyboard Append(this Keyboard keyboard, string text, Func<User, string, string, IMessengerResponse> callback, string arg1, string arg2)
        {
            return keyboard.Append(text, callback.Method.Name + ";" + arg1 + ";" + arg2);
        }

        public static Keyboard Append(this Keyboard keyboard, string text, Func<User, string, string, string, IMessengerResponse> callback, string arg1, string arg2, string arg3)
        {
            return keyboard.Append(text, callback.Method.Name + ";" + arg1 + ";" + arg2 + ";" + arg3);
        }
    }
}
