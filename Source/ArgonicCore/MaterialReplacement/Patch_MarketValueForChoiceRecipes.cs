using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MaterialReplacement.Defs;
using RimWorld;
using Verse;

namespace MaterialReplacement
{
    // Sets CalculableRecipe and CalculatedBaseMarketValue based on vanilla recipe
    [HarmonyPatch]
    public static class Patch_MarketValueForChoiceRecipes
    {
        private static HashSet<ThingDef> added;

        // Interchangable materials from this mod
        private static HashSet<ThingDef> Added => added ??= new HashSet<ThingDef>(
            DefDatabase<MaterialReplacementDef>.AllDefsListForReading.Select(m => m.replaceWith).Where(m => m != null));

        // The slot's materials minus the ones added in this mod
        private static ThingDef Original(IngredientCount slot)
        {
            List<ThingDef> rest = slot.filter.AllowedThingDefs.Except(Added).ToList();
            return rest.Count == 1 ? rest[0] : null;
        }

        public static bool IsFixedForPricing(IngredientCount slot) => slot.IsFixedIngredient || Original(slot) != null;

        public static ThingDef FixedForPricing(IngredientCount slot) => slot.IsFixedIngredient ? slot.FixedIngredient : Original(slot) ?? slot.FixedIngredient;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            // Vanilla's pricing functions that require a single stuff def
            yield return AccessTools.Method(typeof(StatWorker_MarketValue), nameof(StatWorker_MarketValue.CalculableRecipe));
            yield return AccessTools.Method(typeof(StatWorker_MarketValue), nameof(StatWorker_MarketValue.CalculatedBaseMarketValue));
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RedirectSlotReads(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            MethodInfo isFixed = AccessTools.PropertyGetter(typeof(IngredientCount), nameof(IngredientCount.IsFixedIngredient));
            MethodInfo fixedIngredient = AccessTools.PropertyGetter(typeof(IngredientCount), nameof(IngredientCount.FixedIngredient));
            int redirected = 0;
            foreach (CodeInstruction ci in instructions)
            {
                if (ci.Calls(isFixed)) { ci.opcode = System.Reflection.Emit.OpCodes.Call; ci.operand = AccessTools.Method(typeof(Patch_MarketValueForChoiceRecipes), nameof(IsFixedForPricing)); redirected++; }
                else if (ci.Calls(fixedIngredient)) { ci.opcode = System.Reflection.Emit.OpCodes.Call; ci.operand = AccessTools.Method(typeof(Patch_MarketValueForChoiceRecipes), nameof(FixedForPricing)); redirected++; }
                yield return ci;
            }
            if (redirected == 0) Log.Warning("[MaterialReplacement] found no IngredientCount getter calls to redirect in " + original.Name + "; choice-recipe products keep vanilla pricing");
        }
    }
}
