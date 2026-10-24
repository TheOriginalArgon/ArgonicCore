using ArgonicCore.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace ArgonicCore.Comps
{
    public class CompProperties_OneTimeProcessor : CompProperties
    {
        public float daysToProcess = 1f;

        public int minTemperature = -999;
        public int maxTemperature = 999;

        public float minLight = 0f;
        public float maxLight = 1f;

        public float minRainfall = 0f;
        public float maxRainfall = 1f;

        public bool useTemperature = true;
        public bool useLight = true;
        public bool useRainfall = true;

        public bool temperatureRuins = false;
        public bool lightRuins = false;
        public bool rainRuins = false;

        public bool fuelRuins = false;

        public float daysToRuin = 0f;

        public float ruinProgressRate = 1f;

        public ThingDef finishedThingDef;

        public int TicksToProcess => Mathf.RoundToInt(daysToProcess * 60000f);
        public int TicksToRuin => Mathf.RoundToInt(daysToRuin * 60000f);

        public CompProperties_OneTimeProcessor()
        {
            compClass = typeof(CompOneTimeProcessor);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (parentDef.tickerType != TickerType.Normal && parentDef.tickerType != TickerType.Rare)
            {
                yield return "CompOneTimeProcessor requires tickerType to be Normal or Rare.";
            }
        }
    }

    public class CompOneTimeProcessor : ThingComp
    {
        public bool autoDeconstruct = true;

        private float progressInt;
        private float ruinTicksInt;

        public CompProperties_OneTimeProcessor Props => (CompProperties_OneTimeProcessor)props;

        public float ProgressPct => Progress / Props.TicksToProcess;

        public CompRefuelable CompRefuelable => parent.GetComp<CompRefuelable>();

        public float Progress
        {
            get => progressInt;
            set => progressInt = value;
        }

        public int TicksLeftAtCurrentConditions
        {
            get
            {
                float ambientTemperature = parent.AmbientTemperature;
                float lightLevel = parent.Map.skyManager.CurSkyGlow;
                float rainfallLevel = parent.Map.weatherManager.RainRate;
                ambientTemperature = Mathf.RoundToInt(ambientTemperature);
                return TicksToProcessAtConditions(ambientTemperature, lightLevel, rainfallLevel);
            }
        }

        private float UnroofedPercentage
        {
            get
            {
                int cells = 0;
                int roofedCells = 0;
                foreach (IntVec3 cell in parent.OccupiedRect())
                {
                    cells++;
                    if (parent.Map.roofGrid.Roofed(cell)) roofedCells++;
                }
                return (float)(cells - roofedCells) / cells;
            }
        }

        public override void CompTickInterval(int delta)
        {
            TickInterval(delta);
        }

        public override void CompTickRare()
        {
            TickInterval(250);
        }

        private void TickInterval(int delta)
        {
            float ambientTemp = Mathf.RoundToInt(parent.AmbientTemperature);
            float lightLevel = parent.Map.glowGrid.GroundGlowAt(parent.Position);
            float rainfallLevel = UnroofedPercentage == 0f ? 0f : parent.Map.weatherManager.RainRate * UnroofedPercentage;

            float generalRate = 1f;

            bool anyRequirementBlocked = false;
            bool anyRequirementRuins = false;

            if (Props.useTemperature)
            {
                float r = ArgonicUtility.ProcessorProgressRateAtConditions(ambientTemp, Props.minTemperature, Props.maxTemperature);
                if (r > 0f) generalRate *= r;
                else
                {
                    if (Props.temperatureRuins) anyRequirementRuins = true;
                    else anyRequirementBlocked = true;
                }
            }

            if (Props.useLight)
            {
                float r = ArgonicUtility.ProcessorProgressRateAtConditions(lightLevel, Props.minLight, Props.maxLight);
                if (r > 0f) generalRate *= r;
                else
                {
                    if (Props.lightRuins) anyRequirementRuins = true;
                    else anyRequirementBlocked = true;
                }
            }

            if (Props.useRainfall)
            {
                float r = ArgonicUtility.ProcessorProgressRateAtConditions(rainfallLevel, Props.minRainfall, Props.maxRainfall);
                if (r > 0f) generalRate *= r;
                else
                {
                    if (Props.rainRuins) anyRequirementRuins = true;
                    else anyRequirementBlocked = true;
                }
            }

            if (CompRefuelable != null)
            {
                if (CompRefuelable.HasFuel)
                {
                    generalRate *= 1f; // PLACEHOLDER.
                }
                else
                {
                    if (Props.fuelRuins) anyRequirementRuins = true;
                    else anyRequirementBlocked = true;
                }
            }

            if (anyRequirementRuins)
            {
                if (Props.TicksToRuin > 0)
                {
                    ruinTicksInt += delta;
                    if (ruinTicksInt >= Props.TicksToRuin)
                    {
                        IntVec3 pos = parent.Position;
                        Map m = parent.Map;
                        parent.Destroy(DestroyMode.KillFinalize); // DEBUG
                        Messages.Message("AC_MessageProcessingRuined".Translate(parent.LabelCap), new LookTargets(pos, m), MessageTypeDefOf.NegativeEvent);
                    }
                }
            }
            else if (anyRequirementBlocked)
            {
                ruinTicksInt = Mathf.Max(0f, ruinTicksInt - delta);
            }
            else
            {
                Progress += delta * generalRate;
                if (ruinTicksInt > 0f)
                {
                    ruinTicksInt = Mathf.Max(0f, ruinTicksInt - delta);
                }
            }

            Progress = Mathf.Clamp(Progress, 0f, Props.TicksToProcess);

            if (Progress >= Props.TicksToProcess)
            {
                Faction f = parent.Faction;
                IntVec3 pos = parent.Position;
                Map m = parent.Map;
                parent.Destroy(DestroyMode.WillReplace);
                Thing finished = ThingMaker.MakeThing(Props.finishedThingDef);
                finished.SetFaction(f);
                GenSpawn.Spawn(finished, pos, m);
                if (autoDeconstruct && finished.Faction == Faction.OfPlayer)
                {
                    Designation des = new Designation(finished, DesignationDefOf.Deconstruct);
                    m.designationManager.AddDesignation(des);
                }
                Messages.Message("AC_MessageProcessingComplete".Translate(parent.LabelCap), finished, MessageTypeDefOf.PositiveEvent);
            }
        }

        public int TicksToProcessAtConditions(float temp, float light, float rainfall)
        {
            float generalRate = 1f;

            bool anyRequirementBlocked = false;
            bool anyRequirementsRuin = false;

            if (Props.useTemperature)
            {
                float r = ArgonicUtility.ProcessorProgressRateAtConditions(temp, Props.minTemperature, Props.maxTemperature);
                if (r > 0f) generalRate *= r;
                else
                {
                    if (Props.temperatureRuins) anyRequirementsRuin = true;
                    else anyRequirementBlocked = true;
                }
            }

            if (Props.useLight)
            {
                float r = ArgonicUtility.ProcessorProgressRateAtConditions(light, Props.minLight, Props.maxLight);
                if (r > 0f) generalRate *= r;
                else
                {
                    if (Props.lightRuins) anyRequirementsRuin = true;
                    else anyRequirementBlocked = true;
                }
            }

            if (Props.useRainfall)
            {
                float r = ArgonicUtility.ProcessorProgressRateAtConditions(rainfall, Props.minRainfall, Props.maxRainfall);
                if (r > 0f) generalRate *= r;
                else
                {
                    if (Props.rainRuins) anyRequirementsRuin = true;
                    else anyRequirementBlocked = true;
                }
            }

            if (anyRequirementBlocked) return int.MaxValue;

            if (anyRequirementsRuin) return int.MaxValue;

            float ticksLeft = Props.TicksToProcess - Progress;
            return Mathf.RoundToInt(ticksLeft / generalRate);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref progressInt, "progressInt", 0f);
            Scribe_Values.Look(ref ruinTicksInt, "ruinTicksInt", 0f);
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Progress: {ProgressPct.ToStringPercent()}");
            if (DebugSettings.godMode)
                stringBuilder.AppendLine($"Ticks left at current conditions: {TicksLeftAtCurrentConditions}");
            stringBuilder.AppendLine($"Temperature requirements: {Props.minTemperature}ºC ~ {Props.maxTemperature}ºC");
            if (DebugSettings.godMode)
                stringBuilder.AppendLine($"Temperature rate: {ArgonicUtility.ProcessorProgressRateAtConditions(parent.AmbientTemperature, Props.minTemperature, Props.maxTemperature)} Temp: {parent.AmbientTemperature}");
            stringBuilder.AppendLine($"Light requirements: {Props.minLight.ToStringPercent()} ~ {Props.maxLight.ToStringPercent()}");
            if (DebugSettings.godMode)
                stringBuilder.AppendLine($"Light rate: {ArgonicUtility.ProcessorProgressRateAtConditions(parent.Map.skyManager.CurSkyGlow, Props.minLight, Props.maxLight)} Light: {parent.Map.skyManager.CurSkyGlow}");
            stringBuilder.AppendLine($"Rainfall requirements: {Props.minRainfall.ToStringPercent()} ~ {Props.maxRainfall.ToStringPercent()}");
            if (DebugSettings.godMode)
                stringBuilder.AppendLine($"Rainfall rate: {ArgonicUtility.ProcessorProgressRateAtConditions(parent.Map.weatherManager.RainRate, Props.minRainfall, Props.maxRainfall)} Rain: {parent.Map.weatherManager.RainRate}");
            if (DebugSettings.godMode && Props.TicksToRuin > 0)
            {
                float daysSoFar = ruinTicksInt / 60000f;
                float daysNeeded = Props.daysToRuin;
                stringBuilder.AppendLine($"Ruin timer: {daysSoFar:F2}/{daysNeeded:F2} days");
                if (ruinTicksInt >= Props.TicksToRuin)
                    stringBuilder.AppendLine("Ruined.");
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            string str = (autoDeconstruct ? "On".Translate() : "Off".Translate());
            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.isActive = () => autoDeconstruct;
            command_Toggle.toggleAction = delegate
            {
                autoDeconstruct = !autoDeconstruct;
            };
            command_Toggle.defaultLabel = "AC_CommandAutoDeconstructLabel".Translate();
            command_Toggle.defaultDesc = "AC_CommandAutoDeconstructDesc".Translate(str.UncapitalizeFirst().Named("ONOFF"));
            command_Toggle.icon = ContentFinder<Texture2D>.Get("UI/AC_AutoDeconstruct", true);
            command_Toggle.Order = 20f;
            yield return command_Toggle;
        }
    }
}
