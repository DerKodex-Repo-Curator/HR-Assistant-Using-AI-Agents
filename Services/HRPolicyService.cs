using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.AI;
using SKPractice.Models;

namespace SKPractice.Services;
public class HRPolicyService : IHRPolicyService
{
    private readonly string _filePath;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly VectorStoreCollection<string, HRPolicyRecord> _collection;
    public HRPolicyService(string filePath, 
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    VectorStoreCollection<string, HRPolicyRecord> collection)
    {
        _filePath = filePath;
        _embeddingGenerator = embeddingGenerator;
        _collection = collection;
    }

    public async Task IngestDocumentAsync()
    {
        // Check if collection already exists with data — skip if so
        if (await _collection.CollectionExistsAsync())
        {
            var existing = await _collection.GetAsync("0");
            if (existing != null)
            {
                Console.WriteLine("HR Policy already ingested, skipping...");
                return;
            }
        }

        // Create collection if it doesn't exist
        await _collection.EnsureCollectionExistsAsync();

        // Read and split the document into sections
        var content = File.ReadAllText(_filePath);
        var sections = content.Split("Section ", StringSplitOptions.RemoveEmptyEntries);

        // Embed each section and store in Qdrant
        for (ulong i = 0; i < (ulong)sections.Length; i++)
        {
            var sectionText = "Section " + sections[i].Trim();
            var embedding = await _embeddingGenerator.GenerateAsync([sectionText]);
            await _collection.UpsertAsync(new HRPolicyRecord
            {
                Id = i.ToString(),
                SectionText = sectionText,
                Embedding = embedding[0].Vector
            });
        }
        Console.WriteLine($"Ingested {sections.Length} sections into Azure AI Search");        
    }

    public async Task<string> SearchPolicyAsync(string query)
    {
        var queryEmbedding = await _embeddingGenerator.GenerateAsync([query]);
        var results = new List<string>();
        await foreach (var result in _collection.SearchAsync(queryEmbedding[0].Vector, top: 3))  
        {
            results.Add(result.Record.SectionText);
        }

        return results.Count>0
        ? string.Join("\n\n --- \n\n", results)
        : "No matching policy found.";
    }
}
