using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using Bitbucket;
using Bitbucket.BasicAuth;
using Microsoft.Git.CredentialManager;

namespace Atlassian.Authentication.Helper.ViewModels
{
    public class UserPassViewModel : AbstractAuthViewModel
    {
        public UserPassViewModel(string hostUrl, CommandContext context) : base(hostUrl)
        {
            var targetUri = new Uri(hostUrl);
            context.Settings.RemoteUri = targetUri;

            var authenticator = new BasicAuthAuthenticator(context);

            LoginCommand = ReactiveCommand.Create<object>(async param =>
            {
                var scopes = BitbucketHostProvider.BitbucketCredentialScopes;
                // TODO validate credentials
                var result = await authenticator.AcquireTokenAsync(
                targetUri, scopes, 
                new GitCredential(_username, _password));

                if (result.Type == AuthenticationResultType.Success)
                {
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");
                    
                    _output.Add("username", result.Token.UserName);
                    _output.Add("password", result.Token.Password);

                    Success = true;
                }
                else if (result.Type == AuthenticationResultType.TwoFactor)
                {
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' failed");
                    _output.Add("authentication", "2fa");
                    Success = false;
                }
                else
                {
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' failed");
                    Success = false;
                }

                Exit();
            });

            CancelCommand = ReactiveCommand.Create<object>(param =>
            {
                Success = false;
                Exit();
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
    }
}