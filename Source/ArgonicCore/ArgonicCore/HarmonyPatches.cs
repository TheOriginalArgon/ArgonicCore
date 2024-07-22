using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ArgonicCore.Comps;
using ArgonicCore.Defs;
using ArgonicCore.ModExtensions;
using ArgonicCore.Utilities;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArgonicCore
{
    #region Misc. Patches
    // Fuel power handling.
    [HarmonyPatch]
    public static class HarmonyPatch_FuelPower
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new Type[] { typeof(List<Thing>) })]
        public static IEnumerable<CodeInstruction> RefuelTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var methodInfo_01 = AccessTools.Field("Verse.Thing:stackCount");
            var methodInfo_02 = AccessTools.Method(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new Type[] { typeof(float) });

            int insertionIndex_01 = -1;
            int insertionIndex_02 = -1;

            // Code insertion here.
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].LoadsField(methodInfo_01))
                {

                    insertionIndex_01 = i - 1;

                    var newInstructions_01 = new List<CodeInstruction>();

                    newInstructions_01.Add(new CodeInstruction(OpCodes.Conv_R4));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Ldloc_1));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Ldstr, "ArgonicCore_FuelPower"));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DefDatabase<StatDef>), nameof(DefDatabase<StatDef>.GetNamed), new Type[] { typeof(string), typeof(bool) })));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Ldc_I4_M1));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue), new Type[] { typeof(Thing), typeof(StatDef), typeof(bool), typeof(int) })));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Div));
                    newInstructions_01.Add(new CodeInstruction(OpCodes.Conv_I4));

                    if (insertionIndex_01 != -1)
                    {
                        code.InsertRange(insertionIndex_01, newInstructions_01);
                    }

                    break;
                }
            }

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].Calls(methodInfo_02))
                {
                    insertionIndex_02 = i;
                    break;
                }
            }

            var newInstructions_02 = new List<CodeInstruction>();

            newInstructions_02.Add(new CodeInstruction(OpCodes.Ldloc_1));
            newInstructions_02.Add(new CodeInstruction(OpCodes.Ldstr, "ArgonicCore_FuelPower"));
            newInstructions_02.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            newInstructions_02.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DefDatabase<StatDef>), nameof(DefDatabase<StatDef>.GetNamed), new Type[] { typeof(string), typeof(bool) })));
            newInstructions_02.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            newInstructions_02.Add(new CodeInstruction(OpCodes.Ldc_I4_M1));
            newInstructions_02.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue), new Type[] { typeof(Thing), typeof(StatDef), typeof(bool), typeof(int) })));
            newInstructions_02.Add(new CodeInstruction(OpCodes.Mul));

            if (insertionIndex_02 != -1)
            {
                code.InsertRange(insertionIndex_02, newInstructions_02);
            }

            return code.AsEnumerable();
        }

    }

    [HarmonyPatch]
    public static class HarmonyPatches_Generic
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
        private static IEnumerable<Thing> ContaminateProduct(IEnumerable<Thing> originalResult, RecipeDef recipeDef, List<Thing> ingredients)
        {
            if (recipeDef.products != null)
            {
                // Iterate through each product.
                foreach (Thing product in originalResult)
                {
                    // If it can be contaminated.
                    CompContaminable compContaminable1 = product.TryGetComp<CompContaminable>();
                    if (compContaminable1 != null)
                    {
                        // Check if any of the ingredients contamines the product.
                        for (int i = 0; i < ingredients.Count; i++)
                        {
                            CompContaminable compContaminable = ingredients[i].TryGetComp<CompContaminable>();
                            if (compContaminable != null)
                            {
                                if (compContaminable.IngredientContaminesProducts)
                                {
                                    compContaminable1.SetContaminated(compContaminable.HediffsContaminedWith);
                                }
                            }
                        }
                    }
                    yield return product;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GenRecipe), "PostProcessProduct")]
        private static bool ReturnBotchedThing(Thing product, Pawn worker, ref Thing __result)
        {
            if (product.def.HasModExtension<ThingDefExtension_BotchableOnMake>())
            {
                ThingDefExtension_BotchableOnMake extension = product.def.GetModExtension<ThingDefExtension_BotchableOnMake>();

                if (Rand.Chance(2f / worker.skills.GetSkill(extension.skillRequirement).Level))
                {
                    int randomIndex = Rand.RangeInclusive(0, extension.botchProducts.Count);

                    Thing botchedThing = ThingMaker.MakeThing(extension.botchProducts[randomIndex].thingDef);
                    botchedThing.stackCount = extension.botchProducts[randomIndex].count;

                    Messages.Message("TM_MessageProductBotched".Translate(worker.LabelShort, product.Label, worker.Named("PAWN"), product.Named("PRODUCT")), worker, MessageTypeDefOf.NegativeEvent);
                    __result = botchedThing;
                    return false;
                }
            }
            return true;
        }

        // Recipe extension to add hediff upon finish.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
        private static void AddHediffPostRecipeCompletion(RecipeDef recipeDef, Pawn worker)
        {
            if (recipeDef.HasModExtension<RecipeDefExtension_HediffOnFinish>())
            {
                RecipeDefExtension_HediffOnFinish extension = recipeDef.GetModExtension<RecipeDefExtension_HediffOnFinish>();

                if (Rand.Chance(extension.chance))
                {
                    if (!worker.health.hediffSet.HasHediff(extension.hediff))
                    {
                        worker.health.AddHediff(extension.hediff);
                        worker.health.hediffSet.GetFirstHediffOfDef(extension.hediff).Severity += extension.severity;
                    }
                    else
                    {
                        worker.health.hediffSet.GetFirstHediffOfDef(extension.hediff).Severity += extension.severity;
                    }
                }
            }
            else
            {
                for (int i = 0; i < recipeDef.ingredients.Count; i++)
                {
                    // Check if the ingredient (Most likely chemfuel) is made of lead.
                }
            }
        }

        // Patch to yield special products. (That should not mess up the vanilla hardcoded butcher and smelt products)
        static MethodInfo postProcessProduct = AccessTools.Method(typeof(GenRecipe), "PostProcessProduct");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
        private static IEnumerable<Thing> MakeSpecialProducts(IEnumerable<Thing> values, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Precept_ThingStyle precept, ThingDefStyle style, int? overrideGraphicIndex)
        {
            if (!recipeDef.HasModExtension<RecipeDefExtension_SpecialProducts>())
            {
                foreach (Thing v in values)
                {
                    yield return v;
                }
            }
            else
            {
                for (int i = 0; i < ingredients.Count; i++)
                {
                    if (!ingredients[i].def.HasModExtension<ThingDefExtension_SpecialProducts>())
                    {
                        Log.Error("Error: " + ingredients[i].def.defName + " doesn't have ThingDefExtension_SpecialProducts.");
                    }
                    else
                    {
                        ThingDefExtension_SpecialProducts extension = ingredients[i].def.GetModExtension<ThingDefExtension_SpecialProducts>();
                        for (int j = 0; j < extension.productTypeDef.products.Count; j++)
                        {
                            int num = Rand.Range(extension.productTypeDef.products[j].Min, extension.productTypeDef.products[j].Max);
                            if (num > 0)
                            {
                                Thing product = ThingMaker.MakeThing(extension.productTypeDef.products[j].thingDef, null);
                                product.stackCount = num;
                                yield return (Thing)postProcessProduct.Invoke(null, new object[] { product, recipeDef, worker, precept, style, overrideGraphicIndex });
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

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
                Log.Warning($"Momentary thing is an instance of {momentaryThing.def.defName}, which is {momentaryThing}");
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

            if (compatibleLists)
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
            Log.Warning($"{__instance} is requesting:");
            foreach (ThingDefCountClass c in __result)
            {
                Log.Warning($"\t- {c.count}x {c.thingDef}");
            }
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
            Log.Warning($"{__instance} is requesting:");
            foreach (ThingDefCountClass c in __result)
            {
                Log.Warning($"\t- {c.count}x {c.thingDef}");
            }
        }

        //Upon destruction, spawn the materials this Building was built with.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GenLeaving), nameof(GenLeaving.DoLeavingsFor), new Type[] { typeof(Thing), typeof(Map), typeof(DestroyMode), typeof(CellRect), typeof(Predicate<IntVec3>), typeof(List<Thing>) })]
        private static IEnumerable<CodeInstruction> ReturnProperMaterials(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (instruction.Calls(AccessTools.Method(typeof(CostListCalculator), nameof(CostListCalculator.CostListAdjusted), new Type[] { typeof(Thing) })))
                {
                    yield return new CodeInstruction(OpCodes.Stloc_S, 12);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 12);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.GetCustomCostListFor), new Type[] { typeof(List<ThingDefCountClass>), typeof(Thing) }));
                }
            }
        }

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

