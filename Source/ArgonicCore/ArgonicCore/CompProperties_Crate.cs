using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;

namespace ArgonicCore
{
    public class CompProperties_Crate : CompProperties
    {
        public CrateLoadoutDef crateLoadout;
        public bool isLocked = false;

        public CompProperties_Crate()
        {
            compClass = typeof(CompCrate);
        }
    }
}
