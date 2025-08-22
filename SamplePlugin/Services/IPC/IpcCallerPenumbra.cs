using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Ipc;

namespace McdfExporter.Services.Ipc;

public class IpcCallerPenumbra : IDisposable
{
    private readonly GetGameObjectResourcePaths _getResourcePaths;
    private readonly GetPlayerMetaManipulations _getMetaManipulations;
    private readonly ApiVersion _apiVersion;
    private readonly GetModDirectory _getModDirectory;
    public bool ApiAvailable { get; private set; }

    public IpcCallerPenumbra()
    {
        _getResourcePaths = new GetGameObjectResourcePaths(Plugin.PluginInterface);
        _getMetaManipulations = new GetPlayerMetaManipulations(Plugin.PluginInterface);
        _apiVersion = new ApiVersion(Plugin.PluginInterface);
        _getModDirectory = new GetModDirectory(Plugin.PluginInterface);
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

    public string GetModDirectory()
    {
        if (!ApiAvailable) return string.Empty;
        try
        {
            return _getModDirectory.Invoke() ?? string.Empty;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Error calling Penumbra.GetModDirectory");
            return string.Empty;
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

    public IReadOnlyDictionary<string, IReadOnlyList<string>>[] GetGameObjectResourcePaths(int objectIndex)
    {
        if (!ApiAvailable) return Array.Empty<IReadOnlyDictionary<string, IReadOnlyList<string>>>();
        try
        {
            var resourcePathsArray = _getResourcePaths.Invoke((ushort)objectIndex);
            if (resourcePathsArray == null)
                return Array.Empty<IReadOnlyDictionary<string, IReadOnlyList<string>>>();

            var result = new List<IReadOnlyDictionary<string, IReadOnlyList<string>>>();
            foreach (var dict in resourcePathsArray)
            {
                var newDict = dict.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value.ToList());
                result.Add(newDict);
            }
            return result.ToArray();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Error calling Penumbra.Api.GetGameObjectResourcePaths");
            return Array.Empty<IReadOnlyDictionary<string, IReadOnlyList<string>>>();
        }
    }

    public void Dispose() { }
}
