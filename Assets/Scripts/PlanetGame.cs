using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainrotPlanet
{
    public enum MatchState { Menu, Playing, End }

    public sealed class PlanetGame : MonoBehaviour
    {
        public MatchState State { get; private set; } = MatchState.Menu;
        public bool IsLanding => orbitCamera != null && orbitCamera.IsDescending;
        public TerritorySystem Territory { get; private set; }
        public PlanetEffects Effects { get; private set; }
        public PlanetActor Player { get; private set; }
        public IReadOnlyList<PlanetActor> Actors => players;
        private readonly List<PlanetActor> players = new List<PlanetActor>();
        private readonly LineRenderer[] pathRenderers = new LineRenderer[5];
        private readonly string[] characters={"ТРАЛАЛЕРО ТРАЛАЛА ","БОМБАРДИРО КРОКОДИЛО ","ФРИГО КАМЕЛО ","БОМБОМБИНИ ГУСИНИ ","БАЛЕРИНА КАПУЧИНО ","КАПУЧИНО АССАСИНО ","БРР БРР ПАТАПИМ ","ТУНГ ТУНГ ТУНГ САХУР ","ЛИРИЛИ ЛАРИЛА ","ФРУЛЛИ ФРУЛЛА ","БОНЕКА АМБАЛАБУ ","ТРИППИ ТРОППИ "};
        private readonly string[] skillNames={"АКУЛИЙ РЫВОК","3 БОМБЫ","ЛЕДЯНАЯ ВОЛНА","ТУРБО","КОФЕЙНЫЙ ВИХРЬ","ТЕНЕВОЙ РЫВОК","ЛЕСНАЯ ХВАТКА","БАРАБАННЫЙ УДАР","КАКТУСНАЯ БУРЯ","ВОЗДУШНЫЙ ТАНЕЦ","ПРУЖИНА","ВСПЛЕСК КРАСКИ"};
        private int selectedHero=0;
        private bool resolvingHeroSkill;
        private float matchTime,skillCooldown,annTime,scoreTick;
        private float playerLandingGrace;
        private float[] scores = new float[5];
        private int lives=3;
        private string announcement="";
        private OrbitCamera orbitCamera;
        private PlanetActor victoryActor;
        private float victoryReplayTimer;
        private Camera worldCamera;
        private float guiScale=1f;
        private Vector2 movement;
        private Vector2 savedSteering;
        private bool autoMoving;
        private float nextInputDiagnostic;
        private int steerFinger=-1;
        private Vector2 steerOrigin;
        private int heroPage;
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
            if(Debug.isDebugBuild)gameObject.AddComponent<GamePerformanceProbe>();
            var requiredTypes=new GameObject("Runtime collider types");
            requiredTypes.AddComponent<BoxCollider>();
            requiredTypes.AddComponent<CapsuleCollider>();
            Destroy(requiredTypes);
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
            victoryActor=null;victoryReplayTimer=0f;
            matchTime=145f;skillCooldown=0f;scoreTick=0f;
            playerLandingGrace=12f;
            movement=Vector2.zero;savedSteering=Vector2.zero;autoMoving=false;steerFinger=-1;
            nextInputDiagnostic=0f;
            var gameObjectRoot=new GameObject("ONE PLANET / ONE MATCH");
            worldRoot=gameObjectRoot.transform;
            var globe=new GameObject("Spherical walkable planet");globe.transform.SetParent(worldRoot);
            var surface=globe.AddComponent<PlanetSurface>();surface.Build();
            Territory=new TerritorySystem(surface);
            Territory.OnCapture+=HandleCapture;
            Territory.OnTrailDestroyed+=HandleTrailLost;
            Effects=new GameObject("Paint particles and explosions").AddComponent<PlanetEffects>();
            Effects.transform.SetParent(worldRoot);

            // Keep opponents visible in the same planetary sector while
            // maintaining initial separation between the painted home cores.
            Vector3 centre=new Vector3(.91f,.20f,.33f).normalized;
            Vector3 east=Vector3.Cross(Vector3.up,centre).normalized;
            Vector3 north=Vector3.Cross(centre,east).normalized;
            Vector3[] spawnDirections={
                centre,
                (centre*.75f+east*.66f).normalized,
                (centre*.75f-east*.66f).normalized,
                (centre*.74f+north*.67f).normalized
            };
            for(int team=1;team<=4;team++)
            {
                int hero=team==1?selectedHero:(selectedHero+team-1)%characters.Length;
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
                renderer.startWidth=.34f;renderer.endWidth=.34f;
                renderer.numCapVertices=10;
                renderer.numCornerVertices=10;
                renderer.material=Colors.FromResource("PlanetSprite");
                renderer.startColor=Colors.Team(team);
                renderer.endColor=Color.Lerp(Colors.Team(team),Color.white,.35f);
                pathRenderers[team]=renderer;
                Effects.Burst(motor.transform.position,Colors.Team(team),45,4f);
            }
            // The player now falls from above the planet during the camera approach.
            Player.BeginDropFromOrbit();
            orbitCamera.BeginDescent(Player.transform);
            var gems=new GameObject("Crystal pickups").AddComponent<CrystalSystem>();
            gems.transform.SetParent(worldRoot);
            gems.Configure(this);
            CreateStars();
            State=MatchState.Playing;
            sceneLoaded=true;
            Announce(" ДЕСАНТ НА ПЛАНЕТУ!");
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
            if(changed>=150 && !resolvingHeroSkill && !IsLanding)
            {
                // Only meaningful captures trigger a celebration; little paint
                // splashes and crystal pickups do not interrupt movement.
                for(int i=0;i<players.Count;i++)
                    if(players[i].Team==team)
                    {
                        players[i].Celebrate();
                        break;
                    }
            }
            if(team==1)
            {
                float before=scores[1];
                scores[1]=Territory.Percentage(1);
                float gain=Mathf.Max(0f,scores[1]-before);
                Announce((changed>180?" БОЛЬШОЙ ЗАХВАТ! +":" ОБЛАСТЬ ЗАКРАШЕНА! +")+
                    gain.ToString("F1")+"%");
                skillCooldown=Mathf.Max(0f,skillCooldown-Mathf.Min(1.5f,changed/450f));
            }
        }
        private void HandleTrailLost(int team,Vector3 direction)
        {
            if(team==1 && playerLandingGrace>0f)
            {
                // Player is invulnerable to trail hits while landing and learning to move.
                return;
            }
            PlanetActor actor=players.Find(p=>p.Team==team);
            if(actor!=null)actor.HitTrail();
        }
        public void HurtPlayer()
        {
            lives--;
            Announce(" ТВОЙ СЛЕД ПЕРЕСЕКЛИ!  "+lives);
            if(lives<=0)Finish();
        }
        public void ReduceSkillCooldown(float by)=>skillCooldown=Mathf.Max(0,skillCooldown-by);
        public void Announce(string text){announcement=text;annTime=2.4f;}
        public void UseSkill()
        {
            TryUseActorSkill(Player);
        }

        // Shared human/AI ability logic; UI messages and cooldown only for human.
        public bool TryUseActorSkill(PlanetActor caster)
        {
            if(State!=MatchState.Playing||caster==null||!caster.Active||IsLanding)
                return false;
            int hero=caster.Character,team=caster.Team;
            bool human=team==1;
            if(human && skillCooldown>0)return false;
            if(hero==10 && !caster.Grounded)return false;
            if(human)skillCooldown=12f;
            resolvingHeroSkill=true;
            caster.PlaySpecial();
            Vector3 center=caster.transform.position.normalized;
            int cells=0;
            try
            {
                switch(hero)
                {
                    case 0: // Tralalero: shark sprint
                        caster.SpeedSeconds=3.5f;
                        if(human)Announce(" ТРАЛАЛЕРО: АКУЛИЙ РЫВОК!");
                        break;
                    case 1: // Bombardiro: three evenly spread paint bombs
                        for(int i=-1;i<=1;i++)
                        {
                            GameObject bomb=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            bomb.name="Bombardiro paint bomb "+(i+1);
                            bomb.layer=9;
                            bomb.transform.localScale=Vector3.one*.55f;
                            bomb.GetComponent<Renderer>().sharedMaterial=
                                Colors.New(Colors.Team(team),true);
                            Rigidbody rigid=bomb.AddComponent<Rigidbody>();
                            rigid.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                            bomb.AddComponent<PaintBomb>().Launch(this,caster,i*16f,.070f);
                        }
                        if(human)Announce(" БОМБАРДИРО: ТРОЙНАЯ БОМБАРДИРОВКА!");
                        break;
                    case 2: // Frigo: chilled opponents
                        for(int i=0;i<players.Count;i++)
                        {
                            PlanetActor pawn=players[i];
                            if(pawn.Team!=team &&
                               Vector3.Angle(pawn.transform.position.normalized,center)<21f)
                                pawn.SlowSeconds=4f;
                        }
                        if(human)Announce(" ФРИГО: ЛЕДЯНАЯ ВОЛНА!");
                        break;
                    case 3: // Bombombini: jet-powered dash
                        caster.SpeedSeconds=4.0f;
                        if(human)Announce(" ГУСИНИ: ТУРБОРЕЖИМ!");
                        break;
                    case 4: // Ballerina: cappuccino pirouette paint
                        cells=Territory.ClaimDisk(team,center,.125f);
                        if(human)Announce(" БАЛЕРИНА: КОФЕЙНЫЙ ВИХРЬ! +"+cells);
                        break;
                    case 5: // Ninja: shadow dash + narrow paint pocket
                        caster.SpeedSeconds=2.8f;
                        cells=Territory.ClaimDisk(team,center,.08f);
                        if(human)Announce(" АССАСИНО: ТЕНЕВОЙ РЫВОК!");
                        break;
                    case 6: // Patapim: thicket roots slow nearby opponents
                        cells=Territory.ClaimDisk(team,center,.10f);
                        for(int i=0;i<players.Count;i++)
                        {
                            PlanetActor pawn=players[i];
                            if(pawn.Team!=team &&
                               Vector3.Angle(pawn.transform.position.normalized,center)<15f)
                                pawn.SlowSeconds=3f;
                        }
                        if(human)Announce(" ПАТАПИМ: ЛЕСНАЯ ХВАТКА!");
                        break;
                    case 7: // Tung: large percussive shock wave
                        cells=Territory.ClaimDisk(team,center,.09f);
                        for(int i=0;i<players.Count;i++)
                        {
                            PlanetActor pawn=players[i];
                            if(pawn.Team!=team &&
                               Vector3.Angle(pawn.transform.position.normalized,center)<18f)
                                pawn.SlowSeconds=3.5f;
                        }
                        if(human)Announce(" САХУР: БАРАБАННЫЙ УДАР!");
                        break;
                    case 8: // Lirili: thorny cactus spray
                        cells=Territory.ClaimDisk(team,center,.11f);
                        for(int i=0;i<players.Count;i++)
                        {
                            PlanetActor pawn=players[i];
                            if(pawn.Team!=team &&
                               Vector3.Angle(pawn.transform.position.normalized,center)<13f)
                                pawn.SlowSeconds=4f;
                        }
                        if(human)Announce(" ЛИРИЛИ: КАКТУСНАЯ БУРЯ!");
                        break;
                    case 9: // Frulli: flutter movement with small paint splash
                        caster.SpeedSeconds=3.5f;
                        cells=Territory.ClaimDisk(team,center,.075f);
                        if(human)Announce(" ФРУЛЛИ: ВОЗДУШНЫЙ ТАНЕЦ!");
                        break;
                    case 10: // Wheel frog bounces; real paint arrives on touchdown.
                        if(!caster.SpringJump()){if(human)skillCooldown=0f;return false;}
                        if(human)Announce(" БОНЕКА: ПРУЖИНА!");
                        break;
                    case 11: // Trippi: three separate shrimp-paint splashes
                        Vector3 tangent=Vector3.ProjectOnPlane(caster.transform.forward,
                            center).normalized;
                        if(tangent.sqrMagnitude<.01f)
                            tangent=Vector3.Cross(center,Vector3.up).normalized;
                        Vector3 across=Vector3.Cross(center,tangent).normalized;
                        for(int i=-1;i<=1;i++)
                        {
                            Vector3 splash=Quaternion.AngleAxis(i*7.5f,across)*center;
                            cells+=Territory.ClaimDisk(team,splash,.068f);
                        }
                        if(human)Announce(" ТРИППИ: КРЕВЕТОЧНЫЙ ВСПЛЕСК! +"+cells);
                        break;
                }
                Effects.HeroAbility(hero,center,Colors.Team(team));
                Debug.Log("HERO_SPECIAL_ACTIVATED hero="+hero+" team="+team+
                          " cells="+cells+" cooldown="+skillCooldown.ToString("F1"));
            }
            finally{resolvingHeroSkill=false;}
            return true;
        }
        private void Update()
        {
            if(State==MatchState.End)
            {
                if(victoryActor!=null)
                {
                    victoryReplayTimer-=Time.deltaTime;
                    if(victoryReplayTimer<=0f)
                    {
                        victoryReplayTimer=2.90f;
                        victoryActor.Celebrate();
                        Debug.Log("HERO_MATCH_VICTORY_REPLAY team="+victoryActor.Team);
                    }
                }
                return;
            }
            if(State!=MatchState.Playing)return;
            float dt=Time.deltaTime;
            playerLandingGrace=Mathf.Max(0f,playerLandingGrace-dt);
            if(orbitCamera!=null && orbitCamera.IsDescending)
            {
                if(Player!=null)Player.Wish=Vector3.zero;
                return;
            }
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
                    IReadOnlyList<Vector3> path=Territory.VisualPath(team);
                    LineRenderer r=pathRenderers[team];
                    if(r==null)continue;
                    int skip=Mathf.Max(1,path.Count/280);
                    int count=(path.Count+skip-1)/skip;
                    r.positionCount=count;
                    for(int j=0;j<count;j++)
                    {
                        Vector3 exactDirection=path[Mathf.Min(j*skip,path.Count-1)];
                        r.SetPosition(j,PlanetSurface.Point(exactDirection,.56f));
                    }
                }
            }
            if(matchTime<=0)Finish();
        }
        private void HandleInput()
        {
            Vector2 keyboard=new Vector2(
                (Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),
                (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
            if(keyboard.sqrMagnitude>.01f)
            {
                savedSteering=keyboard.normalized;
                autoMoving=true;
            }
            movement=autoMoving ? savedSteering : Vector2.zero;
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
                        if(steerFinger<0){steerFinger=touch.fingerId;steerOrigin=p;}
                    }
                    if(touch.fingerId==steerFinger)
                    {
                        if(touch.phase==TouchPhase.Canceled||touch.phase==TouchPhase.Ended)
                          {steerFinger=-1; /* Keep heading after release. */}
                        else
                        {
                            Vector2 swipe=p-steerOrigin;
                            if(swipe.sqrMagnitude>=64f)
                            {
                                savedSteering=new Vector2(swipe.x,-swipe.y).normalized;
                                if(!autoMoving)Debug.Log("AUTO_MOVE_STARTED");
                                autoMoving=true;
                                movement=savedSteering;
                            }
                        }
                    }
                }
            }
            else if(steerFinger!=-1){steerFinger=-1; /* Motion continues without a finger. */}
            if(Player!=null&&worldCamera!=null)
            {
                Vector3 radial=Player.transform.position.normalized;
                Vector3 right=Vector3.ProjectOnPlane(worldCamera.transform.right,radial).normalized;
                Vector3 up=Vector3.ProjectOnPlane(worldCamera.transform.up,radial).normalized;
                Player.Wish=(right*movement.x+up*movement.y).normalized;
                if(autoMoving && Debug.isDebugBuild && Time.unscaledTime>=nextInputDiagnostic)
                {
                    nextInputDiagnostic=Time.unscaledTime+1.2f;
                    Debug.Log(string.Format("AUTOMOVE actor=({0:F2},{1:F2},{2:F2}) input=({3:F2},{4:F2}) finger={5}",
                        Player.transform.position.x,Player.transform.position.y,Player.transform.position.z,
                        savedSteering.x,savedSteering.y,steerFinger));
                }
            }
        }
        private void Finish()
        {
            if(State==MatchState.End)return;
            int winningTeam=1;
            float best=float.MinValue;
            for(int team=1;team<=4;team++)
                if(scores[team]>best)
                {
                    best=scores[team];winningTeam=team;
                }
            foreach(var pawn in players)
            {
                pawn.Wish=Vector3.zero;
                pawn.Active=false;
                if(pawn.Team==winningTeam)victoryActor=pawn;
            }
            State=MatchState.End;
            victoryReplayTimer=2.90f;
            if(victoryActor!=null)
            {
                orbitCamera.BeginVictoryFocus(victoryActor.transform);
                victoryActor.Celebrate();
                Effects.Burst(victoryActor.transform.position,
                              Colors.Team(winningTeam),55,3.8f);
            }
            Debug.Log("HERO_MATCH_VICTORY team="+winningTeam+
                " cameraFocus="+(victoryActor!=null));
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
        private void DrawEnemySignals(float logicalW,float logicalH)
        {
            if(Player==null||worldCamera==null)return;
            Vector3 playerNormal=Player.transform.position.normalized;
            foreach(PlanetActor other in players)
            {
                if(other==null||other.Team==1||!other.Active)continue;
                Vector3 direction=other.transform.position.normalized;
                if(Vector3.Dot(playerNormal,direction)<.34f)continue;
                Vector3 world=other.transform.position + direction*1.28f;
                Vector3 screen=worldCamera.WorldToViewportPoint(world);
                if(screen.z<=0f)continue;
                bool offscreen=screen.x<.04f||screen.x>.96f||
                               screen.y<.16f||screen.y>.88f;
                float x=Mathf.Clamp(screen.x*logicalW-44f,5f,logicalW-93f);
                float y=Mathf.Clamp((1f-screen.y)*logicalH-30f,199f,logicalH-235f);
                Color paint=Colors.Team(other.Team);
                paint.a=.84f;
                Plate(new Rect(x,y,89f,27f),paint);
                GUI.Label(new Rect(x+2f,y,85f,26f),
                    (offscreen?"! ":"")+"ВРАГ "+other.Team,smallStyle);
            }
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
                GUI.Label(new Rect(16,45,logicalW-32,55)," BRAINROT PLANET.IO",headerStyle);
                GUI.Label(new Rect(22,102,logicalW-44,74),"Десантируйся на планету. Оставляй след и замыкай его, чтобы захватить поверхность!",menuStyle);
                // Paged hero picker: unlike IMGUI scroll views, taps work reliably on Android.
                int pageSize=Mathf.Max(3,Mathf.Min(5,Mathf.FloorToInt((logicalH-440)/84f)));
                int pageCount=(characters.Length+pageSize-1)/pageSize;
                heroPage=Mathf.Clamp(heroPage,0,pageCount-1);
                GUI.Label(new Rect(84,184,logicalW-168,34),
                    "ВЫБЕРИ ГЕРОЯ  "+(heroPage+1)+"/"+pageCount,headerStyle);
                if(heroPage>0 && GUI.Button(new Rect(20,183,56,37),"<<",buttonStyle))
                    heroPage--;
                if(heroPage<pageCount-1 &&
                   GUI.Button(new Rect(logicalW-76,183,56,37),">>",buttonStyle))
                    heroPage++;
                for(int slot=0;slot<pageSize;slot++)
                {
                    int i=heroPage*pageSize+slot;
                    if(i>=characters.Length)break;
                    float y=222+slot*84;
                    Plate(new Rect(19,y,logicalW-38,78),
                        i==selectedHero?new Color(.90f,.25f,.52f,.85f):
                                        new Color(.25f,.34f,.52f,.72f));
                    if(GUI.Button(new Rect(28,y+7,logicalW-56,65),
                        characters[i]+"\n"+skillNames[i],buttonStyle))
                    {
                        selectedHero=i;
                        Debug.Log("HERO_MENU_SELECTED index="+i);
                    }
                }
                if(GUI.Button(new Rect(26,logicalH-166,logicalW-52,60)," ДЕСАНТИРОВАТЬСЯ",buttonStyle))StartGame();
                GUI.Label(new Rect(25,logicalH-94,logicalW-50,70),"145 секунд · 12 героев · планетная физика\nБез рекламы и интернет-подключения",smallStyle);
            }
            else if(State==MatchState.Playing)
            {
                Plate(new Rect(9,9,logicalW-18,76),new Color(.10f,.12f,.22f,.86f));
                GUI.Label(new Rect(14,12,logicalW-28,29)," BRAINROT PLANET.IO",headerStyle);
                GUI.Label(new Rect(15,42,logicalW-30,37),
                    " "+scores[1].ToString("F1")+"%    "+scores[2].ToString("F1")+"%    "+scores[3].ToString("F1")+"%"+
                    "\n "+lives+"     ⏱ "+Mathf.CeilToInt(matchTime)+" сек",smallStyle);
                if(orbitCamera != null && orbitCamera.IsDescending)
                {
                    Plate(new Rect(25,96,logicalW-50,42),new Color(.09f,.10f,.21f,.74f));
                    GUI.Label(new Rect(25,99,logicalW-50,34),"ПРИБЛИЖЕНИЕ К ПЛАНЕТЕ...",smallStyle);
                }
                else if(annTime>0)
                {
                    Plate(new Rect(25,96,logicalW-50,42),new Color(.09f,.10f,.21f,.74f));
                    GUI.Label(new Rect(25,99,logicalW-50,34),announcement,smallStyle);
                }
                // Floating steering input replaces the fixed bottom-left joystick.
                GUI.Label(new Rect(12,logicalH-65,logicalW-135,36),"СВАЙП: МЕНЯЙ НАПРАВЛЕНИЕ",smallStyle);
                if(Territory!=null && Territory.HasTrail(1))
                {
                    Plate(new Rect(25,145,logicalW-50,41),new Color(.80f,.17f,.42f,.75f));
                    GUI.Label(new Rect(30,149,logicalW-60,34),
                        "ЗАМКНИ ПЕТЛЮ: ВЕРНИСЬ НА РОЗОВУЮ ЗОНУ",smallStyle);
                }
                if(steerFinger>=0)
                    GUI.Label(new Rect(steerOrigin.x-25,steerOrigin.y-25,50,50),"+",headerStyle);
                Plate(skillArea,skillCooldown<=0?new Color(.91f,.31f,.56f,.90f):new Color(.37f,.39f,.53f,.83f));
                GUI.Label(skillArea,skillCooldown>0?skillNames[selectedHero]+"\n"+Mathf.CeilToInt(skillCooldown)+" сек":skillNames[selectedHero],smallStyle);
                Plate(jumpArea,new Color(.21f,.62f,.89f,.91f));
                GUI.Label(jumpArea,"\nПРЫЖОК",smallStyle);
                DrawEnemySignals(logicalW,logicalH);
            }
            else
            {
                // The centre of the planet stays visible during the victory
                // dance. The end-of-match information is placed above/below.
                int winner=1;
                float best=float.MinValue;
                for(int team=1;team<=4;team++)
                    if(scores[team]>best){best=scores[team];winner=team;}
                Plate(new Rect(0,0,logicalW,145),
                    new Color(.06f,.08f,.15f,.85f));
                GUI.Label(new Rect(20,17,logicalW-40,57),
                    winner==1?" ПОБЕДА!":" ПЛАНЕТА ЗАХВАЧЕНА!",headerStyle);
                GUI.Label(new Rect(24,72,logicalW-48,68),
                    "Твоя территория: "+scores[1].ToString("F1")+
                    "%\nПобедитель: "+(winner==1?"ТЫ":("КОМАНДА "+winner)),
                    menuStyle);
                Plate(new Rect(0,logicalH-205,logicalW,205),
                    new Color(.06f,.08f,.15f,.80f));
                if(GUI.Button(new Rect(26,logicalH-190,logicalW-52,65),
                              "ИГРАТЬ ЕЩЁ",buttonStyle))StartGame();
                if(GUI.Button(new Rect(26,logicalH-115,logicalW-52,60),
                              "ВЫБРАТЬ ГЕРОЯ",buttonStyle))
                {
                    if(worldRoot!=null)Destroy(worldRoot.gameObject);
                    State=MatchState.Menu;
                    orbitCamera.ClearTarget();
                }
            }
            GUI.matrix=old;
        }
    }
}
