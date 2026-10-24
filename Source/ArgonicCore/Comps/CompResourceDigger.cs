using ArgonicCore.Commands;
using ArgonicCore.ModExtensions;
using ArgonicCore.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ArgonicCore.Comps
{
    public class CompProperties_ResourceDigger : CompProperties
    {
        public List<string> resourceTags;
        public bool isAutomaticDigger = false;
        public float yieldFactor = 1f;
        public float speedFactor = 1f;

        public CompProperties_ResourceDigger()
        {
            compClass = typeof(CompResourceDigger);
        }
    }

    public class CompResourceDigger : ThingComp
    {
        public CompProperties_ResourceDigger Props => (CompProperties_ResourceDigger)props;

        private CompPowerTrader powerComp;
        private float portionProgress;
        private float portionYieldPercent;
        private const float WorkPerPortionBase = 10000f;
        private float WorkPerPortion => WorkPerPortionBase * (TargetResource?.GetModExtension<ThingDefExtension_DiggableResource>()?.difficultyFactor ?? 1f);

        public float ProgressToNextPortionPercent => portionProgress / WorkPerPortion;
        public List<ThingDef> availableResources;
        public string targetResourceDefName;
        public ThingDef TargetResource => ThingDef.Named(targetResourceDefName);

        public List<string> ResourceTags => Props.resourceTags;
        public bool IsAutomaticDigger => Props.isAutomaticDigger;
        public float YieldFactor => Props.yieldFactor;
        public float SpeedFactor => Props.speedFactor;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            powerComp = parent.TryGetComp<CompPowerTrader>();
            availableResources = new List<ThingDef>();
            foreach (ThingDef resource in ArgonicUtility.AllDiggableResources())
            {
                //Log.Message($"Checking resouarce: {resource.defName}");
                ThingDefExtension_DiggableResource diggableResourceExt = resource.GetModExtension<ThingDefExtension_DiggableResource>();
                if (diggableResourceExt.resourceTags.Any(tag => Props.resourceTags.Contains(tag)))
                {
                    availableResources.Add(resource);
                }
            }
            if (!respawningAfterLoad || targetResourceDefName == null)
            {
                targetResourceDefName = availableResources.FirstOrDefault().defName;
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref targetResourceDefName, "targetResourceDefName");
            Scribe_Values.Look(ref portionProgress, "portionProgress", 0f);
            Scribe_Values.Look(ref portionYieldPercent, "portionYieldPercent", 0f);
        }

        public void DoDigWork(Pawn digger, int delta)
        {
            float miningSpeed;
            float miningYield;
            if (digger != null)
            {
                miningSpeed = digger.GetStatValue(StatDefOf.MiningSpeed);
                miningYield = digger.GetStatValue(StatDefOf.MiningYield);
            }
            else
            {
                miningSpeed = SpeedFactor;
                miningYield = YieldFactor;
            }
            float num = miningSpeed * delta;
            float workPerPortion = WorkPerPortion;
            portionProgress += num;
            portionYieldPercent += num * miningYield / workPerPortion;
            if (portionProgress > workPerPortion)
            {
                TryProducePortion(portionYieldPercent, digger);
                portionProgress = 0f;
                portionYieldPercent = 0f;
            }
        }

        public override void CompTickRare()
        {
            if (IsAutomaticDigger && CanDigNow())
            {
                DoDigWork(null, 250);
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            portionProgress = 0f;
            portionYieldPercent = 0f;
        }

        private void TryProducePortion(float yieldPercent, Pawn digger = null)
        {
            if (TargetResource == null)
            {
                return;
            }
            ThingDefExtension_DiggableResource diggableResourceExt = TargetResource.GetModExtension<ThingDefExtension_DiggableResource>();
            if (diggableResourceExt == null)
            {
                return;
            }
            int stackCount = Mathf.Max(1, GenMath.RoundRandom(diggableResourceExt.minimumPortion * Rand.Range(1f, 2f) * yieldPercent));
            Thing thing = ThingMaker.MakeThing(TargetResource);
            thing.stackCount = stackCount;
            GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near, null, (IntVec3 p) => p != parent.Position && p != parent.InteractionCell);
        }

        public bool CanDigNow()
        {
            if (powerComp != null && !powerComp.PowerOn)
            {
                return false;
            }
            return true;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield return new Command_SelectDiggingResource()
            {
                map = parent.Map,
                compResourceDigger = this
            };
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Produce Portion (100% yield)";
                command_Action.action = delegate
                {
                    TryProducePortion(1f);
                };
                yield return command_Action;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (parent.Spawned)
            {
                return "AC_ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0");
            }
            return null;
        }
    }
}
