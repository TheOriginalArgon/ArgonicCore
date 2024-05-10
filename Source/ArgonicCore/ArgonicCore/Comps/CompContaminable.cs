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
    public class CompContaminable : ThingComp
    {
        CompProperties_Contaminable Props => (CompProperties_Contaminable)props;

        public bool IngredientContaminesProducts => Props.contaminesProducts;

        public bool IsBaseContaminator => Props.isBaseContaminator;

        public List<HediffDef> BaseContaminatorHediffs => Props.hediffs;

        public List<HediffDef> HediffsContaminedWith = new List<HediffDef>();

        private bool isContaminated = false;

        public bool IsContaminatedNow()
        {
            if (IsBaseContaminator)
            {
                return true;
            }
            return isContaminated;
        }


        public void SetContaminated(List<HediffDef> hediffDefs)
        {
            if (!isContaminated) { isContaminated = true; }
            foreach (HediffDef contam in hediffDefs)
            {
                if (!HediffsContaminedWith.Contains(contam))
                {
                    HediffsContaminedWith.Add(contam);
                }
            }
        }

        public override void CompTickRare()
        {
            if (IsBaseContaminator)
            {
                for (int i = 0; i < 4; i++)
                {
                    IntVec3 intVec = parent.Position + GenAdj.CardinalDirections[i];
                    List<Thing> nearbyThings = intVec.GetThingList(parent.Map);

                    for (int j = 0; j < nearbyThings.Count; j++)
                    {
                        CompContaminable compContaminable = nearbyThings[j].TryGetComp<CompContaminable>();
                        if (compContaminable != null)
                        {
                            if (!compContaminable.IsContaminatedNow() && !compContaminable.IsBaseContaminator)
                            {
                                if (Rand.Chance(0.015f * Props.contaminationPower))
                                {
                                    compContaminable.SetContaminated(BaseContaminatorHediffs);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isContaminated, "isContaminated", false, false);
            Scribe_Collections.Look(ref HediffsContaminedWith, "HediffsContaminatedWith", LookMode.Def, Array.Empty<object>());
            if (Scribe.mode == LoadSaveMode.PostLoadInit && HediffsContaminedWith == null)
            {
                HediffsContaminedWith = new List<HediffDef>();
            }
        }

        public override void PostSplitOff(Thing piece)
        {
            base.PostSplitOff(piece);
            CompContaminable compContaminable = piece.TryGetComp<CompContaminable>();
            compContaminable.isContaminated = isContaminated;
            compContaminable.HediffsContaminedWith = HediffsContaminedWith;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (HediffsContaminedWith.Any())
            {
                stringBuilder.Append("String_Contaminated".Translate().Colorize(Color.red));
            }


            return stringBuilder.ToString();
        }

    }

    public class CompProperties_Contaminable : CompProperties
    {
        public CompProperties_Contaminable() => compClass = typeof(CompContaminable);

        public bool contaminesProducts = true;

        public bool isBaseContaminator = false;

        public float contaminationPower = 1f;

        public List<HediffDef> hediffs = new List<HediffDef>();
    }
}
