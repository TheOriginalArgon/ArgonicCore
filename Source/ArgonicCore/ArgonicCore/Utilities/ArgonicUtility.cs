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
    }
}
