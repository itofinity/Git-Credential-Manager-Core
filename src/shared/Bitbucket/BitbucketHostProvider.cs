using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Bitbucket.BasicAuth;
using Bitbucket.OAuth;
using GitCredCfg  = Microsoft.Git.CredentialManager.Constants.GitConfiguration.Credential;

namespace Bitbucket
{
    public class BitbucketHostProvider : HostProvider, IBitbucket
    {
        private static readonly string[] BitbucketCredentialScopes =
        {
            BitbucketConstants.TokenScopes.SnippetWrite,
            BitbucketConstants.TokenScopes.RepositoryWrite
        };

        private static readonly string[] BitbucketServerCredentialScopes =
        {
            BitbucketServerConstants.TokenScopes.RepositoryWrite
        };

        //private readonly IBitbucketRestApi _bitbucketApi;
        private readonly IBitbucketAuthentication _bitbucketAuth;

        private readonly BasicAuthAuthenticator _basicAuthAuthenticator;
        private readonly OAuthAuthenticator _oauthAuthenticator;

        public BitbucketHostProvider(CommandContext context)
            : this(context, new BasicAuthAuthenticator(context), new OAuthAuthenticator(context), new BitbucketAuthentication(context)) { }

        public BitbucketHostProvider(ICommandContext context, BasicAuthAuthenticator basicAuthAuthenticator, 
        OAuthAuthenticator oauthAuthenticator,
        IBitbucketAuthentication bitbucketAuth)
            : base(context)
        {
            EnsureArgument.NotNull(basicAuthAuthenticator, nameof(basicAuthAuthenticator));
            EnsureArgument.NotNull(oauthAuthenticator, nameof(oauthAuthenticator));
            EnsureArgument.NotNull(bitbucketAuth, nameof(bitbucketAuth));

            //_bitbucketApi = bitbucketApi;
            _basicAuthAuthenticator = basicAuthAuthenticator;
            _oauthAuthenticator = oauthAuthenticator;
            _bitbucketAuth = bitbucketAuth;
        }

        public override string Id => Name.ToLower();
        
        public override string Name => "Bitbucket";

        public override IEnumerable<string> SupportedAuthorityIds => BitbucketAuthentication.AuthorityIds;

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // TODO Bitbucket Cloud only!!!
            // We should not allow unencrypted communication and should inform the user
            if (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") 
                && "bitbucket.org".Equals(input.Host, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("Unencrypted HTTP is not supported for Bitbucket. Ensure the repository remote URL is using HTTPS.");
            }

            Uri targetUri = GetTargetUri(input);
/*

TODO auto refresh

            // Try for a refresh token.
            string credentialKey = GetRefreshTokenTargetUri(input);
            Context.Trace.WriteLine($"Looking for existing credential in store with key '{credentialKey}'...");
            ICredential refreshCredential = Context.CredentialStore.Get(credentialKey);
            if (refreshCredential is null)
                // No refresh token return null.
                return credentials;

            Credential refreshedCredentials = await RefreshCredentials(targetUri, refreshCredentials.Password, null);

            Credential refreshedCredentials = await RefreshCredentials(targetUri, refreshCredentials.Password, null);
*/

            // TODO Bitbucket CLoud
            //AuthenticationResult result = await _bitbucketApi.AcquireTokenAsync(
              //  targetUri, credentials.UserName, credentials.Password, "", BitbucketCredentialScopes);

            // TODO BbS doesn't tel us if 2FA is 'on' so rely on configuration
            var useOAuth = ForceOAuth;

            // if 2FA is on for BbS there is no point trying for a PAT via username/password
            // the multiple 3rd implementations means there arte two many variables in what might come back
            if(!useOAuth)
            {
                // ask user for credentials
                ICredential credentials = await _bitbucketAuth.GetCredentialsAsync(targetUri);

                // BbC or BbS with out 2FA configured at the client
                //AuthenticationResult result = await _bitbucketApi.AcquireTokenAsync(
                //targetUri, credentials.UserName, credentials.Password, "", BitbucketServerCredentialScopes);
                AuthenticationResult result = await _basicAuthAuthenticator.AcquireTokenAsync(
                targetUri, Scopes, 
                credentials);

                if (result.Type == AuthenticationResultType.Success)
                {
                    Context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");

                    return result.Token;
                }

                if (result.Type == AuthenticationResultType.TwoFactor)
                {
                    // TODO BbC said 2FA is 'on'
                    useOAuth = true;
                }
            }

            if(useOAuth)
            {
                Context.Terminal.WriteLine("OAuth for '{0}'...", targetUri); 

                AuthenticationResult result = await _oauthAuthenticator.AcquireTokenAsync(
                    targetUri, Scopes, 
                    new GitCredential("not used", "anywhere"));
                
                if (result.Type == AuthenticationResultType.Success)
                {
                    Context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");

                    var usernameResult = await _oauthAuthenticator.AquireUserDetailsAsync(targetUri, result.Token.Password);

                    return usernameResult.Token;
                }
            }

            
/*
Credential credentials = null;
            string username;
            string password;

            // Ask the user for basic authentication credentials
            if (AcquireCredentialsCallback("Please enter your Bitbucket credentials for ", targetUri, out username, out password))
            {
                AuthenticationResult result;
                credentials = new Credential(username, password);

                if (result = await BitbucketAuthority.AcquireToken(targetUri, credentials, AuthenticationResultType.None, TokenScope))
                {
                    Trace.WriteLine("token acquisition succeeded");

                    credentials = GenerateCredentials(targetUri, username, ref result);
                    await SetCredentials(targetUri, credentials, username);

                    // If a result callback was registered, call it.
                    AuthenticationResultCallback?.Invoke(targetUri, result);

                    return credentials;
                }
                else if (result == AuthenticationResultType.TwoFactor)
                {
                    // Basic authentication attempt returned a result indicating the user has 2FA on so prompt
                    // the user to run the OAuth dance.
                    if (AcquireAuthenticationOAuthCallback("", targetUri, result, username))
                    {
                        if (result = await BitbucketAuthority.AcquireToken(targetUri, credentials, AuthenticationResultType.TwoFactor, TokenScope))
                        {
                            Trace.WriteLine("token acquisition succeeded");

                            credentials = GenerateCredentials(targetUri, username, ref result);

                            await SetCredentials(targetUri, credentials, username);

                            await SetCredentials(GetRefreshTokenTargetUri(targetUri), 
                                                 new Credential(result.RefreshToken.Type.ToString(),
                                                                result.RefreshToken.Value),
                                                 username);

                            // If a result callback was registered, call it.
                            AuthenticationResultCallback?.Invoke(targetUri, result);

                            return credentials;
                        }
                    }
                }
            }

*/



            throw new Exception($"Interactive logon for '{targetUri}' failed.");
        }

