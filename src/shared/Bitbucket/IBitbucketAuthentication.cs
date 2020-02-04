using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Bitbucket.Auth;
using System.Collections.Generic;

namespace Bitbucket
{
    public interface IBitbucketAuthentication
    {
        Task<AuthenticationResult> GetCredentialsAsync(Uri targetUri, IEnumerable<string> scopes);

        Task<string> GetAuthenticationCodeAsync(Uri targetUri);
    }
}