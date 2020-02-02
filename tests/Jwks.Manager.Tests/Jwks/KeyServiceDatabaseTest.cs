using Bogus;
using FluentAssertions;
using Jwks.Manager.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Jwks.Manager.Jwk;
using Xunit;
using Xunit.Abstractions;

namespace Jwks.Manager.Tests.Jwks
{
    public class KeyServiceDatabaseTest : IClassFixture<WarmupDatabaseInMemory>
    {
        private readonly AspNetGeneralContext _database;
        private readonly IJsonWebKeySetService _keyService;
        private ITestOutputHelper _output;
        public WarmupDatabaseInMemory DatabaseInMemoryData { get; }
        public KeyServiceDatabaseTest(WarmupDatabaseInMemory databaseInMemory, ITestOutputHelper output)
        {
            _output = output;
            DatabaseInMemoryData = databaseInMemory;
            _keyService = DatabaseInMemoryData.Services.GetRequiredService<IJsonWebKeySetService>();
            _database = DatabaseInMemoryData.Services.GetRequiredService<AspNetGeneralContext>();

        }

        [Fact]
        public void ShouldSaveCryptoInDatabase()
        {
            _keyService.GetCurrent();

            _database.SecurityKeys.Count().Should().BePositive();
        }


        [Theory]
        [InlineData(5)]
        [InlineData(2)]
        [InlineData(6)]
        public void ShouldGenerateManyRsa(int quantity)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            var keysGenerated = new List<SigningCredentials>();
            for (int i = 0; i < quantity; i++)
            {
                var sign = _keyService.Generate();
                keysGenerated.Add(sign);
            }

            var current = _keyService.GetLastKeysCredentials(quantity * 4);
            foreach (var securityKey in current)
            {
                keysGenerated.Select(s => s.Key.KeyId).Should().Contain(securityKey.KeyId);
            }
        }

        [Fact]
        public void ShouldSaveCryptoAndRecover()
        {
            var newKey = _keyService.GetCurrent();

            _database.SecurityKeys.Count().Should().BePositive();

            var currentKey = _keyService.GetCurrent();
            newKey.Kid.Should().Be(currentKey.Kid);
        }


        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha384, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha512, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.RsaSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha512, KeyType.RSA)]
        public void ShouldSaveJWKRecoverAndSigning(string algorithm, KeyType keyType)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            _database.SaveChanges();

            var options = new JwksOptions() { Algorithm = Algorithm.Create(algorithm, keyType) };

            var handler = new JsonWebTokenHandler();
            var now = DateTime.Now;

            // Generate right now and in memory
            var newKey = _keyService.GetCurrent(options);

            // recovered from database
            var currentKey = _keyService.GetCurrent();

            newKey.Kid.Should().Be(currentKey.Kid);
            var claims = new ClaimsIdentity(GenerateClaim().Generate(5));
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "me",
                Audience = "you",
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(5),
                Subject = claims,
                SigningCredentials = newKey
            };
            var descriptorFromDb = new SecurityTokenDescriptor
            {
                Issuer = "me",
                Audience = "you",
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(5),
                Subject = claims,
                SigningCredentials = currentKey
            };

            var jwt1 = handler.CreateToken(descriptor);
            var jwt2 = handler.CreateToken(descriptorFromDb);

            jwt1.Should().Be(jwt2);
        }

        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha384, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha512, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.RsaSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha512, KeyType.RSA)]
        public void ShouldGenerateCurrentBasedInOptions(string algorithm, KeyType keyType)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            _database.SaveChanges();
            var options = new JwksOptions() { Algorithm = Algorithm.Create(algorithm, keyType) };
            var newKey = _keyService.GetCurrent(options);
            newKey.Algorithm.Should().Be(algorithm);

        }


        public Faker<Claim> GenerateClaim()
        {
            return new Faker<Claim>().CustomInstantiator(f => new Claim(f.Internet.DomainName(), f.Lorem.Text()));
        }
    }
}
