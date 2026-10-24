using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArgonicCore.IngestionOutcomes
{
    public class IngestionOutcomeDoer_CanGiveHediff : IngestionOutcomeDoer
    {
        public HediffDef hediffDef;
        public bool dependsOnQuality;
        public float baseChance;
        public float severity;

        public float qualityFactorAwful = 1f;
        public float qualityFactorPoor = 1f;
        public float qualityFactorNormal = 1f;
        public float qualityFactorGood = 1f;
        public float qualityFactorExcellent = 1f;
        public float qualityFactorMasterwork = 1f;
        public float qualityFactorLegendary = 1f;

        private bool divideByBodySize;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
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
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, null);
                float num;
                if (severity > 0f)
                {
                    num = severity;
                }
                else
                {
                    num = hediffDef.initialSeverity;
                }
                if (divideByBodySize)
                {
                    num /= pawn.BodySize;
                }
                //AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize_NewTemp(pawn, this.toleranceChemical, ref num, this.multiplyByGeneToleranceFactors);
                hediff.Severity = num;
                pawn.health.AddHediff(hediff, null, null, null);
            } 
        }

        // This is a little bit cargo-cult but yeah maybe doesn't affect much.
        //public override IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef)
        //{
        //    return Enumerable.Empty<StatDrawEntry>();
        //}
    }
}
