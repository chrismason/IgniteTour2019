using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ignite.Common.KeyVault
{
    public static class KeyVaultRepositoryFactory
    {
        public static CloudEnvironment Environment { get; set; }

        static KeyVaultRepositoryFactory()
        {
            Environment = CloudEnvironment.AzureCommercial;
        }

        public static string VaultUrl
        {
            get
            {
                string url = "";
                switch (Environment)
                {
                    case CloudEnvironment.AzureCommercial:
                        url = "https://{0}.vault.azure.net";
                        break;
                    case CloudEnvironment.AzureGovernment:
                        url = "https://{0}.vault.usgovcloudapi.net";
                        break;
                }
                return url;
            }
        }

        public static IKeyVaultRepository GetRepository(ILoggerFactory loggerFactory, IConfiguration configuration, string vaultName = null)
        {
            var logger = loggerFactory.CreateLogger("Ignite.Common.KeyVault");
            var vault = vaultName ?? configuration.GetConfiguration("KeyVault", "Name");
            var appId = configuration.GetConfiguration("KeyVault", "ADApplicationId");
            var thumbprint = configuration.GetConfiguration("KeyVault", "Thumbprint");

            if (!string.IsNullOrEmpty(thumbprint))
            {
                logger.LogInformation("Creating certificate based Key Vault repository");
                return new KeyVaultCertificateRepository(logger, string.Format(VaultUrl, vault), appId, thumbprint);
            }
            else
            {
                logger.LogInformation("Creating MSI based Key Vault repository");
                return new KeyVaultMSIRepository(logger, string.Format(VaultUrl, vault), appId);
            }
        }
    }
}