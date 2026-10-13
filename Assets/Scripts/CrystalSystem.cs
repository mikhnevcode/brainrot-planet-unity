using System.Collections.Generic;
using UnityEngine;

namespace BrainrotPlanet
{
    public sealed class CrystalSystem : MonoBehaviour
    {
        private PlanetGame game;
        private readonly List<Crystal> items=new List<Crystal>();
        private float spawnTick=18f,rareTick=37f;
        public void Configure(PlanetGame director)
        {
            game=director;
            for(int i=0;i<3;i++) Spawn(false);
        }
        private void Update()
        {
            if(game==null||game.State!=MatchState.Playing)return;
            spawnTick-=Time.deltaTime;rareTick-=Time.deltaTime;
            if(spawnTick<=0){spawnTick=23f;if(items.Count<6)Spawn(false);}
            if(rareTick<=0){rareTick=48f;if(items.Count<6)Spawn(true);}
            for(int i=items.Count-1;i>=0;i--)
            {
                Crystal c=items[i];
                if(c.obj==null){items.RemoveAt(i);continue;}
                c.age+=Time.deltaTime;
                c.obj.transform.Rotate(0,65f*Time.deltaTime,0,Space.Self);
                c.obj.transform.position=PlanetSurface.Point(c.n,.95f+.22f*Mathf.Sin(c.age*3f));
                int team=game.Territory.OwnerAt(c.n);
                if(team>0 && c.age>.3f)
                {
                    Collect(c,team);
                    items.RemoveAt(i);
                }
                else if(c.age>(c.rare?48f:29f))
                {
                    Destroy(c.obj);items.RemoveAt(i);
                }
            }
        }
        private sealed class Crystal
        {
            public GameObject obj;public Vector3 n;public bool rare;public float age;
        }
        private void Spawn(bool rare)
        {
            Vector3 n=Vector3.zero;
            bool valid=false;
            for(int j=0;j<40;j++)
            {
                // Give the player a visible nearby objective. Keep a few remote
                // crystals for AI; do not spawn rewards inside owned territory.
                if(j<28&&game.Player!=null&&(items.Count%3!=2||rare))
                {
                    Vector3 center=game.Player.transform.position.normalized;
                    Vector3 tangent=Vector3.ProjectOnPlane(Random.onUnitSphere,center).normalized;
                    float angle=Random.Range(.23f,.38f);
                    n=(center*Mathf.Cos(angle)+tangent*Mathf.Sin(angle)).normalized;
                }
                else n=Random.onUnitSphere;
                if(game.Territory.OwnerAt(n)==0 && PlanetSurface.Elevation(n)<1.7f){valid=true;break;}
            }
            if(!valid)return;
            var obj=new GameObject(rare?"RARE GOLD CRYSTAL":"Energy crystal");
            obj.transform.SetParent(transform);
            obj.transform.position=PlanetSurface.Point(n,1.1f);
            obj.transform.rotation=Quaternion.FromToRotation(Vector3.up,n);
            MeshFilter filter=obj.AddComponent<MeshFilter>();
            filter.sharedMesh=Octahedron();
            obj.AddComponent<MeshRenderer>().sharedMaterial=Colors.New(rare?new Color(1f,.77f,.08f):new Color(.35f,1f,1f),true);
            obj.transform.localScale=Vector3.one*(rare?1.22f:.8f);
            var beam=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name="Crystal light beam";beam.transform.SetParent(obj.transform,false);
            beam.transform.localPosition=new Vector3(0,-.30f,0);
            beam.transform.localScale=new Vector3(.1f,.38f,.1f);
            Destroy(beam.GetComponent<Collider>());
            beam.GetComponent<Renderer>().sharedMaterial=Colors.New(rare?Color.yellow:Color.cyan,true);
            items.Add(new Crystal{obj=obj,n=n,rare=rare});
            if(rare)game.Announce(" РЕДКИЙ КРИСТАЛЛ ПОЯВИЛСЯ!");
        }
        private void Collect(Crystal c,int team)
        {
            game.Effects.CaptureFlash(c.n,c.rare?Color.yellow:Color.cyan,c.rare?130:55);
            game.Territory.ClaimDisk(team,c.n,c.rare?.17f:.09f);
            if(team==1)
            {
                game.ReduceSkillCooldown(c.rare?9f:4f);
                game.Announce(c.rare?" ЛЕГЕНДАРНЫЙ КРИСТАЛЛ!":" КРИСТАЛЛ: КРАСКА + ЭНЕРГИЯ");
            }
            Destroy(c.obj);
        }
        private static Mesh cached;
        private static Mesh Octahedron()
        {
            if(cached!=null)return cached;
            cached=new Mesh{name="Crystal diamond mesh"};
            cached.vertices=new[]{new Vector3(0,1.15f,0),new Vector3(1f,0,0),new Vector3(0,0,1),new Vector3(-1,0,0),new Vector3(0,0,-1),new Vector3(0,-1.15f,0)};
            cached.triangles=new[]{0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};
            cached.RecalculateNormals();return cached;
        }
    }
}
