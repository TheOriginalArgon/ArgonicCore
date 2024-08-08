using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ArgonicCore.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MaterialReplacement
{
    [HarmonyPatch]
    public static class Patch_Interchangability
    {
        // Resource interchangeability has a lot of patches that touch the ingredient lists.
        #region Resource Interchangeability

        // Patches for interchangable stuff.
        [HarmonyPatch]
        public static class HarmonyPatches_ResourceInterchangeability
        {
            private static Thing momentaryThing;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new Type[] { typeof(Thing) })]
            private static bool PrefixToRegisterThing(Thing thing)
            {
                momentaryThing = thing;
                return true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new Type[] { typeof(Thing) })]
            private static void PostfixToRegisterThing()
            {
                momentaryThing = null;
            }

            // CostList with replacement materials. TODO: Maybe now patch this directly and save two harmony methods.
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new Type[] { typeof(BuildableDef), typeof(ThingDef), typeof(bool) })]
            private static void ModifiedCostList(List<ThingDefCountClass> __result)
            {
                if (momentaryThing != null)
                {
                    //Log.Warning($"Momentary thing is an instance of {momentaryThing.def.defName}, which is {momentaryThing}");
                    __result = MaterialExchangingUtility.GetCustomCostListFor(__result, momentaryThing);

                    //foreach (ThingDefCountClass c in __result)
                    //{
                    //    Log.Warning($"{c.thingDef} x{c.count}");
                    //}
                    return;
                }
                else
                {
                    __result = MaterialExchangingUtility.MergeList(__result);
                    return;
                }
            }

            #region Blueprint Handling

            // Add the material selectors for Blueprints that have exchangeable materials.
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.GetGizmos))]
            private static IEnumerable<Gizmo> AddMaterialSelectors(IEnumerable<Gizmo> values, Blueprint_Build __instance)
            {

                bool compatibleLists = true;
                ThingDef thingDef = null;
                List<object> selectedObjects = Find.Selector.SelectedObjects;
                foreach (object obj in selectedObjects)
                {
                    Thing thing = obj as Thing;
                    if (thing != null)
                    {
                        if (thingDef == null)
                        {
                            thingDef = thing.def;
                        }
                        else
                        {
                            if (thing.def != thingDef)
                            {
                                compatibleLists = false;
                            }
                        }
                    }
                }
                List<ThingDefCountClass> costList = __instance.def.entityDefToBuild.CostList;
                foreach (Gizmo gizmo in values) { yield return gizmo; }

                if (compatibleLists && costList.Any())
                {

                    if (__instance.Faction == Faction.OfPlayer)
                    {
                        TechLevel techLevel = MaterialExchangingUtility.GetHigherTechLevel(__instance.def.entityDefToBuild.researchPrerequisites);
                        List<ThingDef> replacementMaterials;
                        for (int i = 0; i < costList.Count; i++)
                        {

                            // If there are any materials that can replace the current one.
                            if (MaterialExchangingUtility.ExistMaterialsToReplaceAtTechLevel(costList[i].thingDef, techLevel, out replacementMaterials))
                            {
                                yield return MaterialExchangingUtility.SelectMaterialCommand(__instance, __instance.Map, costList[i].thingDef, replacementMaterials);
                            }
                        }
                    }
                }
                yield break;
            }

            // Once a Blueprint is turned into a Frame, pass the corresponding replacement materials to it.
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Blueprint_Build), "MakeSolidThing")]
            private static void MakeFrame(Blueprint_Build __instance, ref Thing __result)
            {
                __result.SetMaterialValues(__instance.TryGetMaterialValues());
            }

            // Blueprint request materials.
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.TotalMaterialCost))]
            private static void BlueprintCostList(Blueprint_Build __instance, ref List<ThingDefCountClass> __result)
            {
                __result = MaterialExchangingUtility.GetCustomCostListFor(__result, __instance);
                //Log.Warning($"{__instance} is requesting:");
                //foreach (ThingDefCountClass c in __result)
                //{
                //    Log.Warning($"\t- {c.count}x {c.thingDef}");
                //}
            }
            #endregion

            #region Frame Handling
            // Pass the replacement material values to the finished Building once the Frame is completed.
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
            private static IEnumerable<CodeInstruction> AddMaterialsForThing(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> code = instructions.ToList();

                for (int i = 0; i < code.Count; i++)
                {
                    yield return code[i];

                    if (code[i].Calls(AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn), new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })))
                    {
                        //yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.TryGetMaterialValues)));
                        yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.SetMaterialValues)));
                        i++;
                    }
                }
            }

            // Frame request materials.
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Frame), nameof(Frame.TotalMaterialCost))]
            private static void FrameCostList(Frame __instance, ref List<ThingDefCountClass> __result)
            {
                __result = MaterialExchangingUtility.GetCustomCostListFor(__result, __instance);
                //Log.Warning($"{__instance} is requesting:");
                //foreach (ThingDefCountClass c in __result)
                //{
                //    Log.Warning($"\t- {c.count}x {c.thingDef}");
                //}
            }

            //Upon destruction, spawn the materials this Building was built with.
            //[HarmonyTranspiler]
            //[HarmonyPatch(typeof(GenLeaving), nameof(GenLeaving.DoLeavingsFor), new Type[] { typeof(Thing), typeof(Map), typeof(DestroyMode), typeof(CellRect), typeof(Predicate<IntVec3>), typeof(List<Thing>) })]
            //private static IEnumerable<CodeInstruction> ReturnProperMaterials(IEnumerable<CodeInstruction> instructions)
            //{
            //    foreach (CodeInstruction instruction in instructions)
            //    {
            //        yield return instruction;

            //        if (instruction.Calls(AccessTools.Method(typeof(CostListCalculator), nameof(CostListCalculator.CostListAdjusted), new Type[] { typeof(Thing) })))
            //        {
            //            yield return new CodeInstruction(OpCodes.Stloc_S, 12);
            //            yield return new CodeInstruction(OpCodes.Ldloc_S, 12);
            //            yield return new CodeInstruction(OpCodes.Ldarg_0);
            //            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.GetCustomCostListFor), new Type[] { typeof(List<ThingDefCountClass>), typeof(Thing) }));
            //        }
            //    }
            //}

            // Transpiler to literally fix a line of code in the game that is nonsense.
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Frame), nameof(Frame.GetInspectString))]
            private static IEnumerable<CodeInstruction> InspectStringWrongCall(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> code = instructions.ToList();

                for (int i = 0; i < code.Count; i++)
                {
                    yield return code[i];
                    if (i < 35 && code[i + 9].opcode == OpCodes.Stloc_2 && code[i + 10].opcode == OpCodes.Br_S)
                    {

                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Frame), "TotalMaterialCost"));
                        yield return new CodeInstruction(OpCodes.Stloc_1);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        yield return new CodeInstruction(OpCodes.Stloc_3);
                        i += 9;
                    }
                }
                yield break;
            }
            #endregion
        }
        #endregion
    }
}
