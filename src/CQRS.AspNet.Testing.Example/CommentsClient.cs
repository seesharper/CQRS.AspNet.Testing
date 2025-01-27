namespace CQRS.AspNet.Testing.Example;

public class CommentsClient(HttpClient httpClient)
{
    public async Task<string> GetComments()
    {
        var response = await httpClient.GetAsync(string.Empty);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetComment(string commentId)
    {
        var response = await httpClient.GetAsync($"https://jsonplaceholder.typicode.com/comments/{commentId}");
        return await response.Content.ReadAsStringAsync();
    }
}