using System.Collections.Generic;
using Jwks.Manager.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Jwks.Manager.Jwks
{
    /// <summary>
    /// Util class to allow restoring RSA/ECDsa parameters from JSON as the normal
    /// parameters class won't restore private key info.
    /// </summary>
    public class JwksService : IJsonWebKeySetService
    {
        private readonly IJsonWebKeyStore _store;
        private readonly IJsonWebKeyService _jwkService;
        private readonly IOptions<JwksOptions> _options;

        public JwksService(IJsonWebKeyStore store, IJsonWebKeyService jwkService, IOptions<JwksOptions> options)
        {
            _store = store;
            _jwkService = jwkService;
            _options = options;
        }

        public SigningCredentials Generate(JwksOptions options = null)
        {
            if (options == null)
                options = _options.Value;
            var key = _jwkService.Generate(options.Algorithm);
            var t = new SecurityKeyWithPrivate();
            t.SetParameters(key, options.Algorithm);
            _store.Save(t);
            return new SigningCredentials(key, options.Algorithm);
        }

        /// <summary>
        /// If current doesn't exist will generate new one
        /// </summary>
        public SigningCredentials GetCurrent(JwksOptions options = null)
        {
            if (_store.NeedsUpdate())
                return Generate(options);

            var securityFile = _store.GetCurrentKey();
            return new SigningCredentials(securityFile.GetSecurityKey(), securityFile.Algorithm);
        }

        public IReadOnlyCollection<SecurityKeyWithPrivate> GetLastKeysCredentials(int qty)
        {
            return _store.Get(qty);
        }



    }
}