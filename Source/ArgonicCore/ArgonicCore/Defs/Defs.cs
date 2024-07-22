using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace ArgonicCore.Defs
{
    public sealed class SpecialProductTypeDef : Def
    {
        public List<ThingDefCountRangeClass> products;
    }

    public sealed class MaterialReplacementDef : Def
    {
        public ThingDef materialToReplace;
        public ThingDef replaceWith;
        public TechLevel maxTechLevel;
        public float costModifier;
    }
}
