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
        public Dictionary<Blueprint_Build, List<ThingDef>> optionalMaterialInUse = new Dictionary<Blueprint_Build, List<ThingDef>>();

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
            if (optionalMaterialInUse == null) { optionalMaterialInUse = new Dictionary<Blueprint_Build, List<ThingDef>>(); }
        }
    }
}
