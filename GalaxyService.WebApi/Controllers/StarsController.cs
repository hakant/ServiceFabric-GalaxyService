using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using GalaxyService.Shared.Interfaces;
using GalaxyService.Shared.Models;
using GalaxyService.WebApi.Models;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace GalaxyService.WebApi.Controllers
{
    public class StarsController : ApiController
    {
        private static readonly Uri GalaxyServiceUri = new Uri(@"fabric:/GalaxyService/Processing");

        // GET api/values 
        public async Task<StarsInfo> Get(char c)
        {
            ServicePartitionKey partitionKey = new ServicePartitionKey(char.ToUpper(c) - 'A');

            var galaxyServiceClient = ServiceProxy.Create<IGalaxyStatefulService>(
                GalaxyServiceUri, partitionKey, TargetReplicaSelector.RandomSecondaryReplica
                );

            var result = await galaxyServiceClient.GetAllStarsAsync();

            var proxy = (IServiceProxy) galaxyServiceClient;

            return new StarsInfo
            {
                EndpointRole = proxy.ServicePartitionClient.TargetReplicaSelector.ToString(),
                EndpointAddress = proxy.ServicePartitionClient.ServiceUri.ToString(),
                Stars = result
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

            var galaxyServiceClient = ServiceProxy.Create<IGalaxyStatefulService>(
                GalaxyServiceUri, partitionKey, TargetReplicaSelector.PrimaryReplica
                );

            var result = await galaxyServiceClient.AddStarAsync(star);

            var proxy = (IServiceProxy)galaxyServiceClient;

            return new StarInsertResult
            {
                Result = result,
                PartitionKey = partitionKey.Value.ToString(),
                InputValue = galaxyName,
                ServicePartitionId = string.Empty,
                ServiceReplicaAddress = proxy.ServicePartitionClient.ServiceUri.ToString()
            };
        }
    }
}
