using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArgonicCore.RecipeWorkers
{
    public class Recipe_LowerSeverity : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part)) return false;
            if (!(thing is Pawn pawn))
            {
                //Log.Warning($"This recipe is not available for {thing.def.defName}");
                return false;
            }
            if (pawn.health.hediffSet.HasHediff(recipe.removesHediff, true))
            {
                //Log.Warning($"This recipe is available for {pawn.Name}");
                return true;
            }
            return false;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            float num;
            num = Mathf.Lerp(0f, Mathf.Max(billDoer.skills.GetSkill(SkillDefOf.Medicine).Level, 1f), Rand.Range(0.2f, 0.8f));
            if (billDoer != null)
            {
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[] { billDoer, pawn });
                if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(billDoer))
                {
                    string text;
                    text = "AC_MessageSuccesfullyTreatedHediff".Translate(billDoer.LabelShort, pawn.LabelShort, recipe.removesHediff.label.Named("HEDIFF"), billDoer.Named("SURGEON"), pawn.Named("PATIENT"), num.Named("AMOUNT"));
                    Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }
            Hediff hediff = pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == recipe.removesHediff && x.Part == part && x.Visible);
            if (hediff != null)
            {
                hediff.Severity -= num / 100f;
                if (hediff.Severity < 0.15f && Rand.Range(0f, 1f) <= 0.25f)
                {
                    pawn.health.hediffSet.hediffs.Remove(hediff);
                }
            }
        }
    }
}
