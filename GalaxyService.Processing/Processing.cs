using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using GalaxyService.Shared.Interfaces;
using GalaxyService.Shared.Models;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace GalaxyService.Processing
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class Processing : StatefulService, IGalaxyStatefulService
    {
        public Processing(StatefulServiceContext context)
            : base(context)
        { }

        public Processing(StatefulServiceContext context, IReliableStateManagerReplica stateManagerReplica)
            : base(context, stateManagerReplica)
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
            return new[]
            {
                new ServiceReplicaListener(this.CreateServiceRemotingListener, listenOnSecondary: true)
            };
        }

        public async Task<string> AddStarAsync(StarEntity star)
        {
            var dictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, StarEntity>>("stars");

            using (ITransaction tx = StateManager.CreateTransaction())
            {
                var addResult = await dictionary.TryAddAsync(tx, star.StarName.ToUpperInvariant(), star);

                await tx.CommitAsync();

                return $"Star {star.StarName} {(addResult ? "sucessfully added" : "already exists")}";
            }
        }

        public async Task<IEnumerable<StarEntity>> GetAllStarsAsync()
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
