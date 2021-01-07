using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyBlazorAdmin.Data;

namespace TinyBlazorAdmin
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            // set up a delegate to get function token
            static string functionEndpoint(WebAssemblyHostBuilder builder) =>
                builder.Configuration
                    .GetSection(nameof(UrlShortenerSecuredService))
                    .GetValue<string>(nameof(AzFuncAuthorizationMessageHandler.Endpoint));

            // sets up AAD + user_impersonation to access functions.
            builder.Services.AddMsalAuthentication(options =>
            {
                options.ProviderOptions
                .DefaultAccessTokenScopes.Add("user.read");
                options.ProviderOptions
                .AdditionalScopesToConsent.Add($"{functionEndpoint(builder)}user_impersonation");
                builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
            });


            // set up DI
            builder.Services.AddTransient<AzFuncAuthorizationMessageHandler>();
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            // configure the client to talk to the Azure Functions endpoint.
            builder.Services.AddHttpClient(nameof(UrlShortenerSecuredService),
                client =>
                {
                    client.BaseAddress = new Uri(functionEndpoint(builder));
                }).AddHttpMessageHandler<AzFuncAuthorizationMessageHandler>();

            builder.Services.AddTransient<UrlShortenerSecuredService>();
            builder.Services.AddTransient<AzFuncClient>();

            await builder.Build().RunAsync();
        }
    }
}
