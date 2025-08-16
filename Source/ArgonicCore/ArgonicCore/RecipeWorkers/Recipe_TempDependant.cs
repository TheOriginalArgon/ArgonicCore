using ArgonicCore.Comps;
using ArgonicCore.ModExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.RecipeWorkers
{
    public class Recipe_TempDependant : RecipeWorker
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            RecipeDefExtension_TempRecipe extension = recipe.GetModExtension<RecipeDefExtension_TempRecipe>();
            if (!(thing is Building building))
            {
                //Log.Warning("thing is not building");
                return false;
            }
            CompTemperatureWorkstation tempComp = building.GetComp<CompTemperatureWorkstation>();
            if (tempComp == null)
            {
                //Log.Warning("comp in building is null");
                return false;
            }
            if (extension == null)
            {
                //Log.Warning("extension in recipe is null");
                return false;
            }
            if (tempComp.CurrentTemperature < extension.minTemperature || tempComp.CurrentTemperature > extension.maxTemperature)
            {
                //Log.Warning("temperature not matching");
                return false;
            }
            return true;
        }
    }
}
