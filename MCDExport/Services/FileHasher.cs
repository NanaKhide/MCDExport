using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace McdfExporter.Services;

public static class FileHasher
{
    public static string GetFileHash(string filePath, Dictionary<string, string> cache)
    {
        if (cache.TryGetValue(filePath, out var hash))
        {
            return hash;
        }

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var newHash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        cache[filePath] = newHash;
        return newHash;
    }
}
