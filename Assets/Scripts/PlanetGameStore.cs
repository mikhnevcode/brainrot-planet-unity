using System;
using System.Collections.Generic;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private TestStoreClient store;
        private bool shopOpen,shopDialog,shopHistory;
        private string shopSelection="",shopRecovery="";
        private Vector2 shopScroll;
        private Rect shopListRect;
        private int shopFinger=-1,shopCategory,shopGeneration=-1;
        private bool shopDragging;
        private Vector2 shopTouchStart;
        private readonly string[] shopKinds={"skin","trail","spawn","dance","stage","emote","profile","bundle"};
        private readonly string[] shopLabels={"КОСТЮМЫ","СЛЕДЫ","ПОЯВЛЕНИЕ","ТАНЦЫ","СЦЕНЫ","ЭМОЦИИ","КАРТОЧКИ","КОМПЛЕКТЫ"};
        private StoreProduct ShopProduct
        {
            get{if(store?.Catalog?.products!=null)foreach(var p in store.Catalog.products)if(p.id==shopSelection)return p;return null;}
        }
        private void SetupStore()
        {
            if(DedicatedMatchServer||!TestStoreClient.Enabled)return;
            store=gameObject.AddComponent<TestStoreClient>();ConnectStore();
        }
        private void ConnectStore()
        {
            if(store==null)return;var config=Resources.Load<TextAsset>("StoreTestEndpoint");
            if(config!=null)store.Connect(config.text.Trim(),networkKey);
        }
        private void OpenShop(string product="")
        {
            if(store==null)return;ConnectStore();wardrobeOpen=false;networkPanel=false;shopOpen=true;shopHistory=false;shopDialog=false;
            shopSelection=product;shopCategory=0;shopScroll=Vector2.zero;previewSkin=equippedSkin;previewHero=-1;
            Debug.Log("STORE_TEST_OPEN");
        }
        private bool OwnsSkin(string id)
        {
            if(string.IsNullOrEmpty(id)||store==null||isWardrobeQa)return true;
            if(store.Owned("skin:"+id))return true;
            var entry=skinCatalog?.Entry(id);if(entry!=null)foreach(var e in skinCatalog.Manifest.skins)if(e.hero==entry.hero)return e.id==id;
            return false;
        }
        private bool OwnsProduct(StoreProduct p){if(p==null)return false;foreach(var g in p.grants)if(!store.Owned(g))return false;return true;}
        private string Cosmetic(string kind)
        {
            if(store==null)return "";string key=PlayerPrefs.GetString("BrainrotTestCosmetic_"+kind,"");
            return store.Owned(kind+":"+key)?key:"";
        }
        private void UpdateStoreUi()
        {
            if(store==null)return;
            if(shopGeneration!=store.Generation){shopGeneration=store.Generation;previewHero=-1;}
            if(!shopOpen)return;
            if(ShopProduct==null&&store.Catalog?.products!=null)foreach(var p in store.Catalog.products)if(p.kind==shopKinds[shopCategory]&&(p.hero<0||p.hero==selectedHero)){SelectShopProduct(p);break;}
            for(int i=0;i<Input.touchCount;i++)
            {
                var t=Input.GetTouch(i);var at=new Vector2(t.position.x/guiScale,(Screen.height-t.position.y)/guiScale);
                if(t.phase==TouchPhase.Began&&shopListRect.Contains(at)){shopFinger=t.fingerId;shopTouchStart=at;shopDragging=false;}
                if(t.fingerId!=shopFinger)continue;
                if(t.phase==TouchPhase.Moved){if((at-shopTouchStart).sqrMagnitude>64)shopDragging=true;if(shopDragging)shopScroll.y=Mathf.Max(0,shopScroll.y+t.deltaPosition.y/guiScale);}
                if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)shopFinger=-1;
            }
        }
        private void SelectShopProduct(StoreProduct p)
        {
            shopSelection=p.id;previewSkin=string.IsNullOrEmpty(p.skin)?equippedSkin:p.skin;skinCatalog?.EnsureLoaded(previewSkin);previewHero=-1;
        }
        private void EquipShopProduct(StoreProduct p)
        {
            if(!OwnsProduct(p))return;
            foreach(var grant in p.grants)
            {
                var pair=grant.Split(':');
                if(pair[0]=="skin")
                {var e=skinCatalog.Entry(pair[1]);if(e!=null){equippedSkins[e.hero]=e.id;PlayerPrefs.SetString("BrainrotSkin"+e.hero,e.id);skinCatalog.EnsureLoaded(e.id);}}
                else PlayerPrefs.SetString("BrainrotTestCosmetic_"+pair[0],pair[1]);
            }
            PlayerPrefs.Save();previewHero=-1;Debug.Log("STORE_TEST_EQUIPPED product="+p.id);
        }
        private void DrawShop(float w,float h)
        {
            InitPolishStyles();Plate(new Rect(0,0,w,h),new Color(.025f,.035f,.095f));
            if(shopHistory){DrawShopHistory(w,h);return;}
            if(shopDialog){DrawShopConfirmation(w,h,ShopProduct);return;}
            GUI.Label(new Rect(16,12,w-110,38),"МАГАЗИН",headerStyle);
            if(FlatButton(new Rect(w-98,15,78,32),"ЧЕКИ",new Color(.18f,.25f,.4f)))shopHistory=true;
            Rounded(new Rect(20,57,w-40,29),new Color(.46f,.24f,.08f));GUI.Label(new Rect(22,58,w-44,27),"ТЕСТ · РЕАЛЬНЫХ СПИСАНИЙ НЕТ",captionStyle);
            Rounded(new Rect(20,95,w-40,164),new Color(.06f,.10f,.20f));
            if(heroPreviewTexture!=null)GUI.DrawTexture(new Rect(20,84,176,180),heroPreviewTexture,ScaleMode.ScaleToFit,true);
            var chosen=ShopProduct;
            GUI.Label(new Rect(195,101,w-220,65),chosen?.title??"Выберите образ",heroNameStyle);
            GUI.Label(new Rect(195,170,w-220,64),chosen==null?"Каталог с сервера":chosen.kind=="bundle"?"Комплект: "+chosen.grants.Length+" предметов":OwnsProduct(chosen)?"В тестовом профиле":"Примерка доступна до покупки",captionStyle);
            for(int i=0;i<shopKinds.Length;i++)
            {
                int category=i;float width=(w-52)/4;
                int font=clearButtonStyle.fontSize;clearButtonStyle.fontSize=10;
                bool clicked=FlatButton(new Rect(20+(i%4)*(width+4),272+(i/4)*34,width,29),shopLabels[i],i==shopCategory?new Color(.4f,.29f,.65f):new Color(.12f,.19f,.3f));clearButtonStyle.fontSize=font;
                if(clicked)
                {shopCategory=category;shopSelection="";shopScroll=Vector2.zero;}
            }
            var entries=new List<StoreProduct>();if(store.Catalog?.products!=null)foreach(var p in store.Catalog.products)if(p.kind==shopKinds[shopCategory]&&(p.hero<0||p.hero==selectedHero))entries.Add(p);
            shopListRect=new Rect(20,348,w-40,h-605);
            if(shopDragging&&Event.current.type==EventType.MouseDrag)GUIUtility.hotControl=0;
            shopScroll=GUI.BeginScrollView(shopListRect,shopScroll,new Rect(0,0,w-60,entries.Count*76+8));
            for(int i=0;i<entries.Count;i++)
            {
                var p=entries[i];var r=new Rect(4,i*76+4,w-74,66);
                if(p.id==shopSelection)Rounded(new Rect(r.x-3,r.y-3,r.width+6,r.height+6),new Color(1,.78f,.2f));
                string price=OwnsProduct(p)?"ПОЛУЧЕНО": "Тестовая цена: "+p.priceTest+" ₽ · списание 0 ₽";
                if(FlatButton(r,p.title+"\n"+price,new Color(.13f,.25f,.38f)))SelectShopProduct(p);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(20,h-248,w-40,52),store.Status,captionStyle);
            bool owned=OwnsProduct(chosen);bool ready=chosen!=null&&skinCatalog.Available(previewSkin);
            if(FlatButton(new Rect(24,h-185,(w-56)/2,49),owned?"НАДЕТЬ":"ТЕСТ-ПОКУПКА",new Color(.48f,.25f,.65f))&&chosen!=null)
            {if(owned&&ready)EquipShopProduct(chosen);else if(!owned&&store.Ready&&!store.Busy)shopDialog=true;else if(!store.Ready)ConnectStore();}
            if(FlatButton(new Rect(w/2+4,h-185,(w-56)/2,49),"ВОССТАНОВИТЬ",new Color(.12f,.40f,.47f))){if(store.Ready)store.Restore();else ConnectStore();}
            if(FlatButton(new Rect(24,h-120,w-48,42),"ГЕРОЙ "+(selectedHero+1)+" / 13   >",new Color(.16f,.24f,.36f)))
            {selectedHero=(selectedHero+1)%13;shopSelection="";previewHero=-1;shopScroll=Vector2.zero;}
            if(FlatButton(new Rect(24,h-66,w-48,42),"НАЗАД",new Color(.18f,.20f,.33f)))
            {shopOpen=false;previewSkin=equippedSkin;previewHero=-1;PlayerPrefs.SetInt("BrainrotSelectedHero",selectedHero);PlayerPrefs.Save();}
        }
        private void DrawShopConfirmation(float w,float h,StoreProduct p)
        {
            Plate(new Rect(0,0,w,h),new Color(.015f,.02f,.05f,.98f));
            float y=Mathf.Max(70,(h-450)/2);Rounded(new Rect(20,y,w-40,445),new Color(.07f,.10f,.20f));
            GUI.Label(new Rect(30,y+15,w-60,48),"СИМУЛЯЦИЯ ОПЛАТЫ",headerStyle);
            GUI.Label(new Rect(30,y+66,w-60,70),(p?.title??"")+"\nСписывается 0 ₽. Выберите исход теста.",smallStyle);
            string[] states={"success","cancel","pending","error"};string[] labels={"УСПЕШНО","ОТМЕНА ПОКУПКИ","ОЖИДАНИЕ","ОШИБКА ОПЛАТЫ"};
            for(int i=0;i<4;i++)if(FlatButton(new Rect(36,y+151+i*52,w-72,42),labels[i],i==0?new Color(.1f,.5f,.35f):new Color(.20f,.27f,.40f)))
            {if(p!=null)store.Purchase(p.id,states[i]);shopDialog=false;}
            if(FlatButton(new Rect(36,y+370,w-72,42),"ЗАКРЫТЬ",new Color(.35f,.18f,.28f)))shopDialog=false;
        }
        private void DrawShopHistory(float w,float h)
        {
            GUI.Label(new Rect(16,18,w-32,40),"ТЕСТОВЫЕ ЧЕКИ",headerStyle);
            GUI.Label(new Rect(20,67,w-40,40),"Это не кассовые чеки. Только журнал симулятора.",captionStyle);
            var rows=store.Snapshot?.transactions??Array.Empty<StoreTransaction>();
            shopListRect=new Rect(20,118,w-40,h-450);shopScroll=GUI.BeginScrollView(shopListRect,shopScroll,new Rect(0,0,w-60,rows.Length*83+8));
            for(int i=0;i<rows.Length;i++)
            {
                var t=rows[i];float y=i*83;
                Rounded(new Rect(2,y,w-74,76),new Color(.09f,.16f,.27f));
                GUI.Label(new Rect(9,y+5,w-195,66),t.product+"\n"+t.status,captionStyle);
                if(t.status=="pending"&&FlatButton(new Rect(w-176,y+13,98,47),"ПОДТВЕРДИТЬ",new Color(.15f,.4f,.3f)))store.TestTransaction("approve",t.tx);
                if(t.status=="success"&&FlatButton(new Rect(w-176,y+13,98,47),"ВОЗВРАТ",new Color(.4f,.2f,.25f)))store.TestTransaction("refund",t.tx);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(20,h-318,w-40,42),store.Status,captionStyle);
            GUI.Label(new Rect(20,h-270,w-40,42),"Код переноса тестового профиля — секрет.\nНе публикуйте его и не отправляйте в чат.",captionStyle);
            shopRecovery=GUI.PasswordField(new Rect(24,h-218,w-48,35),shopRecovery,'*',32);
            if(FlatButton(new Rect(24,h-173,(w-56)/2,45),"ПЕРЕНЕСТИ",new Color(.13f,.38f,.42f)))store.Recovery(shopRecovery.Trim());
            if(FlatButton(new Rect(w/2+4,h-173,(w-56)/2,45),"КОПИЯ КОДА",new Color(.25f,.22f,.44f)))GUIUtility.systemCopyBuffer=store.RecoveryCode;
            if(FlatButton(new Rect(24,h-112,w-48,42),"ВОССТАНОВИТЬ",new Color(.13f,.35f,.43f)))store.Restore();
            if(FlatButton(new Rect(24,h-60,w-48,38),"НАЗАД В МАГАЗИН",new Color(.18f,.20f,.33f))){shopHistory=false;shopScroll=Vector2.zero;shopRecovery="";}
        }
        private void PreviewShopCosmetics(Bounds bounds,int layer)
        {
            var p=ShopProduct;if(!shopOpen||p==null)return;
            foreach(var grant in p.grants)
            {
                var pair=grant.Split(':');string k=pair[0],v=pair[1];
                if(k=="dance")heroPreviewRig.AddComponent<CosmeticVictoryMotion>().Configure(v);
                if(k=="stage")heroPreviewRoot.AddComponent<CosmeticStageArt>().Configure(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),v,Mathf.Max(.45f,bounds.size.x*.65f));
                if(k=="trail")
                {var fx=new GameObject("Store trail preview");fx.transform.SetParent(heroPreviewRoot.transform,false);fx.transform.position=bounds.center+Vector3.left*(bounds.extents.x+.13f);fx.AddComponent<CosmeticTrail>().Configure(null,v,true);}
                if(k=="spawn"){var fx=new GameObject("Store appearance preview");fx.transform.SetParent(heroPreviewRoot.transform,false);fx.transform.position=bounds.center;fx.AddComponent<CosmeticBurst>().Configure(v);}
            }
            SetLayerRecursively(heroPreviewRoot,layer);
        }
        private void ApplyActorShopCosmetics(PlanetActor actor,int team)
        {
            if(team!=LocalTeam||store==null)return;
            string trail=Cosmetic("trail");if(trail!="")actor.gameObject.AddComponent<CosmeticTrail>().Configure(actor,trail);
            string spawn=Cosmetic("spawn");if(spawn!=""){var fx=new GameObject("Cosmetic appearance");fx.transform.position=actor.transform.position;fx.AddComponent<CosmeticBurst>().Configure(spawn);}
        }
        private void ApplyVictoryShopCosmetics(Vector3 center,int winner)
        {
            if(winner!=LocalTeam||store==null)return;
            string stage=Cosmetic("stage");if(stage!="")worldRoot.gameObject.AddComponent<CosmeticStageArt>().Configure(center,stage,.8f);
            string dance=Cosmetic("dance");if(dance!="")SkinRigProfile.Visual(victoryActor.transform,victoryActor.Character)?.gameObject.AddComponent<CosmeticVictoryMotion>().Configure(dance);
        }
        private void DrawShopProfileBadge(float w)
        {
            string profile=Cosmetic("profile");if(profile=="")return;
            Rounded(new Rect(20,65,w-40,5),ShopCosmetics.Tint(profile));
        }
        private float emoteUntil,emoteCooldown;
        private void DrawShopEmote(float w,float h)
        {
            string emote=Cosmetic("emote");if(emote=="")return;
            string text=emote=="hello"?"ПРИВЕТ!":emote=="great"?"ОТЛИЧНО!":"ЕЩЁ МАТЧ!";
            if(FlatButton(new Rect(w-104,h-308,80,36),"ЭМОЦИЯ",new Color(.26f,.20f,.43f))&&Time.unscaledTime>emoteCooldown){emoteUntil=Time.unscaledTime+2;emoteCooldown=Time.unscaledTime+5;}
            if(Time.unscaledTime<emoteUntil){Rounded(new Rect(w/2-90,180,180,35),new Color(.15f,.24f,.4f));GUI.Label(new Rect(w/2-90,180,180,35),text,smallStyle);}
        }
    }
}
