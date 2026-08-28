using System.Security.Cryptography;
using System.Text;
using Server.Envir;

namespace Server.Web.Services;

public sealed class AdminCredentialService
{
    public string UserName => Config.AdminUserName;

    public bool Validate(string? userName, string? password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
            return false;

        return FixedTimeEquals(userName, Config.AdminUserName) &&
               FixedTimeEquals(password, Config.MasterPassword);
    }

    public string GetSecurityStamp()
    {
        byte[] value = SHA256.HashData(Encoding.UTF8.GetBytes($"{Config.AdminUserName}\0{Config.MasterPassword}"));
        return Convert.ToHexString(value);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        byte[] rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right));
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }
}
