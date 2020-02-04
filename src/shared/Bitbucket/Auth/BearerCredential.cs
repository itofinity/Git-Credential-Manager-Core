using Microsoft.Git.CredentialManager;

namespace Bitbucket.Auth
{
    public class BearerCredential : ExtendedCredential
    {
        public BearerCredential(string userName, string password) : base(userName, password, Constants.Http.WwwAuthenticateBearerScheme)
        {
        }
    }
}