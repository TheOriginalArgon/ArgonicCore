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
        public float severity;
        public float chance = 1.0f;
    }

    // Add to recipes that yield special products. (I don't know why the hell this is so hardcoded in vanilla)
    public class RecipeDefExtension_SpecialProducts : DefModExtension
    {
        public SpecialProductTypeDef productTypeDef;
    }
}
