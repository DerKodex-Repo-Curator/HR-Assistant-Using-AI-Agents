using Microsoft.Extensions.VectorData;
namespace SKPractice.Models;

public class HRPolicyRecord
{
    [VectorStoreKey]
    public ulong Id {get; set;}
    [VectorStoreData]
    public string SectionText { get; set; }
    [VectorStoreVector(1536)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}