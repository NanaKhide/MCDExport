using K4os.Compression.LZ4.Legacy;
using McdfExporter.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace McdfExporter.Services;

public class CharaDataFileHandler
{
    private readonly CharacterDataFactory _characterDataFactory;

    public CharaDataFileHandler(CharacterDataFactory characterDataFactory)
    {
        _characterDataFactory = characterDataFactory;
    }

    public async Task SaveCharaFileAsync(string description, string filePath)
    {
        var tempFilePath = filePath + ".tmp";

        try
        {
            var data = await _characterDataFactory.CreateCharacterData();
            if (data == null)
            {
                throw new Exception("Could not create character data.");
            }

            var groupedFileReplacements = data.FileReplacements
                .GroupBy(f => f.Hash)
                .Select(g => new FileReplacement
                {
                    Hash = g.Key,
                    GamePaths = g.SelectMany(fr => fr.GamePaths).Distinct().ToList(),
                    Length = g.First().Length,
                    LocalPath = g.First().LocalPath
                })
                .ToList();

            var mareCharaFileData = new MareCharaFileData
            {
                Description = description,
                GlamourerData = data.GlamourerData,
                CustomizePlusData = data.CustomizePlusData,
                ManipulationData = data.ManipulationData,
                Files = groupedFileReplacements.Select(f => new MareCharaFileHeader.FileData
                {
                    GamePaths = f.GamePaths.ToArray(),
                    Hash = f.Hash,
                    Length = f.Length
                }).ToList()
            };

            var outputHeader = new MareCharaFileHeader(MareCharaFileHeader.CurrentVersion, mareCharaFileData);

            using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var lz4 = new LZ4Stream(fs, LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression))
                {
                    using (var writer = new BinaryWriter(lz4))
                    {
                        outputHeader.WriteToStream(writer);

                        foreach (var item in groupedFileReplacements)
                        {
                            if (string.IsNullOrEmpty(item.LocalPath) || !File.Exists(item.LocalPath))
                            {
                                throw new FileNotFoundException($"Could not find local file for hash {item.Hash}");
                            }

                            var fileBytes = await File.ReadAllBytesAsync(item.LocalPath);
                            writer.Write(fileBytes);
                        }
                    }
                }
            }

            File.Move(tempFilePath, filePath, true);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failure saving MCDF file.");
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            throw;
        }
    }
}
