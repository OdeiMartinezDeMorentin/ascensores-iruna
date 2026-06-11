using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace AscensoresIruna.Api.Services;

public class IpHashService
{
    private readonly byte[] _secretKey;

    public IpHashService(IConfiguration configuration)
    {
        var secret = configuration["HmacSecret"]
            ?? throw new InvalidOperationException("HmacSecret is not configured.");
        _secretKey = Encoding.UTF8.GetBytes(secret);
    }

    public string HashIp(string ipAddress)
    {
        var ipBytes = Encoding.UTF8.GetBytes(ipAddress);
        var hash = HMACSHA256.HashData(_secretKey, ipBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}