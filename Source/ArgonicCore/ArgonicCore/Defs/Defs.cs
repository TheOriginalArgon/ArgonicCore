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
        public List<ThingDef> acceptedThingDefs;
        public List<FloatRange> modifiers;
        public List<ThingDefCountClass> specialProducts;
        public List<ThingDefCountClass> additionalSpecialProducts;
        public float additionalChanceBase;
        public List<float> chanceModifiers;
    }
}
