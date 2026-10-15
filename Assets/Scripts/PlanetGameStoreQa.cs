#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private IEnumerator WaitStoreQa()
        {
            float deadline=Time.unscaledTime+35;
            while((store.Busy||!store.Ready)&&Time.unscaledTime<deadline)yield return null;
            if(!store.Ready||store.Busy)throw new Exception("store_timeout");
            yield return new WaitForSecondsRealtime(.6f);
        }
        private static void StoreAssert(bool condition,string gate){if(!condition)throw new Exception(gate);Debug.Log("STORE_QA_GATE_PASS "+gate);}
        private IEnumerator RunStoreQa()
        {
            // Dedicated random TEST account. User's test profile, preferences and
            // gameplay rewards are restored; no receipt or real payment occurs.
            int originalHero=selectedHero;var originalSkins=(string[])equippedSkins.Clone();
            bool originalSetup=nicknameSetupOpen;
            bool hadHero=PlayerPrefs.HasKey("BrainrotSelectedHero");int savedHero=PlayerPrefs.GetInt("BrainrotSelectedHero",0);
            string[] previous=new string[6];bool[] had=new bool[6];string[] kinds={"trail","spawn","dance","stage","emote","profile"};
            for(int i=0;i<6;i++){had[i]=PlayerPrefs.HasKey("BrainrotTestCosmetic_"+kinds[i]);previous[i]=PlayerPrefs.GetString("BrainrotTestCosmetic_"+kinds[i],"");}
            string folder=Path.Combine(Application.persistentDataPath,"store-qa");Directory.CreateDirectory(folder);
            try
            {
                nicknameSetupOpen=false;
                ConnectStore();yield return WaitStoreQa();store.BeginTemporaryQa();yield return WaitStoreQa();
                StoreAssert(store.Catalog.products.Length==177,"catalog177");
                string product=File.ReadAllText(Path.Combine(Application.persistentDataPath,".store-qa")).Trim();
                StoreProduct chosen=null;foreach(var p in store.Catalog.products)if(p.id==product)chosen=p;
                if(chosen==null){product="skin.orig_popkornpykh_polar";foreach(var p in store.Catalog.products)if(p.id==product)chosen=p;}
                if(chosen==null)throw new Exception("original_skin_product_missing");
                string grant=chosen.grants[0];selectedHero=chosen.hero;
                StoreAssert(!store.Owned(grant),"initially_locked");
                store.Purchase(product,"cancel");yield return WaitStoreQa();StoreAssert(!store.Owned(grant),"cancel_no_grant");
                store.Purchase(product,"error");yield return WaitStoreQa();StoreAssert(!store.Owned(grant),"error_no_grant");
                string tx=Guid.NewGuid().ToString("N");store.Purchase(product,"pending",tx);yield return WaitStoreQa();StoreAssert(!store.Owned(grant),"pending_no_grant");
                OpenShop(product);shopHistory=true;yield return new WaitForSecondsRealtime(.4f);yield return CaptureWardrobeQa(Path.Combine(folder,"pending-history.png"));shopHistory=false;
                store.TestTransaction("approve",tx);yield return WaitStoreQa();StoreAssert(store.Owned(grant),"approve_grants");
                int count=store.Snapshot.transactions.Length;store.Purchase(product,"success",tx);yield return WaitStoreQa();StoreAssert(store.Snapshot.transactions.Length==count,"idempotent_transaction");
                store.TestTransaction("refund",tx);yield return WaitStoreQa();StoreAssert(!store.Owned(grant),"refund_revokes");
                store.Purchase(product);yield return WaitStoreQa();StoreAssert(store.Owned(grant),"success_grants");
                store.Purchase("bundle.space");yield return WaitStoreQa();StoreAssert(store.Owned("trail:stars")&&store.Owned("stage:orbital"),"bundle_grants");
                string code=store.RecoveryCode;store.Recovery(Guid.NewGuid().ToString("N"));yield return WaitStoreQa();StoreAssert(store.RecoveryCode==code,"bad_recovery_preserves_profile");
                store.ForgetInMemoryForQa();StoreAssert(!store.Owned(grant),"local_inventory_forgotten");store.Restore();yield return WaitStoreQa();StoreAssert(store.Owned(grant),"server_restore");
                store.Recovery(code);yield return WaitStoreQa();StoreAssert(store.Owned(grant),"recovery_restore");
                float deadline=Time.unscaledTime+25;skinCatalog.EnsureLoaded(chosen.skin);while(!skinCatalog.Available(chosen.skin)&&Time.unscaledTime<deadline)yield return null;
                StoreAssert(skinCatalog.Available(chosen.skin),"skin_download");
                isWardrobeQa=true;EquipShopProduct(chosen);
                foreach(var p in store.Catalog.products)if(p.id=="bundle.space")EquipShopProduct(p);
                OpenShop(product);shopDialog=true;yield return new WaitForSecondsRealtime(.4f);yield return CaptureWardrobeQa(Path.Combine(folder,"confirmation.png"));shopDialog=false;
                foreach(var kind in kinds)
                {
                    foreach(var p in store.Catalog.products)if(p.kind==kind&&p.hero<0){shopCategory=Array.IndexOf(shopKinds,kind);SelectShopProduct(p);break;}
                    yield return new WaitForSecondsRealtime(.6f);yield return CaptureWardrobeQa(Path.Combine(folder,"preview-"+kind+".png"));
                }
                shopOpen=false;StartGame();deadline=Time.unscaledTime+15;while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                RequestJump();UseSkill();yield return new WaitForSecondsRealtime(2);
                StoreAssert(Player!=null&&Player.GetComponent<CosmeticTrail>()!=null,"game_cosmetic_trail");
                yield return CaptureWardrobeQa(Path.Combine(folder,"game.png"));
                var owners=new byte[PlanetMath.CellCount];for(int i=0;i<owners.Length;i++)owners[i]=1;Territory.ApplyNetworkOwners(owners);Finish();
                yield return new WaitForSecondsRealtime(1.5f);
                StoreAssert(worldRoot.GetComponent<CosmeticStageArt>()!=null&&victoryActor.GetComponentInChildren<CosmeticVictoryMotion>()!=null,"victory_stage_dance");
                yield return CaptureWardrobeQa(Path.Combine(folder,"victory.png"));
                Debug.Log("STORE_QA_PASS catalog177 cancel_error_pending_approve_duplicate_refund_restore_game_victory=true realMoney=0");
            }
            finally
            {
                ReturnToMenu();nicknameSetupOpen=originalSetup;shopOpen=false;shopHistory=false;shopDialog=false;isWardrobeQa=false;selectedHero=originalHero;previewHero=-1;
                for(int i=0;i<originalSkins.Length;i++){equippedSkins[i]=originalSkins[i];if(string.IsNullOrEmpty(originalSkins[i]))PlayerPrefs.DeleteKey("BrainrotSkin"+i);else PlayerPrefs.SetString("BrainrotSkin"+i,originalSkins[i]);}
                for(int i=0;i<6;i++){if(had[i])PlayerPrefs.SetString("BrainrotTestCosmetic_"+kinds[i],previous[i]);else PlayerPrefs.DeleteKey("BrainrotTestCosmetic_"+kinds[i]);}
                if(hadHero)PlayerPrefs.SetInt("BrainrotSelectedHero",savedHero);else PlayerPrefs.DeleteKey("BrainrotSelectedHero");PlayerPrefs.Save();store?.EndTemporaryQa();
            }
        }
    }
}
#endif
