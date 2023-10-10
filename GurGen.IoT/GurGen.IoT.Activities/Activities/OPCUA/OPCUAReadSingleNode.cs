using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using GurGen.IoT.Activities.Properties;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;

namespace GurGen.IoT.Activities.Activities.OPCUA
{
    
    [LocalizedDisplayName(nameof(Resources.OPCUAReadSingleNode_DisplayName))]
    [LocalizedDescription(nameof(Resources.OPCUAReadSingleNode_Description))]
    public class OPCUAReadSingleNode : ContinuableAsyncCodeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedDisplayName(nameof(Resources.OPCUAReadSingleNode_ServerURL_DisplayName))]
        [LocalizedDescription(nameof(Resources.OPCUAReadSingleNode_ServerURL_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> ServerURL { get; set; }

        [LocalizedDisplayName(nameof(Resources.OPCUAReadSingleNode_Anonymous_DisplayName))]
        [LocalizedDescription(nameof(Resources.OPCUAReadSingleNode_Anonymous_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<bool> Anonymous { get; set; }

        [LocalizedDisplayName(nameof(Resources.OPCUAReadSingleNode_UseSecureConnection_DisplayName))]
        [LocalizedDescription(nameof(Resources.OPCUAReadSingleNode_UseSecureConnection_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<bool> UseSecureConnection { get; set; }

        [LocalizedDisplayName(nameof(Resources.OPCUAReadSingleNode_NodeID_DisplayName))]
        [LocalizedDescription(nameof(Resources.OPCUAReadSingleNode_NodeID_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> NodeID { get; set; }

        [LocalizedDisplayName(nameof(Resources.OPCUAReadSingleNode_NodeObject_DisplayName))]
        [LocalizedDescription(nameof(Resources.OPCUAReadSingleNode_NodeObject_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<DataValue> NodeObject { get; set; }

        [LocalizedDisplayName(nameof(Resources.OPCUAReadSingleNode_ServerUserName_DisplayName))]
        [LocalizedDescription(nameof(Resources.OPCUAReadSingleNode_ServerUserName_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> ServerUserName { get; set; }

        [LocalizedDisplayName(nameof(Resources.OPCUAReadSingleNode_ServerPassword_DisplayName))]
        [LocalizedDescription(nameof(Resources.OPCUAReadSingleNode_ServerPassword_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> ServerPassword { get; set; }

        private Dictionary<string, Subscription> dic_subscriptions;
        private ApplicationConfiguration m_configuration;
        #endregion


        #region Constructors

        public OPCUAReadSingleNode()
        {
            dic_subscriptions = new Dictionary<string, Subscription>();


            var configuration = new ApplicationConfiguration()
            {
                ApplicationName = "GurGen.IoT",
                ApplicationUri = Utils.Format(@"urn:{0}:{1}", System.Net.Dns.GetHostName(), "GurGen.IoT"),
                ApplicationType = ApplicationType.Client,
                ServerConfiguration = new ServerConfiguration
                {
                    MaxSubscriptionCount = 100000,
                    MaxMessageQueueSize = 1000000,
                    MaxNotificationQueueSize = 1000000,
                    MaxPublishRequestCount = 10000000
                },
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = "GurGen" },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 6000000,
                    MaxStringLength = int.MaxValue,
                    MaxByteStringLength = int.MaxValue,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 419430400,
                    MaxBufferSize = 65535,
                    ChannelLifetime = -1,
                    SecurityTokenLifetime = -1
                },
                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 60000,
                    MinSubscriptionLifetime = -1,
                },
                DisableHiResClock = true, 
                TraceConfiguration = new TraceConfiguration()
            };
            configuration.Validate(ApplicationType.Client).GetAwaiter().GetResult();
            bool haveAppCertificate = configuration.SecurityConfiguration.ApplicationCertificate.Certificate is not null;
            if (!haveAppCertificate)
            {

                X509Certificate2 certificate = CertificateFactory.CreateCertificate(
                    configuration.SecurityConfiguration.ApplicationCertificate.StoreType,
                    configuration.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    configuration.ApplicationUri,
                    configuration.ApplicationName,
                    configuration.SecurityConfiguration.ApplicationCertificate.SubjectName,
                    null,
                    CertificateFactory.DefaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    600,
                    2048,
                    false,
                    null,
                    null
                    );

                configuration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;

            }

            haveAppCertificate = configuration.SecurityConfiguration.ApplicationCertificate.Certificate is not null;

            if (haveAppCertificate)
            {
                
                if (configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    configuration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;
                }
            }
            else
            {

            }

            var application = new ApplicationInstance
            {
                ApplicationName = "GurGen.IoT",
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = configuration
            };
            application.CheckApplicationInstanceCertificate(false, 2048).GetAwaiter().GetResult();
            m_configuration = configuration;


        }

        #endregion
        #region private methods
        private void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = e.Error.StatusCode == StatusCodes.BadCertificateUntrusted;
        }
        #endregion
        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (ServerURL == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(ServerURL)));
            if (Anonymous == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Anonymous)));
            if (UseSecureConnection == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(UseSecureConnection)));
            if (NodeID == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(NodeID)));
            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var serverurl = ServerURL.Get(context);
            var anonymous = Anonymous.Get(context);
            var nodeid = NodeID.Get(context);
            var usesecureconnection = UseSecureConnection.Get(context);
            var serverusername = ServerUserName.Get(context);
            var serverpassword = ServerPassword.Get(context);

            //Define user identity
            IUserIdentity userIdentity = null;
            if (anonymous)
            {
                userIdentity = new UserIdentity(new AnonymousIdentityToken());
            }
            else
            {
                userIdentity = new UserIdentity(serverusername, serverpassword);
            }

            // Create a new ApplicationConfiguration
            EndpointDescription endpointDescription = CoreClientUtils.SelectEndpoint(serverurl, usesecureconnection, 60000);
            EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(m_configuration);
            ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);



            // Create a new Session
            Session session = await Session.Create(
                m_configuration,
                endpoint,
                false,
                false,
                string.IsNullOrEmpty("GurGen.IoT") ? m_configuration.ApplicationName : "GurGen.IoT",
                60000,
                userIdentity,
                new string[] { }); 

            try
            {
                // Browse the server's namespace to find the variable you want to read
                ReferenceDescriptionCollection references;
                byte[] continuationPoint;
                session.Browse(null, null, ObjectIds.ObjectsFolder, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true, (uint)NodeClass.Variable, out continuationPoint, out references);

                // You can iterate through references to find the variable you want, or you can directly specify the NodeId of the variable

                DataValueCollection results;
                DiagnosticInfoCollection diagnosticInfos;

                // Read the variable's value
                ReadValueIdCollection nodesToRead = new ReadValueIdCollection
                {
                    new ReadValueId( )
                    {
                        NodeId = nodeid,
                        AttributeId = Attributes.Value
                    }
                };
                session.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out results, out diagnosticInfos);

                // Check if the read was successful
                if (results.Count > 0 && StatusCode.IsGood(results[0].StatusCode))
                {
                    // Access the value
                    DataValue value = results[0];
                    if (value is not null)
                    {
                        return (ctx) =>
                        {
                            NodeObject.Set(ctx, value);
                        };
                    }
                }

            }
            finally
            {
                // Close the session when done
                session.Close();
            }


            // Outputs
            return (ctx) =>
            {
                NodeObject.Set(ctx, null);
            };
        }

        #endregion
    }
}

