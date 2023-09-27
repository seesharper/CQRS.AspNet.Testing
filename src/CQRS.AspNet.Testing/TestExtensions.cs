using System.Linq.Expressions;
using CQRS.Command.Abstractions;
using CQRS.Query.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace CQRS.AspNet.Testing;

/// <summary>
/// Contains extension methods for mocking services and verifying invocations.
/// </summary>
public static class TestExtensions
{
    /// <summary>
    /// Creates a new <see cref="Mock{T}"/> and registers it as a singleton in the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <typeparam name="T">The type of service to be mocked.</typeparam>
    /// <param name="hostBuilderConfiguration">The <see cref="IHostBuilderConfiguration"/> instance.</param>
    /// <returns>A new cref="Mock{T}"/>.</returns>
    public static Mock<T> MockService<T>(this IHostBuilderConfiguration hostBuilderConfiguration) where T : class
        => RegisterMockAsSingleton<T>(hostBuilderConfiguration);

    /// <summary>
    /// Creates an new <see cref="Mock{T}"/> for the <see cref="ILogger{TService}"/> and registers it as a singleton in the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <typeparam name="TService">The type for which the logger will be mocked.</typeparam>
    /// <param name="hostBuilderConfiguration">The <see cref="IHostBuilderConfiguration"/> instance.</param>
    /// <returns>The mocked service.</returns>
    public static Mock<ILogger<TService>> MockLogger<TService>(this IHostBuilderConfiguration hostBuilderConfiguration)
        => MockService<ILogger<TService>>(hostBuilderConfiguration);

    /// <summary>
    /// Creates a new <see cref="Mock{T}"/> for the <see cref="IQueryHandler{TQuery,TResult}"/> and registers it as a singleton in the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <typeparam name="TQuery">The query type for the <see cref="IQueryHandler{TQuery,TResult}"/> to be mocked.</typeparam>
    /// <typeparam name="TResult">The result type for the <see cref="IQueryHandler{TQuery,TResult}"/> to be mocked.</typeparam>
    /// <param name="hostBuilderConfiguration">The <see cref="IHostBuilderConfiguration"/> instance.</param>
    /// <returns>The mocked query handler.</returns>
    public static Mock<IQueryHandler<TQuery, TResult>> MockQueryHandler<TQuery, TResult>(this IHostBuilderConfiguration hostBuilderConfiguration) where TQuery : IQuery<TResult>
        => MockService<IQueryHandler<TQuery, TResult>>(hostBuilderConfiguration);

    /// <summary>
    /// Creates a new <see cref="Mock{T}"/> for the <see cref="ICommandHandler{TCommand}"/> and registers it as a singleton in the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <typeparam name="TCommand">The type of command handled by the command handler.</typeparam>
    /// <param name="hostBuilderConfiguration">The <see cref="IHostBuilderConfiguration"/> instance.</param>
    /// <returns>The mocked command handler.</returns>
    public static Mock<ICommandHandler<TCommand>> MockCommandHandler<TCommand>(this IHostBuilderConfiguration hostBuilderConfiguration) where TCommand : class
    {
        return MockService<ICommandHandler<TCommand>>(hostBuilderConfiguration);
    }

