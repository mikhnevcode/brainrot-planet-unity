using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        public PlanetBiomeRules Arena {get;private set;}
        private BattlePlanet battlePlanet;
        private int planetSeed,lastBattlePlanet=-1;
        private float planetIntroAt;
        private void PrepareBattlePlanet()
        {
            if(!IsNetworkReplica)
            {
                battlePlanet=(BattlePlanet)PlanetBiomeRules.Next(lastBattlePlanet);
                lastBattlePlanet=(int)battlePlanet;
                planetSeed=Random.Range(1,int.MaxValue);
            }
            Arena=new PlanetBiomeRules(battlePlanet,planetSeed);
            planetIntroAt=Time.unscaledTime;
            if(worldCamera!=null)worldCamera.backgroundColor=Arena.Sky;
            Debug.Log("BATTLE_PLANET_READY id="+(int)battlePlanet+" seed="+planetSeed+" epoch="+networkEpoch+" authority="+!IsNetworkReplica);
        }
        private void DrawPlanetIntro(float w,float safeTop)
        {
            if(Arena==null||paused||Time.unscaledTime-planetIntroAt>5.5f)return;
            Rounded(new Rect(20,safeTop+117,w-40,72),new Color(.035f,.055f,.12f,.95f));
            Plate(new Rect(28,safeTop+125,4,56),Arena.Accent);
            GUI.Label(new Rect(38,safeTop+121,w-76,25),Arena.Title,heroNameStyle);
            GUI.Label(new Rect(38,safeTop+148,w-76,36),Arena.Hint,captionStyle);
        }
    }
}
