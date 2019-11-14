using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bitbucket
{
    public interface IBitbucketRestApi
    {
        Task<AuthenticationResult> AcquireTokenAsync(
            Uri targetUri,
            string username,
            string password,
            string authenticationCode,
            IEnumerable<string> scopes);

        Task<AuthenticationResult> AcquireUserDetailsAsync(Uri targetUri, string token);
    }
}