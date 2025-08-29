using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;

namespace McdfExporter.Services.Ipc
{
    public class IpcCallerCustomizePlus : IDisposable
    {
        private readonly ICallGateSubscriber<(int, int)> _getApiVersion;
        private readonly ICallGateSubscriber<int, string, (int, Guid?)> _setTemporaryProfile;
        private readonly ICallGateSubscriber<int, int> _removeTemporaryProfile;

        private readonly ICallGateSubscriber<ushort, (int, Guid?)> _getActiveProfile;
        private readonly ICallGateSubscriber<Guid, (int, string?)> _getProfileById;

        public bool ApiAvailable { get; private set; }

        public IpcCallerCustomizePlus(IDalamudPluginInterface dalamudPluginInterface)
        {
            _getApiVersion = dalamudPluginInterface.GetIpcSubscriber<(int, int)>("CustomizePlus.General.GetApiVersion");
            _setTemporaryProfile = dalamudPluginInterface.GetIpcSubscriber<int, string, (int, Guid?)>("CustomizePlus.Profile.SetTemporaryProfileOnCharacter");
            _removeTemporaryProfile = dalamudPluginInterface.GetIpcSubscriber<int, int>("CustomizePlus.Profile.DeleteTemporaryProfileOnCharacter");

            _getActiveProfile = dalamudPluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
            _getProfileById = dalamudPluginInterface.GetIpcSubscriber<Guid, (int, string?)>("CustomizePlus.Profile.GetByUniqueId");

            CheckApi();
        }

        public void CheckApi()
        {
            try
            {
                //Nicer to read this way than what i copied
                var (major, minor) = _getApiVersion.InvokeFunc();
                ApiAvailable = major > 5 || (major == 5 && minor >= 1);
            }
            catch
            {
                ApiAvailable = false;
            }
        }

        public string? GetBodyScale(Dalamud.Game.ClientState.Objects.Types.ICharacter character)
        {
            if (!ApiAvailable) return null;
            try
            {
                var activeProfile = _getActiveProfile.InvokeFunc((ushort)character.ObjectIndex);
                if (activeProfile.Item1 != 0 || activeProfile.Item2 == null) return null;

                var profileData = _getProfileById.InvokeFunc(activeProfile.Item2.Value);
                if (profileData.Item1 != 0 || profileData.Item2 == null) return null;

                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(profileData.Item2));
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e, "Error calling CustomizePlus IPC for GetBodyScale");
                return null;
            }
        }

        public void SetTemporaryProfile(int objectIndex, string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data) || !ApiAvailable) return;
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
                _setTemporaryProfile.InvokeFunc(objectIndex, json);
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e, "Error calling CustomizePlus.SetTemporaryProfile");
            }
        }

        public void RemoveTemporaryProfile(int objectIndex)
        {
            if (!ApiAvailable) return;
            try
            {
                _removeTemporaryProfile.InvokeFunc(objectIndex);
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e, "Error calling CustomizePlus.RemoveTemporaryProfile");
            }
        }

        public void Dispose() { }
    }
}
