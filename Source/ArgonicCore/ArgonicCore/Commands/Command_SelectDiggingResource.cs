using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArgonicCore.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArgonicCore.Commands
{
    [StaticConstructorOnStartup]
    public class Command_SelectDiggingResource : Command
    {
        public Map map;
        public CompResourceDigger compResourceDigger;

        public Command_SelectDiggingResource()
        {
            defaultDesc = "AC_ChooseResourceDesc".Translate();
            defaultLabel = "AC_ChooseResource".Translate();

            foreach (object selectedObject in Find.Selector.SelectedObjects)
            {
                if (selectedObject is Building building)
                {
                    compResourceDigger = building.TryGetComp<CompResourceDigger>();
                    if (compResourceDigger != null)
                    {
                        icon = compResourceDigger.targetResource.uiIcon;
                        defaultIconColor = compResourceDigger.targetResource.uiIconColor;
                    }
                }
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (ThingDef res in compResourceDigger.availableResources)
            {
                options.Add(new FloatMenuOption("EM_ChooseMaterialToDig".Translate(res.label), delegate
                {
                    compResourceDigger.targetResource = res;
                }, MenuOptionPriority.Default, null, null, 29f));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
