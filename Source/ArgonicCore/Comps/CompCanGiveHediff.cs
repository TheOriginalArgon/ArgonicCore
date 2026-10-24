//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse;

//namespace ArgonicCore.Comps
//{
//    public class CompCanGiveHediff : ThingComp
//    {
//        public CompProperties_CanGiveHediff Props => (CompProperties_CanGiveHediff)props;

//        private HediffDef Hediff => Props.hediff;
//        private bool DependsOnQuality => Props.dependsOnQuality;
//        private float BaseChance => Props.baseChance;

//        public float QualityFactor
//        {
//            get
//            {
//                switch (parent.TryGetComp<CompQuality>().Quality)
//                {
//                    case QualityCategory.Awful: return Props.qualityFactorAwful;
//                    case QualityCategory.Poor: return Props.qualityFactorPoor;
//                    case QualityCategory.Normal: return Props.qualityFactorNormal;
//                    case QualityCategory.Good: return Props.qualityFactorGood;
//                    case QualityCategory.Excellent: return Props.qualityFactorExcellent;
//                    case QualityCategory.Masterwork: return Props.qualityFactorMasterwork;
//                    case QualityCategory.Legendary: return Props.qualityFactorLegendary;
//                    default: return 1f;
//                }
//            }
//        }

//        public override void PostIngested(Pawn ingester)
//        {
//            if (DependsOnQuality)
//            {
//                if (Rand.Chance(BaseChance * QualityFactor))
//                {
//                    ingester.health.AddHediff(Hediff);
//                }
//            }
//        }

//    }

//    public class CompProperties_CanGiveHediff : CompProperties
//    {
//        public CompProperties_CanGiveHediff() => compClass = typeof(CompCanGiveHediff);

//        public HediffDef hediff;
//        public bool dependsOnQuality;
//        public float baseChance;

//        public float qualityFactorAwful = 1;
//        public float qualityFactorPoor = 1;
//        public float qualityFactorNormal = 1;
//        public float qualityFactorGood = 1;
//        public float qualityFactorExcellent = 1;
//        public float qualityFactorMasterwork = 1;
//        public float qualityFactorLegendary = 1;
//    }
//}
