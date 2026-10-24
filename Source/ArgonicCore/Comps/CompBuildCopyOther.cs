using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ArgonicCore.Comps
{
    public class CompProperties_BuildCopyOther : CompProperties
    {
        public CompProperties_BuildCopyOther()
        {
            compClass = typeof(CompBuildCopyOther);
        }

        public BuildableDef buildCopyDef;
    }

    public class CompBuildCopyOther : ThingComp
    {
        public CompProperties_BuildCopyOther Props => (CompProperties_BuildCopyOther)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command command = BuildCopyCommandUtility.BuildCopyCommand(Props.buildCopyDef, parent.Stuff, parent.StyleSourcePrecept as Precept_Building, parent.StyleDef, true);
            if (command != null)
            {
                yield return command;
            }
        }
    }
}
