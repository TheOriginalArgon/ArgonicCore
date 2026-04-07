using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MaterialReplacement.Compatibility
{
    /// <summary>
    /// Multiplayer mod compatibility layer.
    /// Registers synced methods via reflection to avoid hard dependency on MP API.
    /// 
    /// How it works:
    /// - On game startup, checks if Multiplayer mod is loaded
    /// - If found, registers SyncedActions.SyncSetMaterial as a synced method
    /// - MP will intercept calls and execute them on all clients simultaneously
    /// 
    /// MP can serialize: Thing (by thingIDNumber), ThingDef (by defName)
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MultiplayerCompat
    {
        static MultiplayerCompat()
        {
            Type mpType = AccessTools.TypeByName("Multiplayer.API.MP");
            if (mpType == null) return; // MP not installed

            try
            {
                // Find: ISyncMethod RegisterSyncMethod(Type type, string methodOrPropertyName, SyncType[] argTypes = null)
                MethodInfo registerMethod = null;
                foreach (var m in mpType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name != "RegisterSyncMethod") continue;
                    var pars = m.GetParameters();
                    if (pars.Length >= 2
                        && pars[0].ParameterType == typeof(Type)
                        && pars[1].ParameterType == typeof(string))
                    {
                        registerMethod = m;
                        break;
                    }
                }

                if (registerMethod == null)
                {
                    Log.Warning("[MaterialReplacement] MP API found but RegisterSyncMethod signature not matched.");
                    return;
                }

                // Register with null argTypes (MP will infer from method signature)
                object[] args = registerMethod.GetParameters().Length == 2
                    ? new object[] { typeof(SyncedActions), nameof(SyncedActions.SyncSetMaterial) }
                    : new object[] { typeof(SyncedActions), nameof(SyncedActions.SyncSetMaterial), null };

                registerMethod.Invoke(null, args);
                Log.Message("[MaterialReplacement] Multiplayer compatibility initialized.");
            }
            catch (Exception e)
            {
                Log.Error($"[MaterialReplacement] MP compat failed: {e}");
            }
        }
    }
}