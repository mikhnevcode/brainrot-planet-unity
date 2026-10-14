using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private SkinCatalog skinCatalog;
        private readonly string[] equippedSkins=new string[SkinRigProfile.HeroCount];
        private string previewSkin="";
        private string equippedSkin {get=>equippedSkins[selectedHero]??"";set=>equippedSkins[selectedHero]=value??"";}
        private bool wardrobeOpen;
        private bool isWardrobeQa;
        private int skinGeneration=-1;
        private Vector2 wardrobeScroll;
        private Rect wardrobeListRect;
        private int wardrobeFinger=-1;
        private Vector2 wardrobeTouchStart;
        private bool wardrobeDragging;
        private readonly string[] networkSkins={"","","","",""};
        private void SetupWardrobe()
        {
            if(DedicatedMatchServer)return;
            skinCatalog=gameObject.AddComponent<SkinCatalog>();
            for(int i=0;i<equippedSkins.Length;i++)equippedSkins[i]=PlayerPrefs.GetString("BrainrotSkin"+i,"");
            previewSkin=equippedSkin;
        }
        private void RefreshWardrobePreview()
        {
            if(wardrobeOpen)UpdateWardrobeScroll();
            if(skinCatalog!=null)skinCatalog.EnsureLoaded(wardrobeOpen?previewSkin:equippedSkin);
            if(skinCatalog!=null&&skinGeneration!=skinCatalog.Generation)
            {skinGeneration=skinCatalog.Generation;previewHero=-1;}
        }
        private void UpdateWardrobeScroll()
        {
            for(int i=0;i<Input.touchCount;i++)
            {
                var touch=Input.GetTouch(i);
                var p=new Vector2(touch.position.x/guiScale,(Screen.height-touch.position.y)/guiScale);
                if(touch.phase==TouchPhase.Began&&wardrobeListRect.Contains(p))
                {wardrobeFinger=touch.fingerId;wardrobeTouchStart=p;wardrobeDragging=false;}
                if(touch.fingerId!=wardrobeFinger)continue;
                if(touch.phase==TouchPhase.Moved)
                {
                    if((p-wardrobeTouchStart).sqrMagnitude>64f)wardrobeDragging=true;
                    if(wardrobeDragging)wardrobeScroll.y=Mathf.Max(0,wardrobeScroll.y+touch.deltaPosition.y/guiScale);
                }
                if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled)wardrobeFinger=-1;
            }
        }
        private string ChosenSkinReference()
        {var entry=skinCatalog?.Entry(equippedSkin);return entry!=null&&entry.hero==selectedHero?skinCatalog.Reference(equippedSkin):"";}
        private void ApplyActorSkin(PlanetActor actor,int hero,int team)
        {
            if(skinCatalog==null)return;
            string reference=IsNetworkReplica?networkSkins[team]:team==LocalTeam?skinCatalog.Reference(equippedSkins[hero]):"";
            var pack=skinCatalog.ReferencedPack(reference);
            if(pack!=null&&pack.hero!=hero)pack=null;
            SkinVisual.Apply(actor.transform,pack);
            if(IsNetworkReplica)
            {
                Debug.Log("MATCH_SKIN team="+team+" requested="+reference+" applied="+(pack!=null?pack.id:"base"));
                if(pack==null&&!string.IsNullOrEmpty(reference))StartCoroutine(LoadNetworkActorSkin(actor,hero,team,reference,networkEpoch));
            }
        }
        private System.Collections.IEnumerator LoadNetworkActorSkin(PlanetActor actor,int hero,int team,string reference,uint epoch)
        {
            skinCatalog.EnsureReference(reference);float deadline=Time.unscaledTime+18f;
            while(actor!=null&&epoch==networkEpoch&&Time.unscaledTime<deadline)
            {
                var pack=skinCatalog.ReferencedPack(reference);
                if(pack!=null)
                {
                    if(pack.hero==hero){SkinVisual.Apply(actor.transform,pack);Debug.Log("MATCH_SKIN_LATE team="+team+" applied="+pack.id);}
                    yield break;
                }
                yield return null;
            }
        }
        private void DrawWardrobe(float w,float h)
        {
            InitPolishStyles();
            Plate(new Rect(0,0,w,h),new Color(.025f,.035f,.095f,1));
            GUI.Label(new Rect(14,14,w-28,36),"ГАРДЕРОБ",headerStyle);
            GUI.Label(new Rect(18,50,w-36,38),characters[selectedHero].Trim(),captionStyle);
            Rounded(new Rect(20,94,w-40,220),new Color(.055f,.09f,.18f));
            if(heroPreviewTexture!=null)GUI.DrawTexture(new Rect(w*.5f-125,75,250,250),heroPreviewTexture,ScaleMode.ScaleToFit,true);
            int count=0;foreach(var entry in skinCatalog.Manifest.skins)if(entry.hero==selectedHero)count++;
            wardrobeListRect=new Rect(20,323,w-40,h-535);
            if(wardrobeDragging&&Event.current.type==EventType.MouseDrag)GUIUtility.hotControl=0;
            wardrobeScroll=GUI.BeginScrollView(wardrobeListRect,wardrobeScroll,new Rect(0,0,w-60,70+count*78));
            float y=5;
            WardrobeCard(new Rect(4,y,w-72,55),"","КЛАССИЧЕСКИЙ","Оригинальный образ",new Color(.2f,.45f,.35f));y+=64;
            foreach(var e in skinCatalog.Manifest.skins)
            {
                if(e.hero!=selectedHero)continue;
                WardrobeCard(new Rect(4,y,w-72,69),e.id,e.title,e.description,
                    string.IsNullOrEmpty(e.swatch)?new Color(.1f,.4f,.65f):SkinCatalog.ParseColor(e.swatch)*.62f);y+=78;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(18,h-201,w-36,42),skinCatalog.Status,captionStyle);
            if(FlatButton(new Rect(24,h-150,(w-56)/2,48),"ОБНОВИТЬ",new Color(.16f,.27f,.42f)))skinCatalog.Refresh();
            bool ready=skinCatalog.Available(previewSkin);
            if(FlatButton(new Rect(w/2+4,h-150,(w-56)/2,48),ready?"НАДЕТЬ":"ЗАГРУЗКА…",new Color(.1f,.52f,.42f))&&ready)
            {
                equippedSkin=previewSkin;PlayerPrefs.SetString("BrainrotSkin"+selectedHero,equippedSkin);PlayerPrefs.Save();
                Debug.Log("SKIN_EQUIPPED id="+equippedSkin);
            }
            if(FlatButton(new Rect(24,h-88,w-48,48),"НАЗАД К ГЕРОЯМ",new Color(.18f,.20f,.33f)))
            {wardrobeOpen=false;previewSkin=equippedSkin;previewHero=-1;}
        }
        private void WardrobeCard(Rect rect,string id,string title,string subtitle,Color color)
        {
            bool selected=previewSkin==id;
            if(selected)Rounded(new Rect(rect.x-3,rect.y-3,rect.width+6,rect.height+6),new Color(1f,.78f,.22f));
            string label=title+(equippedSkin==id?"  ✓":"")+"\n"+subtitle;
            if(FlatButton(rect,label,color)){previewSkin=id;previewHero=-1;}
        }
    }
}
