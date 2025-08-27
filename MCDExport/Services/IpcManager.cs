using Dalamud.Plugin;
using McdfExporter.Services.Ipc;
using System;

namespace McdfExporter.Services
{
    public class IpcManager : IDisposable
    {
        public readonly IpcCallerGlamourer Glamourer;
        public readonly IpcCallerPenumbra Penumbra;
        public readonly IpcCallerCustomizePlus CustomizePlus;

        public IpcManager(IDalamudPluginInterface dalamudPluginInterface)
        {
            Glamourer = new IpcCallerGlamourer();
            Penumbra = new IpcCallerPenumbra(dalamudPluginInterface);
            CustomizePlus = new IpcCallerCustomizePlus(dalamudPluginInterface);
        }

        public void CheckApis()
        {
            Glamourer.CheckApi();
            Penumbra.CheckApi();
            CustomizePlus.CheckApi();
        }

        public bool AllApisAvailable()
        {
            return Glamourer.ApiAvailable && Penumbra.ApiAvailable && CustomizePlus.ApiAvailable;
        }

        public void Dispose()
        {
            Glamourer.Dispose();
            Penumbra.Dispose();
            CustomizePlus.Dispose();
        }
    }
}
