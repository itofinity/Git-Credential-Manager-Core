using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using Microsoft.Git.CredentialManager;
using Bitbucket;
using Bitbucket.OAuth;
using Bitbucket.Auth;

namespace Atlassian.Authentication.Helper.ViewModels
{
    public class AuthCodeViewModel : AbstractAuthViewModel
    {
        public AuthCodeViewModel(string hostUrl, CommandContext context) : base(hostUrl)
        {
            var targetUri = new Uri(hostUrl);
            context.Settings.RemoteUri = targetUri;

            var authenticator = new OAuthAuthenticator(context);

            AuthenticateCommand = ReactiveCommand.Create<object>(async param =>
            {
                
                var scopes = BitbucketHostProvider.BitbucketCredentialScopes;

                AuthenticationResult result = await authenticator.AcquireTokenAsync(
                    targetUri, scopes, 
                    new ExtendedCredential("not used", "anywhere", "at all"));

                if (result.Type == AuthenticationResultType.Success)
                {
                    context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");
                    

                    var usernameResult = await authenticator.AquireUserDetailsAsync(targetUri, result.Token.Password);
                    if (usernameResult.Type == AuthenticationResultType.Success)
                    {
                        _output.Add("username", usernameResult.Token.UserName);
                        _output.Add("accesstoken", result.Token.Password);
                        _output.Add("refreshtoken", result.RefreshToken.Password);

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

        public ReactiveCommand<object, Unit> AuthenticateCommand { get; }

        public ReactiveCommand<object, Unit> CancelCommand { get; }

        public string Authcode { get; private set; }
        public string Username { get; private set; }


    }
}