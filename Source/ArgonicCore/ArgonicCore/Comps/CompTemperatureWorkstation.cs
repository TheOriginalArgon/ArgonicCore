using RimWorld;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArgonicCore.Comps
{
    public class CompProperties_TemperatureWorkstation : CompProperties
    {
        public CompProperties_TemperatureWorkstation()
        {
            compClass = typeof(CompTemperatureWorkstation);
        }
    }

    public class CompTemperatureWorkstation : ThingComp
    {
        private CompRefuelable fuelComp;
        private CompAffectedByFacilities facilitiesComp;
        private float tempModifierFacility;
        private float temperatureTarget;
        private float currentTemperature;

        public float CurrentTemperature
        {
            get
            {
                return currentTemperature;
            }
        }

        private float FacilityTempModifierSum()
        {
            if (facilitiesComp == null || facilitiesComp.LinkedFacilitiesListForReading == null)
                return 0f;

            float sum = 0f;
            foreach (Thing facility in facilitiesComp.LinkedFacilitiesListForReading)
            {
                CompTemperatureFacility tempComp = facility.TryGetComp<CompTemperatureFacility>();
                if (tempComp != null)
                {
                    sum += tempComp.TemperatureIncreaseWithFuel();
                }
            }
            return sum;
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            fuelComp = parent.TryGetComp<CompRefuelable>();
            // I can get the facilities' stats or custom comps here. Still need to make the custom comp for facilities.
            facilitiesComp = parent.TryGetComp<CompAffectedByFacilities>();
            tempModifierFacility = FacilityTempModifierSum();

            temperatureTarget = 800f; // DEBUG
            if (fuelComp == null)
            {
                Log.Error("CompTemperatureWorkstation does not have CompRefuelable.");
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref temperatureTarget, "temperatureTarget", 800f);
            Scribe_Values.Look(ref currentTemperature, "currentTemperature");
        }

        private bool ShouldIncreaseTemperature()
        {
            if (!parent.SpawnedOrAnyParentSpawned)
            {
                return false;
            }
            if (!fuelComp.HasFuel)
            {
                return false;
            }
            float ambientTemperature = parent.AmbientTemperature;
            if (currentTemperature < temperatureTarget)
            {
                return true;
            }
            return false;
        }

        private float TemperatureIncrease()
        {
            return 2f; // Fixed amount for initial testing.
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("EMH_CurrentTemperature".Translate() + ": ");
            sb.AppendLine(currentTemperature.ToStringTemperature("F0"));
            sb.Append("EMH_TargetTemperature".Translate() + ": ");
            sb.AppendLine(temperatureTarget.ToStringTemperature("F0"));
            return sb.ToString();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(60))
            {
                if (ShouldIncreaseTemperature())
                {
                    currentTemperature += TemperatureIncrease() + tempModifierFacility;
                }
                else
                {
                    currentTemperature += -TemperatureIncrease(); // Should use same method and return negative numbers quite probably.
                }
            }

            // Remove recipes that can't be made.
            BillStack stack = (parent as Building_WorkTable).BillStack;
            foreach (Bill b in stack)
            {
                if (b != null && !b.recipe.AvailableOnNow(parent))
                {
                    stack.Delete(b);
                    break;
                }
            }
        }

        // Converts temperatures to adapt to game being able to show both Celsius and Farenheit temperatures.
        protected float RoundToCurrentTempOffset(float celsiusTemp)
        {
            return GenTemperature.ConvertTemperatureOffset(Mathf.RoundToInt(GenTemperature.CelsiusToOffset(celsiusTemp, Prefs.TemperatureMode)), Prefs.TemperatureMode, TemperatureDisplayMode.Celsius);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            float offset1 = RoundToCurrentTempOffset(-100f);
            Command_Action command1 = new Command_Action();
            command1.action = delegate
            {
                ChangeTargetTemperature(offset1);
            };
            command1.defaultLabel = offset1.ToStringTemperatureOffset("F0");
            command1.defaultDesc = "EMH_CommandLowerFurnaceTemp".Translate();
            //command1.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLowerFurnace");
            yield return command1;
            float offset2 = RoundToCurrentTempOffset(100f);
            Command_Action command2 = new Command_Action();
            command2.action = delegate
            {
                ChangeTargetTemperature(offset2);
            };
            command2.defaultLabel = offset2.ToStringTemperatureOffset("F0");
            command2.defaultDesc = "EMH_CommandLowerFurnaceTemp".Translate();
            //command1.icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLowerFurnace");
            yield return command2;
        }

        protected void ChangeTargetTemperature(float offset)
        {
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            temperatureTarget += offset;
            temperatureTarget = Mathf.Clamp(temperatureTarget, 0f, 1100f);
        }
    }
}
