using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;
using ArgonicCore.Comps;
using ArgonicCore.ModExtensions;
using ArgonicCore.Utilities;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
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

        // Patches for interchangable stuff.

        public static bool IsNecessaryResourceInList(this ThingDef thingDef, List<ThingDefCountClass> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ThingDef thingDef_compare = list[i].thingDef;

                if (thingDef_compare.defName == thingDef.defName) { return true; }
            }

            return false;
        }

        public static bool IsNecessaryResourceInBuilding(this ThingDef thingDef, BuildableDef buildableDef)
        {
            for (int i = 0; i < buildableDef.costList.Count; i++)
            {
                ThingDef thingDef_compare = buildableDef.costList[i].thingDef;

                if (thingDef_compare.defName == thingDef.defName) { return true; }
            }

            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CostListCalculator), nameof(CostListCalculator.CostListAdjusted), new[] { typeof(BuildableDef), typeof(ThingDef), typeof(bool) })]
        private static IEnumerable<CodeInstruction> AddInterchangableResources(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            Label jumpLabel_01 = generator.DefineLabel();

            int insertion_index1 = -1;
            int insertion_index2 = -1;
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
                    insertion_index1 = i + 2; // Inserts behind.
                    insertion_index2 = i - 2;
                    deletion_index1 = i - 4;
                    deletion_index2 = i - 8;
                }
            }

            List<CodeInstruction> codeToAdd = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_S, 5),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDefCountClass), nameof(ThingDefCountClass.thingDef))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Def), nameof(Def.HasModExtension), new Type[] { }, new Type[] { typeof(ThingDefExtension_InterchangableResource) })),
                new CodeInstruction(OpCodes.Brfalse_S, jumpLabel_01),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_S, 5),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches_Generic), nameof(DuplicateCountClass))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ThingDefCountClass>), "AddRange"))
            };

            //code.InsertRange(insertion_index1, codeToAdd);
            //code.InsertRange(insertion_index2, codeToAdd);
            code.RemoveRange(deletion_index1, 2);
            code.RemoveRange(deletion_index2, 2);

            // Debug.
            //foreach (CodeInstruction i in code)
            //{
            //    Log.Message("[AC] " + i.ToString());
            //}

            return code.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Frame), nameof(Frame.MaterialsNeeded))]
        private static bool MaterialsNeeded(Frame __instance, ref List<ThingDefCountClass> ___cachedMaterialsNeeded, ref List<ThingDefCountClass> __result)
        {
            List<ThingDefCountClass> baseCostList = __instance.def.entityDefToBuild.CostListAdjusted(__instance.Stuff, true);
            ThingDefCountClass stuff = baseCostList[baseCostList.Count - 1];
            Log.Message("[AC]Stuff: " + stuff.ToString());
            List<List<ThingDefCountClass>> interchangableGroups = DivideByGroups(baseCostList, __instance.def.entityDefToBuild);
            int amountToSubstractFromStuff = FindStuffDuplicate(__instance.Stuff, interchangableGroups);
            Log.Message("[AC]Substract " + amountToSubstractFromStuff + " from stuff count");

            ___cachedMaterialsNeeded.Clear();

            for (int i = 0; i < interchangableGroups.Count; i++)
            {
                int resourcesHeld = 0;
                for (int j = 0; j < interchangableGroups[i].Count; j++)
                {
                    ThingDefCountClass resource = interchangableGroups[i][j];
                    Log.Message("[AC]Group " + (i + 1) + ": " + resource.ToString());

                    int amount;
                    if (resource.thingDef == __instance.Stuff)
                    {
                        amount = Mathf.Max(0, __instance.resourceContainer.TotalStackCountOfDef(resource.thingDef) - amountToSubstractFromStuff);
                    }
                    else
                    {
                        amount = __instance.resourceContainer.TotalStackCountOfDef(resource.thingDef);
                    }
                    resourcesHeld += amount;
                }

                int resourcesNeeded = interchangableGroups[i][0].count - resourcesHeld;
                if (resourcesNeeded > 0)
                {
                    for (int j = 0; j < interchangableGroups[i].Count; j++)
                    {
                        ThingDefCountClass resource = interchangableGroups[i][j];
                        ___cachedMaterialsNeeded.Add(new ThingDefCountClass(resource.thingDef, resourcesNeeded));
                    }
                }
            }

            int stuffHeld = Mathf.Max(0, __instance.resourceContainer.TotalStackCountOfDef(__instance.Stuff) - amountToSubstractFromStuff);
            int stuffNeeded = stuff.count - stuffHeld;
            if (stuffNeeded > 0)
            {
                ___cachedMaterialsNeeded.Add(new ThingDefCountClass(__instance.Stuff, stuffNeeded));
            }

            __result = ___cachedMaterialsNeeded;
            foreach (ThingDefCountClass c in ___cachedMaterialsNeeded)
            {
                Log.Message("[AC]Requested: " + c.ToString());
            }
            Log.Message("===========");
            foreach (ThingDefCountClass c in baseCostList)
            {
                Log.Message("[AC]Holding: " + __instance.resourceContainer.TotalStackCountOfDef(c.thingDef) + c.thingDef.ToString());
            }
            return false;
        }

        //private int ExtraCostForIngredient(ThingDefCountClass countClass)
        //{

        //}


        [HarmonyPrefix]
        [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.MaterialsNeeded))]
        private static bool MaterialsNeeded_Blueprint(Blueprint_Build __instance, ref List<ThingDefCountClass> __result)
        {
            if (!__result.Any(d => d.thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())) { return true; }

            // Return the costlist with generic materials
            bool usesStuff = __instance.Stuff != null;
            int numStuff = usesStuff ? 1 : 0;

            for (int i = 0; i < __result.Count; i++)
            {
                if (__result[i].thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())
                {
                    //__result[i].thingDef = __instance.GetActiveOptionalMaterials();
                }
            }

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.GetGizmos))]
        private static IEnumerable<Gizmo> AddMaterialSelectors(IEnumerable<Gizmo> values, Blueprint_Build __instance)
        {
            foreach (Gizmo gizmo in values) { yield return gizmo; }

            List<List<ThingDefCountClass>> exchangeableGroups = DivideByGroups(__instance.def.entityDefToBuild.CostListAdjusted(__instance.stuffToUse, true), __instance.def.entityDefToBuild);
            if (__instance.Faction == Faction.OfPlayer)
            {
                for (int i = 0; i < exchangeableGroups.Count; i++)
                {
                    yield return MaterialExchangingUtility.SelectMaterialCommand(__instance, __instance.Map, exchangeableGroups[i], i);
                }
            }
        }

        private static List<ThingDefCountClass> GetActiveMaterialCostList(List<ThingDefCountClass> costList, List<List<ThingDefCountClass>> groups, List<ThingDef> activeMaterials)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();

            for (int i = 0; i < costList.Count - 1; i++)
            {
                if (!costList[i].thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())
                {
                    result.Add(costList[i]);
                }
            }
            for (int i = 0; i < groups.Count - 1; i++)
            {
                result.Add(new ThingDefCountClass(activeMaterials[i], groups[i][0].count));
            }

            result.Add(costList[costList.Count - 1]);
            return result;
        }

        private static List<List<ThingDefCountClass>> DivideByGroups(List<ThingDefCountClass> list, BuildableDef buildableDef)
        {
            List<List<ThingDefCountClass>> result = new List<List<ThingDefCountClass>>();
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (list[i].thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())
                {
                    ThingDefExtension_InterchangableResource extension = list[i].thingDef.GetModExtension<ThingDefExtension_InterchangableResource>();

                    List<ThingDefCountClass> thingDefCounts = new List<ThingDefCountClass>();
                    thingDefCounts.Add(list[i]);
                    for (int j = 0; j < extension.interchangableWith.Count; j++)
                    {
                        if ((int)GetHigherTechLevel(buildableDef.researchPrerequisites) <= (int)extension.techLevels[j])
                        {
                            thingDefCounts.Add(new ThingDefCountClass(extension.interchangableWith[j], list[i].count));
                        }
                    }
                    result.Add(thingDefCounts);
                }
            }

            return result;
        }

        private static int FindStuffDuplicate(ThingDef stuff, List<List<ThingDefCountClass>> groups)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                for (int j = 0; j < groups[i].Count; j++)
                {
                    ThingDefCountClass item = groups[i][j];

                    if (stuff == item.thingDef)
                    {
                        return item.count;
                    }
                }
            }

            return 0;
        }

        public static bool IsWithin(ThingDefCountClass countClass, List<ThingDefCountClass> compare)
        {
            for (int i = 0; i < compare.Count; i++)
            {
                if (compare[i].thingDef.defName == countClass.thingDef.defName &&
                    compare[i].count == countClass.count)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<ThingDefCountClass> DuplicateCountClass(ThingDefCountClass countClass, BuildableDef buildableDef)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            ThingDefExtension_InterchangableResource extension = countClass.thingDef.GetModExtension<ThingDefExtension_InterchangableResource>();
            List<ThingDef> interchangableDefs = extension.interchangableWith;


            for (int i = 0; i < interchangableDefs.Count; i++)
            {
                if ((int)GetHigherTechLevel(buildableDef.researchPrerequisites) <= (int)extension.techLevels[i])
                {
                    result.Add(new ThingDefCountClass(interchangableDefs[i], countClass.count));
                }
            }

            return result;
        }

        public static TechLevel GetHigherTechLevel(List<ResearchProjectDef> list)
        {
            if (list == null) { return TechLevel.Animal; }
            List<int> techLevels = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                techLevels.Add((int)list[i].techLevel);
                Log.Message("added " + list[i].techLevel.ToString());
            }

            if (techLevels.Any()) { return (TechLevel)techLevels.Max(); } else { return TechLevel.Animal; }
        }

        public static List<ThingDefCountClass> ExtendedCostListFor(BuildableDef buildableDef, bool notMe = true)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            List<ThingDefCountClass> thingDefsWithExtension = (from ThingDefCountClass def in buildableDef.CostList where def.thingDef.HasModExtension<ThingDefExtension_InterchangableResource>() select def).ToList();

            for (int i = 0; i < thingDefsWithExtension.Count; i++)
            {
                ThingDefCountClass thingDefCountClass = thingDefsWithExtension[i];
                if (!notMe)
                {
                    result.Add(new ThingDefCountClass(thingDefCountClass.thingDef, thingDefCountClass.count));
                }

                for (int j = 0; j < thingDefCountClass.thingDef.GetModExtension<ThingDefExtension_InterchangableResource>().interchangableWith.Count; j++)
                {
                    result.Add(new ThingDefCountClass(thingDefCountClass.thingDef.GetModExtension<ThingDefExtension_InterchangableResource>().interchangableWith[j], thingDefCountClass.count));
                }
            }

            return result;
        }
    }
}
