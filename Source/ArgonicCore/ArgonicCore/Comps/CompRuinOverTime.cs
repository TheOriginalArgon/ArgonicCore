using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using UnityEngine;
using Verse;

namespace ArgonicCore.Comps
{
    public class CompRuinOverTime : CompRottable
    {
        public new CompProperties_RuinOverTime PropsRot => (CompProperties_RuinOverTime)props;
        public override void CompTick()
        {
            Tick(1);
        }

        public override void CompTickRare()
        {
            Tick(250);
        }

        private void Tick(int interval)
        {
            if (!Active)
            {
                return;
            }
            float rotProgress = RotProgress;
            RotProgress += interval;
            if (Stage == RotStage.Rotting && PropsRot.rotDestroys)
            {
                if (parent.IsInAnyStorage() && parent.SpawnedOrAnyParentSpawned)
                {
                    Messages.Message("TM_MessageRuinedInStorage".Translate(parent.Label, parent).CapitalizeFirst(), new TargetInfo(parent.PositionHeld, parent.MapHeld, false), MessageTypeDefOf.NegativeEvent, true);
                    LessonAutoActivator.TeachOpportunity(ConceptDefOf.SpoilageAndFreezers, OpportunityType.GoodToKnow);
                }
                parent.Destroy(DestroyMode.Vanish);
                return;
            }
            //if (Mathf.FloorToInt(rotProgress / 60000f) != Mathf.FloorToInt(RotProgress / 60000f) && this.ShouldTakeRotDamage())
            //{
            //    if (Stage == RotStage.Rotting && PropsRot.rotDamagePerDay > 0f)
            //    {
            //        parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, GenMath.RoundRandom(PropsRot.rotDamagePerDay), 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
            //        return;
            //    }
            //    if (Stage == RotStage.Dessicated && PropsRot.dessicatedDamagePerDay > 0f)
            //    {
            //        parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, GenMath.RoundRandom(PropsRot.dessicatedDamagePerDay), 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true));
            //    }
            //}
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder();
            if (PropsRot.TicksToRotStart - RotProgress > 0f)
            {
                stringBuilder.Append("TM_InspectStringRuining".Translate(Mathf.RoundToInt(PropsRot.TicksToRotStart - RotProgress).ToStringTicksToPeriod(true, false, true, true, false)) + ".");
            }
            return stringBuilder.ToString();
        }
    }

    public class CompProperties_RuinOverTime : CompProperties_Rottable
    {
        public CompProperties_RuinOverTime() => compClass = typeof(CompRuinOverTime);
    }
}
