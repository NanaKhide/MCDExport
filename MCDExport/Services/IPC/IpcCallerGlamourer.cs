using Glamourer.Api.Helpers;
using Glamourer.Api.IpcSubscribers;
using System;

namespace McdfExporter.Services.Ipc
{
    public class IpcCallerGlamourer : IDisposable
    {
        private readonly uint _glamLock = 69420;
        private readonly ApiVersion _apiVersion;
        private readonly ApplyState _applyState;
        private readonly GetStateBase64 _getStateBase64;
        private readonly RevertState _revertState;
        private readonly UnlockAll _unlockAll;
        private readonly EventSubscriber<nint> _stateChanged;
        private readonly EventSubscriber<bool> _gposeChanged;

        public event Action<nint>? GlamourerStateChanged;
        public event Action<bool>? GPoseStateChanged;
        public bool ApiAvailable { get; private set; }

        public IpcCallerGlamourer()
        {
            _apiVersion = new ApiVersion(Plugin.PluginInterface);
            _getStateBase64 = new GetStateBase64(Plugin.PluginInterface);
            _applyState = new ApplyState(Plugin.PluginInterface);
            _revertState = new RevertState(Plugin.PluginInterface);
            _unlockAll = new UnlockAll(Plugin.PluginInterface);

            _stateChanged = StateChanged.Subscriber(Plugin.PluginInterface, OnStateChange);
            _gposeChanged = GPoseChanged.Subscriber(Plugin.PluginInterface, OnGPoseChange);
            CheckApi();
        }

        public void Dispose()
        {
            _stateChanged.Dispose();
            _gposeChanged.Dispose();
        }

        private void OnStateChange(nint actorAddress)
        {
            GlamourerStateChanged?.Invoke(actorAddress);
        }

        private void OnGPoseChange(bool inGpose)
        {
            GPoseStateChanged?.Invoke(inGpose);
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
                _applyState.Invoke(state, objectIndex, _glamLock);
            }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Glamourer.ApplyState"); }
        }

        public void RevertState(int objectIndex)
        {
            try
            {
                _revertState.Invoke(objectIndex, _glamLock);
            }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Glamourer.RevertState"); }
        }

        public string? GetStateBase64(int objectIndex)
        {
            try { return _getStateBase64.Invoke(objectIndex).Item2; }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Glamourer.GetStateBase64"); return null; }
        }

        // not needed anymore since i unlock each through cleanup
        // will leave it for now in case of smth broken
        public void UnlockAll()
        {
            _unlockAll.Invoke(_glamLock);
        }
    }
}
