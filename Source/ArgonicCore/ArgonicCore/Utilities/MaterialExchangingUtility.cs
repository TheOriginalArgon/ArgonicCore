using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArgonicCore.Commands;
using ArgonicCore.GameComponents;
using ArgonicCore.ModExtensions;
using RimWorld;
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
                map = passingMap,
                thing = passingThing,
                material = material,
                options = options
            };
        }

        // Utility methods

        public static TechLevel GetHigherTechLevel(List<ResearchProjectDef> list)
        {
            if (list.NullOrEmpty()) { Log.Message("[AC]Max tech level: Animal"); return TechLevel.Animal; }
            List<int> techLevels = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                techLevels.Add((int)list[i].techLevel);
            }

            if (techLevels.Any())
            {
                Log.Message("[AC]Max tech level: " + (TechLevel)techLevels.Max());
                return (TechLevel)techLevels.Max();
            }
            else
            {
                Log.Message("[AC]Max tech level: Animal");
                return TechLevel.Animal;
            }
        }

        public static List<ThingDef> GetMaterialsByTechLevel(ThingDef def, TechLevel max)
        {
            List<ThingDef> materials = new List<ThingDef>();
            ThingDefExtension_InterchangableResource extension = def.GetModExtension<ThingDefExtension_InterchangableResource>();
            for (int i = 0; i < extension.interchangableWith.Count; i++)
            {
                if ((int)extension.techLevels[i] >= (int)max)
                {
                    materials.Add(extension.interchangableWith[i]);
                }
            }
            return materials;
        }

        // Extension methods

        public static ThingDef GetActiveOptionalMaterialFor(this Thing blueprint, ThingDef material)
        {
            try
            {
                if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.TryGetValue(blueprint, out Dictionary<ThingDef, ThingDef> materialInUse))
                {
                    foreach (KeyValuePair<ThingDef, ThingDef> pair in materialInUse)
                    {
                        if (pair.Key == material)
                        {
                            return pair.Value;
                        }
                    }
                }
            }
            catch (NullReferenceException e)
            {
                Log.Error("It's here! " + e.InnerException);
            }
            return material.GetModExtension<ThingDefExtension_InterchangableResource>().defaultThingDef;
        }

        public static void SetActiveOptionalMaterialFor(this Thing blueprint, ThingDef material, ThingDef replacement)
        {
            if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.ContainsKey(blueprint))
            {
                if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse[blueprint].ContainsKey(material))
                {
                    GameComponent_ExtendedThings.Instance.optionalMaterialInUse[blueprint][material] = replacement;
                }
                else
                {
                    GameComponent_ExtendedThings.Instance.optionalMaterialInUse[blueprint].Add(material, replacement);
                }
            }
            else
            {
                GameComponent_ExtendedThings.Instance.optionalMaterialInUse.Add(blueprint, new Dictionary<ThingDef, ThingDef>() { { material, replacement } });
            }
        }

        public static Dictionary<ThingDef, ThingDef> TryGetMaterialValues(this Thing thing)
        {
            if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.TryGetValue(thing, out Dictionary<ThingDef, ThingDef> dict))
            {
                return dict;
            }
            return new Dictionary<ThingDef, ThingDef>();
        }

        public static void SetMaterialValues(this Thing thing, Dictionary<ThingDef, ThingDef> values)
        {
            if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.TryGetValue(thing, out Dictionary<ThingDef, ThingDef> dict))
            {
                GameComponent_ExtendedThings.Instance.optionalMaterialInUse[thing] = values;
            }
            else
            {
                GameComponent_ExtendedThings.Instance.optionalMaterialInUse.Add(thing, values);
            }
        }
    }
}
