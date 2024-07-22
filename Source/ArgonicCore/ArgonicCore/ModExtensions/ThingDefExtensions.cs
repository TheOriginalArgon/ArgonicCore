using ArgonicCore.Defs;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ArgonicCore.ModExtensions
{
    // Add to things that can be botched upon making.
    public class ThingDefExtension_BotchableOnMake : DefModExtension
    {
        public List<ThingDefCountClass> botchProducts;
        public SkillDef skillRequirement;
    }

    // Add to resources that have special product drops.
    public class ThingDefExtension_SpecialProducts : DefModExtension
    {
        public SpecialProductTypeDef productTypeDef;
    }
}
