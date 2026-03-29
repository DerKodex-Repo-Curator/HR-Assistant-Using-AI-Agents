
namespace SKPractice.Services;

public interface IHRPolicyService
{
    Task IngestDocumentAsync(); // new - loads doc into vector DB at startup
    Task<string> SearchPolicyAsync(string query); // changed - vector search
}