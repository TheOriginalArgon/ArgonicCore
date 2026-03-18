using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MaterialReplacement.Defs
{
    public sealed class MaterialReplacementDef : Def
    {
        public ThingDef materialToReplace;
        public ThingDef replaceWith;
        public TechLevel maxTechLevel = TechLevel.Ultra;
        public TechLevel maxRecipeTechLevel = TechLevel.Ultra;
        public float costModifier;
        public List<string> exceptionDefs;
        public bool applyToRecipes = true;
    }
}
