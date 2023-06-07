using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.Hediffs
{
    public class HediffCompProperties_Poison : HediffCompProperties
    {
        public HediffCompProperties_Poison() => compClass = typeof(HediffComp_Poison);

        public float poisonStrength; // How hard the poison progresses.
        public float poisonStability; // How often the poison can progress.
    }

    public class HediffComp_Poison : HediffComp
    {
        public HediffCompProperties_Poison Props => (HediffCompProperties_Poison)props;

        private float intervalFactor;
        private float cureChance;

        public override void CompPostMake()
        {
            base.CompPostMake();
            intervalFactor = Rand.Range(0.95f, 1.95f); // Random-based interval.
            cureChance = (Props.poisonStability * 0.5f) - (Props.poisonStrength * 0.22f);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn != null && Pawn.IsHashIntervalTick((int)(5000f * intervalFactor * Props.poisonStability)))
            {
                parent.Severity += Rand.Range(0.085f, 0.185f) * Props.poisonStrength;
            }
        }

        public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
        {
            base.CompTended(quality, maxQuality, batchPosition);
            //if (Rand.Chance((cureChance + (maxQuality * (cureChance * 0.5f))) * quality))
            float num = cureChance * quality;
            if (Rand.Value < num)
            {
                if (batchPosition == 0 && parent.pawn.Spawned)
                {
                    MoteMaker.ThrowText(parent.pawn.DrawPos, parent.pawn.Map, "TextMote_TreatSuccess".Translate(num.ToStringPercent()), 6.5f);
                }
                parent.pawn.health.RemoveHediff(parent);
                return;
            }
            if (batchPosition == 0 && parent.pawn.Spawned)
            {
                MoteMaker.ThrowText(parent.pawn.DrawPos, parent.pawn.Map, "TextMote_TreatFailed".Translate(num.ToStringPercent()), 6.5f);
            }
        }
    }
}
