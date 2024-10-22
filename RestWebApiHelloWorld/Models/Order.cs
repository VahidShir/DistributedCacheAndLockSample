namespace DistributedCacheSample.Models;

public class Order
{
	public int Id { get; set; }
	public bool IsProcessed { get; set; }
	public string ProcessorInstanceName { get; set; }
}