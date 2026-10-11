using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainrotPlanet
{
    public enum MatchState { Menu, Playing, End }

    public sealed class PlanetGame : MonoBehaviour
    {
        public MatchState State { get; private set; } = MatchState.Menu;
        public TerritorySystem Territory { get; private set; }
        public PlanetEffects Effects { get; private set; }
        public PlanetActor Player { get; private set; }
        private readonly List<PlanetActor> players = new List<PlanetActor>();
        private readonly LineRenderer[] pathRenderers = new LineRenderer[5];
        private readonly string[] characters={"ТРАЛАЛЕРО ТРАЛАЛА 🦈","БОМБАРДИРО КРОКОДИЛО 🐊","ФРИГО КАМЕЛО 🐪"};
        private readonly string[] skillNames={"РЫВОК","КРАСКОБОМБА","ЛЕДЯНАЯ ВОЛНА"};
        private int selectedHero=0;
        private float matchTime,skillCooldown,annTime,scoreTick;
        private float[] scores = new float[5];
        private int lives=3;
        private string announcement="";
        private OrbitCamera orbitCamera;
        private Camera worldCamera;
        private float guiScale=1f;
        private Vector2 movement;
        private int steerFinger=-1;
        private Vector2 steerOrigin;
        private Rect skillArea,jumpArea;
        private Texture2D whitePixel;
        private GUIStyle headerStyle,menuStyle,buttonStyle,smallStyle;
        private float pathUpdate;
        private Transform worldRoot;
        private bool sceneLoaded;

        private void Awake()
        {
            Application.targetFrameRate=60;
            Screen.sleepTimeout=SleepTimeout.NeverSleep;
            Input.multiTouchEnabled=true;
            whitePixel=new Texture2D(1,1);
            whitePixel.SetPixel(0,0,Color.white);whitePixel.Apply();
            SetupCamera();
            SetupLighting();
        }
        private void SetupCamera()
        {
            var go=new GameObject("Main Camera");go.tag="MainCamera";
            worldCamera=go.AddComponent<Camera>();
            worldCamera.clearFlags=CameraClearFlags.SolidColor;
            worldCamera.backgroundColor=new Color(.055f,.065f,.15f);
            worldCamera.nearClipPlane=.15f;worldCamera.farClipPlane=300f;
            worldCamera.fieldOfView=52f;
            orbitCamera=go.AddComponent<OrbitCamera>();
            go.transform.position=new Vector3(0,10,-42);
        }
        private void SetupLighting()
        {
            RenderSettings.ambientLight=new Color(.50f,.55f,.72f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            var sun=new GameObject("Colorful planet sun");
            var light=sun.AddComponent<Light>();light.type=LightType.Directional;
            light.intensity=1.1f;light.color=new Color(1f,.97f,.89f);
            sun.transform.rotation=Quaternion.Euler(35f,-43f,-18f);
        }
        public void StartGame()
        {
            if(worldRoot!=null)Destroy(worldRoot.gameObject);
            foreach(LineRenderer l in pathRenderers)if(l!=null)Destroy(l.gameObject);
            Array.Clear(pathRenderers,0,pathRenderers.Length);
            players.Clear();lives=3;
            matchTime=145f;skillCooldown=0f;scoreTick=0f;
            movement=Vector2.zero;steerFinger=-1;
            var gameObjectRoot=new GameObject("ONE PLANET / ONE MATCH");
            worldRoot=gameObjectRoot.transform;
            var globe=new GameObject("Spherical walkable planet");globe.transform.SetParent(worldRoot);
            var surface=globe.AddComponent<PlanetSurface>();surface.Build();
            Territory=new TerritorySystem(surface);
            Territory.OnCapture+=HandleCapture;
            Territory.OnTrailDestroyed+=HandleTrailLost;
            Effects=new GameObject("Paint particles and explosions").AddComponent<PlanetEffects>();
            Effects.transform.SetParent(worldRoot);

            Vector3[] spawnDirections={
                new Vector3(.91f,.20f,.33f).normalized,
                new Vector3(-.77f,.41f,.47f).normalized,
                new Vector3(-.18f,-.46f,-.87f).normalized,
                new Vector3(.40f,-.78f,.48f).normalized
            };
            for(int team=1;team<=4;team++)
            {
                int hero=team==1?selectedHero:(selectedHero+team-1)%3;
                var actorObject=new GameObject("Landing hero " + team);
                actorObject.transform.SetParent(worldRoot);
                actorObject.AddComponent<Rigidbody>();
                var motor=actorObject.AddComponent<PlanetActor>();
                motor.Configure(this,team,hero,spawnDirections[team-1]);
                motor.Active=true;
                players.Add(motor);
                Territory.Seed(team,spawnDirections[team-1]);
                if(team==1)Player=motor;
                else actorObject.AddComponent<PlanetAI>().Configure(this,motor);
                LineRenderer renderer=new GameObject("Team " +team+ " unfinished glowing trail").AddComponent<LineRenderer>();
                renderer.transform.SetParent(worldRoot);
                renderer.useWorldSpace=true;
                renderer.positionCount=0;
                renderer.startWidth=.27f;renderer.endWidth=.27f;
                renderer.numCapVertices=5;
                renderer.material=new Material(Shader.Find("Sprites/Default"));
                renderer.startColor=Colors.Team(team);
                renderer.endColor=Color.Lerp(Colors.Team(team),Color.white,.35f);
                pathRenderers[team]=renderer;
                Effects.Burst(motor.transform.position,Colors.Team(team),45,4f);
            }
            orbitCamera.Target=Player.transform;
            var gems=new GameObject("Crystal pickups").AddComponent<CrystalSystem>();
            gems.transform.SetParent(worldRoot);
            gems.Configure(this);
            CreateStars();
            State=MatchState.Playing;
            sceneLoaded=true;
            Announce("🚀 ДЕСАНТ НА ПЛАНЕТУ!");
        }
        private void CreateStars()
        {
            // Decorative low-cost point cloud, not a second physics scene.
            var stars=new GameObject("Distant stars");stars.transform.SetParent(worldRoot);
            Material material=Colors.New(new Color(.72f,.85f,1f),true);
            var mesh=new Mesh{name="Star dots"};
            const int count=145;
            var vertices=new Vector3[count*4];
            var uv=new Vector2[count*4];
            var tr=new int[count*6];
            for(int i=0;i<count;i++)
            {
                Vector3 n=UnityEngine.Random.onUnitSphere;
                Vector3 center=n*UnityEngine.Random.Range(75f,125f);
                Vector3 sideways=Vector3.Cross(n,Vector3.up);
                if(sideways.sqrMagnitude<.01f)sideways=Vector3.Cross(n,Vector3.right);
                sideways.Normalize();Vector3 down=Vector3.Cross(n,sideways).normalized;
                float radius=UnityEngine.Random.Range(.08f,.25f);
                int k=i*4,t=i*6;
                vertices[k]=center+(-sideways-down)*radius;
                vertices[k+1]=center+(sideways-down)*radius;
                vertices[k+2]=center+(-sideways+down)*radius;
                vertices[k+3]=center+(sideways+down)*radius;
                uv[k]=Vector2.zero;uv[k+1]=Vector2.right;uv[k+2]=Vector2.up;uv[k+3]=Vector2.one;
                tr[t]=k;tr[t+1]=k+2;tr[t+2]=k+1;
                tr[t+3]=k+1;tr[t+4]=k+2;tr[t+5]=k+3;
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tr;mesh.RecalculateNormals();
            stars.AddComponent<MeshFilter>().sharedMesh=mesh;
            stars.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        private void HandleCapture(int team,int changed,Vector3 direction)
        {
            Effects.CaptureFlash(direction,Colors.Team(team),changed);
            if(team==1)
            {
                if(changed>180)Announce("🎨 БОЛЬШОЙ ЗАХВАТ!");
                skillCooldown=Mathf.Max(0f,skillCooldown-Mathf.Min(1.5f,changed/450f));
            }
        }
        private void HandleTrailLost(int team,Vector3 direction)
        {
            PlanetActor actor=players.Find(p=>p.Team==team);
            if(actor!=null)actor.HitTrail();
        }
        public void HurtPlayer()
        {
            lives--;
            Announce("💥 ТВОЙ СЛЕД ПЕРЕСЕКЛИ! ♥ "+lives);
            if(lives<=0)Finish();
        }
        public void ReduceSkillCooldown(float by)=>skillCooldown=Mathf.Max(0,skillCooldown-by);
        public void Announce(string text){announcement=text;annTime=2.4f;}
        public void UseSkill()
        {
            if(State!=MatchState.Playing||skillCooldown>0||Player==null)return;
            skillCooldown=12f;
            if(selectedHero==0)
            {
                Player.SpeedSeconds=3.5f;
                Effects.Burst(Player.transform.position,Colors.Team(1),25,3f);
                Announce("⚡ ТРАЛАЛЕРО: УСКОРЕНИЕ!");
            }
            else if(selectedHero==1)
            {
                GameObject ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name="Paint bomb / gravity projectile";
                ball.layer=9;
                ball.transform.localScale=Vector3.one*.70f;
                ball.GetComponent<Renderer>().sharedMaterial=Colors.New(Colors.Team(1),true);
                var rigid=ball.AddComponent<Rigidbody>();
                rigid.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                ball.AddComponent<PaintBomb>().Launch(this,Player);
                Announce("💣 КРАСКОБОМБА!");
            }
            else
            {
                Vector3 at=Player.transform.position.normalized;
                foreach(var pawn in players)
                    if(pawn.Team!=1&&Vector3.Angle(pawn.transform.position.normalized,at)<21f)
                        pawn.SlowSeconds=4f;
                Effects.CaptureFlash(at,Color.cyan,110);
                Announce("❄ ФРИГО: ЛЕДЯНАЯ ВОЛНА!");
            }
        }
        private void Update()
        {
            if(State!=MatchState.Playing)return;
            float dt=Time.deltaTime;
            matchTime-=dt;skillCooldown=Mathf.Max(0,skillCooldown-dt);
            annTime=Mathf.Max(0,annTime-dt);
            HandleInput();
            Territory.Update(dt);
            scoreTick-=dt;
            if(scoreTick<=0)
            {
                scoreTick=.75f;
                for(int i=1;i<=4;i++)scores[i]=Territory.Percentage(i);
            }
            pathUpdate-=dt;
            if(pathUpdate<=0)
            {
                pathUpdate=.08f;
                for(int team=1;team<=4;team++)
                {
                    IReadOnlyList<int> path=Territory.Path(team);
                    LineRenderer r=pathRenderers[team];
                    if(r==null)continue;
                    int skip=Mathf.Max(1,path.Count/280);
                    int count=(path.Count+skip-1)/skip;
                    r.positionCount=count;
                    for(int j=0;j<count;j++)
                    {
                        int idx=path[Mathf.Min(j*skip,path.Count-1)];
                        r.SetPosition(j,PlanetSurface.Point(PlanetMath.CellDirection(idx),.56f));
                    }
                }
            }
            if(matchTime<=0)Finish();
        }
        private void HandleInput()
        {
            movement=new Vector2(
                (Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),
                (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
            if(Input.GetKeyDown(KeyCode.Space))Player.Jump();
            if(Input.GetKeyDown(KeyCode.E))UseSkill();
            float unit=GameScale();
            float virtualH=Screen.height/unit;
            skillArea=new Rect(314,virtualH-179,92,75);
            jumpArea=new Rect(314,virtualH-91,92,70);
            if(Input.touchCount>0)
            {
                for(int i=0;i<Input.touchCount;i++)
                {
                    Touch touch=Input.GetTouch(i);
                    Vector2 p=new Vector2(touch.position.x/unit,(Screen.height-touch.position.y)/unit);
                    if(touch.phase==TouchPhase.Began)
                    {
                        if(skillArea.Contains(p)){UseSkill();continue;}
                        if(jumpArea.Contains(p)){Player.Jump();continue;}
                        if(p.x<230&&p.y>virtualH-255){steerFinger=touch.fingerId;steerOrigin=p;}
                    }
                    if(touch.fingerId==steerFinger)
                    {
                        if(touch.phase==TouchPhase.Canceled||touch.phase==TouchPhase.Ended)
                          {steerFinger=-1;movement=Vector2.zero;}
                        else movement=Vector2.ClampMagnitude((p-steerOrigin)/54f,1f)*new Vector2(1,-1);
                    }
                }
            }
            else if(steerFinger!=-1){steerFinger=-1;movement=Vector2.zero;}
            if(Player!=null&&worldCamera!=null)
            {
                Vector3 radial=Player.transform.position.normalized;
                Vector3 right=Vector3.ProjectOnPlane(worldCamera.transform.right,radial).normalized;
                Vector3 up=Vector3.ProjectOnPlane(worldCamera.transform.up,radial).normalized;
                Player.Wish=right*movement.x+up*movement.y;
            }
        }
        private void Finish()
        {
            if(State==MatchState.End)return;
            State=MatchState.End;
            foreach(var pawn in players){pawn.Wish=Vector3.zero;pawn.Active=false;}
        }
        private float GameScale(){return Mathf.Max(.60f,Mathf.Min(Screen.width/420f,Screen.height/790f));}
        private void InitStyles()
        {
            if(headerStyle!=null)return;
            headerStyle=new GUIStyle(GUI.skin.label){fontSize=19,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            headerStyle.normal.textColor=Color.white;
            menuStyle=new GUIStyle(GUI.skin.label){fontSize=15,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            menuStyle.normal.textColor=new Color(.92f,.94f,1f);
            smallStyle=new GUIStyle(GUI.skin.label){fontSize=13,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            smallStyle.normal.textColor=Color.white;
            buttonStyle=new GUIStyle(GUI.skin.button){fontSize=17,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};
        }
        private void Plate(Rect rect,Color color)
        {
            Color original=GUI.color;GUI.color=color;
            GUI.DrawTexture(rect,whitePixel);GUI.color=original;
        }
        private void OnGUI()
        {
            InitStyles();
            guiScale=GameScale();
            float logicalW=Screen.width/guiScale,logicalH=Screen.height/guiScale;
            var old=GUI.matrix;
            GUI.matrix=Matrix4x4.Scale(new Vector3(guiScale,guiScale,1f));
            if(State==MatchState.Menu)
            {
                Plate(new Rect(0,0,logicalW,logicalH),new Color(.055f,.065f,.13f,.97f));
                GUI.Label(new Rect(16,45,logicalW-32,55),"🌍 BRAINROT PLANET.IO",headerStyle);
                GUI.Label(new Rect(22,102,logicalW-44,74),"Десантируйся на планету. Оставляй след и замыкай его, чтобы захватить поверхность!",menuStyle);
                GUI.Label(new Rect(18,184,logicalW-36,32),"ВЫБЕРИ ГЕРОЯ",headerStyle);
                for(int i=0;i<3;i++)
                {
                    float y=227+i*99;
                    Plate(new Rect(19,y,logicalW-38,90),i==selectedHero?new Color(.90f,.25f,.52f,.85f):new Color(.25f,.34f,.52f,.72f));
                    if(GUI.Button(new Rect(28,y+8,logicalW-56,74),characters[i]+"\n"+
                       (i==0?"⚡ Ускорение":i==1?"💣 Бомба по дуге":"❄ Ледяная волна"),buttonStyle)) selectedHero=i;
                }
                if(GUI.Button(new Rect(26,logicalH-166,logicalW-52,60),"🚀 ДЕСАНТИРОВАТЬСЯ",buttonStyle))StartGame();
                GUI.Label(new Rect(25,logicalH-94,logicalW-50,70),"145 секунд · 4 героя · планетная физика\nБез рекламы и интернет-подключения",smallStyle);
            }
            else if(State==MatchState.Playing)
            {
                Plate(new Rect(9,9,logicalW-18,76),new Color(.10f,.12f,.22f,.86f));
                GUI.Label(new Rect(14,12,logicalW-28,29),"🌍 BRAINROT PLANET.IO",headerStyle);
                GUI.Label(new Rect(15,42,logicalW-30,37),
                    "🩷 "+scores[1].ToString("F1")+"%   💙 "+scores[2].ToString("F1")+"%   🟠 "+scores[3].ToString("F1")+"%"+
                    "\n♥ "+lives+"     ⏱ "+Mathf.CeilToInt(matchTime)+" сек",smallStyle);
                if(annTime>0)
                {
                    Plate(new Rect(25,96,logicalW-50,42),new Color(.09f,.10f,.21f,.74f));
                    GUI.Label(new Rect(25,99,logicalW-50,34),announcement,smallStyle);
                }
                Plate(new Rect(15,logicalH-158,136,136),new Color(.13f,.15f,.28f,.48f));
                GUI.Label(new Rect(15,logicalH-146,136,30),"ДВИЖЕНИЕ",smallStyle);
                if(steerFinger>=0)GUI.Label(new Rect(15,logicalH-110,136,66),"●",headerStyle);
                Plate(skillArea,skillCooldown<=0?new Color(.91f,.31f,.56f,.90f):new Color(.37f,.39f,.53f,.83f));
                GUI.Label(skillArea,skillCooldown>0?skillNames[selectedHero]+"\n"+Mathf.CeilToInt(skillCooldown)+" сек":skillNames[selectedHero],smallStyle);
                Plate(jumpArea,new Color(.21f,.62f,.89f,.91f));
                GUI.Label(jumpArea,"↥\nПРЫЖОК",smallStyle);
            }
            else
            {
                Plate(new Rect(0,0,logicalW,logicalH),new Color(.06f,.08f,.15f,.88f));
                float place=0;int winner=0;
                for(int i=1;i<=4;i++)if(scores[i]>place){place=scores[i];winner=i;}
                GUI.Label(new Rect(20,logicalH/2-182,logicalW-40,62),winner==1?"🏆 ПОБЕДА!":"🌍 ПЛАНЕТА ЗАХВАЧЕНА!",headerStyle);
                GUI.Label(new Rect(24,logicalH/2-103,logicalW-48,80),
                    "Твоя территория: "+scores[1].ToString("F1")+"%\nПобедитель: "+(winner==1?"ТЫ":("КОМАНДА "+winner)),menuStyle);
                if(GUI.Button(new Rect(26,logicalH/2+14,logicalW-52,64),"ИГРАТЬ ЕЩЁ",buttonStyle))StartGame();
                if(GUI.Button(new Rect(26,logicalH/2+89,logicalW-52,57),"ВЫБРАТЬ ГЕРОЯ",buttonStyle))
                {if(worldRoot!=null)Destroy(worldRoot.gameObject);State=MatchState.Menu;orbitCamera.Target=null;}
            }
            GUI.matrix=old;
        }
    }
}
