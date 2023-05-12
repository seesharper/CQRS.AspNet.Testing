using System.Net.Http.Json;
using CQRS.Command.Abstractions;
using LightInject;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace CQRS.AspNet.Testing.Tests;

public class MockExtensionsTests
{
    [Fact]
    public void ShouldGetConfiguredValue()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.GetConfiguration().GetValue<string>("SomeConfigKey").Should().Be("SomeConfigValue");
        testApplication.GetConfiguration().GetValue<string>("AnotherConfigKey").Should().Be("AnotherConfigValue");
    }

    [Fact]
    public void ShouldUseConfiguredValues()
    {
        var testApplication = new TestApplication<Program>()
        .WithConfiguration("SomeConfigKey", "SomeOverriddenConfigValue")
        .WithConfiguration("AnotherConfigKey", "AnotherOverriddenConfigValue");

        testApplication.GetConfiguration().GetValue<string>("SomeConfigKey").Should().Be("SomeOverriddenConfigValue");
        testApplication.GetConfiguration().GetValue<string>("AnotherConfigKey").Should().Be("AnotherOverriddenConfigValue");
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

        result!.Value.Should().Be(22.0);
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
        result!.Value.Should().Be(10.0);

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
        testApplication.Services.GetService<Foo>().Should().NotBeNull();
    }

    [Fact]
    public void ShouldConfigureServices()
    {
        var testApplication = new TestApplication<Program>();
        testApplication.ConfigureServices(services => services.AddSingleton<Foo>());
        testApplication.Services.GetService<Foo>().Should().NotBeNull();
    }
    public class Foo { }
}

