using ArgonicCore.Comps;
using ArgonicCore.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArgonicCore.WorkGivers
{
    public class WorkGiver_ConstructCoatWall : WorkGiver_Scanner
    {
        private static List<Thing> tmpRes = new List<Thing>();

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(DefDatabase<DesignationDef>.GetNamed("AC_CoatWall"));
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Designation item in pawn.Map.designationManager.SpawnedDesignationsOfDef(DefDatabase<DesignationDef>.GetNamed("AC_CoatWall")))
            {
                yield return item.target.Thing;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            AcceptanceReport acceptanceReport = ShouldCoatWall(pawn, t, forced, true);
            if (!acceptanceReport)
            {
                if (!acceptanceReport.Reason.NullOrEmpty())
                {
                    JobFailReason.Is(acceptanceReport.Reason);
                }
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompCoatableWall compCoatableWall = t.TryGetComp<CompCoatableWall>();
            tmpRes.Clear();
            tmpRes = ArgonicUtility.FindNearbyResource(pawn, compCoatableWall.Props.coatingResource, forced);
            int stackCountFromThingList = ThingUtility.GetStackCountFromThingList(tmpRes);
            if (!tmpRes.Any())
            {
                return null;
            }
            tmpRes.SortBy((Thing x) => pawn.Position.DistanceToSquared(x.Position));
            int num = 0;
            int costPerWall = compCoatableWall.Props.coatingAmount; // Use cost from comp
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("AC_CoatWall"), t);
            job.AddQueuedTarget(TargetIndex.A, t);
            job.AddQueuedTarget(TargetIndex.B, tmpRes[num]);
            job.countQueue = new List<int> { costPerWall };
            int num2 = Mathf.Min(10 * costPerWall, stackCountFromThingList); // Up to 10 walls, but account for cost
            for (int i = 0; i < 100; i++)
            {
                IntVec3 intVec = t.Position + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(t.Map) || intVec.Fogged(t.Map) || !pawn.CanReach(intVec, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }
                List<Thing> thingList = intVec.GetThingList(t.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing thing = thingList[j];
                    if (thing == t || !ShouldCoatWall(pawn, thing, forced, false) || job.targetQueueA.Contains(thing))
                    {
                        continue;
                    }
                    job.AddQueuedTarget(TargetIndex.A, thing);
                    job.countQueue[0] += costPerWall;
                    if (job.countQueue[0] >= tmpRes[num].stackCount)
                    {
                        num++;
                        if (num >= tmpRes.Count)
                        {
                            break;
                        }
                        job.AddQueuedTarget(TargetIndex.B, tmpRes[num]);
                    }
                }
                if (job.GetTargetQueue(TargetIndex.A).Count * costPerWall >= num2 || num >= tmpRes.Count)
                {
                    break;
                }
            }
            if (job.targetQueueA != null && job.targetQueueA.Count >= 5)
            {
                job.targetQueueA.SortBy((LocalTargetInfo targ) => targ.Cell.DistanceToSquared(pawn.Position));
            }
            return job;
        }

        private AcceptanceReport ShouldCoatWall(Pawn pawn, Thing t, bool forced, bool checkResource)
        {
            CompCoatableWall compCoatableWall = t.TryGetComp<CompCoatableWall>();

            if (t.def.building == null || !t.HasComp<CompCoatableWall>())
            {
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(t, DefDatabase<DesignationDef>.GetNamed("AC_CoatWall")) == null)
            {
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (t.IsBurning())
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            if (checkResource)
            {
                List<Thing> list = pawn.Map.listerThings.ThingsOfDef(compCoatableWall.Props.coatingResource);
                for (int i = 0; i < list.Count; i++)
                {
                    if (!list[i].IsForbidden(pawn) && pawn.CanReserveAndReach(list[i], PathEndMode.ClosestTouch, Danger.Deadly, 1, 1, null, forced))
                    {
                        return true;
                    }
                }
                return "NoIngredient".Translate(compCoatableWall.Props.coatingResource);
            }
            return true;
        }
    }
}
