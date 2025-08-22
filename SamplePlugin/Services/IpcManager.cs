using McdfExporter.Services.Ipc;
using System;

namespace McdfExporter.Services;

public class IpcManager : IDisposable
{
    public readonly IpcCallerGlamourer Glamourer;
    public readonly IpcCallerPenumbra Penumbra;
    public readonly IpcCallerCustomizePlus CustomizePlus;

    public IpcManager()
    {
        Glamourer = new IpcCallerGlamourer();
        Penumbra = new IpcCallerPenumbra();
        CustomizePlus = new IpcCallerCustomizePlus();
    }

    public bool IsIpcReady()
    {
        Glamourer.CheckApi();
        Penumbra.CheckApi();
        // C+ is optional, so we don't check it here for readiness
        return Glamourer.ApiAvailable && Penumbra.ApiAvailable;
    }

    public void Dispose()
    {
        Glamourer.Dispose();
        Penumbra.Dispose();
        CustomizePlus.Dispose();
    }
}