    /// <summary>
    /// Sets up the <see cref="Mock{T}"/> to return the specified value when the <see cref="IQueryHandler{TQuery,TResult}"/> is called.
    /// </summary>
    /// <typeparam name="TQuery">The query type for the mocked <see cref="IQueryHandler{TQuery,TResult}"/>.</typeparam>
    /// <typeparam name="TResult">The result type for the mocked <see cref="IQueryHandler{TQuery,TResult}"/>.</typeparam>
    /// <param name="mock">The <see cref="Mock{T}"/> representing the mocked query handler.</param>
    /// <param name="returnValue">The value to be returned from the query handler.</param>
    /// <returns>The mocked query handler.</returns>
    public static Mock<IQueryHandler<TQuery, TResult>> Returns<TQuery, TResult>(this Mock<IQueryHandler<TQuery, TResult>> mock, TResult returnValue) where TQuery : IQuery<TResult>
    {
        mock.Setup(m => m.HandleAsync(It.IsAny<TQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(returnValue);
        return mock;
    }

    /// <summary>
    /// Verifies that the <see cref="ICommandHandler{TCommand}"/> was called the specified number of times.     
    /// </summary>
    /// <typeparam name="TCommand">The command type of the handler to be verified.</typeparam>
    /// <param name="mock">The <see cref="Mock{T}"/> representing the mocked command handler.</param>
    /// <param name="times">The number of expected invocations.</param>
    public static void VerifyCommandHandler<TCommand>(this Mock<ICommandHandler<TCommand>> mock, Times times) where TCommand : class
        => mock.Verify(m => m.HandleAsync(It.IsAny<TCommand>(), It.IsAny<CancellationToken>()), times);

    /// <summary>
    /// Verifies that the <see cref="ICommandHandler{TCommand}"/> was called the specified number of times and that the command matches the specified expression.
    /// </summary>
    /// <typeparam name="TCommand">The command type of the handler to be verified.</typeparam>
    /// <param name="mock">The <see cref="Mock{T}"/> representing the mocked command handler.</param>
    /// <param name="match">A function used to match the command passed to the command handler.</param>
    /// <param name="times">The number of expected invocations.</param>
    public static void VerifyCommandHandler<TCommand>(this Mock<ICommandHandler<TCommand>> mock, Expression<Func<TCommand, bool>> match, Times times) where TCommand : class
        => mock.Verify(m => m.HandleAsync(It.Is(match), It.IsAny<CancellationToken>()), times);


    /// <summary>
    /// Verifies that the <see cref="IQueryHandler{TQuery, TResult}"/> was called the specified number of times.     
    /// </summary>    
    /// <typeparam name="TQuery">The query type of the handler to be verified.</typeparam>
    /// <typeparam name="TResult">The result type of the handler to be verified.</typeparam>
    /// <param name="mock">The <see cref="Mock{T}"/> representing the mocked query handler.</param>
    /// <param name="times">The number of expected invocations.</param>
    public static void VerifyQueryHandler<TQuery, TResult>(this Mock<IQueryHandler<TQuery, TResult>> mock, Times times) where TQuery : IQuery<TResult>
        => mock.Verify(m => m.HandleAsync(It.IsAny<TQuery>(), It.IsAny<CancellationToken>()), times);

    /// <summary>
    /// Verifies that the <see cref="IQueryHandler{TQuery, TResult}"/> was called the specified number of times and that the query matches the specified expression.
    /// </summary>
    /// <typeparam name="TQuery">The query type of the handler to be verified.</typeparam>
    /// <typeparam name="TResult">The result type of the handler to be verified.</typeparam>
    /// <param name="mock">The <see cref="Mock{T}"/> representing the mocked query handler.</param>
    /// <param name="match">A function used to match the query passed to the command handler.</param>
    /// <param name="times">The number of expected invocations.</param>
    public static void VerifyQueryHandler<TQuery, TResult>(this Mock<IQueryHandler<TQuery, TResult>> mock, Expression<Func<TQuery, bool>> match, Times times) where TQuery : IQuery<TResult>
        => mock.Verify(m => m.HandleAsync(It.Is(match), It.IsAny<CancellationToken>()), times);

    /// <summary>
    /// Verifies that the <see cref="ILogger{TTCategoryName}"/> was called with <see cref="LogLevel.Debug"/> the specified number of times and that the message contains the specified text.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    /// <param name="logger">The target <see cref="ILogger{TCategoryName}"/> instance.</param>
    /// <param name="times">The number of expected invocations.</param>
    /// <param name="containsMessage">The value expected to be contained in the log message.</param>
    public static void VerifyDebugMessage<TCategoryName>(this Mock<ILogger<TCategoryName>> logger, Times times, string? containsMessage = null) =>
        logger.VerifyLogger(LogLevel.Debug, times, containsMessage);

    /// <summary>
    /// Verifies that the <see cref="ILogger{TTCategoryName}"/> was called with <see cref="LogLevel.Information"/> the specified number of times and that the message contains the specified text.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    /// <param name="logger">The target <see cref="ILogger{TCategoryName}"/> instance.</param>
    /// <param name="times">The number of expected invocations.</param>
    /// <param name="containsMessage">The value expected to be contained in the log message.</param>
    public static void VerifyInformationMessage<TCategoryName>(this Mock<ILogger<TCategoryName>> logger, Times times, string? containsMessage = null) =>
        logger.VerifyLogger(LogLevel.Information, times, containsMessage);

    /// <summary>
    /// Verifies that the <see cref="ILogger{TTCategoryName}"/> was called with <see cref="LogLevel.Warning"/> the specified number of times and that the message contains the specified text.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    /// <param name="logger">The target <see cref="ILogger{TCategoryName}"/> instance.</param>
    /// <param name="times">The number of expected invocations.</param>
    /// <param name="containsMessage">The value expected to be contained in the log message.</param>
    public static void VerifyWarningMessage<TCategoryName>(this Mock<ILogger<TCategoryName>> logger, Times times, string? containsMessage = null) =>
        logger.VerifyLogger(LogLevel.Warning, times, containsMessage);

    /// <summary>
    /// Verifies that the <see cref="ILogger{TTCategoryName}"/> was called with <see cref="LogLevel.Error"/> the specified number of times and that the message contains the specified text.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    /// <param name="logger">The target <see cref="ILogger{TCategoryName}"/> instance.</param>
    /// <param name="times">The number of expected invocations.</param>
    /// <param name="containsMessage">The value expected to be contained in the log message.</param>
    public static void VerifyErrorMessage<TCategoryName>(this Mock<ILogger<TCategoryName>> logger, Times times, string? containsMessage = null) =>
        logger.VerifyLogger(LogLevel.Error, times, containsMessage);

    /// <summary>
    /// Verifies that the <see cref="ILogger{TTCategoryName}"/> was called with <see cref="LogLevel.Critical"/> the specified number of times and that the message contains the specified text.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    /// <param name="logger">The target <see cref="ILogger{TCategoryName}"/> instance.</param>
    /// <param name="times">The number of expected invocations.</param>
    /// <param name="containsMessage">The value expected to be contained in the log message.</param>
    public static void VerifyCriticalMessage<TCategoryName>(this Mock<ILogger<TCategoryName>> logger, Times times, string? containsMessage = null) =>
        logger.VerifyLogger(LogLevel.Critical, times, containsMessage);

    /// <summary>
    /// Verifies that the <see cref="ILogger{TTCategoryName}"/> was called the specified number of times and that the message contains the specified text.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    /// <param name="logger">The target <see cref="ILogger{TCategoryName}"/> instance.</param>
    /// <param name="level">The expected <see cref="LogLevel"/>.</param>
    /// <param name="times">The number of expected invocations.</param>
    /// <param name="containsMessage">The value expected to be contained in the log message.</param>
    /// <returns></returns>
    public static Mock<ILogger<TCategoryName>> VerifyLogger<TCategoryName>(this Mock<ILogger<TCategoryName>> logger, LogLevel level, Times times, string? containsMessage = null)
        => logger.VerifyLogger(level, times, message => containsMessage == null || message.Contains(containsMessage));

    /// <summary>
    /// Verifies that the <see cref="ILogger{TTCategoryName}"/> was called the specified number of times and that the message and exception optionally matches the specified expressions.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
    /// <param name="logger">The target <see cref="ILogger{TCategoryName}"/> instance.</param>
    /// <param name="level">The expected <see cref="LogLevel"/>.</param>
    /// <param name="times">The number of expected invocations.</param>
    /// <param name="messageMatcher">A function used to match the log message.</param>
    /// <param name="exceptionMatcher">A function used to match the logged exception.</param>
    /// <returns></returns>
    public static Mock<ILogger<TCategoryName>> VerifyLogger<TCategoryName>(this Mock<ILogger<TCategoryName>> logger, LogLevel level, Times times, Func<string, bool>? messageMatcher = null, Func<Exception?, bool>? exceptionMatcher = null)
    {
        logger.Verify(m => m.Log(
            level,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((x, y) => messageMatcher == null || messageMatcher(x.ToString()!)),
            It.Is<Exception?>(e => exceptionMatcher == null || exceptionMatcher(e)),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
        return logger;
    }

    private static Mock<T> RegisterMockAsSingleton<T>(IHostBuilderConfiguration hostBuilderConfiguration) where T : class
    {
        var mock = new Mock<T>();
        hostBuilderConfiguration.AddHostBuilderConfiguration(hb => hb.ConfigureServices(services => services.AddSingleton(mock.Object)));
        return mock;
    }

    /// <summary>
    /// Configures the <see cref="TestApplication{TEntryPoint}"/> to use the specified configuration key and value.
    /// </summary>
    /// <typeparam name="TEntryPoint">The type that represents the entry point for the application.</typeparam>
    /// <param name="testApplication">The target <see cref="TestApplication{TEntryPoint}"/>.</param>
    /// <param name="key">The key for the configuration value to be set.</param>
    /// <param name="value">The value of the configuration key.</param>
    /// <returns><see cref="TestApplication{TEntryPoint}"/> for chaining calls.</returns>
    public static TestApplication<TEntryPoint> WithConfiguration<TEntryPoint>(this TestApplication<TEntryPoint> testApplication, string key, string? value) where TEntryPoint : class
    {
        testApplication.ConfigureHostBuilder(builder => builder.ConfigureHostConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> { { key, value } })));
        testApplication.ConfigureHostBuilder(builder => builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> { { key, value } })));
        return testApplication;
    }

