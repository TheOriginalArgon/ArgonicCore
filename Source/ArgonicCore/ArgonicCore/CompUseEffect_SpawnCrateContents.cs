using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArgonicCore.Comps;
using RimWorld;
using Verse;

namespace ArgonicCore
{
    internal class CompUseEffect_SpawnCrateContents : CompUseEffect
    {
        private CrateLoadoutDef CrateLoadoutDef => parent.GetComp<CompCrate>().CrateLoadout;
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            for (int i = 0; i < CrateLoadoutDef.Contents.Count; i++)
            {
                Thing contentThing = ThingMaker.MakeThing(CrateLoadoutDef.Contents[i].thingDef);
                contentThing.stackCount = CrateLoadoutDef.Contents[i].count;

                GenPlace.TryPlaceThing(contentThing, parent.Position, parent.Map, ThingPlaceMode.Near, null, (IntVec3 p) => p != parent.Position, default);
            }
        }
    }
}
