using Microsoft.Git.CredentialManager;

namespace Bitbucket.Auth
{
    public class BaseAuthCredential : ExtendedCredential
    {
        public BaseAuthCredential(string userName, string password) : base(userName, password, Constants.Http.WwwAuthenticateBasicScheme)
        {
        }
    }
}