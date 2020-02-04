using Microsoft.Git.CredentialManager;

namespace Bitbucket.Auth
{
    public interface IExtendedCredential : ICredential
    {
         string Scheme { get; }

        string Token { get; }

        string AuthenticationHeaderValue { get; }
    }
}