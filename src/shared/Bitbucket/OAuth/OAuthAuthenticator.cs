using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Git.CredentialManager;
using Bitbucket.Auth;
using GitCredCfg  = Microsoft.Git.CredentialManager.Constants.GitConfiguration.Credential;

namespace Bitbucket.OAuth
{
    public class OAuthAuthenticator : IAuthenticator
    {
        private CommandContext _context;

        public OAuthAuthenticator(CommandContext context)
        {
            _context = context;
        }

        private IOAuthAuthenticator GetAuthenticator()
        {
            if (string.IsNullOrWhiteSpace(BbSConsumerKey) && string.IsNullOrWhiteSpace(BbSConsumerSecret))
            {
                // bitbucket.org
                return new v2.OAuthAuthenticator(_context);
            }

            return new v1.OAuthAuthenticator(_context, BbSConsumerKey, BbSConsumerSecret);
        }


        public async Task<AuthenticationResult> AcquireTokenAsync(Uri targetUri, IEnumerable<string> scopes, ICredential credentials)
        {
            var oauth = GetAuthenticator();
            try
            {
                return await oauth.AcquireTokenAsync(targetUri, scopes, credentials);
            }
            catch (Exception ex)
            {
                _context.Trace.WriteLine($"oauth authentication failed [{ex.Message}]");
                return new AuthenticationResult(AuthenticationResultType.Failure);
            }
        }
        public string BbSConsumerKey => _context.Settings.TryGetSetting(BitbucketServerEnvironmentVariables.BbsConsumerKey, 
        GitCredCfg.SectionName, 
        BitbucketServerGitConfiguration.OAuth.BbSConsumerKey, 
        out string consumerKey) ? consumerKey : null;
        
        public string BbSConsumerSecret => _context.Settings.TryGetSetting(BitbucketServerEnvironmentVariables.BbsConsumerSecret, 
            GitCredCfg.SectionName, 
            BitbucketServerGitConfiguration.OAuth.BbSConsumerSecret, 
            out string consumerKey) ? consumerKey : null;
    }
}
