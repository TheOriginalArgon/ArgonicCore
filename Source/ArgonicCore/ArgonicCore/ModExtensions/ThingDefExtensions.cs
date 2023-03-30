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
        public ThingDef genericThingDef;
    }
}
