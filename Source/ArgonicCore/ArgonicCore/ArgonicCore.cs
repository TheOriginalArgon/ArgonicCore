using HarmonyLib;
using Verse;

namespace ArgonicCore
{
    public partial class ArgonicCore : Mod
    {

        public static Harmony harmony;
        public ArgonicCore(ModContentPack content) : base(content)
        {
            harmony = new Harmony("Argon.CoreLib");
            harmony.PatchAll();
        }

    }


}
