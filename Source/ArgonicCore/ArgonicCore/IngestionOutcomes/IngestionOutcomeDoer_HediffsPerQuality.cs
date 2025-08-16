using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.IngestionOutcomes
{
    public class IngestionOutcomeDoer_HediffsPerQuality : IngestionOutcomeDoer
    {
        public List<HediffDef> hediffDefs;
        private float severity = -1f;
        public ChemicalDef toleranceChemical;
        private bool divideByBodySize;
        public bool multiplyByGeneToleranceFactors;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            CompQuality comp = ingested.TryGetComp<CompQuality>();
            if (comp != null)
            {
                int index;
                switch  (comp.Quality)
                {
                    case QualityCategory.Awful:
                        index = 0;
                        break;
                    case QualityCategory.Poor:
                        index = 1;
                        break;
                    case QualityCategory.Normal:
                        index = 2;
                        break;
                    case QualityCategory.Good:
                        index = 3;
                        break;
                    case QualityCategory.Excellent:
                        index = 4;
                        break;
                    case QualityCategory.Masterwork:
                        index = 5;
                        break;
                    case QualityCategory.Legendary:
                        index = 6;
                        break;
                    default: 
                        index = 2;
                        break;
                }
                Hediff hediff = HediffMaker.MakeHediff(hediffDefs[index], pawn, null);
                float initialSeverity;
                if (severity > 0f)
                {
                    initialSeverity = severity;
                }
                else
                {
                    initialSeverity = hediffDefs[index].initialSeverity;
                }
                AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(pawn, toleranceChemical, ref initialSeverity, multiplyByGeneToleranceFactors, divideByBodySize);
                hediff.Severity = initialSeverity;
                pawn.health.AddHediff(hediff, null, null, null);
            }
        }
    }
}
