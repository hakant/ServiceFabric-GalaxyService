using System.Collections.Generic;
using System.Threading.Tasks;
using GalaxyService.Shared.Models;
using Microsoft.ServiceFabric.Services.Remoting;

namespace GalaxyService.Shared.Interfaces
{
    public interface IGalaxyStatefulService : IService
    {
        Task<string> AddStarAsync(StarEntity star);
        Task<StarInfo> GetAllStarsAsync(string galaxyName);
        Task<StarInfo> GetStarAsync(string galaxyName, string starName);
    }
}
