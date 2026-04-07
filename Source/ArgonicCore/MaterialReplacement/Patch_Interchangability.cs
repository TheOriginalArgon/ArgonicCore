using ArgonicCore.Utilities;
using HarmonyLib;
using MaterialReplacement.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace MaterialReplacement
{
    /// <summary>
    /// Harmony patches for material replacement system.
    /// Allows blueprints/frames to use alternative materials during construction.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Interchangability
    {
        // Resource interchangeability has a lot of patches that touch the ingredient lists.
        #region Resource Interchangeability

        /// <summary>
        /// Patches for interchangeable construction materials.
        /// Intercepts cost calculations to apply material replacements.
        /// </summary>
        [HarmonyPatch]
        public static class HarmonyPatches_ResourceInterchangeability
        {
            // WHY: ThreadStatic ensures each thread has its own copy (future-proofing for MP/async)
            // WHY: Using __state + _callDepth handles nested/recursive CostListAdjusted calls
            [ThreadStatic]
            private static Thing _momentaryThing;
            
            [ThreadStatic]
            private static int _callDepth;

            /// <summary>
            /// Captures Thing reference before cost calculation.
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new Type[] { typeof(Thing) })]
            private static void PrefixToRegisterThing(Thing thing, ref Thing __state)
            {
                // Save previous value for nested calls
                __state = _momentaryThing;
                _momentaryThing = thing;
                _callDepth++;
            }

            /// <summary>
            /// Cleans up Thing reference after cost calculation.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CostListCalculator), "CostListAdjusted", new Type[] { typeof(Thing) })]
            private static void PostfixToRegisterThing(Thing __state)
            {
                _callDepth--;
                // Restore previous value (for nested calls) or clear if top-level
                _momentaryThing = _callDepth > 0 ? __state : null;
            }

            /// <summary>
            /// Applies material replacements to the calculated cost list.
            /// </summary>
            /// <remarks>
            /// TODO: Maybe now patch this directly and save two harmony methods.
            /// </remarks>
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

            #region Blueprint Handling

            /// <summary>
            /// Adds material selector gizmos to blueprints with exchangeable materials.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.GetGizmos))]
            private static IEnumerable<Gizmo> AddMaterialSelectors(IEnumerable<Gizmo> values, Blueprint_Build __instance)
            {
                // Check if all selected objects are the same type (for multi-select)
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
                        else if (thing.def != thingDef)
                        {
                            compatibleLists = false;
                        }
                    }
                }

                List<ThingDefCountClass> costList = __instance.def.entityDefToBuild.CostList;
                
                // Yield original gizmos first
                foreach (Gizmo gizmo in values) 
                { 
                    yield return gizmo; 
                }

                // Add material replacement gizmos for player-owned blueprints
                if (compatibleLists && !costList.NullOrEmpty() && __instance.Faction == Faction.OfPlayer)
                {
                    TechLevel techLevel = MaterialExchangingUtility.GetHigherTechLevel(
                        __instance.def.entityDefToBuild.researchPrerequisites);
                    
                    for (int i = 0; i < costList.Count; i++)
                    {
                        // If there are any materials that can replace the current one
                        if (MaterialExchangingUtility.ExistMaterialsToReplaceAtTechLevel(
                            __instance.def.entityDefToBuild.defName, 
                            costList[i].thingDef, 
                            techLevel, 
                            out List<ThingDef> replacementMaterials))
                        {
                            yield return MaterialExchangingUtility.SelectMaterialCommand(
                                __instance, __instance.Map, costList[i].thingDef, replacementMaterials);
                        }
                    }
                }
            }

            /// <summary>
            /// Passes material replacement values from blueprint to frame.
            /// </summary>
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

            /// <summary>
            /// Applies material replacements to blueprint's material cost display.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.TotalMaterialCost))]
            private static void BlueprintCostList(Blueprint_Build __instance, ref List<ThingDefCountClass> __result)
            {
                __result = MaterialExchangingUtility.GetCustomCostListFor(__result, __instance);
            }
            
            #endregion

            #region Frame Handling

            /// <summary>
            /// Passes material replacement values from frame to completed building.
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
            private static IEnumerable<CodeInstruction> AddMaterialsForThing(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> code = instructions.ToList();

                for (int i = 0; i < code.Count; i++)
                {
                    yield return code[i];

                    // Inject after GenSpawn.Spawn call to pass material values to spawned thing
                    if (code[i].Calls(AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn), 
                        new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), 
                                     typeof(WipeMode), typeof(bool), typeof(bool) })))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, 
                            AccessTools.Method(typeof(MaterialExchangingUtility), 
                                nameof(MaterialExchangingUtility.TryGetMaterialValues)));
                        yield return new CodeInstruction(OpCodes.Callvirt, 
                            AccessTools.Method(typeof(MaterialExchangingUtility), 
                                nameof(MaterialExchangingUtility.SetMaterialValues)));
                        i++;
                    }
                }
            }

            /// <summary>
            /// Applies material replacements to frame's material cost display.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Frame), nameof(Frame.TotalMaterialCost))]
            private static void FrameCostList(Frame __instance, ref List<ThingDefCountClass> __result)
            {
                __result = MaterialExchangingUtility.GetCustomCostListFor(__result, __instance);
            }

            // TODO: Resets the selected material to the selected one if the construction fails.

            /// <summary>
            /// Fixes vanilla bug in Frame.GetInspectString material cost display.
            /// </summary>
            /// <remarks>
            /// Transpiler to literally fix a line of code in the game that is nonsense.
            /// </remarks>
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
                        yield return new CodeInstruction(OpCodes.Call, 
                            AccessTools.Method(typeof(Frame), "TotalMaterialCost"));
                        yield return new CodeInstruction(OpCodes.Stloc_1);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        yield return new CodeInstruction(OpCodes.Stloc_3);
                        i += 9;
                    }
                }
            }
            
            #endregion

            /// <summary>
            /// Adds replacement materials to recipe ingredient filters.
            /// </summary>
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RecipeDefGenerator), nameof(RecipeDefGenerator.SetIngredients))]
            private static void SetReplacementIngredients(RecipeDef r, ThingDef def)
            {
                foreach (IngredientCount ingredient in r.ingredients.Where(ingredient => ingredient.IsFixedIngredient))
                {
                    // WHY: This line of code causes my eyes to bleed, but I guess it works well 
                    // because it's an enum. I have to guess where the research prerequisite is 
                    // because it can be either a single one or a list.
                    TechLevel techLevel = (TechLevel)Math.Max(
                        (byte)(def.recipeMaker.researchPrerequisite?.techLevel ?? TechLevel.Animal), 
                        (byte)MaterialExchangingUtility.GetHigherTechLevel(def.recipeMaker.researchPrerequisites));

                    if (MaterialExchangingUtility.ExistMaterialsToReplaceAtTechLevel(
                        def.defName, ingredient.FixedIngredient, techLevel, 
                        out List<ThingDef> replacementMaterials, true))
                    {
                        foreach (ThingDef replacementMaterial in replacementMaterials)
                        {
                            ingredient.filter.SetAllow(replacementMaterial, true);
                            r.fixedIngredientFilter.SetAllow(replacementMaterial, true);
                        }
                    }
                }
            }

            //[HarmonyPostfix]
            //[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
            //private static IEnumerable<Thing> SetReplacementIngredientsOnProduct(IEnumerable<Thing> processedProducts, RecipeDef recipeDef, List<Thing> ingredients)
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