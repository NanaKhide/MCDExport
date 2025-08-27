using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Penumbra.Api.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PenumbraApi = Penumbra.Api.IpcSubscribers;

namespace McdfExporter.Services.Ipc
{
    public class IpcCallerPenumbra : IDisposable
    {
        private readonly PenumbraApi.ApiVersion _apiVersion;
        private readonly PenumbraApi.RedrawObject _redrawObject;
        private readonly ICallGateSubscriber<string, string, (PenumbraApiEc, Guid Guid)> _createTemporaryCollection;
        private readonly PenumbraApi.DeleteTemporaryCollection _deleteTemporaryCollection;
        private readonly PenumbraApi.AddTemporaryMod _addTemporaryMod;
        private readonly PenumbraApi.AssignTemporaryCollection _assignTemporaryCollection;
        private readonly PenumbraApi.GetGameObjectResourcePaths _getResourcePaths;
        private readonly PenumbraApi.GetPlayerMetaManipulations _getMetaManipulations;
        private readonly PenumbraApi.ResolvePlayerPath _resolvePlayerPath;
        private readonly PenumbraApi.ResolvePlayerPathsAsync _resolvePlayerPathsAsync;

        public bool ApiAvailable { get; private set; }

        public IpcCallerPenumbra(IDalamudPluginInterface dalamudPluginInterface)
        {
            _apiVersion = new PenumbraApi.ApiVersion(Plugin.PluginInterface);
            _redrawObject = new PenumbraApi.RedrawObject(Plugin.PluginInterface);
            _createTemporaryCollection = dalamudPluginInterface.GetIpcSubscriber<string, string, (PenumbraApiEc, Guid Guid)>("Penumbra.CreateTemporaryCollection.V6");
            _deleteTemporaryCollection = new PenumbraApi.DeleteTemporaryCollection(Plugin.PluginInterface);
            _addTemporaryMod = new PenumbraApi.AddTemporaryMod(Plugin.PluginInterface);
            _assignTemporaryCollection = new PenumbraApi.AssignTemporaryCollection(Plugin.PluginInterface);
            _getResourcePaths = new PenumbraApi.GetGameObjectResourcePaths(Plugin.PluginInterface);
            _getMetaManipulations = new PenumbraApi.GetPlayerMetaManipulations(Plugin.PluginInterface);
            _resolvePlayerPath = new PenumbraApi.ResolvePlayerPath(Plugin.PluginInterface);
            _resolvePlayerPathsAsync = new PenumbraApi.ResolvePlayerPathsAsync(Plugin.PluginInterface);
            CheckApi();
        }

        public void CheckApi()
        {
            try
            {
                ApiAvailable = _apiVersion.Invoke().Breaking >= 5;

            }
            catch
            {
                ApiAvailable = false;
            }
        }

        public Guid CreateTemporaryCollection(string name)
        {
            return this._createTemporaryCollection.InvokeFunc("MCDFExporter", name).Guid;
        }

        public void RemoveTemporaryCollection(Guid id) => _deleteTemporaryCollection.Invoke(id);

        public void AddTemporaryMod(string tag, Guid collectionId, Dictionary<string, string> files, string manipData) => _addTemporaryMod.Invoke(tag, collectionId, files, manipData, 0);

        public void AssignTemporaryCollection(Guid id, int objectIndex, bool force = true) => _assignTemporaryCollection.Invoke(id, objectIndex, force);

        public void RedrawActor(int objectIndex)
        {
            try { _redrawObject.Invoke(objectIndex, RedrawType.Redraw); }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Penumbra.RedrawObject"); }
        }
        public string GetPlayerMetaManipulations()
        {
            try { return _getMetaManipulations.Invoke() ?? string.Empty; }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Penumbra.GetPlayerMetaManipulations"); return string.Empty; }
        }
        public Dictionary<string, HashSet<string>>? GetCharacterData(int objectIndex)
        {
            try { return _getResourcePaths.Invoke((ushort)objectIndex)?[0]; }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Penumbra.GetGameObjectResourcePaths"); return null; }
        }
        public string? ResolvePlayerPath(string gamePath)
        {
            try { return _resolvePlayerPath.Invoke(gamePath); }
            catch (Exception e) { Plugin.Log.Error(e, $"Error calling Penumbra.ResolvePlayerPath for {gamePath}"); return null; }
        }
        public async Task<(string[], string[][])> ResolvePlayerPathsAsync(string[] forward, string[] reverse)
        {
            try { return await _resolvePlayerPathsAsync.Invoke(forward, reverse); }
            catch (Exception e) { Plugin.Log.Error(e, "Error calling Penumbra.ResolvePlayerPathsAsync"); return (Array.Empty<string>(), Array.Empty<string[]>()); }
        }
        public void Dispose() { }
    }
}
