using Glamourer.Api.IpcSubscribers;
using System;

namespace McdfExporter.Services.Ipc;

public class IpcCallerGlamourer : IDisposable
{
    private readonly GetStateBase64 _getStateBase64;
    private readonly ApiVersion _apiVersion;
    public bool ApiAvailable { get; private set; }

    public IpcCallerGlamourer()
    {
        _getStateBase64 = new GetStateBase64(Plugin.PluginInterface);
        _apiVersion = new ApiVersion(Plugin.PluginInterface);
        CheckApi();
    }

    public void CheckApi()
    {
        try
        {
            var version = _apiVersion.Invoke();
            ApiAvailable = version.Major >= 1;
        }
        catch
        {
            ApiAvailable = false;
        }
    }

    public string? GetStateBase64(int objectIndex)
    {
        if (!ApiAvailable) return null;
        try
        {
            
            return _getStateBase64.Invoke(objectIndex).Item2;
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Error calling Glamourer.GetStateBase64");
            return null;
        }
    }

    public void Dispose() { }
}
