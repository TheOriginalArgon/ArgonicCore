using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.Comps
{
    public class CompProperties_HediffAura : CompProperties
    {
        public int radius = 1;
        public int tickInterval = 1000;
        public string hediff = "Plague";
        public string hediffSleeping = "Plague";

        public CompProperties_HediffAura()
        {
            this.compClass = typeof(CompHediffAura);
        }
    }

    public class CompHediffAura : ThingComp
    {
        public List<Pawn> pawnList = new List<Pawn>();
        public Pawn thisPawn;
        public CompProperties_HediffAura Props => (CompProperties_HediffAura)props;

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (!parent.IsHashIntervalTick(Props.tickInterval, delta))
            {
                return;
            }
            thisPawn = parent as Pawn;

            if (thisPawn == null || thisPawn.Map == null || thisPawn.Dead || thisPawn.Downed)
            {
                return;
            }
            foreach (Thing item in GenRadial.RadialDistinctThingsAround(thisPawn.Position, thisPawn.Map, Props.radius, true))
            {
                if (item is Pawn pawn && pawn.IsColonist && !pawn.Dead && pawn != thisPawn)
                {
                    if (thisPawn.Awake())
                    {
                        pawn.health.AddHediff(HediffDef.Named(Props.hediff));
                    }
                    else
                    {
                        pawn.health.AddHediff(HediffDef.Named(Props.hediffSleeping));
                    }
                }
            }
        }
    }
}
