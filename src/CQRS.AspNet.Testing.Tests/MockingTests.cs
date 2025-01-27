using System.Net;
using System.Net.Http.Json;
using CQRS.Command.Abstractions;
using LightInject;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RichardSzalay.MockHttp;

namespace CQRS.AspNet.Testing.Tests;

public class MockExtensionsTests
{
    [Fact]
    public void ShouldGetConfiguredValue()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.GetConfiguration().GetValue<string>("SomeConfigKey").ShouldBe("SomeConfigValue");
        testApplication.GetConfiguration().GetValue<string>("AnotherConfigKey").ShouldBe("AnotherConfigValue");
    }

    [Fact]
    public void ShouldUseConfiguredValues()
    {
        var testApplication = new TestApplication<Program>()
        .WithConfiguration("SomeConfigKey", "SomeOverriddenConfigValue")
        .WithConfiguration("AnotherConfigKey", "AnotherOverriddenConfigValue");

        testApplication.GetConfiguration().GetValue<string>("SomeConfigKey").ShouldBe("SomeOverriddenConfigValue");
        testApplication.GetConfiguration().GetValue<string>("AnotherConfigKey").ShouldBe("AnotherOverriddenConfigValue");
    }

    [Fact]
    public async Task ShouldUseConfiguredValuesInApp()
    {
        using var testApplication = new TestApplication<Program>()
        .WithConfiguration("SomeConfigKey", "SomeOverriddenConfigValue")
        .WithConfiguration("AnotherConfigKey", "AnotherOverriddenConfigValue");
        using var client = testApplication.CreateClient();
        var result = await client.GetStringAsync("/config");

    }

    [Fact]
    public async Task ShouldConfigureHostBuilder()
    {
        var testApplication = new TestApplication<Program>();
        var mock = new Mock<ICommandHandler<TemperatureCommand>>();
        testApplication.ConfigureHostBuilder(builder => builder.ConfigureServices(services => services.AddSingleton(mock.Object)));

        var client = testApplication.CreateClient();

        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 10.0)));

        mock.VerifyCommandHandler(Times.Once());
    }


    [Fact]
    public async Task ShouldGetTemperatures()
    {
        var testApplication = new TestApplication<Program>();
        var client = testApplication.CreateClient();

        var result = await client.GetFromJsonAsync<TemperatureQueryResult>("/temperatures/oslo");

        result!.Value.ShouldBe(22.0);
    }

    [Fact]
    public async Task ShouldMockLogger()
    {
        var testApplication = new TestApplication<Program>();
        var mockLogger = testApplication.MockLogger<TemperatureCommand>();


        var client = testApplication.CreateClient();

        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 10.0)));

        mockLogger.VerifyDebugMessage(Times.Once(), "This is a debug message");
        mockLogger.VerifyInformationMessage(Times.Once(), "This is an information message");
        mockLogger.VerifyWarningMessage(Times.Once(), "This is a warning message");
        mockLogger.VerifyErrorMessage(Times.Once(), "This is an error message");
        mockLogger.VerifyCriticalMessage(Times.Once(), "This is a critical message");

        mockLogger.VerifyDebugMessage(Times.Once());
        mockLogger.VerifyInformationMessage(Times.Once());
        mockLogger.VerifyWarningMessage(Times.Once());
        mockLogger.VerifyErrorMessage(Times.Once());
        mockLogger.VerifyCriticalMessage(Times.Once());
    }


    [Fact]
    public async Task ShouldMockCommandHandler()
    {
        var testApplication = new TestApplication<Program>();
        var commandHandlerMock = testApplication.MockCommandHandler<TemperatureCommand>();
        var client = testApplication.CreateClient();

        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 10.0)));

        // Verify that the command handler was called once without checking the command value
        commandHandlerMock.VerifyCommandHandler(Times.Once());

        // Verify that the command handler was called once with the command value 10.0
        commandHandlerMock.VerifyCommandHandler(mock => mock.Value == 10.0, Times.Once());
    }

    [Fact]
    public async Task ShouldQueryHandlerCommandHandler()
    {
        var testApplication = new TestApplication<Program>();
        var queryHandlerMock = testApplication.MockQueryHandler<TemperatureQuery, TemperatureQueryResult>().Returns(new TemperatureQueryResult(10.0));
        var client = testApplication.CreateClient();

        var result = await client.GetFromJsonAsync<TemperatureQueryResult>("/temperatures/oslo");

        // The value is now 10.0 instead of 22.0 which is the value returned from the original query handler.
        result!.Value.ShouldBe(10.0);

        // Verify that the query handler was called once without checking the query value
        queryHandlerMock.VerifyQueryHandler(Times.Once());

        // Verify that the query handler was called once with the query value "Oslo"
        queryHandlerMock.VerifyQueryHandler(query => query.City == "oslo", Times.Once());
    }

    [Fact]
    public void ShouldConfigureContainer()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.ConfigureContainer<IServiceContainer>(c => c.Register<Foo>());
        testApplication.Services.GetService<Foo>().ShouldNotBeNull();
    }

    [Fact]
    public void ShouldConfigureServices()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.ConfigureServices(services => services.AddSingleton<Foo>());
        testApplication.Services.GetService<Foo>().ShouldNotBeNull();
    }

    [Fact]
    public async Task ShouldRegisterMockUsingConfigureContainer()
    {
        var commandHandlerMock = new Mock<ICommandHandler<TemperatureCommand>>();
        var testApplication = new TestApplication<Program>();
        testApplication.ConfigureContainer<IServiceContainer>(sc =>
        {
            var test = sc.AvailableServices.Where(sr => sr.ServiceType == typeof(ICommandHandler<TemperatureCommand>)).ToList();
            sc.RegisterInstance(commandHandlerMock.Object);
            test = sc.AvailableServices.Where(sr => sr.ServiceType == typeof(ICommandHandler<TemperatureCommand>)).ToList();
        });
        var client = testApplication.CreateClient();
        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 10.0)));

        commandHandlerMock.VerifyCommandHandler(Times.Once());
    }

    [Fact]
    public async Task ShouldMockHttpClient()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.MockHttpClient<CommentsClient>()
            .When(HttpMethod.Get, "*/comments")
            .Respond("application/json", "{\"Name\":\"Test\"}");

        var client = testApplication.CreateClient();
        var response = await client.GetAsync("/comments");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.ReadAsStringAsync().Result.ShouldBe("{\"Name\":\"Test\"}");
    }

    [Fact]
    public async Task ShouldNotMockWhenUrlDoesNotMatch()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.MockHttpClient<CommentsClient>()
            .When(HttpMethod.Get, "*/comments")
            .Respond("application/json", "{\"Name\":\"Test\"}");
        var client = testApplication.CreateClient();
        var response = await client.GetAsync("/comments/1");
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Eliseo@gardner.biz");
    }

    [Fact]
    public async Task ShouldMockHttpClientUsingClientName()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.MockHttpClient("CommentsClient")
            .When(HttpMethod.Get, "*/comments")
            .Respond("application/json", "{\"Name\":\"Test\"}");

        var client = testApplication.CreateClient();
        var response = await client.GetAsync("/comments");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("{\"Name\":\"Test\"}");
    }

    [Fact]
    public async Task ShouldOnlyMockSpecifiedClient()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.MockHttpClient("CommentsClient")
            .When(HttpMethod.Get, "*/comments")
            .Respond("application/json", "{\"Name\":\"Test\"}");
        var client = testApplication.CreateClient();
        var response = await client.GetAsync("/posts");
        var posts = await response.Content.ReadFromJsonAsync<Posts[]>();
        posts!.Length.ShouldBe(100);
    }




    public class Foo { }


    /*
    {
    "userId": 1,
    "id": 1,
    "title": "sunt aut facere repellat provident occaecati excepturi optio reprehenderit",
    "body": "quia et suscipit\nsuscipit recusandae consequuntur expedita et cum\nreprehenderit molestiae ut ut quas totam\nnostrum rerum est autem sunt rem eveniet architecto"
  },
    */

    public record Posts(int UserId, int Id, string Title, string Body);
}

