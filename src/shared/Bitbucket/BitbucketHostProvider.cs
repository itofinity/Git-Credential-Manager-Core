using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Bitbucket.BasicAuth;
using Bitbucket.OAuth;
using Bitbucket.Auth;
using GitCredCfg  = Microsoft.Git.CredentialManager.Constants.GitConfiguration.Credential;

namespace Bitbucket
{
    public class BitbucketHostProvider : HostProvider, IBitbucket
    {
        public static readonly string[] BitbucketCredentialScopes =
        {
            BitbucketConstants.TokenScopes.SnippetWrite,
            BitbucketConstants.TokenScopes.RepositoryWrite
        };

        public static readonly string[] BitbucketServerCredentialScopes =
        {
            BitbucketServerConstants.TokenScopes.ProjectRead,
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
                AuthenticationResult result = await _bitbucketAuth.GetCredentialsAsync(targetUri, Scopes);

                // BbC or BbS with out 2FA configured at the client
                //AuthenticationResult result = await _bitbucketApi.AcquireTokenAsync(
                //targetUri, credentials.UserName, credentials.Password, "", BitbucketServerCredentialScopes);
                //AuthenticationResult result = await _basicAuthAuthenticator.AcquireTokenAsync(
                //targetUri, Scopes, 
                //credentials);

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
                var authCode = await _bitbucketAuth.GetAuthenticationCodeAsync(targetUri);

                // For BbC don't need and App Password/PAT as OAuth token can be used, so this only triggers if there is no GUI helper.
                if(string.IsNullOrWhiteSpace(authCode))
                {
                    Context.Terminal.WriteLine("Acquiring OAuth token for '{0}'...", targetUri); 

                    AuthenticationResult result = await _oauthAuthenticator.AcquireTokenAsync(
                                                    targetUri, 
                                                    Scopes, 
                                                    new ExtendedCredential("not used", "anywhere", "at all"));

                    if (result.Type == AuthenticationResultType.Success)
                    {
                        Context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");
                        Context.Terminal.WriteLine("... OAuth token for '{0}' acquired", targetUri); 
                        authCode = result.Token.Password;
                    }
                }
                
                
                if (!string.IsNullOrWhiteSpace(authCode))
                {
                    Context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");

                    var usernameResult = await _oauthAuthenticator.AquireUserDetailsAsync(targetUri, authCode);

                    if(usernameResult.IsSuccess)
                    {
                        return usernameResult.Token;
                    }
                }
            }

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
            var path = input.Path != null && input.Path.ToLower().Contains("/scm/")
                ? input.Path.Substring(0, input.Path.IndexOf("/scm/")) 
                : input.Path;

            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = input.Protocol,
                Host = input.CleanHost,
                Path = path
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
