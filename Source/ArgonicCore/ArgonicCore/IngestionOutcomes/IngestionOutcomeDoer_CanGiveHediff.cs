using RimWorld;
using Verse;

namespace ArgonicCore.IngestionOutcomes
{
    internal class IngestionOutcomeDoer_CanGiveHediff : IngestionOutcomeDoer_GiveHediff
    {
        public HediffDef hediff;
        public bool dependsOnQuality;
        public float baseChance;

        public float qualityFactorAwful = 1f;
        public float qualityFactorPoor = 1f;
        public float qualityFactorNormal = 1f;
        public float qualityFactorGood = 1f;
        public float qualityFactorExcellent = 1f;
        public float qualityFactorMasterwork = 1f;
        public float qualityFactorLegendary = 1f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
        {
            float qualityFactor = 1f;

            if (dependsOnQuality)
            {
                CompQuality compQuality = ingested.TryGetComp<CompQuality>();
                if (compQuality == null)
                {
                    Log.Error("Ingestion outcome has dependsOnQuality = true, but ThingDef has no compQuality.");
                }
                switch (compQuality.Quality)
                {
                    case QualityCategory.Awful: qualityFactor = qualityFactorAwful; break;
                    case QualityCategory.Poor: qualityFactor = qualityFactorPoor; break;
                    case QualityCategory.Normal: qualityFactor = qualityFactorNormal; break;
                    case QualityCategory.Good: qualityFactor = qualityFactorGood; break;
                    case QualityCategory.Excellent: qualityFactor = qualityFactorExcellent; break;
                    case QualityCategory.Masterwork: qualityFactor = qualityFactorMasterwork; break;
                    case QualityCategory.Legendary: qualityFactor = qualityFactorLegendary; break;
                    default: qualityFactor = 1f; break;
                }
            }

            if (Rand.Chance(baseChance * qualityFactor))
            {
                hediffDef = hediff;
                base.DoIngestionOutcomeSpecial(pawn, ingested);
            }
        }
    }
}
