using System.Collections.Generic;

using Verse;

namespace ArgonicCore
{
    public class CrateLoadoutDef : Def
    {
        public List<ThingDefCountClass> Contents;
    }

    public class TechLevelDef : Def
    {
        public List<ThingDef> thingDefs;
    }
}
