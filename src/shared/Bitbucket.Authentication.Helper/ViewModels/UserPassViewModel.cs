using System;
using System.Reactive;
using ReactiveUI;
using Bitbucket;
using Bitbucket.BasicAuth;
using Microsoft.Git.CredentialManager;

namespace Atlassian_Authentication_Helper_App.ViewModels
{
    public class UserPassViewModel : ReactiveObject, IAuthViewModel
    {
        public UserPassViewModel(CommandContext context)
        {
            var authenticator = new BasicAuthAuthenticator(context);

            LoginCommand = ReactiveCommand.Create<object>(async param =>
            {
                var targetUri = new Uri("https://bitbucket.org");
                var scopes = BitbucketHostProvider.BitbucketCredentialScopes;
                // TODO validate credentials
                var result = await authenticator.AcquireTokenAsync(
                targetUri, scopes, 
                new GitCredential(_username, _password));

                if (result.Type == AuthenticationResultType.Success)
                {
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");

                    Success = true;
                }
                else
                {
                    Console.WriteLine("oops");
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' failed");
                    Success = false;
                }

                
            });

            CancelCommand = ReactiveCommand.Create<object>(param =>
            {
                Success = false;
            });
        }

        private string _username;

        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        private string _password;

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public ReactiveCommand<object, Unit> LoginCommand { get; }

        public ReactiveCommand<object, Unit> CancelCommand { get; }

        public string Response => $"username={_username}{Environment.NewLine}password={_password}";

        public bool Success { get; private set; }
    }
}