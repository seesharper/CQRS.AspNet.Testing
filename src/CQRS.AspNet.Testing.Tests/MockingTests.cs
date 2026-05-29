using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CQRS.Command.Abstractions;
using LightInject;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
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
        result.ShouldBe("SomeOverriddenConfigValue");
    }

    [Fact]
    public async Task ShouldChangeConfigurationAfterHostCreation()
    {
        using var testApplication = new TestApplication<Program>()
            .WithConfiguration("SomeConfigKey", "InitialValue");
        using var client = testApplication.CreateClient();

        var initialValue = await client.GetStringAsync("/config");
        initialValue.ShouldBe("InitialValue");

        testApplication.WithConfiguration("SomeConfigKey", "ChangedValue");

        var changedValue = await client.GetStringAsync("/config");
        changedValue.ShouldBe("ChangedValue");
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
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("{\"Name\":\"Test\"}");
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

    [Fact]
    public async Task ShouldUseClaims()
    {
        var testApplication = new TestApplication<Program>();
        var httpContext = new DefaultHttpContext().WithClaims(new Claim("role", "admin"), new Claim("department", "sales"));        
        testApplication.WithHttpContext(httpContext);
        var httpContextAccessor = testApplication.Services.GetRequiredService<IHttpContextAccessor>();
        var httpContextFromAccessor = httpContextAccessor.HttpContext;
        httpContextFromAccessor.ShouldNotBeNull();
        httpContextFromAccessor!.User.ShouldNotBeNull();
        httpContextFromAccessor.User.HasClaim("role", "admin").ShouldBeTrue();
        httpContextFromAccessor.User.HasClaim("department", "sales").ShouldBeTrue();
    }


    [Fact]
    public async Task ShouldResetMockWhenCalledTwiceOnSameTestApplication()
    {
        var testApplication = new TestApplication<Program>();

        // First round — register before CreateClient() so the mock is wired into DI
        var mock1 = testApplication.MockCommandHandler<TemperatureCommand>();
        var client = testApplication.CreateClient();

        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 10.0)));
        mock1.VerifyCommandHandler(cmd => cmd.Value == 10.0, Times.Once());

        // Second round — same instance, reset
        var mock2 = testApplication.MockCommandHandler<TemperatureCommand>();
        mock2.ShouldBeSameAs(mock1);
        mock2.VerifyCommandHandler(Times.Never());

        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 20.0)));
        mock2.VerifyCommandHandler(cmd => cmd.Value == 20.0, Times.Once());
        mock2.VerifyCommandHandler(cmd => cmd.Value == 10.0, Times.Never());
    }

    [Fact]
    public void ShouldWithHttpContextWhenNoExistingHttpContextAccessor()
    {
        var testApplication = new TestApplication<Program>();
        // Remove IHttpContextAccessor before WithHttpContext runs so the null branch is exercised
        testApplication.ConfigureServices(services =>
        {
            var desc = services.FirstOrDefault(s => s.ServiceType == typeof(IHttpContextAccessor));
            if (desc != null) services.Remove(desc);
        });
        var httpContext = new DefaultHttpContext().WithClaims(new Claim("role", "admin"));
        testApplication.WithHttpContext(httpContext);

        var accessor = testApplication.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext.ShouldNotBeNull();
        accessor.HttpContext!.User.HasClaim("role", "admin").ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldMockServiceDirectly()
    {
        var testApplication = new TestApplication<Program>();
        var mock = testApplication.MockService<ICommandHandler<TemperatureCommand>>();
        var client = testApplication.CreateClient();

        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 10.0)));

        mock.Verify(m => m.HandleAsync(It.IsAny<TemperatureCommand>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ShouldVerifyLoggerWithCustomMessageMatcher()
    {
        var testApplication = new TestApplication<Program>();
        var mockLogger = testApplication.MockLogger<TemperatureCommand>();
        var client = testApplication.CreateClient();

        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 10.0)));

        mockLogger.VerifyLogger(LogLevel.Debug, Times.Once(), msg => msg.StartsWith("This is a debug"));
        mockLogger.VerifyLogger(LogLevel.Information, Times.Once(), msg => msg.StartsWith("This is an information"));
    }

    [Fact]
    public async Task ShouldVerifyLoggerWithExceptionMatcher()
    {
        var testApplication = new TestApplication<Program>();
        var mockLogger = testApplication.MockLogger<TemperatureCommand>();
        var client = testApplication.CreateClient();

        await client.PostAsync("/temperatures", JsonContent.Create(new TemperatureCommand("Oslo", 10.0)));

        mockLogger.VerifyLogger(
            LogLevel.Error,
            Times.Once(),
            msg => msg.Contains("error message"),
            ex => ex is Exception { Message: "This is an exception" });

        mockLogger.VerifyLogger(
            LogLevel.Critical,
            Times.Once(),
            msg => msg.Contains("critical message"),
            ex => ex is Exception { Message: "This is a critical exception" });
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

