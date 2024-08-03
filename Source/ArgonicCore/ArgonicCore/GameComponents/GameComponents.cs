using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;

namespace ArgonicCore.GameComponents
{
    public class GameComponent_ExtendedThings : GameComponent
    {
        public static GameComponent_ExtendedThings Instance;
        public Dictionary<Thing, InnerDict> optionalMaterialInUse = new Dictionary<Thing, InnerDict>();
        public List<Thing> dict_thingList = new List<Thing>();
        public List<InnerDict> dict_innerDictList = new List<InnerDict>();

        public GameComponent_ExtendedThings()
        {
            Instance = this;
        }

        public GameComponent_ExtendedThings(Game game)
        {
            Instance = this;
        }

        public override void StartedNewGame()
        {
            Init();
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            Init();
            base.LoadedGame();
        }

        public void Init()
        {
            Instance = this;
            if (optionalMaterialInUse == null) { optionalMaterialInUse = new Dictionary<Thing, InnerDict>(); }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 2000 == 0)
            {
                TryClearDictionary();
                //foreach (KeyValuePair<Thing, InnerDict> pair in optionalMaterialInUse)
                //{
                //    Log.Message(pair.Key.def.defName);
                //    foreach (KeyValuePair<ThingDef, ThingDef> pair2 in pair.Value.materialValues)
                //    {
                //        Log.Message(pair2.Key.defName + " is replaced with " + pair2.Value.defName);
                //    }
                //}
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            TryClearDictionary();
            Scribe_Collections.Look(ref optionalMaterialInUse, "optionalMaterialInUse", LookMode.Reference, LookMode.Deep, ref dict_thingList, ref dict_innerDictList);
        }

        private void TryClearDictionary()
        {
            foreach (KeyValuePair<Thing, InnerDict> pair in optionalMaterialInUse)
            {
                if (pair.Key == null || pair.Key.Destroyed)
                {
                    optionalMaterialInUse.Remove(pair.Key);
                    //Log.Message("Discarded " + pair.Key.ToString() + ". No longer exists.");
                    break;
                }
            }
        }
    }

    public class InnerDict : IExposable
    {
        public Dictionary<ThingDef, ThingDef> materialValues = new Dictionary<ThingDef, ThingDef>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref materialValues, "materialValues_inner", LookMode.Def, LookMode.Def);
        }
    }
}
