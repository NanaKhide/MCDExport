using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace McdfExporter.Services;

public static class FileHasher
{
    private static readonly Dictionary<string, string> _hashCache = new();

    public static string GetFileHash(string filePath)
    {
        if (_hashCache.TryGetValue(filePath, out var hash))
        {
            return hash;
        }

        using var sha1 = SHA1.Create();
        using var stream = File.OpenRead(filePath);
        var newHash = BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        _hashCache[filePath] = newHash;
        return newHash;
    }
}
