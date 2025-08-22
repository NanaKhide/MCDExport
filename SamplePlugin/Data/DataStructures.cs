using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McdfExporter.Data;

public class CharacterData
{
    public string GlamourerData { get; set; } = string.Empty;
    public List<FileReplacement> FileReplacements { get; set; } = new();
    public string ManipulationData { get; set; } = string.Empty;
    public string CustomizePlusData { get; set; } = string.Empty;
}

public class FileReplacement
{
    public string Hash { get; set; } = string.Empty;
    public List<string> GamePaths { get; set; } = new();
    public int Length { get; set; }
    public string LocalPath { get; set; } = string.Empty;
}

public record MareCharaFileData
{
    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("GlamourerData")]
    public string GlamourerData { get; set; } = string.Empty;
    [JsonPropertyName("CustomizePlusData")]
    public string CustomizePlusData { get; set; } = string.Empty;
    [JsonPropertyName("ManipulationData")]
    public string ManipulationData { get; set; } = string.Empty;
    [JsonPropertyName("Files")]
    public List<MareCharaFileHeader.FileData> Files { get; set; } = new();
    [JsonPropertyName("FileSwaps")]
    public List<object> FileSwaps { get; set; } = new();

    public byte[] ToByteArray()
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
    }
}

public record MareCharaFileHeader(byte Version, MareCharaFileData CharaFileData)
{
    public static readonly byte CurrentVersion = 1;

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write('M');
        writer.Write('C');
        writer.Write('D');
        writer.Write('F');
        writer.Write(Version);
        var charaFileDataArray = CharaFileData.ToByteArray();
        writer.Write(charaFileDataArray.Length);
        writer.Write(charaFileDataArray);
    }

    public record FileData
    {
        [JsonPropertyName("GamePaths")]
        public string[] GamePaths { get; set; } = Array.Empty<string>();
        [JsonPropertyName("Hash")]
        public string Hash { get; set; } = string.Empty;
        [JsonPropertyName("Length")]
        public int Length { get; set; }
    }
}
