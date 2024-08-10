using System.Collections.Generic;
using MaterialReplacement.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace MaterialReplacement.Commands
{
    public class Command_SelectMaterial : Command
    {
        public Thing thing;
        public Map map;
        public ThingDef material;
        public List<ThingDef> options;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            for (int i = 0; i < options.Count; i++)
            {
                ThingDef thisOption = options[i];
                list.Add(new FloatMenuOption("AC_MaterialTo".Translate(options[i].label), () =>
                {
                    try
                    {
                        SetMaterialForThisBlueprint(material, thisOption);
                        icon = thisOption.uiIcon;
                    }
                    catch
                    {
                        Log.Error("This is not working... for some reason... contact Argón immediately if you see this.");
                    }
                }, MenuOptionPriority.Default, null, null, 29f, null, null, true, 0));
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        public void SetMaterialForThisBlueprint(ThingDef material, ThingDef replacement)
        {
            foreach (object thisThing in Find.Selector.SelectedObjects)
            {
                thing = thisThing as Thing;
                if (thing != null)
                {
                    thing.SetActiveOptionalMaterialFor(material, replacement);
                }
            }
        }
    }
}