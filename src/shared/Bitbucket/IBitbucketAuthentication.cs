using System;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;

namespace Bitbucket
{
    public interface IBitbucketAuthentication
    {
        Task<ICredential> GetCredentialsAsync(Uri targetUri);

        //Task<string> GetAuthenticationCodeAsync(Uri targetUri, bool isSms);
    }
}