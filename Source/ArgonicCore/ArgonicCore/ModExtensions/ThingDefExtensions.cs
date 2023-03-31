using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
