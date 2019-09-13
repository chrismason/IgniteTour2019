using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Logging;

namespace Ignite.Common.KeyVault
{
    public class BaseKeyVaultRepository : IKeyVaultRepository
    {
        private ILogger _logger;
        protected IKeyVaultClient KeyVaultClient { get; }
        public string Name { get; }

        protected BaseKeyVaultRepository(ILogger logger, IKeyVaultClient client, string name)
        {
            _logger = logger;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            KeyVaultClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<string> GetAsync(string name)
        {
            var secret = "";

            var value = await GetSecretBundleAsync(name);
            if (value != null)
            {
                secret = value.Value;
            }
            return secret;
        }

        public async Task<SecretBundle> GetSecretBundleAsync(string name)
        {
            SecretBundle value;
            try
            {
                _logger.LogInformation($"Looking for secret '{name}'");
                value = await KeyVaultClient.GetSecretAsync(Name, name);
                _logger.LogInformation($"Found secret '{name}'");
            }
            catch (KeyVaultErrorException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Secret '{name}' was not found");
                    return null;
                }
                _logger.LogWarning($"Unable to retrieve '{name}'. Message=[{ex.Message}]");
                throw ex;
            }
            return value;
        }
    }
}