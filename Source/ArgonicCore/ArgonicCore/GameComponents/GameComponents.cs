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
        public Dictionary<Thing, Dictionary<ThingDef, ThingDef>> optionalMaterialInUse = new Dictionary<Thing, Dictionary<ThingDef, ThingDef>>();

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
            if (optionalMaterialInUse == null) { optionalMaterialInUse = new Dictionary<Thing, Dictionary<ThingDef, ThingDef>>(); }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 2000 == 0)
            {
                TryClearDictionary();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref optionalMaterialInUse, "optionalMaterialInUse", LookMode.Reference, LookMode.Reference);
        }

        private void TryClearDictionary()
        {
            foreach (KeyValuePair<Thing, Dictionary<ThingDef, ThingDef>> pair in optionalMaterialInUse)
            {
                if (pair.Key == null || pair.Key.Destroyed)
                {
                    optionalMaterialInUse.Remove(pair.Key);
                    Log.Message("Discarded " + pair.Key.ToString() + ". No longer exists.");
                    break;
                }
            }
        }
    }
}
