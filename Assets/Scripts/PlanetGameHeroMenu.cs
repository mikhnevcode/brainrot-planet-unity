using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private bool settingsOpen;
        private Bounds previewBounds;
        private float previewYaw,previewPitch=10f,previewZoom=1f;
        private int previewFinger=-1;
        private Vector2 previewPointer;
        private float previewPinch;
        private bool previewMouse;
        private GUIStyle compactHeroTitle;

        // Coordinates are shared by drawing and pointer capture, so hero cards
        // and wardrobe/shop scrolling cannot accidentally rotate the portrait.
        private Rect MainPreviewRect(float w,float top)=>new Rect(20,top+51,184,184);
        private float MenuSafeTop(float scale)=>Mathf.Max(8,(Screen.height-Screen.safeArea.yMax)/scale);
        private Rect PreviewGestureRect(float w)
        {
            if(settingsOpen||networkPanel||shopHistory||shopDialog)return Rect.zero;
            if(wardrobeOpen)return new Rect(w*.5f-125,75,250,240);
            if(shopOpen)return new Rect(20,95,176,164);
            return MainPreviewRect(w,MenuSafeTop(GameScale()));
        }
        private void PositionHeroPreviewCamera()
        {
            if(heroPreviewCamera!=null)
                HeroPreviewFraming.Place(heroPreviewCamera,previewBounds,previewYaw,previewPitch,previewZoom);
        }
        private void DragHeroPreview(Vector2 delta)
        {
            Vector2 angles=HeroPreviewFraming.DragAngles(new Vector2(previewYaw,previewPitch),delta);
            previewYaw=angles.x;previewPitch=angles.y;
            PositionHeroPreviewCamera();
        }
        private void UpdateHeroPreviewInput()
        {
            if(heroPreviewRig==null)return;
            float scale=GameScale();
            Rect rect=PreviewGestureRect(Screen.width/scale);
            if(rect.width<=0){previewFinger=-1;previewMouse=false;previewPinch=0;return;}
            if(Input.touchCount>0)
            {
                previewMouse=false;
                for(int i=0;i<Input.touchCount;i++)
                {
                    Touch touch=Input.GetTouch(i);
                    Vector2 p=new Vector2(touch.position.x/scale,(Screen.height-touch.position.y)/scale);
                    if(touch.phase==TouchPhase.Began&&previewFinger<0&&rect.Contains(p))
                    {previewFinger=touch.fingerId;previewPointer=p;}
                    if(touch.fingerId!=previewFinger)continue;
                    if(touch.phase==TouchPhase.Moved&&Input.touchCount==1)DragHeroPreview(p-previewPointer);
                    previewPointer=p;
                    if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled)
                    {
                        Debug.Log("HERO_PREVIEW_DRAG yaw="+previewYaw.ToString("F1")+" pitch="+previewPitch.ToString("F1"));
                        previewFinger=-1;
                    }
                }
                if(Input.touchCount==2&&previewFinger>=0)
                {
                    Touch a=Input.GetTouch(0),b=Input.GetTouch(1);
                    Vector2 pa=new Vector2(a.position.x/scale,(Screen.height-a.position.y)/scale);
                    Vector2 pb=new Vector2(b.position.x/scale,(Screen.height-b.position.y)/scale);
                    if(rect.Contains(pa)&&rect.Contains(pb))
                    {
                        float distance=Vector2.Distance(pa,pb);
                        if(previewPinch>0&&distance>1)
                        {previewZoom=Mathf.Clamp(previewZoom*previewPinch/distance,1f,1.4f);PositionHeroPreviewCamera();}
                        previewPinch=distance;
                    }
                    else previewPinch=0;
                }
                else previewPinch=0;
                return;
            }
            previewFinger=-1;previewPinch=0;
            Vector2 mouse=new Vector2(Input.mousePosition.x/scale,(Screen.height-Input.mousePosition.y)/scale);
            if(Input.GetMouseButtonDown(0)&&rect.Contains(mouse)){previewMouse=true;previewPointer=mouse;}
            if(previewMouse&&Input.GetMouseButton(0)){DragHeroPreview(mouse-previewPointer);previewPointer=mouse;}
            if(Input.GetMouseButtonUp(0))previewMouse=false;
            if(rect.Contains(mouse)&&Mathf.Abs(Input.mouseScrollDelta.y)>.01f)
            {previewZoom=Mathf.Clamp(previewZoom-Input.mouseScrollDelta.y*.08f,1f,1.4f);PositionHeroPreviewCamera();}
        }
        private void SelectMenuHero(int hero)
        {
            selectedHero=hero;
            PlayerPrefs.SetInt("BrainrotSelectedHero",hero);PlayerPrefs.Save();
            GameSound.Play(GameCue.Click);previewHero=-1;
            Debug.Log("HERO_MENU_SELECTED index="+hero+" name="+characters[hero].Trim());
        }
        private void DrawHeroMenu(float w,float h)
        {
            InitPolishStyles();
            if(compactHeroTitle==null)compactHeroTitle=new GUIStyle(headerStyle){fontSize=17};
            float top=MenuSafeTop(guiScale);
            float bottom=h-Mathf.Max(12,Screen.safeArea.yMin/guiScale+8);
            Color color=HeroColors[selectedHero],accent=HeroAccents[selectedHero];
            Plate(new Rect(0,0,w,h),new Color(.025f,.035f,.095f));
            DrawGlow(new Rect(-100,top-95,330,330),new Color(color.r,color.g,color.b,.35f));
            DrawGlow(new Rect(w-170,h-310,330,330),new Color(accent.r,accent.g,accent.b,.18f));
            GUI.Label(new Rect(16,top,w-144,36),"BRAINROT PLANET.IO",compactHeroTitle);
            int originalFont=clearButtonStyle.fontSize;clearButtonStyle.fontSize=12;
            bool openSettings=FlatButton(new Rect(w-122,top+1,104,34),"НАСТРОЙКИ",new Color(.18f,.22f,.36f));
            clearButtonStyle.fontSize=originalFont;
            if(openSettings)settingsOpen=true;
            Rect spotlight=new Rect(18,top+48,w-36,190);
            Rounded(new Rect(15,spotlight.y-3,w-30,196),color);
            Rounded(spotlight,new Color(.045f,.065f,.15f));
            DrawShopProfileBadge(w);
            if(heroPreviewTexture!=null)GUI.DrawTexture(MainPreviewRect(w,top),heroPreviewTexture,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(211,top+57,w-237,50),characters[selectedHero].Trim(),heroNameStyle);
            GUI.Label(new Rect(211,top+110,w-237,32),skillNames[selectedHero],heroSkillStyle);
            GUI.Label(new Rect(211,top+146,w-237,68),SkillHelp[selectedHero],captionStyle);

            float onlineY=bottom-48,botsY=onlineY-66,extrasY=botsY-46;
            float gridY=top+270;
            GUI.Label(new Rect(20,top+242,w-40,23),"ВЫБЕРИ ГЕРОЯ · "+characters.Length,smallStyle);
            int columns=4,rows=(characters.Length+columns-1)/columns;
            float gap=8,width=(w-40-gap*(columns-1))/columns;
            float height=Mathf.Min(width,(extrasY-12-gridY-gap*(rows-1))/rows);
            for(int i=0;i<characters.Length;i++)
            {
                Rect card=new Rect(20+(i%columns)*(width+gap),gridY+(i/columns)*(height+gap),width,height);
                bool picked=i==selectedHero;
                if(picked)Rounded(new Rect(card.x-3,card.y-3,card.width+6,card.height+6),accent);
                Rounded(card,new Color(.57f,.66f,.76f));
                DrawGlow(new Rect(card.x+3,card.y+3,card.width-6,card.height-6),new Color(.82f,.9f,1f,.20f));
                Plate(new Rect(card.x+12,card.y+card.height-5,card.width-24,2),HeroColors[i]);
                float size=Mathf.Min(card.width-6,card.height-4);
                Portrait(new Rect(card.center.x-size*.5f,card.center.y-size*.5f,size,size),i);
                // No name or ability here: the whole tile belongs to the portrait.
                if(GUI.Button(card,GUIContent.none,clearButtonStyle))SelectMenuHero(i);
            }
            if(!heroMenuLogged){heroMenuLogged=true;Debug.Log("HERO_MENU_PRESENTED iconGrid="+characters.Length+" pages=1 preview=3D draggable=true");}
            if(FlatButton(new Rect(22,extrasY,store!=null?(w-52)/2:w-44,36),"ГАРДЕРОБ",new Color(.36f,.22f,.60f)))
            {previewSkin=equippedSkin;previewHero=-1;wardrobeScroll=Vector2.zero;wardrobeOpen=true;}
            if(store!=null&&FlatButton(new Rect(w/2+4,extrasY,(w-52)/2,36),"МАГАЗИН",new Color(.64f,.35f,.14f)))OpenShop();
            if(FlatButton(new Rect(22,botsY,w-44,56),"ИГРАТЬ С БОТАМИ  >",Color.Lerp(color,accent,.24f)))StartGame();
            if(FlatButton(new Rect(22,onlineY,w-44,48),"СЕТЕВОЙ МАТЧ",new Color(.08f,.42f,.62f)))networkPanel=true;
        }
        private void DrawMenuSettings(float w,float h)
        {
            InitPolishStyles();
            float top=MenuSafeTop(guiScale);
            Plate(new Rect(0,0,w,h),new Color(.025f,.035f,.095f));
            DrawGlow(new Rect(-70,top+15,330,330),new Color(.25f,.4f,1f,.22f));
            GUI.Label(new Rect(20,top+10,w-40,40),"НАСТРОЙКИ",headerStyle);
            GUI.Label(new Rect(28,top+77,w-56,28),"СЛОЖНОСТЬ БОТОВ",smallStyle);
            float width=(w-68)/3;
            for(int i=0;i<3;i++)
            {
                Rect r=new Rect(26+i*(width+8),top+118,width,48);
                bool picked=(int)chosenDifficulty==i;
                if(picked)Rounded(new Rect(r.x-3,r.y-3,r.width+6,r.height+6),HeroAccents[selectedHero]);
                if(FlatButton(r,DifficultyLabels[i],picked?DifficultyColors[i]:Color.Lerp(new Color(.08f,.10f,.20f),DifficultyColors[i],.27f)))
                {chosenDifficulty=(AiDifficulty)i;PlayerPrefs.SetInt("BrainrotAiDifficulty",i);PlayerPrefs.Save();GameSound.Play(GameCue.Click);Debug.Log("AI_DIFFICULTY_SELECTED level="+chosenDifficulty);}
            }
            GUI.Label(new Rect(28,top+180,w-56,44),"Влияет только на игру с ботами. Сетевой матч запускается с главного экрана.",captionStyle);
            if(FlatButton(new Rect(26,top+254,w-52,52),"ЗВУК: "+(GameSound.Enabled?"ВКЛ":"ВЫКЛ"),new Color(.16f,.27f,.42f)))GameSound.SetEnabled(!GameSound.Enabled);
            if(FlatButton(new Rect(26,top+320,w-52,52),"ВИБРАЦИЯ: "+(GameSound.Haptics?"ВКЛ":"ВЫКЛ"),new Color(.16f,.27f,.42f)))GameSound.SetHaptics(!GameSound.Haptics);
            if(FlatButton(new Rect(26,h-Mathf.Max(20,Screen.safeArea.yMin/guiScale+8)-58,w-52,52),"НАЗАД К ГЕРОЯМ",new Color(.18f,.20f,.33f)))settingsOpen=false;
        }
    }
}
