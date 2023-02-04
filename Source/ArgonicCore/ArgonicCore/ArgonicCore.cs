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
    public class ArgonicCore : Mod
    {
        public ArgonicCore(ModContentPack content) : base(content)
        {
            harmony = new Harmony("Argon.Framework");
            harmony.PatchAll();
        }

        public static Harmony harmony;
    }
}
