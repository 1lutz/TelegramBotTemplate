using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TelegramBotTemplate.Models;
using TelegramBotTemplate.Models.Replies;

namespace TelegramBotTemplate.Services
{
    public abstract class AbstractDispatchingDialog : AbstractDialog
    {
        private readonly Task<IMessengerResponse> UNKNOWN_COMMAND;
        private readonly Task<IMessengerResponse> UNKNOWN_CALLBACK;
        private readonly Dictionary<string, MethodInfo> _methods;
        private readonly Task<IMessengerResponse> _helpText;

        private Task<IMessengerResponse> Pack(object response)
        {
            return response is Task<IMessengerResponse> t ? t : Task.FromResult((IMessengerResponse)response);
        }

        public AbstractDispatchingDialog()
        {
            UNKNOWN_COMMAND = Task.FromResult(Text("Unrecognized command. You can use /help to show all available commands."));
            UNKNOWN_CALLBACK = Task.FromResult(Nothing());
            _methods = new Dictionary<string, MethodInfo>();
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
                string name = method.Name.ToLower();
                if (name.EndsWith("async")) name = name.Substring(0, name.Length - 5);
                if (name.EndsWith("command")) name = name.Substring(0, name.Length - 7);
                if (name.EndsWith("callback")) name = name.Substring(0, name.Length - 8);
                //Add method to dispatcher
                _methods.Add(name, method);

                if (name != "start")
                {
                    helpTextBuilder.Append('/').Append(name);
                    DescriptionAttribute description = method.GetCustomAttribute<DescriptionAttribute>();
                    if (description != null) helpTextBuilder.Append(" - ").Append(description.Description);
                    helpTextBuilder.AppendLine();
                }
            }
            helpTextBuilder.Append("/help - Shows this page");
            _helpText = Task.FromResult(Text(helpTextBuilder.ToString()));
        }

        public override Task<IMessengerResponse> HandleCommandAsync(User user, string command)
        {
            if (command == "help")
            {
                return _helpText;
            }
            if (_methods.TryGetValue(command, out MethodInfo method))
            {
                return Pack(method.Invoke(this, new object[] { user }));
            }
            else
            {
                return UNKNOWN_COMMAND;
            }
        }

        public override Task<IMessengerResponse> HandleCallbackAsync(User user, string command, string[] args)
        {
            if (_methods.TryGetValue(command, out MethodInfo method))
            {
                object[] methodArgs;

                if (args == null)
                {
                    methodArgs = new object[] { user };
                }
                else
                {
                    methodArgs = new object[args.Length + 1];
                    methodArgs[0] = user;
                    Array.Copy(args, 0, methodArgs, 1, args.Length);
                }
                return Pack(method.Invoke(this, methodArgs));
            }
            else
            {
                return UNKNOWN_CALLBACK;
            }
        }
    }
}
