namespace GalaxyService.Shared.Models
{
    public class StarInsertResult
    {
        public string Result { get; set; }
        public string PartitionKey { get; set; }
        public string InputValue { get; set; }
        public string ServicePartitionId { get; set; }
        public string ServiceReplicaAddress { get; set; }
    }
}
