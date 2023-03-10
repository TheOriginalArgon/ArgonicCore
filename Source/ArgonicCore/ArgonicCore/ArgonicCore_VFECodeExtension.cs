using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore
{
    // This file contains code from Vanilla Expanded Framework that is yet to be implemented.
    // Note to self: Branch this when you plan to do so.
    public partial class ArgonicCore : Mod
    {
        //private static readonly AccessTools.FieldRef<Designator_Build, BuildableDef> desEntDef =
        //    AccessTools.FieldRefAccess<Designator_Build, BuildableDef>("entDef");

        //private static readonly AccessTools.FieldRef<Designator_Build, ThingDef> desStuffDef =
        //    AccessTools.FieldRefAccess<Designator_Build, ThingDef>("stuffDef");

        //private static readonly AccessTools.FieldRef<Designator_Build, bool> desWriteStuff =
        //    AccessTools.FieldRefAccess<Designator_Build, bool>("writeStuff");

        //private static readonly Action<Designator_Build, Event> desBaseProcessInput =
        //    AccessTools.Method(typeof(Designator_Build), "<>n__0").CreateDelegate<Action<Designator_Build, Event>>();

        //private static readonly AccessTools.StructFieldRef<StatRequest, ThingDef> statReqStuff =
        //    AccessTools.StructFieldRefAccess<StatRequest, ThingDef>("stuffDefInt");

        //private static readonly HashSet<ThingDef> costExtended = new HashSet<ThingDef>();
        //private static readonly HashSet<BuildableDef> requireGodMode = new HashSet<BuildableDef>();
        //private static readonly HashSet<ThingDef> prisonerProof = new HashSet<ThingDef>();
        //internal static HashSet<TerrainDef> customBridges = new HashSet<TerrainDef>();
        //private static readonly HashSet<TerrainDef> foundations = new HashSet<TerrainDef>();
        //private static readonly HashSet<ThingDef> ignoreStuffFor = new HashSet<ThingDef>();
        //private static readonly Dictionary<ThingDef, List<ThingDef>> extraStuffFor = new Dictionary<ThingDef, List<ThingDef>>();

        //public static void AdjustStuff(ref ThingDef stuff)
        //{
        //    if (stuff != null && costExtended.Contains(stuff)) stuff = stuff.GetModExtension<StuffExtension_Cost>().thingDef;
        //}

        //public static void AdjustStuff2(ref ThingDef def)
        //{
        //    AdjustStuff(ref def);
        //}

        //public static void StatIgnoreStuff(ref StatRequest req, StatDef ___stat)
        //{
        //    if (req.HasThing && req.StuffDef != null && ignoreStuffFor.Contains(req.Def) &&
        //        req.Def.GetModExtension<ThingExtension_IgnoreStuffFor>().stats.Contains(___stat))
        //        statReqStuff(ref req) = null;
        //}

        //public static IEnumerable<CodeInstruction> ProcessInputTranspiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var info = AccessTools.Method(typeof(List<FloatMenuOption>), "Add");
        //    foreach (var instruction in instructions)
        //    {
        //        yield return instruction;
        //        if (instruction.Calls(info))
        //        {
        //            yield return new CodeInstruction(OpCodes.Ldloc, 2);
        //            yield return new CodeInstruction(OpCodes.Ldloc, 4);
        //            yield return new CodeInstruction(OpCodes.Ldarg_0);
        //            yield return new CodeInstruction(OpCodes.Ldarg_1);
        //            yield return CodeInstruction.Call(typeof(ArgonicCore), nameof(AddExtras));
        //        }
        //    }
        //}

        //public static void AddExtras(List<FloatMenuOption> list, ThingDef stuff, Designator_Build des, Event ev)
        //{
        //    if (extraStuffFor.ContainsKey(stuff) && !DebugSettings.godMode)
        //    {
        //        var thingDef = (ThingDef)desEntDef(des);
        //        foreach (var localStuffDef in extraStuffFor[stuff])
        //        {
        //            string text;
        //            if (des.sourcePrecept != null)
        //                text = "ThingMadeOfStuffLabel".Translate(localStuffDef.LabelAsStuff, des.sourcePrecept.Label);
        //            else
        //                text = GenLabel.ThingLabel(thingDef, localStuffDef);

        //            text = text.CapitalizeFirst();
        //            list.Add(new FloatMenuOption(text, delegate
        //            {
        //                desBaseProcessInput(des, ev);
        //                Find.DesignatorManager.Select(des);
        //                desStuffDef(des) = localStuffDef;
        //                desWriteStuff(des) = true;
        //            }, localStuffDef)
        //            {
        //                tutorTag = "SelectStuff-" + thingDef.defName + "-" + localStuffDef.defName
        //            });
        //        }
        //    }
        //}

        //public static class ReplaceStuffCompat
        //{
        //    public static bool CountStuffHas(Frame __instance, ref int __result)
        //    {
        //        if (costExtended.Contains(__instance.Stuff))
        //        {
        //            __result = __instance.resourceContainer.TotalStackCountOfDef(__instance.Stuff.GetModExtension<StuffExtension_Cost>().thingDef);
        //            return false;
        //        }

        //        return true;
        //    }

        //    public static void TotalStuffNeeded(ref ThingDef stuff)
        //    {
        //        if (costExtended.Contains(stuff)) stuff = stuff.GetModExtension<StuffExtension_Cost>().thingDef;
        //    }

        //    public static void MaterialsNeeded(List<ThingDefCountClass> __result)
        //    {
        //        foreach (var countClass in __result.Where(countClass => costExtended.Contains(countClass.thingDef)))
        //            countClass.thingDef = countClass.thingDef.GetModExtension<StuffExtension_Cost>().thingDef;
        //    }
        //}
    }

    //public class StuffExtension_Cost : DefModExtension
    //{
    //    public ThingDef thingDef;
    //}

    //public class ThingExtension_IgnoreStuffFor : DefModExtension
    //{
    //    public List<StatDef> stats;
    //}
}
