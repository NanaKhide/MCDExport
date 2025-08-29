// One EventManager to rule them all... (And to clean up this mess of a project)

using McdfExporter.Data;
using System;
namespace McdfExporter.Services
{
    public class EventManager
    {
        public event Action<(RegisteredCharacter character, string mcdfPath)> OnMcdfApplyRequested;
        public event Action<(Guid collectionId, int objectIndex, string characterName)> OnMcdfApplicationCleanupRequested;
        public event Action<RegisteredCharacter> OnCharacterRegistered;
        public event Action<(string name, string homeWorld)> OnCharacterUnregistered;


        // Events for UI requests later on when i can be asked
        public void McdfApplyRequested(RegisteredCharacter character, string mcdfPath) =>
            OnMcdfApplyRequested?.Invoke((character, mcdfPath));

        public void McdfApplicationCleanupRequested(Guid collectionId, int objectIndex, string characterName) =>
            OnMcdfApplicationCleanupRequested?.Invoke((collectionId, objectIndex, characterName));

        public void CharacterRegistered(RegisteredCharacter character) => OnCharacterRegistered?.Invoke(character);

        public void CharacterUnregistered(string name, string homeWorld) => OnCharacterUnregistered?.Invoke((name, homeWorld));
    }
}
