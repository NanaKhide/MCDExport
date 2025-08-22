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
    private readonly ICallGateSubscriber<string> _getModDirectory;
    public bool ApiAvailable { get; private set; }

    public IpcCallerPenumbra()
    {
        _getResourcePaths = new GetGameObjectResourcePaths(Plugin.PluginInterface);
        _getMetaManipulations = new GetPlayerMetaManipulations(Plugin.PluginInterface);
        _apiVersion = new ApiVersion(Plugin.PluginInterface);
        _getModDirectory = Plugin.PluginInterface.GetIpcSubscriber<string>("Penumbra.GetModDirectory");
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
            return _getModDirectory.InvokeFunc() ?? string.Empty;
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

    public IReadOnlyDictionary<string, string> GetGameObjectResourcePaths(int objectIndex)
    {
        if (!ApiAvailable) return new Dictionary<string, string>();
        try
        {
            var resourcePathsArray = _getResourcePaths.Invoke((ushort)objectIndex);
            if (resourcePathsArray == null || resourcePathsArray.Length == 0)
            {
                return new Dictionary<string, string>();
            }

            var combinedPaths = new Dictionary<string, string>();
            foreach (var resourcePaths in resourcePathsArray)
            {
                foreach (var (gamePath, localPath) in resourcePaths)
                {
                    if (localPath.Any())
                    {
                        combinedPaths[gamePath] = localPath.First();
                    }
                }
            }
            return combinedPaths;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Error calling Penumbra.Api.GetGameObjectResourcePaths");
            return new Dictionary<string, string>();
        }
    }

    public void Dispose() { }
}
