using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArgonicCore.ModExtensions;
using RimWorld;
using Verse;

namespace ArgonicCore
{
    public class RecipeWorkerCounter_MakeCobblestone : RecipeWorkerCounter
    {
        public override bool CanCountProducts(Bill_Production bill)
        {
            return true;
        }

        public override int CountProducts(Bill_Production bill)
        {
            int num = 0;
            List<ThingDef> childThingDefs = ThingCategoryDef.Named("EM_Cobblestone").childThingDefs;
            for (int i = 0; i < childThingDefs.Count; i++)
            {
                num += bill.Map.resourceCounter.GetCount(childThingDefs[i]);
            }
            return num;
        }

        public override string ProductsDescription(Bill_Production bill)
        {
            return ThingCategoryDef.Named("EM_Cobblestone").label;
        }

        public override bool CanPossiblyStore(Bill_Production bill, ISlotGroup slotGroup)
        {
            foreach (ThingDef allowedThingDef in bill.ingredientFilter.AllowedThingDefs)
            {
                ThingDefExtension_SpecialProducts extension = allowedThingDef.GetModExtension<ThingDefExtension_SpecialProducts>();
                if (extension != null && !extension.SpecialProductsForKey("EM_SpecialProducts_Cobblestone").NullOrEmpty())
                {
                    ThingDef product = extension.SpecialProductsForKey("EM_SpecialProducts_Cobblestone")[0].thingDef;
                    if (!slotGroup.Settings.AllowedToAccept(product))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
