/*
Copyright DTCC 2016 All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

         http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Implementation;
using Hyperledger.Fabric.Shim.Logging;
using Mono.Options;

namespace Hyperledger.Fabric.Shim
{
    public abstract class ChaincodeBaseAsync : IChaincodeAsync, IDisposable
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(ChaincodeBaseAsync));
        public static readonly string CORE_CHAINCODE_LOGGING_SHIM = "CORE_CHAINCODE_LOGGING_SHIM";
        public static readonly string CORE_CHAINCODE_LOGGING_LEVEL = "CORE_CHAINCODE_LOGGING_LEVEL";

        public static readonly string DEFAULT_HOST = "127.0.0.1";
        public static readonly int DEFAULT_PORT = 7051;

        private static readonly string CORE_CHAINCODE_ID_NAME = "CORE_CHAINCODE_ID_NAME";
        private static readonly string CORE_PEER_ADDRESS = "CORE_PEER_ADDRESS";
        private static readonly string CORE_PEER_TLS_ENABLED = "CORE_PEER_TLS_ENABLED";
        private static readonly string CORE_PEER_TLS_ROOTCERT_FILE = "CORE_PEER_TLS_ROOTCERT_FILE";
        private static readonly string ENV_TLS_CLIENT_KEY_PATH = "CORE_TLS_CLIENT_KEY_PATH";
        private static readonly string ENV_TLS_CLIENT_CERT_PATH = "CORE_TLS_CLIENT_CERT_PATH";

        /**
	 * Start chaincode
	 * 
	 * @param args
	 *            command line arguments
	 */
        private CancellationTokenSource exitSource;

        private bool help;
        private OptionSet options;

        public string Host { get; set; } = DEFAULT_HOST;
        public string Id { get; set; }
        public int Port { get; set; } = DEFAULT_PORT;

        public bool IsTlsEnabled { get; set; }
        public string TlsClientKeyPath { get; set; }
        public string TlsClientCertPath { get; set; }
        public string TlsClientRootCertPath { get; set; }

        public abstract Task<Response> InitAsync(IChaincodeStub stub, CancellationToken token = default(CancellationToken));


        public abstract Task<Response> InvokeAsync(IChaincodeStub stub, CancellationToken token = default(CancellationToken));

        public void Dispose()
        {
            try
            {
                exitSource?.Cancel();
            }
            catch (Exception)
            {
                //ignored
            }
        }


        private void ProcessCommandLineOptions(string[] args)
        {
            try
            {
                string peerAddress = null;
                options = new OptionSet {{"a|peerAddress|peer.address=", "Address of peer to connect to", a => peerAddress = a}, {"i|id=", "Identity of chaincode", a => Id = a}, {"h|help|?", "Show help", a => help = a != null},};
                options.Parse(args);
                if (help)
                    return;
                if (!string.IsNullOrEmpty(peerAddress))
                {
                    if (peerAddress.Contains(":"))
                    {
                        string[] spl = peerAddress.Split(':');
                        Host = spl[0];
                        Port = int.Parse(spl[1]);
                    }
                    else
                    {
                        Host = peerAddress;
                        Port = DEFAULT_PORT;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Warn("cli parsing failed with exception", e);
            }

            logger.Info("<<<<<<<<<<<<<CommandLine options>>>>>>>>>>>>");
            logger.Info("CORE_CHAINCODE_ID_NAME: " + Id);
            logger.Info("CORE_PEER_ADDRESS: " + Host + ":" + Port);
            logger.Info("CORE_PEER_TLS_ENABLED: " + IsTlsEnabled);
            logger.Info("CORE_PEER_TLS_ROOTCERT_FILE" + (TlsClientRootCertPath ?? ""));
            logger.Info("CORE_TLS_CLIENT_KEY_PATH" + (TlsClientKeyPath ?? ""));
            logger.Info("CORE_TLS_CLIENT_CERT_PATH" + (TlsClientCertPath ?? ""));
        }

        private void InitializeLogging()
        {
            //TODO mpiva
            //Since we use liblog, which is a log abstraction library. after the real logging library is
            //decided, this can be coded.
            //Swapping liblog, for NET Core logging?...

        }

        private void ValidateOptions()
        {
            if (Id == null)
                throw new ArgumentException($"The chaincode id must be specified using either the -i or --i command line options or the {CORE_CHAINCODE_ID_NAME} environment variable.");
            if (IsTlsEnabled)
            {
                if (TlsClientCertPath == null)
                    throw new ArgumentException($"Client key certificate chain ({ENV_TLS_CLIENT_CERT_PATH}) was not specified.");
                if (TlsClientKeyPath == null)
                    throw new ArgumentException($"Client key ({ENV_TLS_CLIENT_KEY_PATH}) was not specified.");
                if (TlsClientRootCertPath == null)
                    throw new ArgumentException($"Peer certificate trust store ({CORE_PEER_TLS_ROOTCERT_FILE}) was not specified.");
            }
        }

        private void ProcessEnvironmentOptions()
        {
            string env = Environment.GetEnvironmentVariable(CORE_CHAINCODE_ID_NAME);
            if (!string.IsNullOrEmpty(env))
                Id = env;
            env = Environment.GetEnvironmentVariable(CORE_PEER_ADDRESS);
            if (!string.IsNullOrEmpty(env))
            {
                string[] hostArr = env.Split(':');
                if (hostArr.Length == 2)
                {
                    Port = int.Parse(hostArr[1].Trim());
                    Host = hostArr[0].Trim();
                }
                else
                {
                    logger.Error($"peer address argument should be in host:port format, ignoring current {env}");
                }
            }

            env = Environment.GetEnvironmentVariable(CORE_PEER_TLS_ENABLED);
            if (!string.IsNullOrEmpty(env))
            {
                IsTlsEnabled = bool.Parse(env);
                if (IsTlsEnabled)
                {
                    TlsClientRootCertPath = Environment.GetEnvironmentVariable(CORE_PEER_TLS_ROOTCERT_FILE);
                    TlsClientKeyPath = Environment.GetEnvironmentVariable(ENV_TLS_CLIENT_KEY_PATH);
                    TlsClientCertPath = Environment.GetEnvironmentVariable(ENV_TLS_CLIENT_CERT_PATH);
                }
            }

            logger.Info("<<<<<<<<<<<<<Enviromental options>>>>>>>>>>>>");
            logger.Info("CORE_CHAINCODE_ID_NAME: " + Id);
            logger.Info("CORE_PEER_ADDRESS: " + Host);
            logger.Info("CORE_PEER_TLS_ENABLED: " + IsTlsEnabled);
            logger.Info("CORE_PEER_TLS_ROOTCERT_FILE" + (TlsClientRootCertPath ?? ""));
            logger.Info("CORE_TLS_CLIENT_KEY_PATH" + (TlsClientKeyPath ?? ""));
            logger.Info("CORE_TLS_CLIENT_CERT_PATH" + (TlsClientCertPath ?? ""));
        }

        public async Task StartAsync(string[] args, CancellationToken token = default(CancellationToken))
        {
            exitSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
            try
            {
                ProcessEnvironmentOptions();
                ProcessCommandLineOptions(args);
                InitializeLogging();
                ValidateOptions();
                if (help)
                {
                    StringWriter wr = new StringWriter();
                    wr.WriteLine("Usage chaincode [OPTIONS]");
                    wr.WriteLine("Options:");
                    options.WriteOptionDescriptions(wr);
                    logger.Info(wr.ToString());
                    Console.Write(wr.ToString());
                    return;
                }

                if (Id == null)
                {
                    string error = $"The chaincode id must be specified using either the -i or --i command line options or the {CORE_CHAINCODE_ID_NAME} environment variable.";
                    logger.Error(error);
                    Console.WriteLine(error);
                }
                logger.Trace("chaincode started");
                Channel connection = NewPeerClientConnection();
                logger.Trace("connection created");
                ChaincodeSupportStream stream = new ChaincodeSupportStream();
                await stream.ProcessAndBlockAsync(connection, this, Id, exitSource.Token).ConfigureAwait(false);
                logger.Trace("Finished");
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                Console.WriteLine("Error: "+e.Message);
            }
            finally
            {
                AppDomain.CurrentDomain.ProcessExit -= ProcessExit;
            }
        }

        public void ProcessExit(object ob, EventArgs args)
        {
            logger.Info("ProcessExit!");
            exitSource.Cancel();
        }

        public Channel NewPeerClientConnection()
        {
            logger.Info("Configuring channel connection to peer.");
            ChannelCredentials cred = ChannelCredentials.Insecure;
            if (IsTlsEnabled)
            {
                if (!File.Exists(TlsClientRootCertPath))
                {
                    string msg = $"Root certificate not found at {TlsClientRootCertPath}";
                    logger.Error(msg);
                    throw new ArgumentException(msg);
                }

                string rootcert = File.ReadAllText(TlsClientRootCertPath);
                if (string.IsNullOrEmpty(TlsClientCertPath) || !File.Exists(TlsClientKeyPath))
                {
                    if (!string.IsNullOrEmpty(TlsClientKeyPath))
                    {
                        string msg = $"Certificate not found";
                        logger.Error(msg);
                        throw new ArgumentException(msg);
                    }
                }

                if (string.IsNullOrEmpty(TlsClientKeyPath) || !File.Exists(TlsClientCertPath))
                {
                    if (!string.IsNullOrEmpty(TlsClientCertPath))
                    {
                        string msg = $"Key not found";
                        logger.Error(msg);
                        throw new ArgumentException(msg);
                    }
                }

                string clientcert = null;
                string clientkey = null;
                if (!string.IsNullOrEmpty(TlsClientKeyPath) && !string.IsNullOrEmpty(TlsClientCertPath))
                {
                    clientcert = File.ReadAllText(TlsClientCertPath);
                    clientkey = File.ReadAllText(TlsClientKeyPath);
                }

                logger.Info("TLS is enabled");
                cred = clientcert != null ? new SslCredentials(rootcert, new KeyCertificatePair(clientcert, clientkey)) : new SslCredentials(rootcert);
            }

            return new Channel(Host, Port, cred);
        }

        protected static Response NewSuccessResponse(string message, byte[] payload)
        {
            return new Response(Status.SUCCESS, message, payload);
        }

        protected static Response NewSuccessResponse()
        {
            return NewSuccessResponse(null, null);
        }

        protected static Response NewSuccessResponse(string message)
        {
            return NewSuccessResponse(message, null);
        }

        protected static Response NewSuccessResponse(byte[] payload)
        {
            return NewSuccessResponse(null, payload);
        }

        protected static Response NewErrorResponse(string message, byte[] payload)
        {
            return new Response(Status.INTERNAL_SERVER_ERROR, message, payload);
        }

        protected static Response NewErrorResponse()
        {
            return NewErrorResponse(null, null);
        }

        protected static Response NewErrorResponse(string message)
        {
            return NewErrorResponse(message, null);
        }

        protected static Response NewErrorResponse(byte[] payload)
        {
            return NewErrorResponse(null, payload);
        }

        protected static Response newErrorResponse(Exception throwable)
        {
            return NewErrorResponse(throwable.Message, throwable.StackTrace.ToBytes());
        }
    }
}