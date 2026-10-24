using ArgonicCore.Utilities;
using HarmonyLib;
using MaterialReplacement.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
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
            private static void ModifiedCostList(ref List<ThingDefCountClass> __result)
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
                //else
                //{
                //    //__result = MaterialExchangingUtility.GetCustomCostListFor(__result, entDef);

                //    //foreach (ThingDefCountClass c in __result)
                //    //{
                //    //    Log.Error($"{c.thingDef} x{c.count}");
                //    //}
                //    return;
                //}
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

                if (compatibleLists && !costList.NullOrEmpty())
                {

                    if (__instance.Faction == Faction.OfPlayer)
                    {
                        TechLevel techLevel = MaterialExchangingUtility.GetHigherTechLevel(__instance.def.entityDefToBuild.researchPrerequisites);
                        List<ThingDef> replacementMaterials;
                        for (int i = 0; i < costList.Count; i++)
                        {
                            //Log.Error("Like and subscribe or else you're doomed to have red errors forever!"); This was an old joke I'm keeping because I find it funny.
                            // If there are any materials that can replace the current one.
                            if (MaterialExchangingUtility.ExistMaterialsToReplaceAtTechLevel(__instance.def.entityDefToBuild.defName, costList[i].thingDef, techLevel, out replacementMaterials))
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
                Dictionary<ThingDef, ThingDef> materialValues = __instance.TryGetMaterialValues();
                if (materialValues != null)
                {
                    __result.SetMaterialValues(materialValues);
                    //Log.Message($"Passing material values to {__result} from {__instance}");
                }
                //else
                //{
                //    Log.Message($"Made {__result} from {__instance}. material values null.");
                //}
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

            // Resets the selected material to the selected one if the construction fails.
            // TODO

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

            [HarmonyPostfix]
            [HarmonyPatch(typeof(RecipeDefGenerator), nameof(RecipeDefGenerator.SetIngredients))]
            private static void SetReplacementIngredients(RecipeDef r, ThingDef def)
            {
                foreach (IngredientCount ingredient in r.ingredients.Where(ingredient => ingredient.IsFixedIngredient))
                {
                    // This line of code causes my eyes to bleed, but I guess it works well because it's an enum. I have to guess where the research prerequisite is because it can be either a single one or a list.
                    TechLevel techLevel = (TechLevel)Math.Max((byte)(def.recipeMaker.researchPrerequisite?.techLevel ?? TechLevel.Animal), (byte)MaterialExchangingUtility.GetHigherTechLevel(def.recipeMaker.researchPrerequisites));

                    List<ThingDef> replacementMaterials;
                    if (MaterialExchangingUtility.ExistMaterialsToReplaceAtTechLevel(def.defName, ingredient.FixedIngredient, techLevel, out replacementMaterials, true))
                    {
                        foreach (ThingDef replacementMaterial in replacementMaterials)
                        {
                            ingredient.filter.SetAllow(replacementMaterial, true);
                            r.fixedIngredientFilter.SetAllow(replacementMaterial, true);
                            //r.defaultIngredientFilter.SetAllow(replacementMaterial, false); // Add the ingredient, but set it to disallowed by default.
                        }
                    }
                }
            }

            //[HarmonyPostfix]
            //[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
            //private static IEnumerable<Thing> SetReplacentIngredientsOnProduct(IEnumerable<Thing> processedProducts, RecipeDef recipeDef, List<Thing> ingredients)
            //{
            //    if (recipeDef.specialProducts == null && recipeDef.products != null)
            //    {
            //        // If I ever discover a way to pass the recipe's replacement ingredients to the final thing, cool.
            //    }
            //}
        }
        #endregion
    }
}
