using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Ipc;
using System;
using System.Text;

namespace McdfExporter.Services.Ipc;

public class IpcCallerCustomizePlus : IDisposable
{
    private readonly ICallGateSubscriber<(int, int)> _getApiVersion;
    private readonly ICallGateSubscriber<ushort, (int, Guid?)> _getActiveProfile;
    private readonly ICallGateSubscriber<Guid, (int, string?)> _getProfileById;
    public bool ApiAvailable { get; private set; }

    public IpcCallerCustomizePlus()
    {
        _getApiVersion = Plugin.PluginInterface.GetIpcSubscriber<(int, int)>("CustomizePlus.General.GetApiVersion");
        _getActiveProfile = Plugin.PluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        _getProfileById = Plugin.PluginInterface.GetIpcSubscriber<Guid, (int, string?)>("CustomizePlus.Profile.GetByUniqueId");
        CheckApi();
    }

    public void CheckApi()
    {
        try
        {
            var version = _getApiVersion.InvokeFunc();
            ApiAvailable = version.Item1 >= 6;
        }
        catch
        {
            ApiAvailable = false;
        }
    }

    public string? GetBodyScale(ICharacter character)
    {
        if (!ApiAvailable) return null;
        try
        {
            var activeProfile = _getActiveProfile.InvokeFunc((ushort)character.ObjectIndex);
            if (activeProfile.Item1 != 0 || activeProfile.Item2 == null) return null;

            var profileData = _getProfileById.InvokeFunc(activeProfile.Item2.Value);
            if (profileData.Item1 != 0 || profileData.Item2 == null) return null;

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(profileData.Item2));
        }
        catch (Exception e)
        {
            Plugin.Log.Warning(e, "Error calling CustomizePlus IPC");
            return null;
        }
    }

    public void Dispose() { }
}
