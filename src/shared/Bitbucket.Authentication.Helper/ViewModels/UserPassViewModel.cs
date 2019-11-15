using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using Bitbucket;
using Bitbucket.BasicAuth;
using Microsoft.Git.CredentialManager;

namespace Atlassian_Authentication_Helper_App.ViewModels
{
    public class UserPassViewModel : ReactiveObject, IAuthViewModel
    {
        public event EventHandler ExitEvent;

        private Dictionary<string, string> _output = new Dictionary<string, string>();

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
                    
                    _output.Add("username", result.Token.UserName);
                    _output.Add("password", result.Token.Password);

                    Success = true;
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

        public void Exit()
        {
            if (ExitEvent != null)
            {
                ExitEvent(this, new EventArgs());
            }
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

        public Dictionary<string,string> Output {
            get
            {
                return _output;
            }
        }

        public bool Success { get; private set; }
    }
}