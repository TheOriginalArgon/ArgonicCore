using ArgonicCore.Comps;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.IngestionOutcomes
{
    public class IngestionOutcomeDoer_GiveHediffIfContaminated : IngestionOutcomeDoer_GiveHediff
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            CompContaminable compContaminable = ingested.TryGetComp<CompContaminable>();
            if (compContaminable != null)
            {
                if (compContaminable.IsContaminatedNow())
                {
                    for (int i = 0; i < compContaminable.HediffsContaminedWith.Count; i++)
                    {
                        hediffDef = compContaminable.HediffsContaminedWith[i];
                        base.DoIngestionOutcomeSpecial(pawn, ingested, ingestedCount);
                    }
                }
            }
        }
    }
}
