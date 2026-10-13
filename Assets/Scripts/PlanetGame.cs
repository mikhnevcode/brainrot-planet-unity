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
        private static readonly Color[] HeroColors={
            new Color(.08f,.72f,1f),new Color(.32f,.77f,.20f),
            new Color(.24f,.82f,.96f),new Color(1f,.48f,.12f),
            new Color(1f,.30f,.67f),new Color(.61f,.31f,.94f),
            new Color(.20f,.77f,.37f),new Color(.82f,.50f,.16f),
            new Color(.49f,.82f,.20f),new Color(.22f,.70f,1f),
            new Color(1f,.40f,.22f),new Color(.91f,.24f,.98f)};
        private static readonly Color[] HeroAccents={
            new Color(.77f,.96f,1f),new Color(.92f,1f,.33f),
            new Color(.82f,.98f,1f),new Color(1f,.88f,.27f),
            new Color(1f,.77f,.91f),new Color(.90f,.72f,1f),
            new Color(.76f,1f,.43f),new Color(1f,.79f,.34f),
            new Color(.91f,1f,.48f),new Color(.70f,.94f,1f),
            new Color(1f,.82f,.35f),new Color(.52f,.94f,1f)};
        private static readonly Color[] DifficultyColors={
            new Color(.15f,.72f,.92f),new Color(1f,.57f,.14f),new Color(.94f,.20f,.38f)};
        private static readonly string[] DifficultyLabels={"ЛЕГКО","СРЕДНЕ","СЛОЖНО"};
        private int selectedHero=0;
        private AiDifficulty chosenDifficulty=AiDifficulty.Medium;
        public AiDifficulty Difficulty => chosenDifficulty;
        private bool resolvingHeroSkill;
        private float matchTime,skillCooldown,annTime,scoreTick;
        private float playerLandingGrace;
        private float[] scores = new float[5];
        private int lives=3;
        private string announcement="";
        private OrbitCamera orbitCamera;
        private PlanetActor victoryActor;
        private AiBalanceTelemetry balanceTelemetry;
        private float victoryReplayTimer;
        private Camera worldCamera;
        private float guiScale=1f;
        private Vector2 movement;
        private Vector2 savedSteering;
        private bool autoMoving;
        private float nextInputDiagnostic;
        private float threatTick;
        private float nearestEnemyDegrees=180f;
        private bool playerBoostWasActive;
        private bool playerSlowWasActive;
        private int steerFinger=-1;
        private Vector2 steerOrigin;
        private int heroPage;
        private Rect skillArea,jumpArea;
        private Texture2D whitePixel,softCircle;
        private RenderTexture heroPreviewTexture;
        private Camera heroPreviewCamera;
        private GameObject heroPreviewRoot,heroPreviewRig;
        private int previewHero=-1;
        private bool heroMenuLogged;
        private GUIStyle headerStyle,menuStyle,buttonStyle,smallStyle,enemyArrowStyle;
        private GUIStyle heroNameStyle,heroSkillStyle,heroBadgeStyle,clearButtonStyle;
        private float pathUpdate;
        private Transform worldRoot;
        private bool sceneLoaded;

        private void Awake()
        {
            Application.targetFrameRate=60;
            Screen.sleepTimeout=SleepTimeout.NeverSleep;
            Input.multiTouchEnabled=true;
            chosenDifficulty=(AiDifficulty)Mathf.Clamp(
                PlayerPrefs.GetInt("BrainrotAiDifficulty",1),0,2);
            Debug.Log("AI_DIFFICULTY_LOADED level="+chosenDifficulty);
            if(Debug.isDebugBuild)gameObject.AddComponent<GamePerformanceProbe>();
            var requiredTypes=new GameObject("Runtime collider types");
            requiredTypes.AddComponent<BoxCollider>();
            requiredTypes.AddComponent<CapsuleCollider>();
            Destroy(requiredTypes);
            whitePixel=new Texture2D(1,1);
            whitePixel.SetPixel(0,0,Color.white);whitePixel.Apply();
            softCircle=CreateSoftCircle(96);
            SetupCamera();
            SetupLighting();
        }
        private static Texture2D CreateSoftCircle(int size)
        {
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name="Hero menu soft glow";
            texture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float px=(x+.5f)/size*2f-1f;
                float py=(y+.5f)/size*2f-1f;
                float alpha=Mathf.Clamp01(1f-Mathf.Sqrt(px*px+py*py));
                pixels[y*size+x]=new Color(1f,1f,1f,alpha*alpha);
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            return texture;
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
            HideHeroPreview();
            Debug.Log("AI_DIFFICULTY_MATCH level="+chosenDifficulty);
            if(worldRoot!=null)Destroy(worldRoot.gameObject);
            foreach(LineRenderer l in pathRenderers)if(l!=null)Destroy(l.gameObject);
            Array.Clear(pathRenderers,0,pathRenderers.Length);
            players.Clear();lives=3;
            victoryActor=null;victoryReplayTimer=0f;
            balanceTelemetry=null;
            matchTime=145f;skillCooldown=0f;scoreTick=0f;
            playerLandingGrace=AiBalanceTuning.StartGrace(chosenDifficulty);
            movement=Vector2.zero;savedSteering=Vector2.zero;autoMoving=false;steerFinger=-1;
            nextInputDiagnostic=0f;
            threatTick=0f;nearestEnemyDegrees=180f;
            playerBoostWasActive=false;playerSlowWasActive=false;
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
            // Wider opening on Easy, compact combat sector on Hard.
            // All spawn cores remain separated; no bot receives a speed bonus.
            float botSpawnOffset=AiBalanceTuning.SpawnAngle(chosenDifficulty);
            float radial=Mathf.Tan(botSpawnOffset*Mathf.Deg2Rad);
            Vector3 centre=new Vector3(.91f,.20f,.33f).normalized;
            Vector3 east=Vector3.Cross(Vector3.up,centre).normalized;
            Vector3 north=Vector3.Cross(centre,east).normalized;
            Vector3[] spawnDirections={
                centre,
                (centre+east*radial).normalized,
                (centre-east*radial).normalized,
                (centre+north*radial).normalized
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
            // Stop rigidbody-to-rigidbody pushing, but keep terrain collisions
            // and the independent logical trail-crossing game rules.
            ApplyActorPassThrough("spawn");

            // The player now falls from above the planet during the camera approach.
            Player.BeginDropFromOrbit();
            orbitCamera.BeginDescent(Player.transform);
            var gems=new GameObject("Crystal pickups").AddComponent<CrystalSystem>();
            gems.transform.SetParent(worldRoot);
            gems.Configure(this);
            CreateStars();
            State=MatchState.Playing;
            sceneLoaded=true;
            if(Debug.isDebugBuild)
            {
                balanceTelemetry=gameObjectRoot.AddComponent<AiBalanceTelemetry>();
                balanceTelemetry.Configure(this,players,chosenDifficulty);
            }
            Announce(" ДЕСАНТ НА ПЛАНЕТУ!");
        }
        public void ApplyActorPassThrough(string reason)
        {
            int applied=ActorCollisionRules.Apply(players);
            int ignored=ActorCollisionRules.CountIgnoredPairs(players);
            int expected=ActorCollisionRules.PairCount(players.Count);
            Debug.Log("HERO_BODY_PASS_THROUGH reason="+reason+
                      " ignoredPairs="+ignored+"/"+expected+
                      " applied="+applied+
                      " radius="+ActorCollisionRules.Radius.ToString("F2"));
            if(ignored!=expected)
                Debug.LogError("HERO_BODY_PASS_THROUGH_INCOMPLETE "+
                    ignored+"/"+expected+" reason="+reason);
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
            if(balanceTelemetry!=null)balanceTelemetry.RecordCapture(team);
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
            if(balanceTelemetry!=null)balanceTelemetry.RecordTrailLost(team);
            if(team==1 && playerLandingGrace>0f)
            {
                // Player is invulnerable to trail hits while landing and learning to move.
                return;
            }
            PlanetActor actor=players.Find(p=>p.Team==team);
            if(actor!=null)actor.HitTrail();
        }
        public void OnPlayerRespawn()
        {
            savedSteering=Vector2.zero;
            movement=Vector2.zero;
            autoMoving=false;
            steerFinger=-1;
            if(Player!=null)Player.Wish=Vector3.zero;
            playerLandingGrace=Mathf.Max(playerLandingGrace,
                AiBalanceTuning.RespawnGrace(chosenDifficulty));
            Announce(" ПОТЕРЯН СЛЕД! СДЕЛАЙ НОВЫЙ СВАЙП");
            Debug.Log("PLAYER_RESPAWN_RECOVERY level="+chosenDifficulty+
                " seconds="+playerLandingGrace.ToString("F1"));
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
            if(State==MatchState.Menu)
            {
                EnsureHeroPreview();
                if(heroPreviewRig!=null)
                {
                    float portraitYaw=selectedHero==1?148f:0f;
                    heroPreviewRig.transform.localRotation=Quaternion.Euler(
                        0f,portraitYaw+Mathf.Sin(Time.unscaledTime*.72f)*7f,0f);
                }
                return;
            }
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
            matchTime-=dt;
            float cooldownBefore=skillCooldown;
            skillCooldown=Mathf.Max(0,skillCooldown-dt);
            annTime=Mathf.Max(0,annTime-dt);
            HandleInput();
            if(cooldownBefore>0f && skillCooldown<=0f)
            {
                Announce(" СПОСОБНОСТЬ ГОТОВА!");
                if(Player!=null)Effects.Burst(Player.transform.position,
                    Colors.Team(1),14,1.2f);
                Debug.Log("PLAYER_SKILL_READY hero="+selectedHero);
            }
            bool boostActive=Player!=null && Player.SpeedSeconds>0f;
            bool slowActive=Player!=null && Player.SlowSeconds>0f;
            if(playerBoostWasActive && !boostActive)
            {
                Announce(" УСКОРЕНИЕ ЗАКОНЧИЛОСЬ");
                Debug.Log("PLAYER_SPEED_EFFECT_ENDED");
            }
            if(playerSlowWasActive && !slowActive)
            {
                Announce(" ЗАМЕДЛЕНИЕ ЗАКОНЧИЛОСЬ");
                Debug.Log("PLAYER_SLOW_EFFECT_ENDED");
            }
            playerBoostWasActive=boostActive;playerSlowWasActive=slowActive;
            threatTick-=dt;
            if(threatTick<=0f)
            {
                threatTick=.20f;
                nearestEnemyDegrees=180f;
                if(Player!=null)
                {
                    Vector3 playerDirection=Player.transform.position.normalized;
                    for(int i=0;i<players.Count;i++)
                    {
                        PlanetActor candidate=players[i];
                        if(candidate==null||candidate.Team==1||!candidate.Active)continue;
                        nearestEnemyDegrees=Mathf.Min(nearestEnemyDegrees,
                            Vector3.Angle(playerDirection,candidate.transform.position.normalized));
                    }
                }
            }
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
            for(int team=1;team<=4;team++)scores[team]=Territory.Percentage(team);
            int winningTeam=1;
            float best=float.MinValue;
            for(int team=1;team<=4;team++)
                if(scores[team]>best)
                {
                    best=scores[team];winningTeam=team;
                }
            if(balanceTelemetry!=null)balanceTelemetry.Report(winningTeam,scores);
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
                Vector3 stageCenter=CreateVictoryStage(winningTeam);
                Vector3 winnerPosition=stageCenter+Vector3.up*.37f;
                Vector3 cameraSide=Vector3.back;
                victoryActor.EnterVictoryStage(winnerPosition,cameraSide);
                orbitCamera.BeginVictoryFocus(victoryActor.transform,stageCenter);
                victoryActor.Celebrate();
                Effects.Burst(victoryActor.transform.position,
                              Colors.Team(winningTeam),55,3.8f);
            }
            Debug.Log("HERO_MATCH_VICTORY team="+winningTeam+
                " cameraFocus="+(victoryActor!=null));
        }
        private Vector3 CreateVictoryStage(int winningTeam)
        {
            Vector3 center=new Vector3(0f,38f,-55f);
            GameObject baseObject=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObject.name="Stable victory platform base";
            baseObject.transform.SetParent(worldRoot);
            baseObject.transform.position=center-Vector3.up*.18f;
            baseObject.transform.localScale=new Vector3(3.35f,.28f,3.35f);
            baseObject.GetComponent<Renderer>().sharedMaterial=
                Colors.New(new Color(.075f,.09f,.16f));
            Collider baseCollider=baseObject.GetComponent<Collider>();
            if(baseCollider!=null)Destroy(baseCollider);

            GameObject top=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name="Stable victory platform team colour";
            top.transform.SetParent(worldRoot);
            top.transform.position=center;
            top.transform.localScale=new Vector3(3.05f,.20f,3.05f);
            top.GetComponent<Renderer>().sharedMaterial=
                Colors.New(Colors.Team(winningTeam),true);
            Collider topCollider=top.GetComponent<Collider>();
            if(topCollider!=null)Destroy(topCollider);
            Debug.Log("VICTORY_STAGE_READY team="+winningTeam+
                " worldUp=true center="+center);
            return center;
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
            enemyArrowStyle=new GUIStyle(GUI.skin.label){fontSize=40,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            enemyArrowStyle.normal.textColor=Color.white;
            buttonStyle=new GUIStyle(GUI.skin.button){fontSize=17,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            heroNameStyle=new GUIStyle(GUI.skin.label){fontSize=15,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,wordWrap=true};
            heroNameStyle.normal.textColor=Color.white;
            heroSkillStyle=new GUIStyle(GUI.skin.label){fontSize=11,alignment=TextAnchor.MiddleLeft,wordWrap=true};
            heroSkillStyle.normal.textColor=new Color(.92f,.96f,1f);
            heroBadgeStyle=new GUIStyle(GUI.skin.label){fontSize=11,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            heroBadgeStyle.normal.textColor=Color.white;
            clearButtonStyle=new GUIStyle(GUI.skin.button){fontSize=16,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            clearButtonStyle.normal.background=null;clearButtonStyle.hover.background=null;
            clearButtonStyle.active.background=null;clearButtonStyle.focused.background=null;
            clearButtonStyle.normal.textColor=Color.white;clearButtonStyle.hover.textColor=Color.white;
            clearButtonStyle.active.textColor=Color.white;clearButtonStyle.focused.textColor=Color.white;
        }
        private static void SetLayerRecursively(GameObject root,int layer)
        {
            root.layer=layer;
            foreach(Transform child in root.transform)SetLayerRecursively(child.gameObject,layer);
        }
        private void EnsureHeroPreview()
        {
            const int previewLayer=30;
            if(heroPreviewCamera==null)
            {
                var cameraObject=new GameObject("Hero menu portrait camera");
                heroPreviewCamera=cameraObject.AddComponent<Camera>();
                heroPreviewCamera.clearFlags=CameraClearFlags.SolidColor;
                heroPreviewCamera.backgroundColor=new Color(0f,0f,0f,0f);
                heroPreviewCamera.cullingMask=1<<previewLayer;
                heroPreviewCamera.fieldOfView=31f;
                heroPreviewCamera.nearClipPlane=.03f;heroPreviewCamera.farClipPlane=30f;
                heroPreviewCamera.allowHDR=false;heroPreviewCamera.allowMSAA=true;
                heroPreviewTexture=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);
                heroPreviewTexture.name="Selected hero portrait";
                heroPreviewTexture.antiAliasing=4;
                heroPreviewTexture.Create();
                heroPreviewCamera.targetTexture=heroPreviewTexture;
                if(worldCamera!=null)worldCamera.cullingMask&=~(1<<previewLayer);

                var keyObject=new GameObject("Hero menu key light");
                keyObject.transform.SetParent(cameraObject.transform,false);
                keyObject.transform.localRotation=Quaternion.Euler(28f,-32f,0f);
                var key=keyObject.AddComponent<Light>();key.type=LightType.Directional;
                key.color=new Color(1f,.88f,.73f);key.intensity=1.25f;key.cullingMask=1<<previewLayer;
                var fillObject=new GameObject("Hero menu fill light");
                fillObject.transform.SetParent(cameraObject.transform,false);
                fillObject.transform.localPosition=new Vector3(-2f,1.5f,-2f);
                var fill=fillObject.AddComponent<Light>();fill.type=LightType.Point;
                fill.color=new Color(.28f,.68f,1f);fill.intensity=2.3f;fill.range=7f;
                fill.cullingMask=1<<previewLayer;
            }
            heroPreviewCamera.enabled=true;
            if(previewHero==selectedHero&&heroPreviewRoot!=null)return;
            if(heroPreviewRoot!=null)Destroy(heroPreviewRoot);
            heroPreviewRoot=new GameObject("Selected hero menu preview");
            heroPreviewRig=new GameObject("Portrait turntable");
            heroPreviewRig.transform.SetParent(heroPreviewRoot.transform,false);
            AvatarArt.Make(heroPreviewRig.transform,selectedHero,HeroColors[selectedHero]);
            heroPreviewRig.transform.localRotation=Quaternion.Euler(0f,selectedHero==1?148f:0f,0f);
            SetLayerRecursively(heroPreviewRoot,previewLayer);

            Renderer[] renderers=heroPreviewRig.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length>0)
            {
                Bounds bounds=renderers[0].bounds;
                for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
                float footprint=Mathf.Max(bounds.size.x,bounds.size.z)*.72f;
                var platform=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                platform.name="Hero portrait colour podium";
                platform.transform.SetParent(heroPreviewRoot.transform,false);
                platform.transform.position=new Vector3(bounds.center.x,bounds.min.y-.035f,bounds.center.z);
                platform.transform.localScale=new Vector3(Mathf.Max(.42f,footprint),.035f,Mathf.Max(.42f,footprint));
                platform.GetComponent<Renderer>().sharedMaterial=Colors.New(HeroColors[selectedHero],true);
                Collider collider=platform.GetComponent<Collider>();if(collider!=null)Destroy(collider);
                SetLayerRecursively(platform,previewLayer);

                float largest=Mathf.Max(bounds.size.y,Mathf.Max(bounds.size.x,bounds.size.z));
                float portraitDistanceScale=selectedHero==1?1.65f:1.85f;
                float distance=Mathf.Max(1.75f,largest*portraitDistanceScale);
                Vector3 target=bounds.center+Vector3.up*(bounds.size.y*.035f);
                heroPreviewCamera.transform.position=target+new Vector3(0f,bounds.size.y*.04f,-distance);
                heroPreviewCamera.transform.LookAt(target);
            }
            previewHero=selectedHero;
            Debug.Log("HERO_MENU_PREVIEW_READY index="+selectedHero+
                " color="+ColorUtility.ToHtmlStringRGB(HeroColors[selectedHero]));
        }
        private void HideHeroPreview()
        {
            if(heroPreviewCamera!=null)heroPreviewCamera.enabled=false;
            if(heroPreviewRoot!=null)Destroy(heroPreviewRoot);
            heroPreviewRoot=null;heroPreviewRig=null;previewHero=-1;
        }
        private void OnDestroy()
        {
            if(heroPreviewTexture!=null)
            {
                heroPreviewTexture.Release();
                Destroy(heroPreviewTexture);
            }
            if(softCircle!=null)Destroy(softCircle);
            if(whitePixel!=null)Destroy(whitePixel);
        }
        private void DrawGlow(Rect rect,Color color)
        {
            Color original=GUI.color;GUI.color=color;
            GUI.DrawTexture(rect,softCircle,ScaleMode.StretchToFill,true);GUI.color=original;
        }
        private bool FlatButton(Rect rect,string label,Color color)
        {
            Plate(new Rect(rect.x+2f,rect.y+4f,rect.width,rect.height),new Color(0f,0f,0f,.30f));
            Plate(rect,color);
            Plate(new Rect(rect.x+2f,rect.y+2f,rect.width-4f,2f),new Color(1f,1f,1f,.24f));
            return GUI.Button(rect,label,clearButtonStyle);
        }
        private void DrawEnemySignals(float logicalW,float logicalH)
        {
            if(Player==null||worldCamera==null)return;
            foreach(PlanetActor other in players)
            {
                if(other==null||other.Team==1||!other.Active)continue;
                Vector3 direction=other.transform.position.normalized;
                Vector3 world=other.transform.position + direction*1.28f;
                Vector3 screen=worldCamera.WorldToViewportPoint(world);
                Vector2 arrowDirection=new Vector2(screen.x-.5f,.5f-screen.y);
                if(screen.z<0f)arrowDirection=-arrowDirection;
                if(arrowDirection.sqrMagnitude<.001f)arrowDirection=Vector2.up;
                arrowDirection.Normalize();
                bool onScreen=screen.z>0f&&screen.x>.08f&&screen.x<.92f&&
                              screen.y>.19f&&screen.y<.86f;
                Vector2 center=new Vector2(logicalW*.5f,logicalH*.53f);
                Vector2 point=onScreen?
                    new Vector2(screen.x*logicalW,(1f-screen.y)*logicalH):
                    center+new Vector2(arrowDirection.x*logicalW*.41f,
                                       arrowDirection.y*logicalH*.30f);
                point.x=Mathf.Clamp(point.x,24f,logicalW-24f);
                point.y=Mathf.Clamp(point.y,205f,logicalH-235f);
                float angle=Mathf.Atan2(arrowDirection.y,arrowDirection.x)*
                            Mathf.Rad2Deg+90f;
                Matrix4x4 oldMatrix=GUI.matrix;
                GUIUtility.RotateAroundPivot(angle,point);
                Color oldColor=GUI.color;
                GUI.color=new Color(0f,0f,0f,.72f);
                GUI.Label(new Rect(point.x-17f,point.y-15f,38f,38f),"^",enemyArrowStyle);
                GUI.color=Colors.Team(other.Team);
                GUI.Label(new Rect(point.x-19f,point.y-17f,38f,38f),"^",enemyArrowStyle);
                GUI.color=oldColor;
                GUI.matrix=oldMatrix;
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
                Color heroColor=HeroColors[selectedHero];
                Color heroAccent=HeroAccents[selectedHero];
                Plate(new Rect(0,0,logicalW,logicalH),new Color(.025f,.035f,.095f,1f));
                DrawGlow(new Rect(-105,-95,330,330),new Color(heroColor.r,heroColor.g,heroColor.b,.43f));
                DrawGlow(new Rect(logicalW-205,145,330,330),new Color(heroAccent.r,heroAccent.g,heroAccent.b,.20f));
                DrawGlow(new Rect(65,logicalH-300,300,300),new Color(.75f,.12f,.95f,.13f));
                Plate(new Rect(0,0,logicalW,58f),new Color(.04f,.055f,.14f,.82f));
                GUI.Label(new Rect(14,8,logicalW-28,32),"BRAINROT PLANET.IO",headerStyle);
                GUI.Label(new Rect(14,36,logicalW-28,19),"СОБЕРИ СВОЕГО ЧЕМПИОНА",smallStyle);

                Rect spotlight=new Rect(18,68,logicalW-36,188);
                Plate(new Rect(spotlight.x-3,spotlight.y-3,spotlight.width+6,spotlight.height+6),
                    new Color(heroColor.r,heroColor.g,heroColor.b,.90f));
                Plate(spotlight,new Color(.045f,.065f,.15f,.94f));
                Plate(new Rect(spotlight.x,spotlight.y,7f,spotlight.height),heroAccent);
                DrawGlow(new Rect(18,64,195,195),new Color(heroColor.r,heroColor.g,heroColor.b,.44f));
                if(heroPreviewTexture!=null)
                    GUI.DrawTexture(new Rect(21,71,176,180),heroPreviewTexture,ScaleMode.ScaleToFit,true);
                Plate(new Rect(201,82,79,24),new Color(heroColor.r,heroColor.g,heroColor.b,.90f));
                GUI.Label(new Rect(201,82,79,24),"ГЕРОЙ "+(selectedHero+1).ToString("00"),heroBadgeStyle);
                GUI.Label(new Rect(200,109,spotlight.width-193,61),characters[selectedHero].Trim(),heroNameStyle);
                GUI.Label(new Rect(200,172,spotlight.width-193,20),"СУПЕРСИЛА",heroSkillStyle);
                GUI.Label(new Rect(200,191,spotlight.width-193,43),skillNames[selectedHero],heroNameStyle);

                // Four large colour cards per page remain reliable touch targets on Android.
                const int pageSize=4;
                int pageCount=(characters.Length+pageSize-1)/pageSize;
                heroPage=Mathf.Clamp(heroPage,0,pageCount-1);
                GUI.Label(new Rect(83,270,logicalW-166,38),
                    "ВЫБОР ГЕРОЯ  "+(heroPage+1)+"/"+pageCount,headerStyle);
                if(heroPage>0 && FlatButton(new Rect(20,271,53,36),"<",new Color(heroColor.r,heroColor.g,heroColor.b,.84f)))
                {
                    heroPage--;
                    Debug.Log("HERO_MENU_PAGE page="+(heroPage+1)+"/"+pageCount);
                }
                if(heroPage<pageCount-1 &&
                   FlatButton(new Rect(logicalW-73,271,53,36),">",new Color(heroColor.r,heroColor.g,heroColor.b,.84f)))
                {
                    heroPage++;
                    Debug.Log("HERO_MENU_PAGE page="+(heroPage+1)+"/"+pageCount);
                }
                float cardWidth=(logicalW-52f)/2f;
                for(int slot=0;slot<pageSize;slot++)
                {
                    int i=heroPage*pageSize+slot;
                    if(i>=characters.Length)break;
                    int column=slot%2,row=slot/2;
                    Rect card=new Rect(21f+column*(cardWidth+10f),316f+row*88f,cardWidth,80f);
                    Color cardColor=HeroColors[i],cardAccent=HeroAccents[i];
                    Plate(new Rect(card.x+2f,card.y+4f,card.width,card.height),new Color(0f,0f,0f,.32f));
                    if(i==selectedHero)
                        Plate(new Rect(card.x-3f,card.y-3f,card.width+6f,card.height+6f),cardAccent);
                    Plate(card,Color.Lerp(new Color(.045f,.06f,.14f,.98f),cardColor,.38f));
                    Plate(new Rect(card.x,card.y,6f,card.height),cardColor);
                    Plate(new Rect(card.x+card.width-31f,card.y+7f,24f,22f),new Color(cardColor.r,cardColor.g,cardColor.b,.93f));
                    GUI.Label(new Rect(card.x+card.width-31f,card.y+7f,24f,22f),(i+1).ToString("00"),heroBadgeStyle);
                    GUI.Label(new Rect(card.x+13f,card.y+5f,card.width-48f,39f),characters[i].Trim(),heroNameStyle);
                    GUI.Label(new Rect(card.x+13f,card.y+44f,card.width-21f,29f),skillNames[i],heroSkillStyle);
                    if(GUI.Button(card,"",clearButtonStyle))
                    {
                        selectedHero=i;
                        previewHero=-1;
                        Debug.Log("HERO_MENU_SELECTED index="+i+" name="+characters[i].Trim());
                    }
                }
                if(!heroMenuLogged)
                {
                    heroMenuLogged=true;
                    Debug.Log("HERO_MENU_PRESENTED colorfulCards=12 pages="+pageCount+" preview=3D");
                }

                float diffY=logicalH-226f;
                float diffWidth=(logicalW-68f)/3f;
                GUI.Label(new Rect(22,diffY-30f,logicalW-44f,25f),"СЛОЖНОСТЬ СОПЕРНИКОВ",smallStyle);
                for(int i=0;i<3;i++)
                {
                    float x=26f+i*(diffWidth+8f);
                    bool picked=(int)chosenDifficulty==i;
                    if(picked)Plate(new Rect(x-3f,diffY-3f,diffWidth+6f,48f),HeroAccents[selectedHero]);
                    Color difficultyColor=picked?DifficultyColors[i]:Color.Lerp(new Color(.08f,.10f,.20f),DifficultyColors[i],.27f);
                    if(FlatButton(new Rect(x,diffY,diffWidth,42f),DifficultyLabels[i],difficultyColor))
                    {
                        chosenDifficulty=(AiDifficulty)i;
                        PlayerPrefs.SetInt("BrainrotAiDifficulty",i);
                        PlayerPrefs.Save();
                        Debug.Log("AI_DIFFICULTY_SELECTED level="+chosenDifficulty);
                    }
                }
                if(FlatButton(new Rect(26,logicalH-166,logicalW-52,60),
                    "ДЕСАНТИРОВАТЬСЯ  >",Color.Lerp(heroColor,heroAccent,.30f)))StartGame();
                GUI.Label(new Rect(25,logicalH-94,logicalW-50,70),
                    "145 секунд · 12 героев · планетная физика\nБез рекламы и интернет-подключения",smallStyle);
            }
            else if(State==MatchState.Playing)
            {
                Plate(new Rect(9,9,logicalW-18,91),new Color(.10f,.12f,.22f,.86f));
                GUI.Label(new Rect(14,12,logicalW-28,29)," BRAINROT PLANET.IO",headerStyle);
                GUI.Label(new Rect(15,39,logicalW-30,55),
                    "ТЫ "+scores[1].ToString("F1")+"%   СИНИЙ "+scores[2].ToString("F1")+
                    "%   ЖЁЛТЫЙ "+scores[3].ToString("F1")+"%   ЗЕЛЁНЫЙ "+scores[4].ToString("F1")+"%"+
                    "\nЖИЗНИ "+lives+"     ⏱ "+Mathf.CeilToInt(matchTime)+" сек",smallStyle);
                if(orbitCamera != null && orbitCamera.IsDescending)
                {
                    Plate(new Rect(25,106,logicalW-50,42),new Color(.09f,.10f,.21f,.74f));
                    GUI.Label(new Rect(25,109,logicalW-50,34),"ПРИБЛИЖЕНИЕ К ПЛАНЕТЕ...",smallStyle);
                }
                else if(annTime>0)
                {
                    Plate(new Rect(25,106,logicalW-50,42),new Color(.09f,.10f,.21f,.74f));
                    GUI.Label(new Rect(25,109,logicalW-50,34),announcement,smallStyle);
                }
                // Floating steering input replaces the fixed bottom-left joystick.
                string motionLabel=autoMoving
                    ? "ДВИЖЕНИЕ ПРОДОЛЖАЕТСЯ: "+GameplayFeedback.DirectionLabel(savedSteering)
                    : GameplayFeedback.DirectionLabel(Vector2.zero);
                GUI.Label(new Rect(12,logicalH-72,logicalW-135,43),motionLabel,smallStyle);
                if(Territory!=null && Territory.HasTrail(1))
                {
                    int trailCells=Territory.Path(1).Count;
                    TrailRisk risk=GameplayFeedback.TrailRiskFor(trailCells,nearestEnemyDegrees);
                    Color riskColor=risk==TrailRisk.Critical?new Color(.95f,.08f,.20f,.88f):
                        risk==TrailRisk.Exposed?new Color(.92f,.28f,.10f,.82f):
                        new Color(.80f,.17f,.42f,.75f);
                    string riskText=risk==TrailRisk.Critical?"КРИТИЧЕСКИЙ РИСК! ЗАМКНИ ПЕТЛЮ":
                        risk==TrailRisk.Exposed?"ОТКРЫТЫЙ СЛЕД: ПОРА ВОЗВРАЩАТЬСЯ":
                        "КОРОТКАЯ ПЕТЛЯ: ВЕРНИСЬ НА РОЗОВУЮ ЗОНУ";
                    Plate(new Rect(25,154,logicalW-50,41),riskColor);
                    GUI.Label(new Rect(30,158,logicalW-60,34),riskText,smallStyle);
                }
                if(playerLandingGrace>0f && (orbitCamera==null || !orbitCamera.IsDescending))
                {
                    Plate(new Rect(logicalW-151,201,126,31),new Color(.16f,.62f,.84f,.82f));
                    GUI.Label(new Rect(logicalW-149,202,122,28),
                        "ЗАЩИТА "+Mathf.CeilToInt(playerLandingGrace)+" сек",smallStyle);
                }
                else if(nearestEnemyDegrees<15f)
                {
                    Plate(new Rect(25,201,logicalW-50,34),new Color(.92f,.12f,.20f,.82f));
                    GUI.Label(new Rect(30,203,logicalW-60,29),
                        Territory!=null&&Territory.HasTrail(1)?"ВРАГ РЯДОМ — ЗАЩИТИ СЛЕД!":"ВРАГ ОЧЕНЬ БЛИЗКО!",smallStyle);
                }
                if(steerFinger>=0)
                    GUI.Label(new Rect(steerOrigin.x-25,steerOrigin.y-25,50,50),"+",headerStyle);
                Plate(skillArea,skillCooldown<=0?new Color(.91f,.31f,.56f,.90f):new Color(.37f,.39f,.53f,.83f));
                string skillLabel=skillCooldown>0?skillNames[selectedHero]+"\n"+Mathf.CeilToInt(skillCooldown)+" сек":skillNames[selectedHero]+"\nГОТОВО";
                if(Player!=null&&Player.SpeedSeconds>0f)skillLabel="УСКОРЕНИЕ\n"+Mathf.CeilToInt(Player.SpeedSeconds)+" сек";
                else if(Player!=null&&Player.SlowSeconds>0f)skillLabel="ЗАМЕДЛЕНИЕ\n"+Mathf.CeilToInt(Player.SlowSeconds)+" сек";
                GUI.Label(skillArea,skillLabel,smallStyle);
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