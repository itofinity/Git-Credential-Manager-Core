using System;
using Atlassian_Authentication_Helper_App.ViewModels;
using Atlassian_Authentication_Helper_App.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Git.CredentialManager;
using Bitbucket.BasicAuth;
using Bitbucket.OAuth;

namespace Atlassian_Authentication_Helper_App
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp().Start(AppMain, args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UseReactiveUI()
                .UsePlatformDetect()
                .LogToDebug();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Avalonia.Application app, string[] args)
        {
            var cla = new CommandLineApplication();
            cla.HelpOption();
            var optionPromptType = cla.Option("-p|--prompt <USERPASS,AUTHCODE>", "The prompt type", CommandOptionType.SingleValue);

            cla.OnExecute(() =>
                    {
                        var prompt = optionPromptType.HasValue()
                            ? optionPromptType.Value()
                            : "userpass";

                        using (var context = new CommandContext())
                        {
                            var viewModel = GetViewModel(prompt, context);
                            var window = GetWindow(prompt, viewModel);
                            app.Run(window);

                            if (viewModel.Success)
                            {
                                context.Streams.Out.WriteDictionary(viewModel.Output);
                            }
                        }        

                        return 0;
                    });

            var result = cla.Execute(args);
        }

        private static IAuthViewModel GetViewModel(string prompt, CommandContext context)
        {
            switch (prompt.ToLower())
            {
                case "authcode":
                    return new AuthCodeViewModel(context);
                case "userpass":
                default:
                    return new UserPassViewModel(context);
            }
        }

        private static Window GetWindow(string prompt, IAuthViewModel viewModel)
        {
            switch (prompt.ToLower())
            {
                case "authcode":
                    return new AuthCodeWindow() { DataContext = viewModel };
                case "userpass":
                default:
                    return new UserPassWindow() { DataContext = viewModel };
            }
        }
    }
}
