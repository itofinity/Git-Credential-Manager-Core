using System;
using System.IO;
using System.Text;
using Itofinity.Bitbucket.Authentication.BasicAuth;
using Itofinity.Bitbucket.Authentication.OAuth;
using Microsoft.Git.CredentialManager;
using BbAuthMsft = Itofinity.Bitbucket.Authentication.Helpers.Microsoft.Git.CredentialManager;

namespace Bitbucket
{
    public class BitbucketAuthContext
    {
        private BbAuthMsft.ITrace _trace;
        private BbAuthMsft.ISettings _settings;
        private BbAuthMsft.IStandardStreams _streams;
        private BbAuthMsft.IHttpClientFactory _httpClientFactory;

        private BasicAuthAuthenticator _basicAuthAuthenticator;
        private OAuthAuthenticator _oauthAuthenticator;
        private IBitbucketAuthentication _bitbucketAuthentication;

        private readonly ICommandContext _context;

        public BitbucketAuthContext(CommandContext context)
        {
            _context = context;
        }

        public OAuthAuthenticator GetOAuthAuthenticator()
        {
            if(_oauthAuthenticator != null) {
                return _oauthAuthenticator;
            }

            _oauthAuthenticator = new OAuthAuthenticator(GetTrace(), GetHttpClientFactory(), GetSettings());

            return _oauthAuthenticator;
        }

        public BasicAuthAuthenticator GetBasicAuthAuthenticator()
        {
            if(_basicAuthAuthenticator == null) {
                _basicAuthAuthenticator = new BasicAuthAuthenticator(GetTrace(), GetHttpClientFactory());
            }
            
            return _basicAuthAuthenticator;
        }

        public BbAuthMsft.IHttpClientFactory GetHttpClientFactory() 
        {
            if(_httpClientFactory == null)
            {
                _httpClientFactory = new BbAuthMsft.HttpClientFactory(GetTrace(), GetSettings(), GetStandardStreams());
            }

            return _httpClientFactory;
        }

        public BbAuthMsft.ITrace GetTrace()
        {
            if(_trace == null)
            {
                _trace = new BbAuthMsft.Trace();
                ConfigureTrace(_trace, GetSettings(), (BbAuthMsft.StandardStreams)GetStandardStreams());
            }
            return _trace;
        }

        internal IBitbucketAuthentication GetBitbucketAuthentication()
        {
            if(_bitbucketAuthentication == null)
            {
                _bitbucketAuthentication = new BitbucketAuthentication((CommandContext)_context);
            }

            return _bitbucketAuthentication;
        }

        public BbAuthMsft.ISettings GetSettings()
        {
            if(_settings == null)
            {
                _settings = new BbAuthMsft.Settings(_context.Settings.RemoteUri, 
                _context.Settings.GetProxyConfiguration(out bool x), 
                false, // verifySsl, 
                null, //oAuthConsumerKey, 
                null, //oAuthConsumerSecret, 
                null); //traceConfig);
            }

            return _settings;
        }

        public BbAuthMsft.IStandardStreams GetStandardStreams()
        {
            if(_streams == null)
            {
                _streams = new BbAuthMsft.StandardStreams();
            }
            return _streams;
        }

        private static void ConfigureTrace(BbAuthMsft.ITrace trace, 
            BbAuthMsft.ISettings settings, 
            BbAuthMsft.StandardStreams streams)
        {
            if (settings.GetTracingEnabled(out string traceValue))
            {
                if (traceValue.IsTruthy()) // Trace to stderr
                {
                    trace.AddListener(streams.Error);
                }
                else if (Path.IsPathRooted(traceValue)) // Trace to a file
                {
                    try
                    {
                        Stream stream = File.Open(traceValue, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        TextWriter _traceFileWriter = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: false);

                        trace.AddListener(_traceFileWriter);
                    }
                    catch (Exception ex)
                    {
                        streams.Error.WriteLine($"warning: unable to trace to file '{traceValue}': {ex.Message}");
                    }
                }
                else
                {
                    streams.Error.WriteLine($"warning: unknown value for trace '{traceValue}'");
                }
            }
        }
    }
}