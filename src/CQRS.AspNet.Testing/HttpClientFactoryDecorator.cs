using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace CQRS.AspNet.Testing;

/// <summary>
/// Decorates the <see cref="IHttpClientFactory"/> to return a mocked <see cref="HttpClient"/> if a <see cref="MockHttpMessageHandler"/> is registered.
/// </summary>
/// <param name="httpClientFactory">The decorated <see cref="IHttpClientFactory"/>.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> that is used to look up the <see cref="MockHttpMessageHandler"/>.</param>
public class HttpClientFactoryDecorator(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider) : IHttpClientFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public HttpClient CreateClient(string name)
    {
        var mockHttpMessageHandler = serviceProvider.GetKeyedService<MockHttpMessageHandler>(name);

        if (mockHttpMessageHandler is not null)
        {
            var actualHttpClient = httpClientFactory.CreateClient(name);
            mockHttpMessageHandler.Fallback.Respond(actualHttpClient);
            var mockedHttpClient = mockHttpMessageHandler.ToHttpClient();
            mockedHttpClient.BaseAddress = actualHttpClient.BaseAddress;
            return mockedHttpClient;
        }
        return httpClientFactory.CreateClient(name);
    }
}