using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArgonicCore.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArgonicCore.Commands
{
    public class Command_SelectMaterial : Command
    {
        public Blueprint_Build blueprint;
        public Map map;
        public List<ThingDefCountClass> materialGroup;
        public int groupIndex;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            for (int i = 0; i < materialGroup.Count; i++)
            {
                list.Add(new FloatMenuOption("AC_MaterialTo".Translate(materialGroup[i].thingDef.label), delegate ()
                {
                    SetMaterial(materialGroup[i].thingDef);
                }, MenuOptionPriority.Default, null, null, 29f, null, null, true, 0));
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        public void SetMaterial(ThingDef material)
        {
            blueprint.SetActiveOptionalMaterial(material, groupIndex);
        }
    }
}