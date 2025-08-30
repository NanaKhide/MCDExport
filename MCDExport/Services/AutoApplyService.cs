using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using McdfExporter.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace McdfExporter.Services
{
    public class AutoApplyService : IDisposable
    {
        private readonly IFramework _framework;
        private readonly IObjectTable _objectTable;
        private readonly IClientState _clientState;
        private readonly IPluginLog _log;
        private readonly EventManager _eventManager;
        private readonly RegistrationService _registrationService;
        private readonly McdfApplicationService _mcdfApplier;
        private readonly IpcManager _ipcManager;

        private readonly Stopwatch _updateStopwatch = new();
        private const long UpdateIntervalMs = 250;
        
        private class ActiveMcdfApplication
        {
            public Guid PenumbraCollectionId { get; set; }
            public int ObjectIndex { get; set; }
            public string CharacterName { get; set; } = string.Empty;
            public string McdfFilePath { get; set; } = string.Empty;
        }

        private readonly Dictionary<string, ActiveMcdfApplication> _activeApplications = new();
        private readonly HashSet<string> _processingCharacters = new HashSet<string>();

        public AutoApplyService(IFramework framework, ITargetManager targetManager, IObjectTable objectTable,
                                IClientState clientState, IPluginLog log, EventManager eventManager,
                                RegistrationService registrationService, McdfApplicationService mcdfApplier, IpcManager ipcManager)
        {
            _framework = framework;
            _objectTable = objectTable;
            _clientState = clientState;
            _log = log;
            _eventManager = eventManager;
            _registrationService = registrationService;
            _mcdfApplier = mcdfApplier;
            _ipcManager = ipcManager;

            _framework.Update += OnFrameworkUpdate;
            _eventManager.OnMcdfApplicationCleanupRequested += OnCleanupRequested;
            _eventManager.OnCharacterUnregistered += OnCharacterUnregistered;
            _eventManager.OnCharacterRegistered += OnCharacterRegistered;

            _ipcManager.Glamourer.GlamourerStateChanged += OnGlamourerStateChanged;
            _ipcManager.Glamourer.GPoseStateChanged += OnGposeStateChanged;
            _updateStopwatch.Start();
        }

        private void OnCharacterRegistered(RegisteredCharacter registeredCharacter)
        {
            var character = _objectTable
                .OfType<IPlayerCharacter>()
                .FirstOrDefault(p => p.Name.TextValue == registeredCharacter.Name && p.HomeWorld.Value.Name.ToString() == registeredCharacter.HomeWorld);

            if (character != null && character.IsValid())
            {
                var worldName = character.HomeWorld.Value.Name.ToString();
                var key = $"{character.Name.TextValue.ToLowerInvariant()}@{worldName.ToLowerInvariant()}";

                if (!_activeApplications.ContainsKey(key) && !_processingCharacters.Contains(key))
                {
                    ApplyMcdfToCharacter(key, character, registeredCharacter.McdfFilePath);
                }
            }
        }
        public void ReapplyAllCharacters()
        {
            _log.Verbose("Reapplying all registered characters.");

            foreach (var app in _activeApplications.Values.ToList())
            {
                CleanupApplication(app);
            }
            _activeApplications.Clear();
            _processingCharacters.Clear();
        }

        public void Dispose()
        {
            _framework.Update -= OnFrameworkUpdate;
            _eventManager.OnMcdfApplicationCleanupRequested -= OnCleanupRequested;
            _eventManager.OnCharacterUnregistered -= OnCharacterUnregistered;

            _ipcManager.Glamourer.GlamourerStateChanged -= OnGlamourerStateChanged;

            foreach (var app in _activeApplications.Values.ToList())
            {
                CleanupApplication(app);
            }
            _activeApplications.Clear();
        }

        private void OnGposeStateChanged(bool isGposeActive)
        {
            if (isGposeActive)
            {
                _log.Verbose("GPose entered. Cleaning up and discarding ActiveApplicationss.");

                foreach (var app in _activeApplications.Values.ToList())
                {
                    CleanupApplication(app);
                }
                _activeApplications.Clear();
            }
        }

        private void OnGlamourerStateChanged(nint actorAddress)
        {
            if (_clientState.IsGPosing) return;
            var character = _objectTable.FirstOrDefault(obj => obj.Address == actorAddress);
            if (character is not IPlayerCharacter playerCharacter || !playerCharacter.IsValid()) return;

            var worldName = playerCharacter.HomeWorld.Value.Name.ToString();
            var key = $"{playerCharacter.Name.TextValue.ToLowerInvariant()}@{worldName.ToLowerInvariant()}";

            if (_activeApplications.ContainsKey(key) && !_processingCharacters.Contains(key))
            {
                _log.Verbose($"Glamourer state changed for managed character {playerCharacter.Name}. Re-applying MCDF.");
                var registered = _registrationService.GetRegisteredCharacter(playerCharacter.Name.TextValue, worldName);
                if (registered != null)
                {
                    ApplyMcdfToCharacter(key, playerCharacter, registered.McdfFilePath);
                }
            }
        }

        private void OnCharacterUnregistered((string name, string homeWorld) data)
        {
            var key = $"{data.name.ToLowerInvariant()}@{data.homeWorld.ToLowerInvariant()}";
            if (_activeApplications.Remove(key, out var app))
            {
                CleanupApplication(app);
            }
        }

        private void OnCleanupRequested((Guid collectionId, int objectIndex, string characterName) data)
        {
            CleanupApplication(new ActiveMcdfApplication
            {
                PenumbraCollectionId = data.collectionId,
                ObjectIndex = data.objectIndex,
                CharacterName = data.characterName
            });
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (_clientState.LocalPlayer == null || !_clientState.IsLoggedIn || _clientState.IsGPosing) return;

            if (_updateStopwatch.ElapsedMilliseconds < UpdateIntervalMs) return;
            _updateStopwatch.Restart();

            if (!_ipcManager.AllApisAvailable()) return;

            var keysToCleanUp = new List<string>();
            foreach (var (key, app) in _activeApplications)
            {
                if (_processingCharacters.Contains(key)) continue;

                var obj = _objectTable[app.ObjectIndex];
                if (obj is not IPlayerCharacter player || !player.IsValid() || player.Name.TextValue != app.CharacterName)
                {
                    keysToCleanUp.Add(key);
                }
            }

            foreach (var key in keysToCleanUp)
            {
                if (_activeApplications.Remove(key, out var app))
                {
                    CleanupApplication(app);
                }
            }

            var onScreenCharacters = _objectTable
                .OfType<IPlayerCharacter>()
                .Where(p => p.IsValid() && p != _clientState.LocalPlayer && p.HomeWorld.RowId != 0);

            foreach (var character in onScreenCharacters)
            {
                var worldName = character.HomeWorld.Value.Name.ToString();
                var key = $"{character.Name.TextValue.ToLowerInvariant()}@{worldName.ToLowerInvariant()}";

                if (!_activeApplications.ContainsKey(key) && !_processingCharacters.Contains(key))
                {
                    var registered = _registrationService.GetRegisteredCharacter(character.Name.TextValue, worldName);
                    if (registered != null)
                    {
                        ApplyMcdfToCharacter(key, character, registered.McdfFilePath);
                    }
                }
            }
        }

        private void CleanupApplication(ActiveMcdfApplication app)
        {
            _log.Verbose($"Cleaning up MCDF for {app.CharacterName} (Index: {app.ObjectIndex})");
            _ipcManager.CustomizePlus.RemoveTemporaryProfile(app.ObjectIndex);
            _ipcManager.Glamourer.RevertState(app.ObjectIndex);
            if (app.PenumbraCollectionId != Guid.Empty)
            {
                _ipcManager.Penumbra.RemoveTemporaryCollection(app.PenumbraCollectionId);
            }
        }

        private void ApplyMcdfToCharacter(string key, IPlayerCharacter character, string mcdfPath)
        {
            if (_clientState.IsGPosing) return;
            _processingCharacters.Add(key);

            var newApp = new ActiveMcdfApplication
            {
                ObjectIndex = character.ObjectIndex,
                CharacterName = character.Name.TextValue,
                McdfFilePath = mcdfPath
            };

            _activeApplications[key] = newApp;

            _log.Verbose($"[Registration] Applying MCDF to {character.Name}.");
            _ = Task.Run(async () =>
            {
                try
                {
                    var collectionId = await _mcdfApplier.ApplyMcdf(character, mcdfPath);
                    if (collectionId.HasValue)
                    {
                        if (_activeApplications.TryGetValue(key, out var app))
                        {
                            app.PenumbraCollectionId = collectionId.Value;
                        }
                        else
                        {
                            _log.Warning($"Character {key} disappeared during MCDF application. Cleaning up generated collection.");
                            _ipcManager.Penumbra.RemoveTemporaryCollection(collectionId.Value);
                        }
                    }
                    else
                    {
                        _log.Error($"Failed to apply MCDF for {key}. Removing from active list.");
                        await _framework.RunOnFrameworkThread(() => _activeApplications.Remove(key));
                    }
                }
                finally
                {
                    _processingCharacters.Remove(key);
                }
            });
        }
    }
}
