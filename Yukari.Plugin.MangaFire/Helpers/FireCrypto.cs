using System.Text;

namespace Yukari.Plugin.MangaFire.Helpers;

internal static class FireCrypto
{
    private static readonly (byte[] Table, byte[] Key, byte Iv)[] Stages = new[]
    {
        (
            Convert.FromBase64String(
                "yINlmUNho8VYJT+ibTIP+9ESiULpVEtMOoD6U6lRE0R/xwXo/Xp9NrUgC4cw/Lmo33vUyjUE40kUoEWIr/fxfNNcq2s79ShQ5NhNrFnJ4hXPwOu/SuXzIbuTQKGFvfm08E9jvCfqAtoDqvQq3dVWPQFmJjgvkISBeXY3BgANR+yVnjGbcxZ47d6kLNfZPIayTq3/YGySb1KuVZodWp/WGNAO5pfMcpaK53Hhs0allBszaMaxuouOwdxbwgxIw6YunSsXjI05Yi0j9j4eHKfSXR8Ifo/Od+8iamRfCXTyvm7NGRGYdcQ0ywcK/u6RXhrbcCm4t2eCtrDgQVecJGkQ+A=="
            ),
            Convert.FromBase64String("0Ec58JOY3uBzJK9m3zqIOpdlF7UFiax9DmA="),
            (byte)0x5A
        ),
        (
            Convert.FromBase64String(
                "IUFltCxD3Oc2cwCgkJffthaOg9cgPUb0LgW6H/VtfcF0kc5F25t+aWj6JH9VOhOaY0rAFdUxlDnl5BLNvwEJvQtP5qcw7vdb/K+chnbwnspSHT8mz5lqwz41TezG0hkO06FTjJZhsyNuFLDpD2ZZxQj/QIRcF90zpmQ7Byu483WsQqUE0C342HL+JXngRB6fRzxRyVTaKu83h7UYTJ0QMt6ixFh6S3F8gqkKwrGTL3jHNBsD45UnifK8+RGtishQV2K3rujLKEkiZxpr2dYcudFW4oFsDKhad3CLBvuyTqsCo4B7mL5IKQ1vXo/MOOvq1I1d8ar9X6Ttu5KF4fZgiA=="
            ),
            Convert.FromBase64String("AAdjb1iPY8CiDmq9H34tKTBF8a3oDQ=="),
            (byte)0x35
        ),
        (
            Convert.FromBase64String(
                "NQHlu1/wVO5EmkwQymF810qqY2xG1k2obcas4Z9mCsPEIFl9pRIjFxbJ7ybMHbBckT5Ton85E0FOeHezbh/mjlEYpmpnlXOS8dgrqeq2KfxImTh1YK9y0PeMNhzA1OQzSY9brYOJq/l2QnE/hwOeZIhPixVSKIUlDb5vLcH6RWKxkIEMuP0bDwIqQ71AJJaEaMJL7A6YtyIwoRT+L5v4aZzodN/0+3nOGsfblFjgxSfPzVDjNFeNl5P26+kEC/8AHgdrpAbt3hHz3HrRN1Y6e+JHgF7ncFWnoF0y3THL1S71WgWGCa6KtSzTCCG58n68nTyj2T3Sshk7utqCtMi/ZQ=="
            ),
            Convert.FromBase64String("DELOJgPsVaCcblDtTGMdHzM="),
            (byte)0xBA
        ),
    };

    public static string ComputeVrf(string url, Dictionary<string, string[]>? queryParams = null)
    {
        var uri = new Uri(url);
        var path = uri.AbsolutePath.Replace("/api", "");

        var paramStrings = new List<string>();
        if (queryParams != null)
        {
            var sortedKeys = queryParams.Keys.OrderBy(k => k, StringComparer.Ordinal);
            foreach (var key in sortedKeys)
            {
                var values = queryParams[key];
                if (values.Length == 0)
                    continue;

                if (key.EndsWith("[]"))
                {
                    var baseKey = key[..^2];
                    for (int i = 0; i < values.Length; i++)
                        paramStrings.Add($"{baseKey}[{i}]={values[i]}");
                }
                else
                {
                    foreach (var value in values)
                        paramStrings.Add($"{key}={value}");
                }
            }
        }

        var data = path;
        if (paramStrings.Count > 0)
            data += "?" + string.Join("&", paramStrings);

        var bytes = Encoding.UTF8.GetBytes(data);

        foreach (var (table, key, iv) in Stages)
            bytes = EncryptStage(bytes, table, key, iv);

        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static byte[] EncryptStage(byte[] data, byte[] table, byte[] key, byte iv)
    {
        var outBytes = new byte[data.Length];
        int prev = iv;
        int keyLen = key.Length;

        for (int i = 0; i < data.Length; i++)
        {
            int value = data[i];
            int xor = value ^ key[i % keyLen] ^ prev;
            prev = table[xor & 0xFF] & 0xFF;
            outBytes[i] = (byte)prev;
        }

        return outBytes;
    }
}
