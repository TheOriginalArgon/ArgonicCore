using ArgonicCore.Comps;
using ArgonicCore.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ArgonicCore.JobDrivers
{
    public class JobDriver_CoatWall : JobDriver
    {
        private float coatingTime;
        private const TargetIndex CoatingTargetIndex = TargetIndex.A;
        private const TargetIndex ResourceIndex = TargetIndex.B;
        private const float CoatTimeSecondsBase = 6f;
        private Thing CoatTarget => job.GetTarget(CoatingTargetIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(CoatingTargetIndex), job);
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(ResourceIndex), job);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            int resCost = CoatTarget.TryGetComp<CompCoatableWall>().Props.coatingAmount;
            Toil removeBadTargets = Toils_JobTransforms.ClearDespawnedNullOrForbiddenQueuedTargets(CoatingTargetIndex);
            yield return Toils_Jump.JumpIf(removeBadTargets, () => job.GetTargetQueue(ResourceIndex).NullOrEmpty());
            foreach (Toil item in CollectResourceToils())
            {
                yield return item;
            }
            yield return removeBadTargets;
            yield return Toils_JobTransforms.SucceedOnNoTargetInQueue(CoatingTargetIndex);
            yield return Toils_JobTransforms.ExtractNextTargetFromQueue(CoatingTargetIndex);
            yield return Toils_Goto.GotoThing(CoatingTargetIndex, PathEndMode.Touch).JumpIfDespawnedOrNullOrForbidden(CoatingTargetIndex, removeBadTargets).JumpIfThingMissingDesignation(CoatingTargetIndex, DefDatabase<DesignationDef>.GetNamed("AC_CoatWall"), removeBadTargets);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                coatingTime = 0f;
            };
            toil.tickIntervalAction = delegate (int delta)
            {
                pawn.rotationTracker.FaceTarget(CoatTarget);
                coatingTime += pawn.GetStatValue(StatDefOf.WorkSpeedGlobal) / 60f * delta;
                pawn.skills?.Learn(SkillDefOf.Construction, 0.1f * delta);
                if (coatingTime >= CoatTimeSecondsBase)
                {
                    pawn.carryTracker.CarriedThing?.SplitOff(resCost)?.Destroy();
                    Designation designation = CoatTarget.Map.designationManager.DesignationOn(CoatTarget, DefDatabase<DesignationDef>.GetNamed("AC_CoatWall"));
                    if (designation != null)
                    {
                        // Coat the wall. Swap the object.
                        ArgonicUtility.CoatWall(CoatTarget, pawn);
                        Map.designationManager.RemoveDesignation(designation);
                        //CoatTarget.Map.designationManager.RemoveDesignation(designation);
                    }
                    ReadyForNextToil();
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect(EffecterDefOf.ConstructDirt, CoatingTargetIndex); // Maybe change the effect some century.
            toil.WithProgressBar(CoatingTargetIndex, () => coatingTime / CoatTimeSecondsBase, true);
            toil.JumpIfDespawnedOrNullOrForbidden(CoatingTargetIndex, removeBadTargets);
            toil.JumpIfThingMissingDesignation(CoatingTargetIndex, DefDatabase<DesignationDef>.GetNamed("AC_CoatWall"), removeBadTargets);
            toil.activeSkill = () => SkillDefOf.Construction;
            toil.handlingFacing = true;
            yield return toil;
            yield return Toils_Jump.Jump(removeBadTargets);
        }

        private IEnumerable<Toil> CollectResourceToils()
        {
            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(ResourceIndex, false);
            yield return extract;
            Toil jumpIfHaveTargetInQueue = Toils_Jump.JumpIfHaveTargetInQueue(ResourceIndex, extract);
            yield return Toils_Goto.GotoThing(ResourceIndex, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(ResourceIndex).FailOnSomeonePhysicallyInteracting(ResourceIndex);
            yield return Toils_Haul.StartCarryThing(ResourceIndex, true);
            yield return jumpIfHaveTargetInQueue;
        }
    }
}
