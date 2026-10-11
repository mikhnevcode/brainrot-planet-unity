using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>AI makes triangular loops on a sphere, with the same physics as the player.</summary>
    [RequireComponent(typeof(PlanetActor))]
    public sealed class PlanetAI : MonoBehaviour
    {
        private PlanetActor actor;
        private PlanetGame game;
        private Vector3 goal,home,corner;
        private float rethink;
        private int leg;
        public void Configure(PlanetGame director,PlanetActor pawn)
        {
            game=director;actor=pawn;
            home=pawn.SpawnDirection;
            ChooseTrip();
        }
        private void Awake(){actor=GetComponent<PlanetActor>();}
        private void ChooseTrip()
        {
            leg=0;
            Vector3 launch=transform.position.normalized;
            Vector3 away=PlanetMath.RandomAround(launch,Random.Range(.29f,.44f));
            int tries=0;
            while(PlanetSurface.Elevation(away)>2.1f && tries++<12)
                away=PlanetMath.RandomAround(launch,Random.Range(.29f,.44f));
            Vector3 turning=PlanetMath.RandomAround(away,Random.Range(.24f,.39f));
            tries=0;
            while((PlanetSurface.Elevation(turning)>2.1f || Vector3.Angle(launch,turning)<17f) && tries++<15)
                turning=PlanetMath.RandomAround(away,Random.Range(.24f,.39f));
            corner=turning;
            goal=away;
            rethink=7f;
        }
        private void NextLeg()
        {
            if(leg==0){leg=1;goal=corner;rethink=7f;}
            else if(leg==1){leg=2;goal=home;rethink=8f;}
            else ChooseTrip();
        }
        private void Update()
        {
            if(game==null || game.State!=MatchState.Playing)return;
            Vector3 normal=transform.position.normalized;
            rethink-=Time.deltaTime;
            if(Vector3.Angle(normal,goal)<7.5f || rethink<=0)NextLeg();
            Vector3 dir=PlanetMath.Tangent(normal,goal);
            Vector3 probe=Vector3.Slerp(normal,goal,.09f);
            if(PlanetSurface.Elevation(probe)>2.3f)
            {
                dir=Quaternion.AngleAxis(76f,normal)*dir;
                rethink=Mathf.Min(rethink,2.1f);
            }
            actor.Wish=dir;
            if(Random.value<.0030f && actor.Grounded)actor.Jump();
        }
    }
}
