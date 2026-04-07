using Verse;
using MaterialReplacement.Utilities;

namespace MaterialReplacement
{
    /// <summary>
    /// Methods synced via Multiplayer API.
    /// 
    /// When MP mod is active, calls to these methods are intercepted,
    /// serialized, sent to all clients, and executed simultaneously.
    /// 
    /// Requirements for MP serialization:
    /// - Thing: serialized by thingIDNumber (must exist on all clients)
    /// - ThingDef: serialized by defName
    /// - Primitives (int, float, bool, string): supported
    /// </summary>
    public static class SyncedActions
    {
        /// <summary>
        /// Sets material replacement for a blueprint/frame.
        /// Called from Command_SelectMaterial when player changes material.
        /// 
        /// Without MP: executes locally only (causes desync)
        /// With MP: executes on ALL clients simultaneously (no desync)
        /// </summary>
        /// <param name="thing">Blueprint or Frame to modify</param>
        /// <param name="originalMaterial">Material being replaced (e.g., Steel)</param>
        /// <param name="replacement">New material (e.g., MildSteel)</param>
        public static void SyncSetMaterial(Thing thing, ThingDef originalMaterial, ThingDef replacement)
        {
            if (thing == null || thing.Destroyed) return;
            thing.SetActiveOptionalMaterialFor(originalMaterial, replacement);
        }
    }
}