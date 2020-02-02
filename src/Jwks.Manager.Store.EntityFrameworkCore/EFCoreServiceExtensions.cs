using Jwks.Manager.Interfaces;
using Jwks.Manager.Store;
using Jwks.Manager.Store.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Builder extension methods for registering crypto services
    /// </summary>
    public static class EFCoreServiceExtensions
    {
        /// <summary>
        /// Sets the signing credential.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="credential">The credential.</param>
        /// <returns></returns>
        public static IServiceCollection UseDatabaseStore<TContext>(this IServiceCollection services) where TContext : DbContext, ISecurityKeyContext
        {
            services.AddScoped<IJsonWebKeyStore, DatabaseJsonWebKeyStore<TContext>>();

            return services;
        }
    }
}