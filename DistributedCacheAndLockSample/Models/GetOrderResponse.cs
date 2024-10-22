namespace DistributedCacheAndLockSample.Models;

public record GetOrderResponse
{
    public int Id { get; set; }
    public bool IsProcessed { get; set; }
    public string ProcessorInstanceName { get; set; }
}