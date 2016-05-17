using System.Collections.Generic;
using GalaxyService.Shared.Models;

namespace GalaxyService.WebApi.Models
{
    public class StarsInfo
    {
        public string EndpointRole { get; set; }

        public string EndpointAddress { get; set; }

        public IEnumerable<StarEntity> Stars { get; set; }
    }
}
