using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using GalaxyService.Shared.Models;
using GalaxyService.WebApi.Models;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GalaxyService.WebApi.Controllers
{
    public class StarsController : ApiController
    {
        private static readonly Uri GalaxyServiceUri = new Uri(@"fabric:/GalaxyService/Processing");
        private readonly ServicePartitionResolver _servicePartitionResolver = ServicePartitionResolver.GetDefault();
        private readonly HttpClient _httpClient = new HttpClient();

        // GET api/values 
        public async Task<StarsInfo> Get(char c)
        {
            ServicePartitionKey partitionKey = new ServicePartitionKey(char.ToUpper(c) - 'A');

            // This contacts the Service Fabric Naming Services to get the addresses of the replicas of the processing service 
            // for the partition with the partition key generated above. 
            // Note that this gets the most current addresses of the partition's replicas,
            // however it is possible that the replicas have moved between the time this call is made and the time that the address is actually used
            // a few lines below.
            // For a complete solution, a retry mechanism is required.
            // For more information, see http://aka.ms/servicefabricservicecommunication
            ResolvedServicePartition partition =
                await _servicePartitionResolver.ResolveAsync(GalaxyServiceUri, partitionKey, CancellationToken.None);

            // Use anything but the primary replica, this is a read-only operation
            var secondaryEndpoints =
                partition.Endpoints
                    .Where(e => e.Role == ServiceEndpointRole.StatefulSecondary)
                    .ToArray();

            var randomEndpoint = secondaryEndpoints[new Random().Next(secondaryEndpoints.Length)];

            var addresses = JObject.Parse(randomEndpoint.Address);
            var secondaryReplicaAddress = (string)addresses["Endpoints"].First();

            var result = await _httpClient.GetStringAsync(secondaryReplicaAddress);

            return new StarsInfo
            {
                EndpointRole = randomEndpoint.Role.ToString(),
                EndpointAddress = secondaryReplicaAddress,
                Stars = JsonConvert.DeserializeObject<IEnumerable<StarEntity>>(result)
            };
        }

        // POST api/values 
        public async Task<StarInsertResult> Post(StarEntity star)
        {
            var galaxyName = star.GalaxyName;

            // The partitioning scheme of the processing service is a range of integers from 0 - 25.
            // This generates a partition key within that range by converting the first letter of the input name
            // into its numerica position in the alphabet.
            char partitionInput = galaxyName.First();
            var partitionKey = new ServicePartitionKey(char.ToUpper(partitionInput) - 'A');

            // This contacts the Service Fabric Naming Services to get the addresses of the replicas of the processing service 
            // for the partition with the partition key generated above. 
            // Note that this gets the most current addresses of the partition's replicas,
            // however it is possible that the replicas have moved between the time this call is made and the time that the address is actually used
            // a few lines below.
            // For a complete solution, a retry mechanism is required.
            // For more information, see http://aka.ms/servicefabricservicecommunication
            ResolvedServicePartition partition =
                await _servicePartitionResolver.ResolveAsync(GalaxyServiceUri, partitionKey, CancellationToken.None);

            // Get the primary replica because we're going to write to state
            ResolvedServiceEndpoint ep = partition.Endpoints.First(e => e.Role == ServiceEndpointRole.StatefulPrimary);

            JObject addresses = JObject.Parse(ep.Address);
            string primaryReplicaAddress = (string)addresses["Endpoints"].First();

            UriBuilder primaryReplicaUriBuilder = new UriBuilder(primaryReplicaAddress);

            var result = await _httpClient.PostAsJsonAsync(primaryReplicaUriBuilder.Uri, star);

            return new StarInsertResult
            {
                Result = await result.Content.ReadAsStringAsync(),
                PartitionKey = partitionKey.Value.ToString(),
                InputValue = galaxyName,
                ServicePartitionId = partition.Info.Id.ToString(),
                ServiceReplicaAddress = primaryReplicaAddress
            };
        }
    }
}
