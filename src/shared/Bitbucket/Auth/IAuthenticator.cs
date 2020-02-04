using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Git.CredentialManager;

namespace Bitbucket.Auth
{
    public interface IAuthenticator
    {    
        Task<AuthenticationResult> AcquireTokenAsync(Uri targetUri, IEnumerable<string> scopes, IExtendedCredential credentials);    
    }
}