using ArgonicCore.Defs;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ArgonicCore.ModExtensions
{
    // Add to things that can be botched upon making.
    public class ThingDefExtension_BotchableOnMake : DefModExtension
    {
        public List<ThingDefCountClass> botchProducts;
        public SkillDef skillRequirement;
    }

    // Add to walls that can be coated.
    public class ThingDefExtension_CoatableWall : DefModExtension
    {
        public ThingDef coatingResource;
        public ThingDef coatedThingDef;
        public int coatingAmount = 1;
        public int coatingWork = 360; // UNUSED YET.
    }

    // Add to coated walls.
    public class ThingDefExtension_CoatedWall : DefModExtension
    {
        public ThingDef uncoatedThingDef;
    }

    // Add to items which inherit quality from an ingredient.
    public class ThingDefExtension_InheritsQuality : DefModExtension
    {
        public ThingDef keyIngredient;
    }

    // Add to resources that can be digged from the ground.
    public class ThingDefExtension_DiggableResource : DefModExtension
    {
        public int minimumPortion;
        public float difficultyFactor = 1f;
        public int priorityInList = 1;
        public List<string> resourceTags;
    }
}
