using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.Comps
{
    public class CompCoatableWall : ThingComp
    {
        public CompProperties_CoatableWall Props => (CompProperties_CoatableWall)props;

        private bool isCoated = false;

        public bool IsCoated => isCoated;

    }

    public class CompProperties_CoatableWall : CompProperties
    {
        public ThingDef coatingResource;
        public ThingDef coatedThingDef;
        public int coatingAmount = 1;

        public CompProperties_CoatableWall()
        {
            compClass = typeof(CompCoatableWall);
        }
    }
}
