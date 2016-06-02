using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using GalaxyService.Shared.Interfaces;
using GalaxyService.Shared.Models;
using GalaxyService.WebApi.Sharding;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace GalaxyService.WebApi.Controllers
{
    public class StarsController : ApiController
    {
        private static readonly Uri GalaxyServiceUri = new Uri(@"fabric:/GalaxyService/Processing");

        // GET api/values 
        public async Task<StarInfo> Get(string galaxyName, string starName)
        {
            ServicePartitionKey partitionKey = new ServicePartitionKey(PartitionKeyGenerator.Generate(galaxyName));

            var galaxyServiceClient = ServiceProxy.Create<IGalaxyStatefulService>(
                GalaxyServiceUri, partitionKey, TargetReplicaSelector.RandomSecondaryReplica
                );

            var proxy = (IServiceProxy)galaxyServiceClient;

            StarInfo result;

            if (string.IsNullOrEmpty(starName))
                result = await galaxyServiceClient.GetAllStarsAsync();
            else
                result = await galaxyServiceClient.GetStarAsync(galaxyName, starName);

            result.EndpointRole = proxy.ServicePartitionClient.TargetReplicaSelector.ToString();
            return result;
        }

        // POST api/values 
        public async Task<StarInsertResult> Post(StarEntity star)
        {
            var galaxyName = star.GalaxyName;

            ServicePartitionKey partitionKey = new ServicePartitionKey(PartitionKeyGenerator.Generate(galaxyName));

            var partition = await ServicePartitionResolver.GetDefault()
                .ResolveAsync(GalaxyServiceUri, partitionKey, CancellationToken.None);

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
                ServicePartitionId = partition.Info.Id.ToString(),
                ServiceReplicaAddress = proxy.ServicePartitionClient.ServiceUri.ToString()
            };
        }
    }
}
