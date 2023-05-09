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
    using System.Threading;
    using System.Diagnostics;
    using Castle.Components.DictionaryAdapter;

    [TestClass]
    public class ClientTelemetryConfigurationTest 
    {
        private const string EndpointUrl = "http://dummy.test.com/";


        [TestInitialize]
        public void TestInitialize()
        {
            
        }

        /*  [TestMethod]
          public async Task Validate_ClientTelemetryJob_Is_Running_if_EnabledAsync()
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
                                  IsEnabled = true,
                                  Endpoint = EndpointUrl
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

              ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity("MyTestPkValue");
              ItemResponse<ToDoActivity> createResponse = await container.CreateItemAsync<ToDoActivity>(testItem);

              Assert.IsNotNull(this.GetClient().DocumentClient.ClientTelemetryTask);
          }*/

        /* [TestMethod]
         public async Task Validate_ClientTelemetryJob_Is_Not_Running_if_Disabled()
         {
             HttpClientHandlerHelper httpHandler = new HttpClientHandlerHelper
             {
                 RequestCallBack = (request, cancellation) =>
                 {
                     if (request.RequestUri.AbsoluteUri.Contains(Documents.Paths.ClientConfigPathSegment))
                     {
                         HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

                         AccountClientConfigProperties clientConfigProperties = new AccountClientConfigProperties
                         {
                             ClientTelemetryConfiguration = new ClientTelemetryConfiguration
                             {
                                 IsEnabled = false,
                                 Endpoint = EndpointUrl
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

             ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity("MyTestPkValue");
             ItemResponse<ToDoActivity> createResponse = await container.CreateItemAsync<ToDoActivity>(testItem);

             Assert.IsNull(this.GetClient().DocumentClient.ClientTelemetryTask);
         }
 */

        public virtual int c { set; get; }

        [TestMethod]
        public async Task Validate_ClientTelemetryJob_Is_Stopping_if_First_Enabled_Then_Disabled()
        {
            CosmosClientBuilder cosmosClientBuilder = TestCommon.GetDefaultConfiguration();
            Console.WriteLine("Thread Id : " + Thread.CurrentThread.ManagedThreadId + " Thread Name : " + Thread.CurrentThread.Name);
  
            Stopwatch watch = Stopwatch.StartNew();
            HttpClientHandlerHelper httpHandler = new HttpClientHandlerHelper
            {
                RequestCallBack = (request, cancellation) =>
                {
                    Console.WriteLine(request.RequestUri.AbsoluteUri);
                    Console.WriteLine("Thread Id : " + Thread.CurrentThread.ManagedThreadId + " Thread Name : " + Thread.CurrentThread.Name);
                    if (request.RequestUri.AbsoluteUri.Contains(Documents.Paths.ClientConfigPathSegment))
                    {
                        Console.WriteLine(this.c++);
                        Console.WriteLine(" ElapsedMilliseconds => " + watch.ElapsedMilliseconds);

                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

                        AccountClientConfigProperties clientConfigProperties = new AccountClientConfigProperties
                        {
                            ClientTelemetryConfiguration = new ClientTelemetryConfiguration
                            {
                                IsEnabled = true,
                                Endpoint = EndpointUrl
                            }
                        };
                        if (this.c > 5)
                        {
                            clientConfigProperties = new AccountClientConfigProperties
                            {
                                ClientTelemetryConfiguration = new ClientTelemetryConfiguration
                                {
                                    IsEnabled = false,
                                    Endpoint = null
                                }
                            };
                        }

                        Console.WriteLine("ClientTelemetry flag is enabled " + clientConfigProperties.ClientTelemetryConfiguration.IsEnabled);
                        string payload = JsonConvert.SerializeObject(clientConfigProperties);
                        result.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                        return Task.FromResult(result);
                    }
                    return null;
                }
            };

            cosmosClientBuilder
                .WithHttpClientFactory(() => new HttpClient(httpHandler));

            CosmosClient client = cosmosClientBuilder.Build();
/*
            FieldInfo field = typeof(GlobalEndpointManager).GetField("backgroundRefreshAccountClientConfigTimeIntervalInMS", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(client.DocumentClient.GlobalEndpointManager, 10);
*/
            Database database = await client.CreateDatabaseAsync(Guid.NewGuid().ToString());

            Container container = (Container)await database.CreateContainerAsync(Guid.NewGuid().ToString(), "/pk");

            ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity("MyTestPkValue");
            ItemResponse<ToDoActivity> createResponse = await container.CreateItemAsync<ToDoActivity>(testItem);

            Assert.IsNotNull(client.DocumentClient.ClientTelemetryTask);

            //await Task.Delay(TimeSpan.FromMilliseconds(100));
            
            //Assert.IsNull(client.DocumentClient.ClientTelemetryTask);
        }

       /* [TestMethod]
        public async Task Validate_ClientTelemetryJob_Is_Starting_if_First_Disabled_Then_Enabled()
        {
            int counter = 0;
            HttpClientHandlerHelper httpHandler = new HttpClientHandlerHelper
            {
                RequestCallBack = (request, cancellation) =>
                {
                    if (request.RequestUri.AbsoluteUri.Contains(Documents.Paths.ClientConfigPathSegment))
                    {
                        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

                        AccountClientConfigProperties clientConfigProperties = new AccountClientConfigProperties
                        {
                            ClientTelemetryConfiguration = new ClientTelemetryConfiguration
                            {
                                IsEnabled = false,
                                Endpoint = null
                            }
                        };
                        if (counter > 5)
                        {
                            clientConfigProperties = new AccountClientConfigProperties
                            {
                                ClientTelemetryConfiguration = new ClientTelemetryConfiguration
                                {
                                    IsEnabled = true,
                                    Endpoint = EndpointUrl
                                }
                            };
                        }

                        string payload = JsonConvert.SerializeObject(clientConfigProperties);
                        result.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                        counter++;
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

            ToDoActivity testItem = ToDoActivity.CreateRandomToDoActivity("MyTestPkValue");
            ItemResponse<ToDoActivity> createResponse = await container.CreateItemAsync<ToDoActivity>(testItem);

            Assert.IsNull(this.GetClient().DocumentClient.ClientTelemetryTask);

            await Task.Delay(TimeSpan.FromMilliseconds(100));

            Assert.IsNotNull(this.GetClient().DocumentClient.ClientTelemetryTask);
        }*/
    }
}
