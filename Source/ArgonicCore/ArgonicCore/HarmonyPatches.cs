using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ArgonicCore.Comps;
using ArgonicCore.ModExtensions;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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

            int insertion_index = -1;

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
                    insertion_index = i + 2; // Inserts behind.
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

            code.InsertRange(insertion_index, codeToAdd);
            code.InsertRange(insertion_index - 4, codeToAdd);

            // Debug.
            foreach (CodeInstruction i in code)
            {
                Log.Message("[AC] " + i.ToString());
            }

            return code.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Frame), nameof(Frame.MaterialsNeeded))]
        private static bool MaterialsNeeded(Frame __instance, ref List<ThingDefCountClass> ___cachedMaterialsNeeded, ref ThingOwner ___resourceContainer, ref List<ThingDefCountClass> __result)
        {
            ___cachedMaterialsNeeded.Clear();
            List<ThingDefCountClass> list = __instance.def.entityDefToBuild.CostListAdjusted(__instance.Stuff, true);
            for (int i = 0; i < list.Count; i++)
            {
                ThingDefCountClass thingDefCountClass = list[i];

                int num = ___resourceContainer.TotalStackCountOfDef(thingDefCountClass.thingDef);

                if (thingDefCountClass.thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())
                {
                    List<ThingDef> interchangableDefs = thingDefCountClass.thingDef.GetModExtension<ThingDefExtension_InterchangableResource>().interchangableWith;
                    for (int j = 0; j < interchangableDefs.Count; j++)
                    {
                        num += ___resourceContainer.TotalStackCountOfDef(interchangableDefs[j]);
                    }
                }
                else
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].thingDef.HasModExtension<ThingDefExtension_InterchangableResource>())
                        {
                            List<ThingDef> thingDefs_compare = list[j].thingDef.GetModExtension<ThingDefExtension_InterchangableResource>().interchangableWith;

                            if (thingDefs_compare.Contains(thingDefCountClass.thingDef))
                            {
                                num += ___resourceContainer.TotalStackCountOfDef(list[j].thingDef);
                            }
                        }
                    }
                }

                int num2 = thingDefCountClass.count - num;
                if (num2 > 0)
                {
                    ___cachedMaterialsNeeded.Add(new ThingDefCountClass(thingDefCountClass.thingDef, num2));
                }
            }
            __result = ___cachedMaterialsNeeded;

            return false;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Frame), nameof(Frame.MaterialsNeeded))]
        //private static bool MaterialsNeeded(Frame __instance, ref List<ThingDefCountClass> ___cachedMaterialsNeeded, ref ThingOwner ___resourceContainer, ref List<ThingDefCountClass> __result)
        //{
        //    ___cachedMaterialsNeeded.Clear();
        //    List<ThingDefCountClass> list = __instance.def.entityDefToBuild.CostListAdjusted(__instance.Stuff, true);
        //    for (int i = 0; i < list.Count; i++)
        //    {
        //        ThingDefCountClass thingDefCountClass = list[i]; // Iterator.

        //        int num = ___resourceContainer.TotalStackCountOfDef(thingDefCountClass.thingDef);
        //        int num2 = thingDefCountClass.count - num;

        //        if (num2 > 0 && thingDefCountClass.thingDef.IsNecessaryResourceInList(__instance.def.entityDefToBuild.costList))
        //        {
        //            ___cachedMaterialsNeeded.Add(new ThingDefCountClass(thingDefCountClass.thingDef, num2));
        //        }
        //    }
        //    __result = ___cachedMaterialsNeeded;

        //    return false;
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Blueprint_Build), nameof(Blueprint_Build.GetGizmos))]
        private static IEnumerable<Gizmo> AddMaterialSelectors(IEnumerable<Gizmo> values, Blueprint_Build __instance)
        {
            if (__instance.Faction == Faction.OfPlayer)
            {
                ThingDef thingDef;
                if ((thingDef = __instance.def.entityDefToBuild as ThingDef) != null)
                {
                    List<ThingDefCountClass> duplicated = new List<ThingDefCountClass>();
                    for (int i = 0; i < __instance.def.CostList.Count; i++)
                    {
                        duplicated.AddRange(DuplicateCountClass(__instance.def.costList[i], __instance.def.entityDefToBuild));
                    }

                    for (int i = 0; i < duplicated.Count; i++)
                    {
                        Command_Action action_material = new Command_Action
                        {
                            defaultLabel = "String_SelectMaterial".Translate().CapitalizeFirst(),
                            defaultDesc = "String_SelectMaterial_desc".Translate().CapitalizeFirst(),
                            // TODO: Icon
                            Order = 20f,
                            action = () =>
                            {

                            }
                        };
                    }
                }
            }
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
    }
}
