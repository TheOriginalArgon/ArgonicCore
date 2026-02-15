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

    // Add to resources that have special product drops.
    public class ThingDefExtension_SpecialProducts : DefModExtension
    {
        public SpecialProductTypeDef productTypeDef; // Maybe remove later. Kept for compatibiltiy.

        public List<SpecialProductDictionaryEntry> specialProductDictionaryEntries;

        private Dictionary<string, List<ThingDefCountClass>> specialProducts;

        public override void ResolveReferences(Def parentDef)
        {
            base.ResolveReferences(parentDef);
            if (specialProductDictionaryEntries != null)
            {
                specialProducts = new Dictionary<string, List<ThingDefCountClass>>();
                foreach (SpecialProductDictionaryEntry entry in specialProductDictionaryEntries)
                {
                    if (!specialProducts.ContainsKey(entry.key))
                    {
                        specialProducts.Add(entry.key, entry.products);
                    }
                }
            }
        }

        public IEnumerable<Thing> GetSpecialProducts(string key, Pawn worker, bool usesEfficiency, SkillDef efficiencySkill = null)
        {
            if (specialProducts == null)
            {
                yield break;
            }
            List<ThingDefCountClass> specialProductsSet = specialProducts[key];
            for (int i = 0; i < specialProductsSet.Count; i++)
            {
                ThingDefCountClass thingDefCountClass = specialProductsSet[i];
                int num = GenMath.RoundRandom(thingDefCountClass.count * (usesEfficiency == false ? 1 : ((float)worker.skills.GetSkill(efficiencySkill).levelInt) / 5)); // Make this more complex.
                if (num > 0)
                {
                    Thing thing = ThingMaker.MakeThing(thingDefCountClass.thingDef);
                    thing.stackCount = num;
                    yield return thing;
                }
            }
        }

        public class SpecialProductDictionaryEntry
        {
            public string key;
            public List<ThingDefCountClass> products;
        }
    }

    // Add to walls that can be coated.
    public class ThingDefExtension_CoatableWall : DefModExtension
    {
        public ThingDef coatingResource;
        public ThingDef coatedThingDef;
        public int coatingAmount = 1;
        public int coatingWork = 360; // UNUSED YET.
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
