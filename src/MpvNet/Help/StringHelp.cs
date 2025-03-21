using System.Security.Cryptography;
using System.Text;

namespace MpvNet.Help;

public static class StringHelp
{
    public static string GetMd5Hash(string txt)
    {
        using var md5         = MD5.Create();
        var       inputBuffer = Encoding.UTF8.GetBytes(txt);
        var       hashBuffer  = md5.ComputeHash(inputBuffer);
        return BitConverter.ToString(md5.ComputeHash(inputBuffer)).Replace("-", "");
    }
}
