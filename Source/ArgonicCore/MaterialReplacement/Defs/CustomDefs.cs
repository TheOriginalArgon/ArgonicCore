using RimWorld;
using Verse;

namespace MaterialReplacement.Defs
{
    public sealed class MaterialReplacementDef : Def
    {
        public ThingDef materialToReplace;
        public ThingDef replaceWith;
        public TechLevel maxTechLevel;
        public float costModifier;
    }
}
