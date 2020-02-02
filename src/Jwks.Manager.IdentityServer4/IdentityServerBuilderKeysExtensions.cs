using IdentityServer4.Stores;
using Jwks.Manager;
using Jwks.Manager.IdentityServer4;
using Jwks.Manager.Interfaces;
using Jwks.Manager.Jwk;
using System;
using Jwks.Manager.Jwks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder extension methods for registering crypto services
    /// </summary>
    public static class IdentityServerBuilderKeysExtensions
    {
        /// <summary>
        /// Sets the signing credential.
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddAutoSigningCredential(this IServiceCollection services, Action<JwksOptions> action = null)
        {
            if (action != null)
                services.Configure(action);

            services.AddScoped<IJsonWebKeyService, JwkService>();
            services.AddScoped<IJsonWebKeySetService, JwksService>();
            services.AddScoped<ISigningCredentialStore, IdentityServer4KeyStore>();
            services.AddScoped<IValidationKeysStore, IdentityServer4KeyStore>();

            return services;
        }
    }
}