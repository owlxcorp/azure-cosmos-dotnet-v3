//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.SDK.EmulatorTests
{
    using System.Net.Http;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Resource.Settings;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Microsoft.Azure.Cosmos.Fluent;
    using System;
    using Microsoft.Azure.Cosmos.Routing;
    using System.Reflection;

    [TestClass]
    public class ClientTelemetryConfigurationTest : BaseCosmosClientHelper
    {
        private const string EndpointUrl = "http://dummy.test.com/";
        private CosmosClientBuilder cosmosClientBuilder;

        [TestInitialize]
        public void TestInitialize()
        {
            this.cosmosClientBuilder = TestCommon.GetDefaultConfiguration();
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await base.TestCleanup();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Validate_ClientTelemetryJob_Status_if_Enabled_Or_DisabledAsync(bool isEnabled)
        {
            HttpClientHandlerHelper httpHandler = new HttpClientHandlerHelper
            {
                RequestCallBack = (request, cancellation) =>
                {
                    if (request.RequestUri.AbsoluteUri.Equals(EndpointUrl))
                    {
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                        return Task.FromResult(result);
                    }
                    else if (request.RequestUri.AbsoluteUri.Contains(Documents.Paths.ClientConfigPathSegment))
                    {
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

                        AccountClientConfigProperties clientConfigProperties = new AccountClientConfigProperties
                        {
                            ClientTelemetryConfiguration = new ClientTelemetryConfiguration
                            {
                                IsEnabled = isEnabled,
                                Endpoint = isEnabled ? EndpointUrl: null
                            }
                        };

                        string payload = JsonConvert.SerializeObject(clientConfigProperties);
                        result.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                        return Task.FromResult(result);
                    }
                    return null;
                }
            };

            this.cosmosClientBuilder
                .WithHttpClientFactory(() => new HttpClient(httpHandler));

            this.SetClient(this.cosmosClientBuilder.Build());

            Database database = await this.GetClient().CreateDatabaseAsync(Guid.NewGuid().ToString());

            Container container = (Container)await database.CreateContainerAsync(Guid.NewGuid().ToString(), "/pk");

            if (isEnabled)
            {
                Assert.IsNotNull(this.GetClient().DocumentClient.ClientTelemetryTask);
            }
            else
            {
                Assert.IsNull(this.GetClient().DocumentClient.ClientTelemetryTask);
            }
        }
        
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Validate_ClientTelemetryJob_When_Flag_Is_Switched(bool isEnabledInitially)
        {
            int counter = 0;
            HttpClientHandlerHelper httpHandler = new HttpClientHandlerHelper
            {
                RequestCallBack = (request, cancellation) =>
                {
                    if (request.RequestUri.AbsoluteUri.Contains(Documents.Paths.ClientConfigPathSegment))
                    {
                        counter++;
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

                        AccountClientConfigProperties clientConfigProperties = counter < 5
                            ? new AccountClientConfigProperties
                            {
                                ClientTelemetryConfiguration = new ClientTelemetryConfiguration
                                {
                                    IsEnabled = isEnabledInitially,
                                    Endpoint = isEnabledInitially ? EndpointUrl : null
                                }
                            }
                            : new AccountClientConfigProperties
                            {
                                ClientTelemetryConfiguration = new ClientTelemetryConfiguration
                                {
                                    IsEnabled = !isEnabledInitially,
                                    Endpoint = !isEnabledInitially ? EndpointUrl : null
                                }
                            };
                        
                        string payload = JsonConvert.SerializeObject(clientConfigProperties);
                        result.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                        
                        return Task.FromResult(result);
                    }
                    return null;
                }
            };

            this.cosmosClientBuilder
                .WithHttpClientFactory(() => new HttpClient(httpHandler));

            this.SetClient(this.cosmosClientBuilder.Build());

            FieldInfo field = typeof(GlobalEndpointManager).GetField("backgroundRefreshAccountClientConfigTimeIntervalInMS", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(this.GetClient().DocumentClient.GlobalEndpointManager, 10);

            Database database = await this.GetClient().CreateDatabaseAsync(Guid.NewGuid().ToString());

            if (isEnabledInitially)
            {
                Assert.IsNotNull(this.GetClient().DocumentClient.ClientTelemetryTask, "Before: Client Telemetry Job should be Running");
            }
            else
            {
                Assert.IsNull(this.GetClient().DocumentClient.ClientTelemetryTask, "Before: Client Telemetry Job should be Stopped");
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            
            if (isEnabledInitially)
            {
                Assert.IsNull(this.GetClient().DocumentClient.ClientTelemetryTask, "After: Client Telemetry Job should be Stopped");
            }
            else
            {
                Assert.IsNotNull(this.GetClient().DocumentClient.ClientTelemetryTask, "After: Client Telemetry Job should be Running");
            }
            
        }
    }
}
