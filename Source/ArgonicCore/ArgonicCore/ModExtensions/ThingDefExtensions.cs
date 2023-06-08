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
}