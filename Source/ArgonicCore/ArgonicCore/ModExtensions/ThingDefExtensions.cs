using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ArgonicCore.ModExtensions
{
    public class ThingDefExtension_BotchableOnMake : DefModExtension
    {
        public List<ThingDefCountClass> botchProducts;
        public SkillDef skillRequirement;
    }

    public class ThingDefExtension_InterchangableResource : DefModExtension
    {
        public List<ThingDef> interchangableWith;
        public List<TechLevel> techLevels;
        public List<float> costModifiers;
        public ThingDef defaultThingDef;

        public List<ThingDef> MaterialsByTechLevel(TechLevel techLevel)
        {
            List<ThingDef> ret = new List<ThingDef>();
            for (int i = 0; i < interchangableWith.Count; i++)
            {
                if ((int)techLevels[i] >= (int)techLevel)
                {
                    ret.Add(interchangableWith[i]);
                }
            }
            ret.Add(defaultThingDef);
            return ret;
        }

        public float CostModifierFor(ThingDef material)
        {
            if (!interchangableWith.Contains(material)) { return 1f; }
            else
            {
                int index = interchangableWith.IndexOf(material);
                return costModifiers[index];
            }
        }
    }
}
