using RimWorld;
using Verse;

namespace ArgonicCore.Hediffs
{
    public class Hediff_PoisonOld : HediffWithComps
    {
        private float intervalFactor;
        private float tendQualityTotal;
        private float timeFactor;

        public override void PostMake()
        {
            base.PostMake();
            intervalFactor = Rand.Range(0.1f, 0.5f);
            timeFactor = Rand.Range(0.93f, 1.12f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref intervalFactor, "intervalFactor", 0f, false);
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick((int)(5000f * intervalFactor)))
            {
                float extraSeverity = Rand.Range(-0.003f, 0.053f) * timeFactor;
                Severity += extraSeverity;
                timeFactor += Rand.Range(0.01f, 0.07f);
                tendQualityTotal -= extraSeverity * Rand.Range(0.63f, 0.93f);
            }
        }

        public override bool TendableNow(bool ignoreTimer = false)
        {
            return tendQualityTotal <= 0;
        }

        public override void Tended(float quality, float maxQuality, int batchPosition = 0)
        {
            base.Tended(quality, maxQuality, batchPosition);
            float num = 0.05f * quality;
            if (Rand.Value < num)
            {
                if (batchPosition == 0 && pawn.Spawned)
                {
                    tendQualityTotal += num * 0.48f;
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TextMote_TreatSuccess".Translate(num.ToStringPercent()), 6.5f);
                }
                Severity -= 0.433f;
                return;
            }
            if (batchPosition == 0 && pawn.Spawned)
            {
                tendQualityTotal += num;
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TextMote_TreatFailed".Translate(num.ToStringPercent()), 6.5f);
            }
        }
    }
}
