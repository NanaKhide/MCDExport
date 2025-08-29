using McdfExporter.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace McdfExporter.Services
{
    public class RegistrationService
    {
        private readonly string _configPath;
        private readonly EventManager _eventManager;
        public Dictionary<string, RegisteredCharacter> RegisteredCharacters { get; private set; } = new();

        public RegistrationService(EventManager eventManager)
        {
            _eventManager = eventManager;
            _configPath = Path.Combine(Plugin.PluginInterface.GetPluginConfigDirectory(), "registered_characters.json");
            Load();
        }

        public void Load()
        {
            if (!File.Exists(_configPath)) return;
            var json = File.ReadAllText(_configPath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, RegisteredCharacter>>(json);
            RegisteredCharacters = loaded?.ToDictionary(p => GetKey(p.Value.Name, p.Value.HomeWorld), p => p.Value)
                                 ?? new Dictionary<string, RegisteredCharacter>();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(RegisteredCharacters, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }

        private string GetKey(string name, string homeWorld) => $"{name.ToLowerInvariant()}@{homeWorld.ToLowerInvariant()}";

        public void RegisterCharacter(string name, string homeWorld, string mcdfFilePath)
        {
            var newCharacter = new RegisteredCharacter { Name = name, HomeWorld = homeWorld, McdfFilePath = mcdfFilePath };
            var key = GetKey(name, homeWorld);
            RegisteredCharacters[key] = newCharacter;
            Save();
            _eventManager.CharacterRegistered(newCharacter);
        }

        public void UnregisterCharacter(string name, string homeWorld)
        {
            var key = GetKey(name, homeWorld);
            if (RegisteredCharacters.ContainsKey(key))
            {
                RegisteredCharacters.Remove(key);
                Save();
                _eventManager.CharacterUnregistered(name, homeWorld);
            }
        }

        public RegisteredCharacter? GetRegisteredCharacter(string name, string homeWorld)
        {
            var key = GetKey(name, homeWorld);
            return RegisteredCharacters.GetValueOrDefault(key);
        }
    }
}
