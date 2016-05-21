using System.Collections.Generic;

namespace GalaxyService.Shared.Models
{
    public class StarEntity
    {
        public string Id { get; set; }

        public string GalaxyId { get; set; }

        public string GalaxyName { get; set; }

        public string StarName { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Data { get; set; }
    }
}
