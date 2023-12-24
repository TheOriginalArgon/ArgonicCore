using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using MonoMod.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArgonicCore
{
    public partial class ArgonicCore : Mod
    {
        
        

        public static Harmony harmony;
        public ArgonicCore(ModContentPack content) : base(content)
        {
            harmony = new Harmony("Argon.CoreLib");
            harmony.PatchAll();

            // The code below is commented as it was intended to be used as a reference for a feature.
            // I didn't use it at the end but I don't want to delete it either.
            #region VFE Code
            //LongEventHandler.ExecuteWhenFinished(() =>
            //{
            //    foreach (var def in DefDatabase<ThingDef>.AllDefs)
            //    {
            //        if (def.HasModExtension<StuffExtension_Cost>())
            //        {
            //            costExtended.Add(def);
            //            var key = def.GetModExtension<StuffExtension_Cost>().thingDef;
            //            if (!extraStuffFor.ContainsKey(key)) extraStuffFor.Add(key, new List<ThingDef>());
            //            extraStuffFor[key].Add(def);
            //        }

            //        if (def.HasModExtension<ThingExtension_IgnoreStuffFor>()) ignoreStuffFor.Add(def);
            //    }

            //    if (costExtended.Any())
            //    {
            //        harmony.Patch(AccessTools.Method(typeof(CostListCalculator), nameof(CostListCalculator.CostListAdjusted),
            //                new[] { typeof(BuildableDef), typeof(ThingDef), typeof(bool) }),
            //            new HarmonyMethod(typeof(ArgonicCore), nameof(AdjustStuff)));
            //        harmony.Patch(AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing)),
            //            new HarmonyMethod(typeof(ArgonicCore), nameof(AdjustStuff2)));
            //        harmony.Patch(AccessTools.Method(typeof(Designator_Build), nameof(Designator_Build.ProcessInput)),
            //            transpiler: new HarmonyMethod(typeof(ArgonicCore), nameof(ProcessInputTranspiler)));
            //        if (ModLister.HasActiveModWithName("Replace Stuff"))
            //        {
            //            Log.Message("[Argonic Core] Activating Replace Stuff compatibility patch");
            //            var replaceFrame = AccessTools.TypeByName("Replace_Stuff.ReplaceFrame");
            //            harmony.Patch(AccessTools.Method(replaceFrame, "CountStuffHas"),
            //                new HarmonyMethod(typeof(ReplaceStuffCompat), nameof(ReplaceStuffCompat.CountStuffHas)));
            //            harmony.Patch(AccessTools.Method(replaceFrame, "TotalStuffNeeded", new[] { typeof(BuildableDef), typeof(ThingDef) }),
            //                new HarmonyMethod(typeof(ReplaceStuffCompat), nameof(ReplaceStuffCompat.TotalStuffNeeded)));
            //            harmony.Patch(AccessTools.Method(replaceFrame, "MaterialsNeeded"),
            //                postfix: new HarmonyMethod(typeof(ReplaceStuffCompat), nameof(ReplaceStuffCompat.MaterialsNeeded)));
            //        }
            //    }

            //    if (ignoreStuffFor.Any())
            //    {
            //        harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)),
            //            new HarmonyMethod(typeof(ArgonicCore), nameof(StatIgnoreStuff)));
            //        harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetExplanationFull)),
            //            new HarmonyMethod(typeof(ArgonicCore), nameof(StatIgnoreStuff)));
            //    }
            //});
            #endregion


        }

    }


}
