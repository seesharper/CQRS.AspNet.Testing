using System.Security.Cryptography;
using CQRS.AspNet.Testing.Example;
using CQRS.Command.Abstractions;
using CQRS.Query.Abstractions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseLightInject(sr => sr.RegisterFrom<CompositionRoot>());
builder.Services.AddHttpClient("PostsClient", client => client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/posts"));
builder.Services.AddHttpClient<CommentsClient>(client => client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/comments"));
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/temperatures/{city}", ([FromServices] IQueryExecutor queryExecutor, string city)
    => queryExecutor.ExecuteAsync(new TemperatureQuery(city)));

app.MapPost("/temperatures", ([FromServices] ICommandExecutor commandExecutor, [FromBody] TemperatureCommand command)
    => commandExecutor.ExecuteAsync(command));

app.MapGet("/config", ([FromServices] IConfiguration configuration)
    => configuration.GetValue<string>("SomeConfigKey"));


app.MapGet("/comments", async (CommentsClient commentsClient) =>
{
    return await commentsClient.GetComments();
});

app.MapGet("/posts", async ([FromServices] IHttpClientFactory httpClientFactory) =>
{
    var postsClient = httpClientFactory.CreateClient("PostsClient");
    var response = await postsClient.GetAsync(string.Empty);
    return await response.Content.ReadAsStringAsync();
});

app.MapGet("/comments/{commentId}", async (CommentsClient commentsClient, string commentId) =>
{
    return await commentsClient.GetComment(commentId);
});


app.Run();

public partial class Program { }