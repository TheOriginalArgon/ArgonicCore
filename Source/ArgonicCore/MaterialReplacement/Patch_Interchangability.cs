using ArgonicCore.Utilities;
using HarmonyLib;
using MaterialReplacement.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace MaterialReplacement
{
    [HarmonyPatch]
    public static class Patch_Interchangability
    {
        #region Resource Interchangeability

        [HarmonyPatch]
        public static class HarmonyPatches_ResourceInterchangeability
        {
            [ThreadStatic]
            private static Thing _momentaryThing;
            
            [ThreadStatic]
            private static int _callDepth;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new Type[] { typeof(Thing) })]
            private static void PrefixToRegisterThing(Thing thing, ref Thing __state)
            {
                __state = _momentaryThing;
                _momentaryThing = thing;
                _callDepth++;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new Type[] { typeof(Thing) })]
            private static void PostfixToRegisterThing(Thing __state)
            {
                _callDepth--;
                _momentaryThing = _callDepth > 0 ? __state : null;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new Type[] { typeof(BuildableDef), typeof(ThingDef), typeof(bool) })]
            private static void ModifiedCostList(ref List<ThingDefCountClass> __result)
            {
                Thing thing = _momentaryThing;
                if (thing != null)
                {
                    __result = MaterialExchangingUtility.GetCustomCostListFor(__result, thing);
                }
            }
        }

        #region Blueprint Handling

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.GetGizmos))]
        private static IEnumerable<Gizmo> AddMaterialSelectors(IEnumerable<Gizmo> values, Blueprint_Build __instance)
        {
            bool compatibleLists = true;
            ThingDef thingDef = null;
            List<object> selectedObjects = Find.Selector.SelectedObjects;
            foreach (object obj in selectedObjects)
            {
                if (obj is Thing thing)
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

            if (compatibleLists && !costList.NullOrEmpty())
            {
                if (__instance.Faction == Faction.OfPlayer)
                {
                    TechLevel techLevel = MaterialExchangingUtility.GetHigherTechLevel(__instance.def.entityDefToBuild.researchPrerequisites);
                    for (int i = 0; i < costList.Count; i++)
                    {
                        if (MaterialExchangingUtility.ExistMaterialsToReplaceAtTechLevel(__instance.def.entityDefToBuild.defName, costList[i].thingDef, techLevel, out List<ThingDef> replacementMaterials))
                        {
                            yield return MaterialExchangingUtility.SelectMaterialCommand(__instance, __instance.Map, costList[i].thingDef, replacementMaterials);
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Blueprint_Build), "MakeSolidThing")]
        private static void MakeFrame(Blueprint_Build __instance, ref Thing __result)
        {
            Dictionary<ThingDef, ThingDef> materialValues = __instance.TryGetMaterialValues();
            if (materialValues != null)
            {
                __result.SetMaterialValues(materialValues);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.TotalMaterialCost))]
        private static void BlueprintCostList(Blueprint_Build __instance, ref List<ThingDefCountClass> __result)
        {
            __result = MaterialExchangingUtility.GetCustomCostListFor(__result, __instance);
        }
        #endregion

        #region Frame Handling

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
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.TryGetMaterialValues)));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.SetMaterialValues)));
                    i++;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Frame), nameof(Frame.TotalMaterialCost))]
        private static void FrameCostList(Frame __instance, ref List<ThingDefCountClass> __result)
        {
            __result = MaterialExchangingUtility.GetCustomCostListFor(__result, __instance);
        }

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
        }
        #endregion

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RecipeDefGenerator), nameof(RecipeDefGenerator.SetIngredients))]
        private static void SetReplacementIngredients(RecipeDef r, ThingDef def)
        {
            foreach (IngredientCount ingredient in r.ingredients.Where(ingredient => ingredient.IsFixedIngredient))
            {
                TechLevel techLevel = (TechLevel)Math.Max((byte)(def.recipeMaker.researchPrerequisite?.techLevel ?? TechLevel.Animal), (byte)MaterialExchangingUtility.GetHigherTechLevel(def.recipeMaker.researchPrerequisites));

                if (MaterialExchangingUtility.ExistMaterialsToReplaceAtTechLevel(def.defName, ingredient.FixedIngredient, techLevel, out List<ThingDef> replacementMaterials, true))
                {
                    foreach (ThingDef replacementMaterial in replacementMaterials)
                    {
                        ingredient.filter.SetAllow(replacementMaterial, true);
                        r.fixedIngredientFilter.SetAllow(replacementMaterial, true);
                    }
                }
            }
        }

        #endregion
    }
}