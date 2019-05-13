using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Hosting;
using AzureFunctionsAndKeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(CustomWebJobsStartup))]
namespace AzureFunctionsAndKeyVault
{
    public class ShowConfigVaules
    {
        private readonly ConfigValues configValues;
        public ShowConfigVaules(ConfigValues configValues)
        {
            this.configValues = configValues;
        }

        [FunctionName("ShowConfigVaules")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(configValues);
        }
    }

    public class CustomWebJobsStartup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(
                    azureServiceTokenProvider.KeyVaultTokenCallback));

            // See for more info, https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-2.2#use-managed-identities-for-azure-resources
            var buildconfig = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddEnvironmentVariables()
                .Build();

            var azureKeyVaultName = buildconfig.GetConnectionStringOrSetting("AzureKeyVaultName");

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddAzureKeyVault($"https://{azureKeyVaultName}.vault.azure.net/",
                        keyVaultClient,
                        new DefaultKeyVaultSecretManager())
                .Build();

            // Add configuration values to DI
            builder.Services
                .AddScoped(s => new ConfigValues { Children = config.GetChildren() })
                .BuildServiceProvider(true);
        }
    }

    public class ConfigValues
    {
        public int Count
        {
            get
            {
                return Children != null ? Children.Count() : 0;
            }
        }
        public IEnumerable<IConfigurationSection> Children { get; set; }
    }
}
