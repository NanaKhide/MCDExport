// Perhaps later for general detection of actors
// Method i have right now works so cba to experiment now
using Dalamud.Game.ClientState.Objects.Types;

namespace McdfExporter.Extensions
{
    public static class GameObjectEx
    {
        public static unsafe bool IsDrawing(this IGameObject actor)
        {
            if (actor == null || actor.Address == nint.Zero)
                return false;
            return ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actor.Address)->RenderFlags == 0;
        }
    }
}
