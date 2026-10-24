using HarmonyLib;
using Verse;

namespace MaterialReplacement
{
    public partial class MaterialReplacement : Mod
    {
        public static Harmony harmony;
        public MaterialReplacement(ModContentPack contentPack) : base(contentPack)
        {
            harmony = new Harmony("Argon.CoreLib.MaterialReplacement");
            harmony.PatchAll();
        }
    }
}
