using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArgonicCore.Commands;
using ArgonicCore.GameComponents;
using RimWorld;
using Verse;

namespace ArgonicCore.Utilities
{
    public static class MaterialExchangingUtility
    {
        public static Command_SelectMaterial SelectMaterialCommand(Blueprint_Build passingBlueprint, Map passingMap, List<ThingDefCountClass> materialGroup, int groupIndex)
        {
            return new Command_SelectMaterial
            {
                defaultDesc = "AC_SelectMaterial".Translate(),
                map = passingMap,
                blueprint = passingBlueprint,
                materialGroup = materialGroup,
                groupIndex = groupIndex
            };
        }

        // Extension methods

        public static List<ThingDef> GetActiveOptionalMaterials(this Blueprint_Build blueprint)
        {
            List<ThingDef> materials = new List<ThingDef>();
            if (GameComponent_ExtendedThings.Instance.optionalMaterialInUse.TryGetValue(blueprint, out materials))
            {
                return materials;
            }
            return null;
        }

        public static void SetActiveOptionalMaterial(this Blueprint_Build blueprint, ThingDef material, int index)
        {
            if (!GameComponent_ExtendedThings.Instance.optionalMaterialInUse.ContainsKey(blueprint))
            {
                GameComponent_ExtendedThings.Instance.optionalMaterialInUse.Add(blueprint, new List<ThingDef>() { null, null });
            }
            GameComponent_ExtendedThings.Instance.optionalMaterialInUse[blueprint][index] = material;
        }
    }
}
