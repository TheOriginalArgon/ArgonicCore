using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using MonoMod.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArgonicCore
{
    public partial class ArgonicCore : Mod
    {
        
        

        public static Harmony harmony;
        public ArgonicCore(ModContentPack content) : base(content)
        {
            harmony = new Harmony("Argon.Framework");
            harmony.PatchAll();
        }

    }


}
