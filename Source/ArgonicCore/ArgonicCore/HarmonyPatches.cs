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

                    var newInstructions_01 = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Conv_R4),
                        new CodeInstruction(OpCodes.Ldloc_1),
                        new CodeInstruction(OpCodes.Ldstr, "ArgonicCore_FuelPower"),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DefDatabase<StatDef>), nameof(DefDatabase<StatDef>.GetNamed), new Type[] { typeof(string), typeof(bool) })),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldc_I4_M1),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue), new Type[] { typeof(Thing), typeof(StatDef), typeof(bool), typeof(int) })),
                        new CodeInstruction(OpCodes.Div),
                        new CodeInstruction(OpCodes.Conv_I4)
                    };

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

            var newInstructions_02 = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldstr, "ArgonicCore_FuelPower"),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DefDatabase<StatDef>), nameof(DefDatabase<StatDef>.GetNamed), new Type[] { typeof(string), typeof(bool) })),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ldc_I4_M1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue), new Type[] { typeof(Thing), typeof(StatDef), typeof(bool), typeof(int) })),
                new CodeInstruction(OpCodes.Mul)
            };

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
                    float sv = extension.severity * Mathf.Max(1f - worker.GetStatValue(StatDefOf.ToxicEnvironmentResistance), 0f);
                    if (!worker.health.hediffSet.HasHediff(extension.hediff))
                    {
                        worker.health.AddHediff(extension.hediff);
                        worker.health.hediffSet.GetFirstHediffOfDef(extension.hediff).Severity += sv;
                    }
                    else
                    {
                        worker.health.hediffSet.GetFirstHediffOfDef(extension.hediff).Severity += sv;
                    }
                }
            }
            //else
            //{
            //    for (int i = 0; i < recipeDef.ingredients.Count; i++)
            //    {
            //        //Check if the ingredient (Most likely chemfuel) is made of lead.
            //    }
            //}
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

        private static List<Thing> temp_ingredients;
        private static Pawn temp_worker;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
        private static void RegisterIngredients(Pawn worker, List<Thing> ingredients)
        {
            temp_ingredients = ingredients;
            temp_worker = worker;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GenRecipe), "PostProcessProduct")]
        private static void InheritQuality(Thing __result, Pawn worker)
        {
            if (__result.HasComp<CompQuality>() && __result.def.HasModExtension<ThingDefExtension_InheritsQuality>())
            {
                ThingDefExtension_InheritsQuality ext = __result.def.GetModExtension<ThingDefExtension_InheritsQuality>();
                if (temp_ingredients.Any(x => x.def == ext.keyIngredient) && temp_worker == worker)
                {
                    Thing keyIng = temp_ingredients.First(x => x.def == ext.keyIngredient);
                    if (keyIng.HasComp<CompQuality>())
                    {
                        __result.TryGetComp<CompQuality>().SetQuality(keyIng.TryGetComp<CompQuality>().Quality, ArtGenerationContext.Colony);
                        temp_ingredients = null;
                        temp_worker = null;
                    }
                }
            }
        }
    }
    #endregion
}

