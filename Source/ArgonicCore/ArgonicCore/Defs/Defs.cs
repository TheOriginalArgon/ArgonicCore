using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace ArgonicCore.Defs
{
    public sealed class SpecialProductsDef : Def
    {
        public List<ThingDefCountClass> products;
        public Dictionary<string, List<ThingDefCountClass>> keyedProducts;
    }
}
