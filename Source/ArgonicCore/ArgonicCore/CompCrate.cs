using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;

namespace ArgonicCore
{
    public class CompCrate : ThingComp
    {
        public CompProperties_Crate Props => (CompProperties_Crate)props;
        public CrateLoadoutDef CrateLoadout => Props.crateLoadout;
        public bool IsLocked => Props.isLocked;

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("String_CrateContents".Translate() + ": ");
            if (IsLocked)
            {
                stringBuilder.Append("String_CrateContentUnknown".Translate());
            }
            else
            {
                for (int i = 0; i < CrateLoadout.Contents.Count; i++)
                {
                    stringBuilder.Append(CrateLoadout.Contents[i].thingDef.label + " x" + CrateLoadout.Contents[i].count.ToString());
                    if (i != CrateLoadout.Contents.Count - 1)
                    {
                        stringBuilder.Append(", ");
                    }
                }
            }

            return stringBuilder.ToString();
        }
    }
}
