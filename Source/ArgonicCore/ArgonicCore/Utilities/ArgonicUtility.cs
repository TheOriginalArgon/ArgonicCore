using ArgonicCore.Comps;
using ArgonicCore.ModExtensions;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace ArgonicCore.Utilities
{
    public static class ArgonicUtility
    {

        static MethodInfo postProcessProduct = AccessTools.Method(typeof(GenRecipe), "PostProcessProduct");

        public static IEnumerable<Thing> RandomProductYield(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Precept_ThingStyle precept, ThingDefStyle style, int? overrideGraphicIndex)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (!ingredients[i].def.HasModExtension<ThingDefExtension_SpecialProducts>())
                {
                    Log.Error("Error: " + ingredients[i].def.defName + " doesn't have ThingDefExtension_SpecialProducts.");
                }
                else
                {
                    ThingDefExtension_SpecialProducts extension = ingredients[i].def.GetModExtension<ThingDefExtension_SpecialProducts>();
                    for (int j = 0; j < extension.productTypeDef.products.Count; j++)
                    {
                        int num = Rand.Range(extension.productTypeDef.products[j].Min, extension.productTypeDef.products[j].Max);
                        if (num > 0)
                        {
                            Thing product = ThingMaker.MakeThing(extension.productTypeDef.products[j].thingDef, null);
                            product.stackCount = num;
                            yield return (Thing)postProcessProduct.Invoke(null, new object[] { product, recipeDef, worker, precept, style, overrideGraphicIndex });
                        }
                    }
                }
            }
        }

        // THIS HAS FIXED INGREDIENTS. THEY'RE FIXED.
        public static IEnumerable<Thing> GetQualityModifiedThings(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Precept_ThingStyle precept, ThingDefStyle style, int? overrideGraphicIndex)
        {
            ThingDef thingDef;
            List<ThingDef> products;
            RecipeDefExtension_QualityProduct extension = recipeDef.GetModExtension<RecipeDefExtension_QualityProduct>();
            if (extension != null)
            {
                products = extension.qualityProducts;
                QualityCategory baseQuality = QualityUtility.GenerateQualityCreatedByPawn(worker, recipeDef.workSkill);
                switch (baseQuality)
                {
                    case QualityCategory.Awful: thingDef = products[0]; break;
                    case QualityCategory.Poor: thingDef = products[1]; break;
                    case QualityCategory.Normal: thingDef = products[2]; break;
                    case QualityCategory.Good: thingDef = products[3]; break;
                    case QualityCategory.Excellent: thingDef = products[4]; break;
                    case QualityCategory.Masterwork: thingDef = products[5]; break;
                    case QualityCategory.Legendary: thingDef = products[6]; break;
                    default: thingDef = products[2]; break;
                }
                yield return ThingMaker.MakeThing(thingDef, null);
            }
        }

        #region Wall Coating

        public static List<Thing> FindNearbyResource(Pawn pawn, ThingDef resource, bool forced = false)
        {
            List<Thing> list = new List<Thing>();
            List<Thing> list2 = pawn.Map.listerThings.ThingsOfDef(resource);
            for (int i = 0; i < list2.Count; i++)
            {
                if (!list2[i].IsForbidden(pawn) && pawn.CanReserveAndReach(list2[i], PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced))
                {
                    list.Add(list2[i]);
                }
            }
            return list;
        }


        // Might eventually need. This will only work if I make coated walls work as the smoothed walls do.

        //private static bool IsBlocked(IntVec3 pos, Map map)
        //{
        //    if (!pos.InBounds(map))
        //    {
        //        return false;
        //    }
        //    if (pos.Walkable(map))
        //    {
        //        return false;
        //    }
        //    Building edifice = pos.GetEdifice(map);
        //    if (edifice == null)
        //    {
        //        return false;
        //    }
        //    if (!edifice.def.IsSmoothed)
        //    {
        //        return edifice.def.building.isNaturalRock;
        //    }
        //    return true;
        //}

        //public static void Notify_CoatedByPawn(Thing t, Pawn p)
        //{
        //    for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
        //    {
        //        IntVec3 c = t.Position + GenAdj.CardinalDirections[i];
        //        if (!c.InBounds(t.Map))
        //        {
        //            continue;
        //        }
        //        Building edifice = c.GetEdifice(t.Map);
        //        if (edifice == null || !edifice.def.HasComp<CompCoatableWall>())
        //        {
        //            continue;
        //        }
        //        bool flag = true;
        //        int num = 0;
        //        for (int j = 0; j < GenAdj.CardinalDirections.Length; j++)
        //        {
        //            IntVec3 intVec = edifice.Position + GenAdj.CardinalDirections[j];
        //            if (!IsBlocked(intVec, t.Map))
        //            {
        //                flag = false;
        //                break;
        //            }
        //            Building edifice2 = intVec.GetEdifice(t.Map);
        //            if (edifice2 != null && edifice2.def.IsSmoothed)
        //            {
        //                num++;
        //            }
        //        }
        //        if (!flag || num < 2)
        //        {
        //            continue;
        //        }
        //        for (int k = 0; k < GenAdj.DiagonalDirections.Length; k++)
        //        {
        //            if (!IsBlocked(edifice.Position + GenAdj.DiagonalDirections[k], t.Map))
        //            {
        //                CoatWall(edifice, p);
        //                break;
        //            }
        //        }
        //    }
        //}

        public static Thing CoatWall(Thing target, Pawn coater)
        {
            Map map = target.Map;
            target.Destroy(DestroyMode.WillReplace);
            Thing thing = ThingMaker.MakeThing(target.TryGetComp<CompCoatableWall>().Props.coatedThingDef);
            thing.SetFaction(coater?.Faction ?? Faction.OfPlayer);
            GenSpawn.Spawn(thing, target.Position, map, target.Rotation);
            return thing;
        }

        #endregion
    }
}
