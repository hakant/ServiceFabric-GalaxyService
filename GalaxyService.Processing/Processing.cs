using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaxyService.Shared.Models;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GalaxyService.Processing
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Processing : StatefulService
    {
        public Processing(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see http://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] { new ServiceReplicaListener(CreateInternalListener, listenOnSecondary: true) };
        }

        private ICommunicationListener CreateInternalListener(ServiceContext context)
        {
            // Partition replica's URL is the node's IP, port, PartitionId, ReplicaId, Guid
            EndpointResourceDescription internalEndpoint = context.CodePackageActivationContext.GetEndpoint("ProcessingServiceEndpoint");

            // Multiple replicas of this service may be hosted on the same machine,
            // so this address needs to be unique to the replica which is why we have partition ID + replica ID in the URL.
            // HttpListener can listen on multiple addresses on the same port as long as the URL prefix is unique.
            // The extra GUID is there for an advanced case where secondary replicas also listen for read-only requests.
            // When that's the case, we want to make sure that a new unique address is used when transitioning from primary to secondary
            // to force clients to re-resolve the address.
            // '+' is used as the address here so that the replica listens on all available hosts (IP, FQDM, localhost, etc.)

            string uriPrefix =
                $"{internalEndpoint.Protocol}://+:{internalEndpoint.Port}/{context.PartitionId}/{context.ReplicaOrInstanceId}-{Guid.NewGuid()}/";

            var nodeIp = FabricRuntime.GetNodeContext().IPAddressOrFQDN;

            // The published URL is slightly different from the listening URL prefix.
            // The listening URL is given to HttpListener.
            // The published URL is the URL that is published to the Service Fabric Naming Service,
            // which is used for service discovery. Clients will ask for this address through that discovery service.
            // The address that clients get needs to have the actual IP or FQDN of the node in order to connect,
            // so we need to replace '+' with the node's IP or FQDN.
            var uriPublished = uriPrefix.Replace("+", nodeIp);
            return new HttpCommunicationListener(uriPrefix, uriPublished, ProcessInternalRequest);
        }

        private async Task ProcessInternalRequest(HttpListenerContext context, CancellationToken cancelRequest)
        {
            string output = null;

            try
            {
                switch (context.Request.HttpMethod)
                {
                    case "GET":
                        {
                            var stars = await GetAllStarsAsync();
                            output = JsonConvert.SerializeObject(stars, Formatting.None);
                            break;
                        }
                    case "POST":
                        {
                            var reader = new StreamReader(context.Request.InputStream);
                            var star = reader.ReadToEnd();

                            output = await AddStarAsync(JsonConvert.DeserializeObject<StarEntity>(star));
                            break;

                        }
                }
            }
            catch (Exception ex)
            {
                output = ex.Message;
            }

            using (HttpListenerResponse response = context.Response)
            {
                if (output != null)
                {
                    byte[] outBytes = Encoding.UTF8.GetBytes(output);
                    response.OutputStream.Write(outBytes, 0, outBytes.Length);
                }
            }
        }

        private async Task<string> AddStarAsync(StarEntity star)
        {
            var dictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, StarEntity>>("stars");

            using (ITransaction tx = StateManager.CreateTransaction())
            {
                var addResult = await dictionary.TryAddAsync(tx, star.StarName.ToUpperInvariant(), star);

                await tx.CommitAsync();

                return $"Star {star.StarName} {(addResult ? "sucessfully added" : "already exists")}";
            }
        }

        private async Task<IEnumerable<StarEntity>> GetAllStarsAsync()
        {
            var stars = new List<StarEntity>();

            var result = await StateManager.TryGetAsync<IReliableDictionary<string, StarEntity>>("stars");

            if (!result.HasValue)
                return stars;

            var dictionary = result.Value;

            using (ITransaction tx = StateManager.CreateTransaction())
            {
                var values = await dictionary.CreateEnumerableAsync(tx, EnumerationMode.Unordered);
                var enumerator = values.GetAsyncEnumerator();

                var cont = await enumerator.MoveNextAsync(CancellationToken.None);

                while (cont)
                {
                    stars.Add(enumerator.Current.Value);
                    cont = await enumerator.MoveNextAsync(CancellationToken.None);
                }
            }

            return stars;
        }
    }
}
