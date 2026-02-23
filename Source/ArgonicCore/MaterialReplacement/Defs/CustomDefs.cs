using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MaterialReplacement.Defs
{
    public sealed class MaterialReplacementDef : Def
    {
        public ThingDef materialToReplace;
        public ThingDef replaceWith;
        public TechLevel maxTechLevel;
        public float costModifier;
        public List<string> exceptionDefs;
        //public List<TerrainDef> exceptionTerrainDefs;
    }
}
