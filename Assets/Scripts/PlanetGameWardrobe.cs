using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private SkinCatalog skinCatalog;
        private string equippedSkin="",previewSkin="";
        private bool wardrobeOpen;
        private int skinGeneration=-1;
        private Vector2 wardrobeScroll;
        private readonly string[] networkSkins={"","","","",""};
        private void SetupWardrobe()
        {
            if(DedicatedMatchServer)return;
            skinCatalog=gameObject.AddComponent<SkinCatalog>();
            equippedSkin=PlayerPrefs.GetString("BrainrotSkin1","");previewSkin=equippedSkin;
        }
        private void RefreshWardrobePreview()
        {
            if(skinCatalog!=null&&skinGeneration!=skinCatalog.Generation)
            {skinGeneration=skinCatalog.Generation;previewHero=-1;}
        }
        private string ChosenSkinReference()=>selectedHero==1&&skinCatalog!=null?skinCatalog.Reference(equippedSkin):"";
        private void ApplyActorSkin(PlanetActor actor,int hero,int team)
        {
            if(hero!=1||skinCatalog==null)return;
            var pack=IsNetworkReplica?skinCatalog.ReferencedPack(networkSkins[team]):team==LocalTeam?skinCatalog.Pack(equippedSkin):null;
            SkinVisual.Apply(actor.transform,pack);
            if(IsNetworkReplica)Debug.Log("MATCH_SKIN team="+team+" requested="+networkSkins[team]+" applied="+(pack!=null?pack.id:"base"));
        }
        private void DrawWardrobe(float w,float h)
        {
            InitPolishStyles();
            Plate(new Rect(0,0,w,h),new Color(.025f,.035f,.095f,1));
            GUI.Label(new Rect(14,18,w-28,40),"ГАРДЕРОБ · БОМБАРДИРО",headerStyle);
            GUI.Label(new Rect(18,57,w-36,28),"ТЕСТОВЫЕ ОБРАЗЫ · БЕЗ ПЛАТЕЖЕЙ",captionStyle);
            Rounded(new Rect(20,94,w-40,220),new Color(.055f,.09f,.18f));
            if(heroPreviewTexture!=null)GUI.DrawTexture(new Rect(w*.5f-125,75,250,250),heroPreviewTexture,ScaleMode.ScaleToFit,true);
            wardrobeScroll=GUI.BeginScrollView(new Rect(20,323,w-40,h-535),wardrobeScroll,new Rect(0,0,w-60,70+skinCatalog.Manifest.skins.Length*78));
            float y=5;
            WardrobeCard(new Rect(4,y,w-72,55),"","КЛАССИЧЕСКИЙ","Оригинальный образ",new Color(.2f,.45f,.35f));y+=64;
            foreach(var e in skinCatalog.Manifest.skins)
            {
                WardrobeCard(new Rect(4,y,w-72,69),e.id,e.title,e.description,
                    e.id.Contains("lava")?new Color(.62f,.20f,.10f):new Color(.10f,.40f,.65f));y+=78;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(18,h-201,w-36,42),skinCatalog.Status,captionStyle);
            if(FlatButton(new Rect(24,h-150,(w-56)/2,48),"ОБНОВИТЬ",new Color(.16f,.27f,.42f)))skinCatalog.Refresh();
            if(FlatButton(new Rect(w/2+4,h-150,(w-56)/2,48),"НАДЕТЬ",new Color(.1f,.52f,.42f))&&skinCatalog.Available(previewSkin))
            {
                equippedSkin=previewSkin;PlayerPrefs.SetString("BrainrotSkin1",equippedSkin);PlayerPrefs.Save();
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
