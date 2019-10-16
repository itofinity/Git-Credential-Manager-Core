using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bitbucket.Authentication;
using Microsoft.Git.CredentialManager;

namespace Bitbucket
{
    public class BitbucketHostProvider : HostProvider
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

        private readonly IBitbucketRestApi _bitbucketApi;
        private readonly IBitbucketAuthentication _bitbucketAuth;

        public BitbucketHostProvider(CommandContext context)
            : this(context, new BitbucketRestApi(context), new BitbucketAuthentication(context)) { }

        public BitbucketHostProvider(ICommandContext context, IBitbucketRestApi bitbucketApi, IBitbucketAuthentication bitbucketAuth)
            : base(context)
        {
            EnsureArgument.NotNull(bitbucketApi, nameof(bitbucketApi));
            EnsureArgument.NotNull(bitbucketAuth, nameof(bitbucketAuth));

            _bitbucketApi = bitbucketApi;
            _bitbucketAuth = bitbucketAuth;
        }

        public override string Id => Name.ToLower();
        
        public override string Name => "Bitbucket";

        public override IEnumerable<string> SupportedAuthorityIds => BitbucketAuthentication.AuthorityIds;

        public override async Task<ICredential> GenerateCredentialAsync(InputArguments input)
        {
            ThrowIfDisposed();

            // TODO Bitbucker Cloud only!!!
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

            // ask user for credentials
            ICredential credentials = await _bitbucketAuth.GetCredentialsAsync(targetUri);

            // TODO Bitbucket CLoud
            //AuthenticationResult result = await _bitbucketApi.AcquireTokenAsync(
              //  targetUri, credentials.UserName, credentials.Password, "", BitbucketCredentialScopes);

            AuthenticationResult result = await _bitbucketApi.AcquireTokenAsync(
                targetUri, credentials.UserName, credentials.Password, "", BitbucketServerCredentialScopes);

            if (result.Type == BitbucketAuthenticationResultType.Success)
            {
                Context.Trace.WriteLine($"Token acquisition for '{targetUri}' succeeded");

                return result.Token;
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
            Uri uri = new UriBuilder
            {
                Scheme = input.Protocol,
                Host = input.CleanHost,
            }.Uri;

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
