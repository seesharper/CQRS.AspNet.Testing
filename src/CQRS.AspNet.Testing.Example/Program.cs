using CQRS.AspNet.Testing.Example;
using CQRS.Command.Abstractions;
using CQRS.Query.Abstractions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseLightInject(sr => sr.RegisterFrom<CompositionRoot>());

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/temperatures/{city}", ([FromServices] IQueryExecutor queryExecutor, string city)
    => queryExecutor.ExecuteAsync(new TemperatureQuery(city)));

app.MapPost("/temperatures", ([FromServices] ICommandExecutor commandExecutor, [FromBody] TemperatureCommand command)
    => commandExecutor.ExecuteAsync(command));

app.Run();

public partial class Program { }