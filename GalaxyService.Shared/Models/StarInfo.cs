using System.Collections.Generic;

namespace GalaxyService.Shared.Models
{
    public class StarInfo
    {
        public string EndpointRole { get; set; }

        public string PartitionId { get; set; }

        public IEnumerable<StarEntity> Stars { get; set; }
    }
}
