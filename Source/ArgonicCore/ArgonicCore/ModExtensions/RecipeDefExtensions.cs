using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.ModExtensions
{
    public class RecipeDefExtension_HediffOnFinish : DefModExtension
    {
        public HediffDef hediff;
        public float severity;
        public float chance = 1.0f;
    }
}
