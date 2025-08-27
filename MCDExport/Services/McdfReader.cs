using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using K4os.Compression.LZ4.Legacy;
using McdfExporter.Data;

namespace McdfExporter.Services
{
    public sealed class McdfReader : IDisposable
    {
        private readonly FileStream _stream;
        private readonly LZ4Stream _lz4;
        private readonly MareCharaFileData _data;

        private McdfReader(FileStream stream, LZ4Stream lz4, MareCharaFileData data)
        {
            _stream = stream;
            _lz4 = lz4;
            _data = data;
        }

        public static McdfReader? FromPath(string path)
        {
            try
            {
                var stream = File.OpenRead(path);
                var lz4 = new LZ4Stream(stream, LZ4StreamMode.Decompress);
                var br = new BinaryReader(lz4);

                if (new string(br.ReadChars(4)) != "MCDF") return null;

                br.ReadByte();
                var len = br.ReadInt32();
                var bytes = br.ReadBytes(len);
                var data = JsonSerializer.Deserialize<MareCharaFileData>(Encoding.UTF8.GetString(bytes));

                return new McdfReader(stream, lz4, data!);
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e, "Failed to read MCDF file header.");
                return null;
            }
        }

        public MareCharaFileData GetData() => _data;

        public Dictionary<string, string> ExtractFiles(string tempDir)
        {
            using var br = new BinaryReader(_lz4);
            var files = new Dictionary<string, string>();

            foreach (var fileData in _data.Files)
            {
                var filePath = Path.Combine(tempDir, $"mcdf_{fileData.Hash}.tmp");
                var bytes = br.ReadBytes(fileData.Length);
                File.WriteAllBytes(filePath, bytes);

                foreach (var gamePath in fileData.GamePaths)
                {
                    files[gamePath] = filePath;
                }
            }
            return files;
        }

        public void Dispose()
        {
            _lz4.Dispose();
            _stream.Dispose();
        }
    }
}
