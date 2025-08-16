using ArgonicCore.Defs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.ModExtensions
{
    // Add to recipes that add a hediff upon completion.
    public class RecipeDefExtension_HediffOnFinish : DefModExtension
    {
        public HediffDef hediff;
        public bool affectsMechanoids = false;
        public float severity;
        public float chance = 1.0f;
    }

    // Add to recipes that yield special products. (I don't know why the hell this is so hardcoded in vanilla)
    public class RecipeDefExtension_SpecialProducts : DefModExtension
    {
        public SpecialProductTypeDef productTypeDef;
    }

    // Add to recipes whose product can vary depending on quality (but the product needs individual defs for quality.)
    public class RecipeDefExtension_QualityProduct : DefModExtension
    {
        public List<ThingDef> qualityProducts;
    }

    /* NOTE: It would be wise to know if all those different def extensions should be merged into an individual one. At the momen't I'm unaware
     * if this affects performance in some way or if it just feels like clutter but doesn't really affect much.
     */
}
