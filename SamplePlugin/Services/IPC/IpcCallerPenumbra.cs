using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace McdfExporter.Services.Ipc;

public class IpcCallerPenumbra : IDisposable
{
    private readonly GetGameObjectResourcePaths _getResourcePaths;
    private readonly GetPlayerMetaManipulations _getMetaManipulations;
    private readonly ResolvePlayerPath _resolvePlayerPath;
    private readonly ResolvePlayerPathsAsync _resolvePlayerPathsAsync;
    private readonly ApiVersion _apiVersion;
    public bool ApiAvailable { get; private set; }

    public IpcCallerPenumbra()
    {
        _getResourcePaths = new GetGameObjectResourcePaths(Plugin.PluginInterface);
        _getMetaManipulations = new GetPlayerMetaManipulations(Plugin.PluginInterface);
        _resolvePlayerPath = new ResolvePlayerPath(Plugin.PluginInterface);
        _resolvePlayerPathsAsync = new ResolvePlayerPathsAsync(Plugin.PluginInterface);
        _apiVersion = new ApiVersion(Plugin.PluginInterface);
        CheckApi();
    }

    public void CheckApi()
    {
        try
        {
            var version = _apiVersion.Invoke();
            ApiAvailable = version.Breaking >= 1;
        }
        catch
        {
            ApiAvailable = false;
        }
    }

    public string? ResolvePlayerPath(string gamePath)
    {
        if (!ApiAvailable) return null;
        try
        {
            return _resolvePlayerPath.Invoke(gamePath);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, $"Error calling Penumbra.ResolvePlayerPath for {gamePath}");
            return null;
        }
    }

    public async Task<(string[] forward, string[][] reverse)> ResolvePlayerPathsAsync(string[] forward, string[] reverse)
    {
        if (!ApiAvailable) return (Array.Empty<string>(), Array.Empty<string[]>());
        try
        {
            return await _resolvePlayerPathsAsync.Invoke(forward, reverse);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Error calling Penumbra.ResolvePlayerPathsAsync");
            return (Array.Empty<string>(), Array.Empty<string[]>());
        }
    }

    public string GetPlayerMetaManipulations()
    {
        if (!ApiAvailable) return string.Empty;
        try
        {
            return _getMetaManipulations.Invoke() ?? string.Empty;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Error calling Penumbra.GetPlayerMetaManipulations");
            return string.Empty;
        }
    }

    public Dictionary<string, HashSet<string>>? GetCharacterData(int objectIndex)
    {
        if (!ApiAvailable) return null;
        try
        {
            return _getResourcePaths.Invoke((ushort)objectIndex)?[0];
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Error calling Penumbra.Api.GetGameObjectResourcePaths");
            return new Dictionary<string, HashSet<string>>();
        }
    }

    public void Dispose() { }
}
