using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bitbucket.Authentication;
using Microsoft.Git.CredentialManager;

namespace Bitbucket.BasicAuth
{
    /// <summary>
    ///     Provides the functionality for validating basic auth credentials with Bitbucket.org
    /// </summary>
    public class BasicAuthAuthenticator
    {
        private CommandContext _context;

        public async Task<AuthenticationResult> GetAuthAsync(Uri targetUri, IEnumerable<string> scopes, int requestTimeout, Uri restRootUrl, GitCredential credentials)
        {
            // Use the provided username and password and attempt a basic authentication request to a known REST API resource.
            var result = await ( new BitbucketRestApi(_context)).AcquireTokenAsync(targetUri, credentials.UserName, credentials.Password, "", scopes);

            if (result.Type.Equals(BitbucketAuthenticationResultType.Success))
            {
                // Success with username/password indicates 2FA is not on so the 'token' is actually
                // the password if we had a successful call then the password is good.
                var token = new GitCredential(credentials.UserName, credentials.Password);
                if (!string.IsNullOrWhiteSpace(result.RemoteUsername) && !credentials.UserName.Equals(result.RemoteUsername))
                {
                    // TOD Logging Trace.WriteLine($"remote username [{result.RemoteUsername}] != [{credentials.UserName}] supplied username");
                    return new AuthenticationResult(BitbucketAuthenticationResultType.Success, token, result.RemoteUsername);
                }

                return new AuthenticationResult(BitbucketAuthenticationResultType.Success, token);
            }

            // TODO logging Trace.WriteLine("authentication failed");
            return result;
        }
    }
}
