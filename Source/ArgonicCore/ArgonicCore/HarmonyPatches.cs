using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
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
    /// <summary>
    /// A Harmony transpiler that patches the refueling code so that the fuel power of a given <see cref="ThingDef"/> is correctly applied.
    /// </summary>
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

        #region Resource Interchangeability
        // Patches for interchangable stuff.

        // Do not merge the costlist when stuff matches other resources.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CostListCalculator), nameof(CostListCalculator.CostListAdjusted), new[] { typeof(BuildableDef), typeof(ThingDef), typeof(bool) })]
        private static IEnumerable<CodeInstruction> AddInterchangableResources(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            Label jumpLabel_01 = generator.DefineLabel();

            int deletion_index1 = -1;
            int deletion_index2 = -1;

            for (int i = 0; i < code.Count; i++)
            {
                // Check for label insertion.
                if (code[i].Calls(AccessTools.Method(typeof(List<ThingDefCountClass>), "Add")) && code[i + 1].opcode == OpCodes.Ldloc_S)
                {
                    code[i + 1].labels.Add(jumpLabel_01);
                }

                // Insert new code.
                if (code[i].opcode == OpCodes.Ldloc_S && code[i + 1].Calls(AccessTools.Method(typeof(List<ThingDefCountClass>), "Add")))
                {
                    deletion_index1 = i - 4;
                    deletion_index2 = i - 8;
                }
            }

            code.RemoveRange(deletion_index1, 2);
            code.RemoveRange(deletion_index2, 2);

            return code.AsEnumerable();
        }

        // Pass the replacement material values to the finished Building once the Frame is completed.
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
        private static IEnumerable<CodeInstruction> AddMaterialsForThing(IEnumerable<CodeInstruction> instructions)
        {
            bool flag1 = false;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (instruction.Calls(AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing))))
                {
                    flag1 = true;
                    continue;
                }
                if (flag1)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.TryGetMaterialValues)));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.SetMaterialValues)));
                    flag1 = false;
                }
            }
        }

        // Upon destruction, spawn the materials this Building was built with.
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
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MaterialExchangingUtility), nameof(MaterialExchangingUtility.GetCustomCostList)));
                }
            }
        }

        // Once a Blueprint is turned into a Frame, pass the corresponding replacement materials to it.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Blueprint_Build), "MakeSolidThing")]
        private static void MakeFrame(Blueprint_Build __instance, ref Thing __result)
        {
            __result.SetMaterialValues(__instance.TryGetMaterialValues());
        }

        // Request the replacement materials (Frame).
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Frame), nameof(Frame.MaterialsNeeded))]
        private static bool MaterialsNeeded(Frame __instance, ref List<ThingDefCountClass> ___cachedMaterialsNeeded, ref List<ThingDefCountClass> __result)
        {
            ___cachedMaterialsNeeded.Clear();
            int stuffIndex = __instance.Stuff == null ? 0 : 1;
            List<ThingDefCountClass> list = __instance.def.entityDefToBuild.CostListAdjusted(__instance.Stuff, true);
            for (int i = 0; i < list.Count - stuffIndex; i++)
            {
                ThingDefCountClass thingDefCountClass = list[i];
                int num;
                int num2;
                if (thingDefCountClass.thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())
                {
                    ThingDef optionalMaterial = __instance.GetActiveOptionalMaterialFor(thingDefCountClass.thingDef);
                    num = __instance.resourceContainer.TotalStackCountOfDef(optionalMaterial);
                    num2 = Mathf.RoundToInt(thingDefCountClass.count * thingDefCountClass.thingDef.GetModExtension<ThingDefExtension_InterchangableResource>().CostModifierFor(optionalMaterial)) - num;
                    if (num2 > 0)
                    {
                        ___cachedMaterialsNeeded.Add(new ThingDefCountClass(__instance.GetActiveOptionalMaterialFor(thingDefCountClass.thingDef), num2));
                    }
                }
                else
                {
                    num = __instance.resourceContainer.TotalStackCountOfDef(thingDefCountClass.thingDef);
                    num2 = thingDefCountClass.count - num;
                    if (num2 > 0)
                    {
                        ___cachedMaterialsNeeded.Add(new ThingDefCountClass(thingDefCountClass.thingDef, num2));
                    }
                }
            }
            if (stuffIndex == 1)
            {
                int num = __instance.resourceContainer.TotalStackCountOfDef(__instance.Stuff);
                int num2 = list[list.Count - 1].count - num;
                if (num2 > 0)
                {
                    ___cachedMaterialsNeeded.Add(new ThingDefCountClass(__instance.Stuff, num2));
                }
            }
            __result = ___cachedMaterialsNeeded;
            return false;
        }

        // Request the replacement materials (Blueprint).
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.MaterialsNeeded))]
        private static bool MaterialsNeeded_Blueprint(Blueprint_Build __instance, ref List<ThingDefCountClass> __result)
        {
            List<ThingDefCountClass> costList = __instance.def.entityDefToBuild.CostListAdjusted(__instance.stuffToUse, true);
            if (!costList.Any(d => d.thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())) { return true; }
            if (__result == null) { __result = new List<ThingDefCountClass>(); }
            if (__result.Any()) { __result.Clear(); }

            // Return the costlist with generic materials
            bool usesStuff = __instance.stuffToUse != null;
            int numStuff = usesStuff ? 1 : 0;

            for (int i = 0; i < costList.Count - numStuff; i++)
            {
                if (costList[i].thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())
                {
                    ThingDef optionalMaterial = __instance.GetActiveOptionalMaterialFor(costList[i].thingDef);
                    __result.Add(new ThingDefCountClass(optionalMaterial, Mathf.RoundToInt(costList[i].count * costList[i].thingDef.GetModExtension<ThingDefExtension_InterchangableResource>().CostModifierFor(optionalMaterial))));
                }
                else
                {
                    __result.Add(costList[i]);
                }
            }
            if (usesStuff) { __result.Add(costList[costList.Count - 1]); }
            return false;
        }

        // Add the material selectors for Blueprints that have exchangeable materials.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.GetGizmos))]
        private static IEnumerable<Gizmo> AddMaterialSelectors(IEnumerable<Gizmo> values, Blueprint_Build __instance)
        {
            List<ThingDefCountClass> costList = __instance.def.entityDefToBuild.CostListAdjusted(__instance.stuffToUse, true);
            foreach (Gizmo gizmo in values) { yield return gizmo; }

            if (__instance.Faction == Faction.OfPlayer)
            {
                int stuffNum = __instance.Stuff == null ? 0 : 1;
                for (int i = 0; i < costList.Count - stuffNum; i++)
                {
                    if (costList[i].thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())
                    {
                        TechLevel techLevel = MaterialExchangingUtility.GetHigherTechLevel(__instance.def.entityDefToBuild.researchPrerequisites);
                        ThingDefExtension_InterchangableResource extension = costList[i].thingDef.GetModExtension<ThingDefExtension_InterchangableResource>();
                        yield return MaterialExchangingUtility.SelectMaterialCommand(__instance, __instance.Map, costList[i].thingDef, extension.MaterialsByTechLevel(techLevel));
                    }
                }
            }
            yield break;
        }
        #endregion

        // Recipe extension to add hediff upon finish.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
        private static void AddHediffPostRecipeCompletion(RecipeDef recipeDef, Pawn worker)
        {
            if (recipeDef != null && recipeDef.HasModExtension<RecipeDefExtension_HediffOnFinish>())
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
        }

        // Patch to yield special products. (That should not mess up the vanilla hardcoded butcher and smelt products)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
        private static IEnumerable<Thing> MakeSpecialProducts(IEnumerable<Thing> values, RecipeDef recipeDef, List<Thing> ingredients)
        {
            if (recipeDef.HasModExtension<RecipeDefExtension_SpecialProducts>())
            {
                SpecialProductTypeDef specialProducts = recipeDef.GetModExtension<RecipeDefExtension_SpecialProducts>().productTypeDef;

                int index = specialProducts.acceptedThingDefs.IndexOf(ingredients[0].def);

                for (int i = 0; i < specialProducts.specialProducts.Count; i++)
                {
                    Thing resourceDrop = ThingMaker.MakeThing(specialProducts.specialProducts[i].thingDef, null);
                    resourceDrop.stackCount = Mathf.RoundToInt(specialProducts.specialProducts[i].count * Rand.Range(specialProducts.modifiers[index].min, specialProducts.modifiers[index].max));

                    if (Rand.Chance(specialProducts.additionalChanceBase + (specialProducts.chanceModifiers[index])))
                    {
                        for (int j = 0; j < specialProducts.additionalSpecialProducts.Count; j++)
                        {
                            Thing extraDrop = ThingMaker.MakeThing(specialProducts.additionalSpecialProducts[j].thingDef, null);
                            extraDrop.stackCount = Mathf.RoundToInt(specialProducts.additionalSpecialProducts[j].count * Rand.Range(specialProducts.modifiers[index].min, specialProducts.modifiers[index].max));
                            yield return extraDrop;
                        }
                    }

                    yield return resourceDrop;
                    yield break;
                }
            }
            foreach (Thing v in values)
            {
                yield return v;
            }
        }
    }
}
