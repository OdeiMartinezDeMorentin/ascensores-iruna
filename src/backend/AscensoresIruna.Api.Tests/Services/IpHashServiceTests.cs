using AscensoresIruna.Api.Services;
using Microsoft.Extensions.Configuration;

namespace AscensoresIruna.Api.Tests.Services;

public class IpHashServiceTests
{
    private static IpHashService CreateService(string secret = "test-secret-key-for-hmac-256")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HmacSecret"] = secret
            })
            .Build();
        return new IpHashService(config);
    }

    [Fact]
    public void SameIp_SameHash()
    {
        var service = CreateService();
        var hash1 = service.HashIp("192.168.1.1");
        var hash2 = service.HashIp("192.168.1.1");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void DifferentIp_DifferentHash()
    {
        var service = CreateService();
        var hash1 = service.HashIp("192.168.1.1");
        var hash2 = service.HashIp("192.168.1.2");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashFormat_IsLowercaseHex()
    {
        var service = CreateService();
        var hash = service.HashIp("10.0.0.1");

        Assert.Matches("^[0-9a-f]+$", hash);
    }

    [Fact]
    public void HashLength_Is64Characters()
    {
        var service = CreateService();
        var hash = service.HashIp("10.0.0.1");

        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void DifferentSecret_DifferentHash()
    {
        var service1 = CreateService("secret-one");
        var service2 = CreateService("secret-two");

        var hash1 = service1.HashIp("192.168.1.1");
        var hash2 = service2.HashIp("192.168.1.1");

        Assert.NotEqual(hash1, hash2);
    }
}
