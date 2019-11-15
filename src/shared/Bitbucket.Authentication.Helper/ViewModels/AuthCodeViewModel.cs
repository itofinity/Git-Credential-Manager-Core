using System;
using System.Reactive;
using ReactiveUI;
using Microsoft.Git.CredentialManager;
using Bitbucket;
using Bitbucket.OAuth;

namespace Atlassian_Authentication_Helper_App.ViewModels
{
    public class AuthCodeViewModel : ReactiveObject, IAuthViewModel
    {
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
                    Console.WriteLine("errr");
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");
                    

                    var usernameResult = await authenticator.AquireUserDetailsAsync(targetUri, result.Token.Password);
                    if (usernameResult.Type == AuthenticationResultType.Success)
                    {
                        Authcode = result.Token.Password;
                        Username = usernameResult.Token.UserName;

                        Success = true;
                    }
                    else
                    {
                        Success = false;
                    }
                    
                }
                else
                {
                    Console.WriteLine("oops");
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' failed");
                    Success = false;
                }

                // TODO OAuth
                // TODO validate credentials
            });

            CancelCommand = ReactiveCommand.Create<object>(param =>
            {
                Success = false;
            });
        }

        public ReactiveCommand<object, Unit> AuthenticateCommand { get; }

        public ReactiveCommand<object, Unit> CancelCommand { get; }

        public string Authcode { get; private set; }
        public string Username { get; private set; }

        public string Response => $"authcode={Authcode}{Environment.NewLine}username={Username}";

        public bool Success { get; private set; }
    }
}