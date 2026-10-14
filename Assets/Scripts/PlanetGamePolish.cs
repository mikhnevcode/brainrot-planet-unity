using UnityEngine;

namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        public bool IsPaused=>paused;
        private bool paused,newRecord;
        private int capturesThisMatch,captureStreak,tutorialStep;
        private float bestCapture,captureReward,captureRewardTime,damageFlash;
        private string finishReason="ВРЕМЯ ВЫШЛО";
        private Texture2D portraitAtlas;
        private GUIStyle numberStyle,captionStyle;
        private Texture2D roundedPanelTexture;
        private GUIStyle roundedPanelStyle;
        private static readonly string[] SkillHelp={
            "Рывок на 3,5 секунды. Быстро вернись к своей краске или перехвати чужой след.",
            "Три бомбы летят вперёд веером. Закрась площадку на пути и расширь владения.",
            "Ледяная волна замедляет соседних соперников. Применяй, когда твоей петле угрожают.",
            "Турбины дают ускорение на 4 секунды. Используй для рискованной петли и возвращения.",
            "Пируэт закрашивает круг вокруг тебя. Помогает расширить владения возле границы.",
            "Теневой рывок даёт скорость и небольшую область краски. Подходит для внезапной атаки.",
            "Корни закрашивают землю и замедляют соседей. Защищай территорию и свой след.",
            "Барабанный удар закрашивает круг и замедляет врагов. Дождись их приближения.",
            "Кактусная буря закрашивает землю и мешает соседним соперникам.",
            "Воздушный танец помогает ускориться и обойти соперника.",
            "Пружина подбрасывает героя. При приземлении возникает круг краски.",
            "Три креветочных всплеска покрывают землю. Расширяй владения в выбранном направлении.",
            "Брюшной плеск закрашивает круг. Используй у края своей территории."};

        private void SetupPolish()
        {
            gameObject.AddComponent<GameSound>();
            worldCamera.gameObject.AddComponent<AudioListener>();
            // A camera-relative fill keeps faces readable on both hemispheres.
            var fill=new GameObject("Soft camera fill");fill.transform.SetParent(worldCamera.transform,false);
            fill.transform.localRotation=Quaternion.Euler(15,-25,0);
            var light=fill.AddComponent<Light>();light.type=LightType.Directional;
            light.color=new Color(.75f,.85f,1f);light.intensity=.72f;light.shadows=LightShadows.None;
            light.cullingMask=~(1<<30);
            portraitAtlas=Resources.Load<Texture2D>("HeroPortraits064");
        }
        private bool IsTrailProtected(int team)=>DedicatedMatchServer?networkGrace[team]>0f:
            team==LocalTeam?playerLandingGrace>0f:offlineBotGrace[team]>0f;
        private void SetPaused(bool value)
        {
            if(DedicatedMatchServer)return;
            bool changed=paused!=value;
            paused=value;Time.timeScale=value&&!IsNetworkMatch?0f:1f;
            if(IsNetworkReplica && value){inputButtons=0;if(Player!=null)Player.Wish=Vector3.zero;}
            GameSound.PauseGameplay(value);
            steerFinger=-1;
            if(changed)Debug.Log((value?"MATCH_PAUSED":"MATCH_RESUMED")+" remaining="+matchTime.ToString("F2"));
        }
        private void OnApplicationPause(bool value){if(value&&State==MatchState.Playing)SetPaused(true);}
        private void OnApplicationFocus(bool value){if(!value&&State==MatchState.Playing)SetPaused(true);}
        private void InitPolishStyles()
        {
            if(numberStyle!=null)return;
            numberStyle=new GUIStyle(headerStyle){fontSize=25};
            captionStyle=new GUIStyle(smallStyle){fontSize=11};
        }
        private void Rounded(Rect r,Color c)
        {
            if(roundedPanelStyle==null)
            {
                const int size=40;const float radius=14f;
                roundedPanelTexture=new Texture2D(size,size,TextureFormat.RGBA32,false);
                roundedPanelTexture.name="Soft rounded UI mask";
                roundedPanelTexture.wrapMode=TextureWrapMode.Clamp;
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float dx=x-Mathf.Clamp(x,radius,size-1-radius),dy=y-Mathf.Clamp(y,radius,size-1-radius);
                    pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy)));
                }
                roundedPanelTexture.SetPixels(pixels);roundedPanelTexture.Apply(false,true);
                roundedPanelStyle=new GUIStyle{border=new RectOffset(16,16,16,16)};
                roundedPanelStyle.normal.background=roundedPanelTexture;
            }
            // Nine-slice corners keep their radius on both pill buttons and cards.
            Color old=GUI.color;
            GUI.color=new Color(0,0,0,.19f);GUI.Box(new Rect(r.x,r.y+3,r.width,r.height),GUIContent.none,roundedPanelStyle);
            GUI.color=Color.Lerp(c,new Color(.7f,.87f,1f,c.a),.20f);GUI.Box(r,GUIContent.none,roundedPanelStyle);
            GUI.color=c;GUI.Box(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),GUIContent.none,roundedPanelStyle);
            GUI.color=old;
        }
        private void Portrait(Rect r,int hero)
        {
            if(portraitAtlas==null)return;
            GUI.DrawTextureWithTexCoords(r,portraitAtlas,new Rect((hero%4)/4f,(3-hero/4)/4f,.25f,.25f),true);
        }
        private void DrawMenuPolish(float w,float h)
        {
            InitPolishStyles();
            float top=541f;
            float available=Mathf.Min(142,h-265-top);
            if(available<52)return;
            Rounded(new Rect(21,top,w-42,available),new Color(.07f,.095f,.18f,.95f));
            if(available<124)
            {
                GUI.Label(new Rect(32,top+7,w-64,available-14),SkillHelp[selectedHero],captionStyle);
                return;
            }
            GUI.Label(new Rect(32,top+8,w-64,22),"КАК ИГРАТЬ ЭТИМ ГЕРОЕМ",heroSkillStyle);
            GUI.Label(new Rect(32,top+32,w-64,52),SkillHelp[selectedHero],smallStyle);
            GUI.Label(new Rect(32,top+91,w-64,24),"РЕКОРД  "+PlayerPrefs.GetFloat("BrainrotBestArea",0).ToString("F1")+"%   •   ПОБЕДЫ  "+PlayerPrefs.GetInt("BrainrotWins",0),captionStyle);
        }
        private void DrawPlayPolish(float w,float h)
        {
            InitPolishStyles();
            float safeTop=Mathf.Max(8,(Screen.height-Screen.safeArea.yMax)/guiScale);
            if(IsLanding)
            {
                Rounded(new Rect(20,safeTop+10,w-40,32),new Color(.035f,.055f,.12f,.86f));
                GUI.Label(new Rect(30,safeTop+13,w-60,26),"СОЛНЕЧНАЯ СИСТЕМА  •  ПЕРЕЛЁТ К ПЛАНЕТЕ",captionStyle);
                DrawPlanetIntro(w,safeTop);
                return;
            }
            int rank=1;for(int i=1;i<5;i++)if(i!=LocalTeam&&scores[i]>scores[LocalTeam])rank++;
            Rounded(new Rect(10,safeTop,w-20,60),new Color(.035f,.055f,.12f,.92f));
            GUI.Label(new Rect(15,safeTop+3,100,27),scores[LocalTeam].ToString("F1")+"%",numberStyle);
            GUI.Label(new Rect(18,safeTop+31,94,22),"МЕСТО "+rank+" / 4",captionStyle);
            int seconds=Mathf.CeilToInt(matchTime);
            GUI.Label(new Rect(w*.5f-42,safeTop+3,84,29),(seconds/60)+":"+(seconds%60).ToString("00"),numberStyle);
            GUI.Label(new Rect(w*.5f-65,safeTop+34,130,19),"ПОБЕДА ПО ПЛОЩАДИ",captionStyle);
            if(FlatButton(new Rect(w-66,safeTop+12,42,36),"II",new Color(.13f,.21f,.32f)))SetPaused(true);
            Plate(new Rect(12,safeTop+62,(w-24)*Mathf.Clamp01(matchTime/145f),2),new Color(.2f,.85f,.95f));

            string hint="";Color hintColor=new Color(.07f,.13f,.21f,.88f);
            if(playerLandingGrace>0){hint="ЩИТ СЛЕДА  "+Mathf.CeilToInt(playerLandingGrace)+" с";hintColor=new Color(.08f,.32f,.40f,.90f);}
            else if(Territory!=null&&Territory.HasTrail(LocalTeam))
            {
                hint=nearestEnemyDegrees<15?"ОПАСНО! ВЕРНИСЬ НА СВОЮ КРАСКУ":"ЗАМКНИ ПЕТЛЮ НА СВОЕЙ КРАСКЕ";
                if(nearestEnemyDegrees<15)hintColor=new Color(.63f,.075f,.20f,.91f);
            }
            else if(tutorialStep==0)hint="СВАЙП — ДВИЖЕНИЕ ПРОДОЛЖАЕТСЯ";
            else if(tutorialStep==1)hint="ВЫЙДИ ИЗ СВОЕЙ ЗОНЫ И ВЕРНИСЬ";
            else if(annTime>0)hint=announcement.Trim();
            if(hint.Length>0){Rounded(new Rect(22,safeTop+76,w-44,32),hintColor);GUI.Label(new Rect(27,safeTop+77,w-54,29),hint,captionStyle);}
            DrawPlanetIntro(w,safeTop);
            if(captureRewardTime>0)
            {
                Color old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(captureRewardTime*2));
                GUI.Label(new Rect(w*.5f-100,h*.36f-captureRewardTime*6,200,36),"+"+captureReward.ToString("F2")+"%",numberStyle);
                GUI.Label(new Rect(w*.5f-100,h*.36f+32,200,23),captureStreak>1?"СЕРИЯ ЗАХВАТОВ  "+captureStreak:"ТВОЯ ТЕРРИТОРИЯ РАСТЁТ",captionStyle);GUI.color=old;
            }
            if(damageFlash>0)
            {
                Color flash=new Color(1,.08f,.18f,damageFlash*.6f);
                Plate(new Rect(0,0,5,h),flash);Plate(new Rect(w-5,0,5,h),flash);
            }
            string motion=autoMoving?"КУРС: "+GameplayFeedback.DirectionLabel(savedSteering):"СВАЙП ДЛЯ ДВИЖЕНИЯ";
            GUI.Label(new Rect(12,h-46,w-125,30),motion,captionStyle);
            DrawGlow(new Rect(skillArea.x-5,skillArea.y-5,skillArea.width+10,skillArea.height+10),skillCooldown<=0?new Color(1,.18f,.6f,.4f):Color.clear);
            Rounded(skillArea,skillCooldown<=0?new Color(.84f,.15f,.47f,.94f):new Color(.12f,.17f,.29f,.95f));
            GUI.Label(new Rect(skillArea.x+3,skillArea.y+5,skillArea.width-6,43),skillNames[selectedHero],captionStyle);
            GUI.Label(new Rect(skillArea.x,skillArea.y+47,skillArea.width,23),skillCooldown<=0?"ГОТОВО":Mathf.CeilToInt(skillCooldown)+" с",smallStyle);
            Plate(new Rect(skillArea.x+14,skillArea.yMax-7,(skillArea.width-28)*(1-Mathf.Clamp01(skillCooldown/12f)),2),new Color(1,.65f,.83f));
            Rounded(jumpArea,new Color(.045f,.42f,.63f,.94f));
            GUI.Label(new Rect(jumpArea.x,jumpArea.y+6,jumpArea.width,24),"^",headerStyle);
            GUI.Label(new Rect(jumpArea.x,jumpArea.y+31,jumpArea.width,28),"ПРЫЖОК",captionStyle);
            if(Player!=null&&(Player.SpeedSeconds>0||Player.SlowSeconds>0))
                GUI.Label(new Rect(w*.5f-90,h*.59f,180,25),Player.SlowSeconds>0?"ЗАМЕДЛЕНИЕ":"УСКОРЕНИЕ",captionStyle);
            DrawEnemySignals(w,h);
            if(paused)DrawPause(w,h);
        }
        private void DrawPause(float w,float h)
        {
            Plate(new Rect(0,0,w,h),new Color(.015f,.025f,.065f,.91f));
            GUI.Label(new Rect(30,h*.25f,w-60,50),"ПАУЗА",numberStyle);
            if(FlatButton(new Rect(45,h*.37f,w-90,55),"ПРОДОЛЖИТЬ",new Color(.12f,.59f,.70f)))SetPaused(false);
            if(FlatButton(new Rect(45,h*.37f+75,w-90,45),"ЗВУК: "+(GameSound.Enabled?"ВКЛ":"ВЫКЛ"),new Color(.13f,.19f,.30f)))GameSound.SetEnabled(!GameSound.Enabled);
            if(FlatButton(new Rect(45,h*.37f+133,w-90,45),"ВИБРАЦИЯ: "+(GameSound.Haptics?"ВКЛ":"ВЫКЛ"),new Color(.13f,.19f,.30f)))GameSound.SetHaptics(!GameSound.Haptics);
            if(FlatButton(new Rect(45,h*.37f+207,w-90,45),"ВЫЙТИ В МЕНЮ",new Color(.30f,.12f,.23f)))ReturnToMenu();
        }
        private void ReturnToMenu()
        {
            SetPaused(false);if(worldRoot!=null)Destroy(worldRoot.gameObject);
            CloseNetwork();networkPanel=false;
            GameSound.StopGameplay();
            State=MatchState.Menu;orbitCamera.ClearTarget();
            if(worldCamera!=null)worldCamera.backgroundColor=new Color(.055f,.065f,.15f);
        }
        public void ResolveBombImpact(int team,Vector3 direction,float radius)
        {
            EmitNetworkEffect(1,team,direction);
            // Territory scoring/reward still happens, but its ascending chirp must
            // not mask the explosion. Keep the flag local to this synchronous event.
            bool previous=resolvingBombImpact;resolvingBombImpact=true;
            try{Territory.ClaimDisk(team,direction,radius);}
            finally{resolvingBombImpact=previous;}
            Vector3 at=PlanetSurface.Point(direction,1f);
            Effects.Burst(at,Colors.Team(team),62,6f);
            if(Player!=null&&worldCamera!=null)
                GameSound.Explosion(at,Player.transform.position,worldCamera.transform.right);
        }
        private void DrawResultPolish(float w,float h)
        {
            InitPolishStyles();bool win=victoryActor!=null&&victoryActor.Team==LocalTeam;
            Plate(new Rect(0,0,w,151),new Color(.035f,.05f,.12f,.93f));
            GUI.Label(new Rect(20,19,w-40,39),win?"ПОБЕДА!":"ЕЩЁ ОДНА ПОПЫТКА?",numberStyle);
            GUI.Label(new Rect(20,62,w-40,23),finishReason,captionStyle);
            GUI.Label(new Rect(20,94,w-40,27),"ТВОЯ ПЛАНЕТА  "+scores[LocalTeam].ToString("F1")+"%",headerStyle);
            GUI.Label(new Rect(20,123,w-40,23),"ЗАХВАТЫ "+capturesThisMatch+"   •   ЛУЧШИЙ +"+bestCapture.ToString("F2")+"%",captionStyle);
            Plate(new Rect(0,h-224,w,224),new Color(.035f,.05f,.12f,.93f));
            GUI.Label(new Rect(20,h-215,w-40,27),newRecord?"НОВЫЙ ЛИЧНЫЙ РЕКОРД!":"РЕКОРД: "+PlayerPrefs.GetFloat("BrainrotBestArea",0).ToString("F1")+"%",smallStyle);
            if(FlatButton(new Rect(26,h-178,w-52,57),IsNetworkReplica&&networkMessage.Length>0?networkMessage:"ИГРАТЬ ЕЩЁ",new Color(.83f,.17f,.45f)))RequestReplay();
            if(FlatButton(new Rect(26,h-107,w-52,52),"ВЫБРАТЬ ГЕРОЯ",new Color(.11f,.24f,.36f)))ReturnToMenu();
        }
    }
}
