using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Ignite.Common.KeyVault
{
    public class KeyVaultMSIRepository : BaseKeyVaultRepository
    {
        public KeyVaultMSIRepository(ILogger logger, string vault, string msiClientId)
        : base(logger, new KeyVaultClient((authority, resource, scope) => GetTokenByMSI(logger, resource, msiClientId)), vault)
        {
        }

        // https://stackoverflow.com/a/54241207
        public static async Task<string> GetTokenByMSI(ILogger logger, string resource, string clientId = null)
        {
            logger.LogInformation("Attempting to get token from MSI");
            var endpoint = Environment.GetEnvironmentVariable("MSI_ENDPOINT", EnvironmentVariableTarget.Process);
            var secret = Environment.GetEnvironmentVariable("MSI_SECRET", EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(endpoint))
            {
                logger.LogWarning("MSI_ENDPOINT variable not set");
                throw new InvalidOperationException("MSI_ENDPOINT environment variable not set");
            }
            if (string.IsNullOrEmpty(secret))
            {
                logger.LogWarning("MSI_SECRET variable not set");
                throw new InvalidOperationException("MSI_SECRET environment variable not set");
            }

            Uri uri;
            if (clientId == null)
            {
                logger.LogInformation("Obtain token using system assigned identity");
                uri = new Uri($"{endpoint}?resource={resource}&api-version=2017-09-01");
            }
            else
            {
                logger.LogInformation("Obtain token using user assigned identity");
                uri = new Uri($"{endpoint}?resource={resource}&api-version=2017-09-01&clientid={clientId}");
            }

            // get token from MSI
            var tokenRequest = new HttpRequestMessage()
            {
                RequestUri = uri,
                Method = HttpMethod.Get
            };
            tokenRequest.Headers.Add("secret", secret);
            var httpClient = new HttpClient();

            var response = await httpClient.SendAsync(tokenRequest);

            string token = string.Empty;

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Obtained MSI access token");
                var body = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(body);

                token = result["access_token"].ToString();
            }
            else
            {
                logger.LogWarning("Failed to obtain MSI access token");
            }
            return token;

        }
    }
}