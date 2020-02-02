using System;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Jwks.Manager.Jwk
{
    public static class CryptoService
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        /// <summary>
        /// Creates a new RSA security key.
        /// Key size recommendations: https://www.keylength.com/en/compare/
        /// </summary>
        /// <returns></returns>
        public static RsaSecurityKey CreateRsaSecurityKey(int keySize = 2048)
        {
            return new RsaSecurityKey(RSA.Create(keySize))
            {
                KeyId = CreateUniqueId()
            };
        }

        internal static string CreateUniqueId(int length = 16)
        {
            return Base64UrlEncoder.Encode(CreateRandomKey(length));
        }

        /// <summary>
        /// Creates a new ECDSA security key.
        /// </summary>
        /// <param name="curve">The name of the curve as defined in
        /// https://tools.ietf.org/html/rfc7518#section-6.2.1.1.</param>
        /// <returns></returns>
        internal static ECDsaSecurityKey CreateECDsaSecurityKey(string curve = JsonWebKeyECTypes.P256)
        {
            return new ECDsaSecurityKey(ECDsa.Create(GetCurveFromCrvValue(curve)))
            {
                KeyId = CreateUniqueId()
            };
        }

        /// <summary>
        /// Creates a new HMAC security key.
        /// 
        /// Key size is selected based on NIST Special Publication 800-107 Revision 1 
        /// Recommendation for Applications Using Approved Hash Algorithms
        /// Section 5.3.4 Security Effect of the HMAC Key
        /// </summary>
        internal static HMAC CreateHmacSecurityKey(Algorithm algorithm)
        {
            var hmac = algorithm.Selected switch
            {
                SecurityAlgorithms.HmacSha256 => (HMAC)new HMACSHA256(CreateRandomKey(64)),
                SecurityAlgorithms.HmacSha384 => new HMACSHA384(CreateRandomKey(128)),
                SecurityAlgorithms.HmacSha512 => new HMACSHA512(CreateRandomKey(128)),
                _ => throw new CryptographicException("Could not create HMAC key based on algorithm " + algorithm +
                                                      " (Could not parse expected SHA version)")
            };

            return hmac;
        }


        /// <summary>
        /// Creates a AES security key.
        /// </summary>
        internal static Aes CreateAESSecurityKey(Algorithm algorithm)
        {
            var aesKey = Aes.Create();
            var aesKeySize = algorithm.Selected switch
            {
                SecurityAlgorithms.Aes128KW => 128,
                SecurityAlgorithms.Aes256KW => 256,
                _ => throw new CryptographicException("Could not create AES key based on algorithm " + algorithm)
            };
            aesKey.KeySize = aesKeySize;
            aesKey.GenerateKey();
            return aesKey;
        }

        /// <summary>
        /// Returns the matching named curve for RFC 7518 crv value
        /// </summary>
        internal static ECCurve GetCurveFromCrvValue(string crv)
        {
            return crv switch
            {
                JsonWebKeyECTypes.P256 => ECCurve.NamedCurves.nistP256,
                JsonWebKeyECTypes.P384 => ECCurve.NamedCurves.nistP384,
                JsonWebKeyECTypes.P521 => ECCurve.NamedCurves.nistP521,
                _ => throw new InvalidOperationException($"Unsupported curve type of {crv}"),
            };
        }

        /// <summary>Creates a random key byte array.</summary>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        internal static byte[] CreateRandomKey(int length)
        {
            byte[] data = new byte[length];
            Rng.GetBytes(data);
            return data;
        }
    }
}