    /// <summary>
    /// Gets the <see cref="IConfiguration"/> from the <see cref="TestApplication{TEntryPoint}"/>.
    /// </summary>
    /// <typeparam name="TEntryPoint">The type that represents the entry point for the application.</typeparam>
    /// <param name="testApplication">The target <see cref="TestApplication{TEntryPoint}"/>.</param>
    /// <returns>The <see cref="IConfiguration"/> retrieved from the <see cref="IServiceProvider"/> used by the test application.</returns>
    public static IConfiguration GetConfiguration<TEntryPoint>(this TestApplication<TEntryPoint> testApplication) where TEntryPoint : class
        => testApplication.Services.GetRequiredService<IConfiguration>();


    /// <summary>
    /// Configures the given <typeparamref name="TContainer"/>
    /// </summary>
    /// <typeparam name="TContainer">The type of container to be configured.</typeparam>
    /// <param name="hostBuilderConfiguration">The <see cref="IHostBuilderConfiguration"/> instance.</param>
    /// <param name="configureContainer">The function used to configure the <typeparamref name="TContainer"/> instance.</param>
    /// <returns>The <see cref="IHostBuilderConfiguration"/> for chaining calls.</returns>
    public static IHostBuilderConfiguration ConfigureContainer<TContainer>(this IHostBuilderConfiguration hostBuilderConfiguration, Action<TContainer> configureContainer)
        where TContainer : class
    {
        hostBuilderConfiguration.AddHostBuilderConfiguration(builder => builder.ConfigureContainer<TContainer>(configureContainer));
        return hostBuilderConfiguration;
    }

    /// <summary>
    /// Configures the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="hostBuilderConfiguration">The <see cref="IHostBuilderConfiguration"/> instance.</param>
    /// <param name="configureServices">The function used to configure the <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IHostBuilderConfiguration"/> for chaining calls.</returns>
    public static IHostBuilderConfiguration ConfigureServices(this IHostBuilderConfiguration hostBuilderConfiguration, Action<IServiceCollection> configureServices)
    {
        hostBuilderConfiguration.AddHostBuilderConfiguration(builder => builder.ConfigureServices(configureServices));
        return hostBuilderConfiguration;
    }
}