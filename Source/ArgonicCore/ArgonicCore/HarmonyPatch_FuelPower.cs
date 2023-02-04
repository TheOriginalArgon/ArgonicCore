using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using RimWorld;
using Verse;

namespace ArgonicCore
{
    [HarmonyPatch]
    public static class HarmonyPatch_FuelPower
    {
        [HarmonyPatch]
        [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new Type[] { typeof(List<Thing>) })]
        public static bool Refuel(List<Thing> fuelThings)
        {
            
        }
    }
}
