using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using System;
using Dalamud.Plugin.Ipc;

namespace McdfExporter.Services.Ipc
{
    public class IpcCallerGlamourer : IDisposable
    {
        private readonly ApiVersion _apiVersion;
        private readonly ApplyState _applyState;
        private readonly GetStateBase64 _getStateBase64;
        private readonly RevertState _revertState;

        public bool ApiAvailable { get; private set; }

        public IpcCallerGlamourer()
        {
            _apiVersion = new ApiVersion(Plugin.PluginInterface);
            _getStateBase64 = new GetStateBase64(Plugin.PluginInterface);
            
            _applyState = new ApplyState(Plugin.PluginInterface);
            _revertState = new RevertState(Plugin.PluginInterface);

            CheckApi();
        }

        public void CheckApi()
        {
            try { ApiAvailable = _apiVersion.Invoke().Major >= 1; }
            catch { ApiAvailable = false; }
        }

        public void ApplyState(string state, int objectIndex)
        {
            if (string.IsNullOrEmpty(state)) return;
            try
            {
                _applyState.Invoke(state, objectIndex);
            }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Glamourer.ApplyState"); }
        }
        
        public void RevertState(int objectIndex)
        {
            try 
            {
                _revertState.Invoke(objectIndex);
            }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Glamourer.RevertState"); }
        }

        public string? GetStateBase64(int objectIndex)
        {
            try { return _getStateBase64.Invoke(objectIndex).Item2; }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Glamourer.GetStateBase64"); return null; }
        }

        public void Dispose() { }
    }
}
