using RimWorld;
using Verse.AI;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArgonicCore.Comps;

namespace ArgonicCore.WorkGivers
{
    public class WorkGiver_ResourceDig : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return (from x in pawn.Map.listerBuildings.allBuildingsColonist where x.HasComp<CompResourceDigger>() select x).ToList(); // This could be optimized later. To be fair I have little understanding of how this works performance-wise.
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                Building building = allBuildingsColonist[i];
                if (building.GetComp<CompResourceDigger>() != null)
                {
                    CompPowerTrader powerComp = building.GetComp<CompPowerTrader>();
                    if (powerComp == null || powerComp.PowerOn)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            if (!(t is Building building))
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            if (!building.TryGetComp<CompResourceDigger>().CanDigNow() || building.TryGetComp<CompResourceDigger>().IsAutomaticDigger)
            {
                return false;
            }
            if (building.IsBurning())
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("AC_ResourceDig"), t, 1500, true);
        }
    }
}
