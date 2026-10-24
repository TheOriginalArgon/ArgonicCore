using ArgonicCore.Comps;
using ArgonicCore.ModExtensions;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ArgonicCore.Designators
{
    public class Designator_CoatWalls : Designator
    {
        protected override DesignationDef Designation => DefDatabase<DesignationDef>.GetNamed("AC_CoatWall");
        public override bool DragDrawMeasurements => true;
        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Paint;

        public Designator_CoatWalls()
        {
            defaultLabel = "AC_DesignatorCoatWalls".Translate();
            defaultDesc = "AC_DesignatorCoatWallsDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/AC_CoatWalls", true);
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_SmoothSurface;
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (t.def.building == null || !t.def.HasModExtension<ThingDefExtension_CoatableWall>())
            {
                return false;
            }
            if (t.Faction != Faction.OfPlayer)
            {
                return false;
            }
            if (Map.designationManager.DesignationOn(t, DesignationDefOf.Uninstall) != null)
            {
                return false;
            }
            return true;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map) || c.Fogged(Map))
            {
                return false;
            }
            List<Thing> thingList = c.GetThingList(Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if ((bool)CanDesignateThing(thingList[i]))
                {
                    return true;
                }
            }
            return "AC_MustDesignateCoatableWalls".Translate();
        }

        public override void DesignateThing(Thing t)
        {
            Map.designationManager.TryRemoveDesignationOn(t, Designation);
            Map.designationManager.TryRemoveDesignationOn(t, DesignationDefOf.Deconstruct);
            Map.designationManager.AddDesignation(new Designation(t, Designation));
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList(Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (CanDesignateThing(thingList[i]).Accepted)
                {
                    DesignateThing(thingList[i]);
                }
            }
        }
    }
}
