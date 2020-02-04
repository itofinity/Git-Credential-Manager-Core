using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Bitbucket.Auth;

namespace Bitbucket.BasicAuth
{
    /// <summary>
    ///     Provides the functionality for validating basic auth credentials with Bitbucket.org
    /// </summary>
    public class BasicAuthAuthenticator : IAuthenticator
    {
        // TODO duplicated
        private static readonly string[] BitbucketCredentialScopes =
        {
            BitbucketConstants.TokenScopes.SnippetWrite,
            BitbucketConstants.TokenScopes.RepositoryWrite
        };

        private static readonly string[] BitbucketServerCredentialScopes =
        {
            BitbucketServerConstants.TokenScopes.ProjectRead,
            BitbucketServerConstants.TokenScopes.RepositoryWrite
        };

        private CommandContext _context;
        private readonly IBitbucketRestApi _bitbucketServerApi;
        private readonly IBitbucketRestApi _bitbucketApi;

        public BasicAuthAuthenticator(CommandContext context)
        { 
            EnsureArgument.NotNull(context, nameof(context));
            _context = context;

            _bitbucketServerApi = new Server.BitbucketRestApi(context);

            _bitbucketApi = new Cloud.BitbucketRestApi(context);

        }
        public async Task<AuthenticationResult> AcquireTokenAsync(Uri targetUri, IEnumerable<string> scopes, IExtendedCredential credentials)
        {
            if (targetUri.AbsoluteUri.Contains("bitbucket.org")/* TODO Rest.Cloud.RestClient.IsAcceptableUri(targetUri)*/)
            {
                return await GetCloudAuthAsync(targetUri, scopes, credentials);
            }
            else
            {
                return await GetServerAuthAsync(targetUri, scopes, credentials);
            }
        }

        private async Task<AuthenticationResult> GetServerAuthAsync(Uri targetUri, IEnumerable<string> scopes, IExtendedCredential credentials)
        {
            // Use the provided username and password and attempt a basic authentication request to a known REST API resource.
            //var result = await (new Rest.Server.RestClient(Context)).TryGetUser(targetUri, requestTimeout, restRootUrl, credentials);
            return await _bitbucketServerApi.AcquireTokenAsync(
                targetUri, credentials, BitbucketServerCredentialScopes);
        }

        public async Task<AuthenticationResult> GetCloudAuthAsync(Uri targetUri, IEnumerable<string> scopes, IExtendedCredential credentials)
        {
            return await _bitbucketApi.AcquireTokenAsync(
                targetUri, credentials.UserName, credentials.Password, "", BitbucketCredentialScopes);
        }
    }
}
