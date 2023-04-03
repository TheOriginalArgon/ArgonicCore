using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ArgonicCore.Comps;
using ArgonicCore.ModExtensions;
using HarmonyLib;
using RimWorld;
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
    }
}
