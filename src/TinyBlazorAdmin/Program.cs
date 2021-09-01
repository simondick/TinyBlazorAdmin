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

            builder.Services.AddOidcAuthentication(options =>
            {
                // Replace the Okta placeholders with your Okta values in the appsettings.json file.
                options.ProviderOptions.Authority = builder.Configuration.GetValue<string>("Okta:Authority");
                options.ProviderOptions.ClientId = builder.Configuration.GetValue<string>("Okta:ClientId"); ;
                options.ProviderOptions.ResponseType = "code";
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
