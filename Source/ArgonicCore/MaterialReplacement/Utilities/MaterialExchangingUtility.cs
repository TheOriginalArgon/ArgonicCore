using System;
using System.Collections.Generic;
using System.Linq;
using ArgonicCore.Commands;
using ArgonicCore.Defs;
using ArgonicCore.GameComponents;
using MaterialReplacement.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArgonicCore.Utilities
{
    public static class MaterialExchangingUtility
    {
        public static Command_SelectMaterial SelectMaterialCommand(Thing passingThing, Map passingMap, ThingDef material, List<ThingDef> options)
        {
            return new Command_SelectMaterial
            {
                defaultDesc = "AC_SelectMaterial".Translate(),
                defaultLabel = "AC_MaterialUsing".Translate(passingThing.GetActiveOptionalMaterialFor(material).label),
                map = passingMap,
                thing = passingThing,
                material = material,
                options = options,
                icon = Widgets.GetIconFor(passingThing.GetActiveOptionalMaterialFor(material))
            };
        }

        // Utility methods

        public static TechLevel GetHigherTechLevel(List<ResearchProjectDef> list)
        {
            if (list.NullOrEmpty()) { return TechLevel.Animal; }
            List<int> techLevels = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                techLevels.Add((int)list[i].techLevel);
            }

            if (techLevels.Any())
            {
                return (TechLevel)techLevels.Max();
            }
            else
            {
                return TechLevel.Animal;
            }
        }

        public static bool ExistMaterialsToReplace(ThingDef thingDef, out List<ThingDef> materials)
        {
            // TODO: Cache this somehow.
            List<ThingDef> replacementMaterials = (from x in DefDatabase<MaterialReplacementDef>.AllDefsListForReading where x.materialToReplace == thingDef select x.replaceWith).ToList();

            if (replacementMaterials.Any()) { replacementMaterials.Add(thingDef); materials = replacementMaterials; return true; }
            materials = null;
            return false;
        }

        public static bool ExistMaterialsToReplaceAtTechLevel(ThingDef thingDef, TechLevel techLevel, out List<ThingDef> materials)
        {
            // TODO: Cache this somehow.
            List<ThingDef> replacementMaterials = (from x in DefDatabase<MaterialReplacementDef>.AllDefsListForReading where x.materialToReplace == thingDef && x.maxTechLevel >= techLevel select x.replaceWith).ToList();

            if (replacementMaterials.Any()) { replacementMaterials.Add(thingDef); materials = replacementMaterials; return true; }
            materials = null;
            return false;
        }

        public static float GetCostModifierFor(ThingDef thingDef, ThingDef replacement)
        {
            float costMod = (from x in DefDatabase<MaterialReplacementDef>.AllDefsListForReading where x.materialToReplace == thingDef && x.replaceWith == replacement select x.costModifier).FirstOrDefault();
            return costMod;
        }

        public static bool IsMaterialBeingReplacedIn(ThingDef thingDef, Thing thing)
        {
            return thing.GetActiveOptionalMaterialFor(thingDef) != thingDef;
        }

        public static List<ThingDefCountClass> GetCustomCostListFor(List<ThingDefCountClass> list, Blueprint_Build callingThing)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();

            // TODO: Implement material cost modifiers.
            for (int i = 0; i < list.Count; i++)
            {
                ThingDef material = list[i].thingDef;
                int cost = list[i].count;
                if (IsMaterialBeingReplacedIn(material, callingThing))
                {
                    ThingDef replacementMaterial = GetActiveOptionalMaterialFor(callingThing, material);
                    if (material == callingThing.stuffToUse)
                    {
                        int stuffCost = Mathf.RoundToInt(callingThing.def.entityDefToBuild.CostStuffCount / callingThing.stuffToUse.VolumePerUnit);
                        if (stuffCost < 1)
                        {
                            stuffCost = 1;
                        }
                        int actualMaterialCost = cost - stuffCost;
                        if (actualMaterialCost > 0)
                        {
                            result.Add(new ThingDefCountClass(replacementMaterial, Mathf.RoundToInt(actualMaterialCost * GetCostModifierFor(material, replacementMaterial))));
                        }
                        result.Add(new ThingDefCountClass(callingThing.stuffToUse, stuffCost));
                    }
                    else
                    {
                        result.Add(new ThingDefCountClass(replacementMaterial, Mathf.RoundToInt(cost * GetCostModifierFor(material, replacementMaterial))));
                    }
                }
                else
                {
                    result.Add(list[i]);
                }
            }

            result = MergeList(result);

            //foreach (ThingDefCountClass c in result)
            //{
            //    Log.Warning($" BLUEPRINT: {c.thingDef} x{c.count}");
            //}

            return result;
        }

        public static List<ThingDefCountClass> GetCustomCostListFor(List<ThingDefCountClass> list, Thing callingThing)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();

            //Log.WarningOnce($"{callingThing} is {callingThing.def.defName} and its stuff is {callingThing.Stuff}", 1);

            // TODO: Implement material cost modifiers.
            for (int i = 0; i < list.Count; i++)
            {
                ThingDef material = list[i].thingDef;
                int cost = list[i].count;
                if (IsMaterialBeingReplacedIn(material, callingThing))
                {
                    ThingDef replacementMaterial = GetActiveOptionalMaterialFor(callingThing, material);
                    if (material == callingThing.Stuff)
                    {
                        int stuffCost;
                        if (callingThing is Frame)
                        {
                            stuffCost = Mathf.RoundToInt(callingThing.def.entityDefToBuild.CostStuffCount / callingThing.Stuff.VolumePerUnit);
                        }
                        else
                        {
                            stuffCost = Mathf.RoundToInt(callingThing.def.CostStuffCount / callingThing.Stuff.VolumePerUnit);
                        }
                        if (stuffCost < 1)
                        {
                            stuffCost = 1;
                        }
                        int actualMaterialCost = cost - stuffCost;
                        if (actualMaterialCost > 0)
                        {

                            result.Add(new ThingDefCountClass(replacementMaterial, Mathf.RoundToInt(actualMaterialCost * GetCostModifierFor(material, replacementMaterial))));
                        }
                        result.Add(new ThingDefCountClass(callingThing.Stuff, stuffCost));
                    }
                    else
                    {
                        result.Add(new ThingDefCountClass(replacementMaterial, Mathf.RoundToInt(cost * GetCostModifierFor(material, replacementMaterial))));
                    }
                }
                else
                {
                    result.Add(list[i]);
                }
            }

            result = MergeList(result);

            //foreach (ThingDefCountClass c in result)
            //{
            //    Log.Warning($"THING({callingThing.GetType().Name}): {c.thingDef} x{c.count}");
            //}

            return result;
        }

        public static List<ThingDefCountClass> MergeList(List<ThingDefCountClass> list)
        {
            Dictionary<ThingDef, int> mergedDict = new Dictionary<ThingDef, int>();

            for (int i = 0; i < list.Count; i++)
            {
                if (mergedDict.ContainsKey(list[i].thingDef))
                {
                    mergedDict[list[i].thingDef] += list[i].count;
                }
                else
                {
                    mergedDict[list[i].thingDef] = list[i].count;
                }
            }

            List<ThingDefCountClass> mergedList = mergedDict.Select(kvp => new ThingDefCountClass { thingDef = kvp.Key, count = kvp.Value }).ToList();
            return mergedList;
        }


        // Extension methods


        // Gets the alternative material "y" for a given material "x" associated to a blueprint.
        public static ThingDef GetActiveOptionalMaterialFor(this Thing blueprint, ThingDef material)
        {
            try
            {
                if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.TryGetValue(blueprint, out InnerDict materialInUse))
                {
                    foreach (KeyValuePair<ThingDef, ThingDef> pair in materialInUse.materialValues)
                    {
                        if (pair.Key == material)
                        {
                            //Log.Message($"Retrieved optional material in blueprint {blueprint}: {pair.Value} is replacement for {material}");
                            return pair.Value;
                        }
                    }
                }
            }
            catch (NullReferenceException e)
            {
                Log.Error("[Argonic Core] - Attempted to get null material for" + blueprint.ToString() + " -> " + material.label + ": " + e.Message);
            }
            //Log.Message($"Retrieved {material} as default material in {blueprint}. (Material list has no match.)");
            return material;
        }

        // Sets the alternative material associated to a blueprint.
        public static void SetActiveOptionalMaterialFor(this Thing blueprint, ThingDef material, ThingDef replacement)
        {
            if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.ContainsKey(blueprint))
            {
                if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse[blueprint].materialValues.ContainsKey(material))
                {
                    GameComponent_ExtendedThings.Instance.optionalMaterialInUse[blueprint].materialValues[material] = replacement;
                    //Log.Message($"Set material replacement for {blueprint}: {replacement} now replaces {material}");
                }
                else
                {
                    GameComponent_ExtendedThings.Instance.optionalMaterialInUse[blueprint].materialValues.Add(material, replacement);
                    //Log.Message($"Added material replacement for {blueprint}: {replacement} now replaces {material} (Added material entry just now.)");
                }
            }
            else
            {
                GameComponent_ExtendedThings.Instance.optionalMaterialInUse.Add(blueprint, new InnerDict() { materialValues = new Dictionary<ThingDef, ThingDef> { { material, replacement } } });
                //Log.Message($"Added {blueprint} to dictionary: {replacement} now replaces {material}.");
            }
        }

        // Attempts to retrieve the list of replacement materials for a given Thing from the dictionary.
        public static Dictionary<ThingDef, ThingDef> TryGetMaterialValues(this Thing thing)
        {
            if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.TryGetValue(thing, out InnerDict dict))
            {
                // DEBUG
                //Log.Message($"Retrieved material list of {thing}: {dict.materialValues.Count} entries.");
                //foreach (KeyValuePair<ThingDef, ThingDef> pair in dict.materialValues)
                //{
                //    Log.Message($"\t- {pair.Key} is replaced with {pair.Value}");
                //}

                return dict.materialValues;
            }
            // DEBUG
            //Log.Message($"Couldn't retrieve a dictionary for {thing}, returning an empty one...");

            return new Dictionary<ThingDef, ThingDef>();
        }

        // Sets the list of replacement materials for a given Thing and stores it in the dictionary.
        public static void SetMaterialValues(this Thing thing, Dictionary<ThingDef, ThingDef> values)
        {
            if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.TryGetValue(thing, out InnerDict dict))
            {
                GameComponent_ExtendedThings.Instance.optionalMaterialInUse[thing].materialValues = values;
                // DEBUG
                //Log.Message($"Updated material list for {thing}: {dict.materialValues.Count} entries.");
                //foreach (KeyValuePair<ThingDef, ThingDef> pair in GameComponent_ExtendedThings.Instance.optionalMaterialInUse[thing].materialValues)
                //{
                //    Log.Message($"\t- {pair.Key} was replaced with {pair.Value}");
                //}
                //foreach (KeyValuePair<ThingDef, ThingDef> pair in values)
                //{
                //    Log.Message($"\t- {pair.Key} is now replaced with {pair.Value}");
                //}
            }
            else
            {
                GameComponent_ExtendedThings.Instance.optionalMaterialInUse.Add(thing, new InnerDict() { materialValues = values });
                // DEBUG
                //Log.Message($"Created material replacement list for {thing}");
                //foreach (KeyValuePair<ThingDef, ThingDef> pair in values)
                //{
                //    Log.Message($"\t- Initialized: {pair.Key} is now replaced with {pair.Value}");
                //}
            }
        }
    }
}
