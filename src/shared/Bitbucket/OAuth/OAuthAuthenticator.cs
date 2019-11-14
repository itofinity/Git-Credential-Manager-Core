using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Git.CredentialManager;
using Bitbucket.Auth;
using GitCredCfg  = Microsoft.Git.CredentialManager.Constants.GitConfiguration.Credential;

namespace Bitbucket.OAuth
{
    public class OAuthAuthenticator : IAuthenticator, IBitbucket
    {
        private CommandContext _context;
        private readonly IBitbucketRestApi _bitbucketServerApi;
        private readonly IBitbucketRestApi _bitbucketApi;

        public OAuthAuthenticator(CommandContext context)
        {
            _context = context;
            _bitbucketApi = new Cloud.BitbucketRestApi(context);
            _bitbucketServerApi = new Server.BitbucketRestApi(context);
        }

        private IOAuthAuthenticator GetAuthenticator()
        {
            if (IsCloud)
            {
                // bitbucket.org
                return new v2.OAuthAuthenticator(_context);
            }
            else
            {
                return new v1.OAuthAuthenticator(_context, BbSConsumerKey, BbSConsumerSecret);
            }
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

        public bool IsCloud => RemoteUrl.Contains("bitbucket.org");

        public string RemoteUrl => _context.Settings.RemoteUri.AbsoluteUri;

        public async Task<AuthenticationResult> AquireUserDetailsAsync(Uri targetUri, string token)
        {
            if(IsCloud)
            {
                return await _bitbucketApi.AcquireUserDetailsAsync(targetUri, token);
            }
            else
            {
                return await _bitbucketServerApi.AcquireUserDetailsAsync(targetUri, token);
            }
        }
    }
}
