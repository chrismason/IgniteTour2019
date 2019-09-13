using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Ignite.Common.KeyVault
{
    public class KeyVaultCertificateRepository : BaseKeyVaultRepository
    {
        public KeyVaultCertificateRepository(ILogger logger, string vault, string adAppId, string thumbprint)
        : base(logger, new KeyVaultClient((authority, resource, scope) => GetTokenByThumbprint(logger, adAppId, thumbprint, authority, resource, CancellationToken.None)), vault)
        {
        }

        private static async Task<string> GetTokenByThumbprint(ILogger logger, string adAppId, string thumbprint, string authority, string resource, CancellationToken cancellationToken)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var certificate = CertificateExtensions.FindByThumbprint(thumbprint);
            var clientAssertion = new ClientAssertionCertificate(adAppId, certificate);

            var result = await context.AcquireTokenAsync(resource, clientAssertion).ConfigureAwait(false);

            if (result?.AccessToken == null)
            {
                logger.LogError("Failed to obtain access token using certificate");
                throw new InvalidOperationException($"Unable to acquire token for resource {resource}. Authority: {authority}. ApplicationId: {adAppId}. Thumbprint: {thumbprint}");
            }

            logger.LogInformation("Obtained access token using certificate");
            return result.AccessToken;
        }
    }
}