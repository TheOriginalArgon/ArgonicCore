using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

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
            if (Rand.Chance(CureChance(quality)))
            {
                parent.pawn.health.RemoveHediff(parent);
                Messages.Message("TM_BrootPoisoningCured".Translate(parent.pawn.Name.Named("PAWN")), parent.pawn, MessageTypeDefOf.PositiveEvent);
                return;
            }
        }

        private float CureChance(float quality)
        {
            float rnd = Rand.Range(0f, 1f);
            float num = (1 - Props.poisonStrength) * (1 + (rnd / 2) - Props.poisonStability) * (1 + rnd) * quality;
            return num;
        }
    }
}
