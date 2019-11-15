using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using Microsoft.Git.CredentialManager;
using Bitbucket;
using Bitbucket.OAuth;

namespace Atlassian_Authentication_Helper_App.ViewModels
{
    public class AuthCodeViewModel : ReactiveObject, IAuthViewModel
    {
        public event EventHandler ExitEvent;
        private Dictionary<string, string> _output = new Dictionary<string, string>();

        public AuthCodeViewModel(CommandContext context)
        {
            var targetUri = new Uri("https://bitbucket.org");
            context.Settings.RemoteUri = targetUri;
            var authenticator = new OAuthAuthenticator(context);

            AuthenticateCommand = ReactiveCommand.Create<object>(async param =>
            {
                
                var scopes = BitbucketHostProvider.BitbucketCredentialScopes;

                AuthenticationResult result = await authenticator.AcquireTokenAsync(
                    targetUri, scopes, 
                    new GitCredential("not used", "anywhere"));

                if (result.Type == AuthenticationResultType.Success)
                {
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");
                    

                    var usernameResult = await authenticator.AquireUserDetailsAsync(targetUri, result.Token.Password);
                    if (usernameResult.Type == AuthenticationResultType.Success)
                    {
                        _output.Add("username", usernameResult.Token.UserName);
                        _output.Add("authcode", result.Token.Password);

                        Success = true;
                    }
                    else
                    {
                        Success = false;
                    }
                    
                }
                else
                {
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' failed");
                    Success = false;
                }

                // TODO OAuth
                // TODO validate credentials
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

        public ReactiveCommand<object, Unit> AuthenticateCommand { get; }

        public ReactiveCommand<object, Unit> CancelCommand { get; }

        public string Authcode { get; private set; }
        public string Username { get; private set; }

        public Dictionary<string,string> Output {
            get
            {
                return _output;
            }
        }

        public bool Success { get; private set; }
    }
}