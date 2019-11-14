using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Git.CredentialManager;
using Bitbucket.Auth;

namespace Bitbucket.OAuth
{
    public interface IOAuthAuthenticator : IAuthenticator
    {
        Task<AuthenticationResult> RefreshAuthAsync(Uri targetUri, string refreshToken, CancellationToken cancellationToken);
        Task<AuthenticationResult> GetAuthAsync(Uri targetUri, IEnumerable<string> scopes, CancellationToken cancellationToken);
        Task<AuthenticationResult> Authenticate(string restRootUrl, Uri targetUri, GitCredential credentials, IEnumerable<string> scopes);
    }
}