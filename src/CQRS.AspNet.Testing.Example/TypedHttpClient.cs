namespace CQRS.AspNet.Testing.Example;


public interface ITypedHttpClient
{
    Task<int> GetValue();
}


public class TypedHttpClient : ITypedHttpClient
{
    private readonly HttpClient _httpClient;

    public TypedHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<int> GetValue()
    {
        return 42;
    }
}