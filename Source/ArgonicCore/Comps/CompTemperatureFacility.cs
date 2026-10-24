using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace ArgonicCore.Comps
{
    public class CompProperties_TemperatureFacility : CompProperties
    {
        public CompProperties_TemperatureFacility()
        {
            compClass = typeof(CompTemperatureFacility);
        }

        public bool increasesTemperatureWithFuel = false;
    }

    public class CompTemperatureFacility : ThingComp
    {
        public CompProperties_TemperatureFacility Props => (CompProperties_TemperatureFacility)props;

        private CompRefuelable fuelComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            fuelComp = parent.TryGetComp<CompRefuelable>();
            if (Props.increasesTemperatureWithFuel && fuelComp == null)
            {
                Log.Error("CompTemperatureFacility increases temperature with fuel but does not have CompRefuelable.");
            }
        }

        // This should be one of many methods used to calculate increases with many factors and then sent to one that is collected by the fueled workstation comp.
        public float TemperatureIncreaseWithFuel()
        {
            // TESTING: Should be more complex calculation.
            if (fuelComp.HasFuel)
            {
                return 1f;
            }
            else
            {
                return 0f;
            }
        }
    }
}