        public string BbSConsumerKey => Context.Settings.TryGetSetting(BitbucketServerEnvironmentVariables.BbsConsumerKey, 
            GitCredCfg.SectionName, 
            BitbucketServerGitConfiguration.OAuth.BbSConsumerKey, 
            out string consumerKey) ? consumerKey : null;
            
        public string BbSConsumerSecret => Context.Settings.TryGetSetting(BitbucketServerEnvironmentVariables.BbsConsumerSecret, 
            GitCredCfg.SectionName, 
            BitbucketServerGitConfiguration.OAuth.BbSConsumerSecret, 
            out string consumerKey) ? consumerKey : null;


        public bool ForceOAuth => IsCloud || IsServerOAuth;

        public bool IsServerOAuth => !string.IsNullOrWhiteSpace(BbSConsumerKey) && !string.IsNullOrWhiteSpace(BbSConsumerSecret);

        public bool IsCloud => RemoteUrl.Contains("bitbucket.org");

        public string RemoteUrl => Context.Settings.RemoteUri.AbsoluteUri;

        public string[] Scopes =>  IsCloud ? BitbucketCredentialScopes : BitbucketServerCredentialScopes;

        public override string GetCredentialKey(InputArguments input)
        {
            string url = GetTargetUri(input).AbsoluteUri;

            // Trim trailing slash
            if (url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }

            return $"git:{url}";
        }

        private static Uri GetTargetUri(InputArguments input)
        {
            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = input.Protocol,
                Host = input.CleanHost,
                Path = input.Path
            };
            
            if(input.Port.HasValue)
            {
                uriBuilder.Port = input.Port.Value;
            }

            Uri uri = uriBuilder.Uri;

            return NormalizeUri(uri);
        }

        private static Uri NormalizeUri(Uri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            return targetUri;
        }

        public override bool IsSupported(InputArguments input)
        {
            // Bitbucket Cloud

            // We do not support unencrypted HTTP communications to GitHub,
            // but we report `true` here for HTTP so that we can show a helpful
            // error message for the user in `CreateCredentialAsync`.
            return input != null &&
                   (StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "http") ||
                    StringComparer.OrdinalIgnoreCase.Equals(input.Protocol, "https")) &&
                   (StringComparer.OrdinalIgnoreCase.Equals(input.Host, BitbucketConstants.BitbucketBaseUrlHost));
        }
    }
}
