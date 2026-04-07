using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MaterialReplacement.Commands
{
    /// <summary>
    /// Gizmo command for selecting replacement material on blueprints/frames.
    /// Uses SyncedActions for MP compatibility.
    /// </summary>
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
                list.Add(new FloatMenuOption(
                    "AC_MaterialTo".Translate(thisOption.label),
                    () => SetMaterialForSelected(material, thisOption),
                    MenuOptionPriority.Default, null, null, 29f, null, null, true, 0));
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        private void SetMaterialForSelected(ThingDef originalMaterial, ThingDef replacement)
        {
            foreach (object obj in Find.Selector.SelectedObjects)
            {
                if (obj is Thing t)
                {
                    // Calls synced method - MP will execute on all clients
                    SyncedActions.SyncSetMaterial(t, originalMaterial, replacement);
                }
            }
            icon = replacement.uiIcon;
            defaultIconColor = replacement.uiIconColor;
        }
    }
}