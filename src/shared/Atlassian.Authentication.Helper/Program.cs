﻿using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Authentication.Helper.ViewModels;
using Atlassian.Authentication.Helper.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Git.CredentialManager;
using Bitbucket.BasicAuth;
using Bitbucket.OAuth;
using Bitbucket;

namespace Atlassian.Authentication.Helper
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
            var optionPromptType = cla.Option("-p|--prompt <USERPASS,AUTHCODE>", "The prompt type",
            CommandOptionType.SingleValue);
            var optionHostUrl = cla.Option("-h|--host <HOST_URL>", "The URL of the target host, displayed to the user and used to determine host type",
            CommandOptionType.SingleValue);

            cla.OnExecute(() =>
                    {
                        var prompt = optionPromptType.HasValue()
                            ? optionPromptType.Value()
                            : "userpass";

                        var hostUrl = optionHostUrl.HasValue()
                            ? optionHostUrl.Value()
                            : BitbucketConstants.BitbucketBaseUrl; // default to https://bitbucket.org

                        using (var context = new CommandContext())
                        {
                            // Enable tracing
                            ConfigureTrace(context);

                            var viewModel = GetViewModel(prompt, hostUrl, context);
                            var window = GetWindow(viewModel);
                            app.Run(window);

                            if (viewModel.Output != null
                                    && viewModel.Output.Any())
                            {
                                context.Streams.Out.WriteDictionary(viewModel.Output);
                            }
                        }

                        return 0;
                    });

            var result = cla.Execute(args);
        }

        private static void ConfigureTrace(CommandContext context)
        {
            if (context.Settings.GetTracingEnabled(out string traceValue))
            {
                if (traceValue.IsTruthy()) // Trace to stderr
                {
                    context.Trace.AddListener(context.Streams.Error);
                }
                else if (Path.IsPathRooted(traceValue)) // Trace to a file
                {
                    try
                    {
                        Stream stream = context.FileSystem.OpenFileStream(traceValue, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        TextWriter _traceFileWriter = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: false);

                        context.Trace.AddListener(_traceFileWriter);
                    }
                    catch (Exception ex)
                    {
                        context.Streams.Error.WriteLine($"warning: unable to trace to file '{traceValue}': {ex.Message}");
                    }
                }
                else
                {
                    context.Streams.Error.WriteLine($"warning: unknown value for {Constants.EnvironmentVariables.GcmTrace} '{traceValue}'");
                }
            }
        }

        private static IAuthViewModel GetViewModel(string prompt, string hostUrl, CommandContext context)
        {
            switch (prompt.ToLower())
            {
                case "authcode":
                    return new AuthCodeViewModel(hostUrl, context);
                case "userpass":
                default:
                    return new UserPassViewModel(hostUrl, context);
            }
        }

        private static Window GetWindow(IAuthViewModel viewModel)
        {
            switch (viewModel.GetType().Name.ToLower())
            {
                case "authcodeviewmodel":
                    return new AuthCodeWindow() { DataContext = viewModel };
                case "userpassviewmodel":
                default:
                    return new UserPassWindow() { DataContext = viewModel };
            }
        }
    }
}
