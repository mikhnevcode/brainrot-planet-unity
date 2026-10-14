using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
namespace BrainrotPlanet
{
    [Serializable] public sealed class StoreProduct {public string id,title,kind,skin;public int hero,priceTest;public string[] grants;}
    [Serializable] public sealed class StoreCatalog {public int schema,revision;public string mode;public StoreProduct[] products;public string[] free;}
    [Serializable] public sealed class StoreTransaction {public string tx,product,status;}
    [Serializable] public sealed class StoreSnapshot {public int schema;public string mode,request,status;public bool ok;public string[] owned;public StoreTransaction[] transactions;public StoreCatalog catalog;}
    [Serializable] public sealed class StoreCommand {public string action,token,request,tx,product,scenario;}
    [Serializable] internal sealed class StoreSession {public string token;}
    [Serializable] internal sealed class StoreLocalInventory {public string account;public StoreSnapshot snapshot;}

    // Development sandbox only. This is NOT Apple/Google/Robokassa billing.
    // Its credentials, cache and ledger cannot authorize production purchases.
    public sealed class TestStoreClient : MonoBehaviour
    {
        public StoreCatalog Catalog {get;private set;}
        public StoreSnapshot Snapshot {get;private set;}
        public string Status {get;private set;}="Тестовый магазин · без списаний";
        public bool Busy=>peer!=null&&(!peer.Connected||outstanding!=null);
        public bool Ready=>Snapshot!=null&&peer!=null&&peer.Connected;
        public int Generation {get;private set;}
        public string RecoveryCode=>token;
        public bool Owned(string grant)=>owned.Contains(grant);
        private readonly HashSet<string> owned=new HashSet<string>();
        private IMatchPeer peer;
        private string token,endpoint,key;
        private StoreCommand outstanding,retry;
        private bool needsHello;
        private string recoveryCandidate;
        private bool temporaryQa;
        private string qaOriginalToken;
        private StoreSnapshot qaOriginalSnapshot;
        private float deadline;
        private string FilePath(string name)=>Path.Combine(Application.persistentDataPath,name);
        public static bool Enabled
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        public static bool ValidToken(string s)=>s!=null&&System.Text.RegularExpressions.Regex.IsMatch(s,"^[a-f0-9]{32}\\z");
        private void Awake()
        {
            if(!Enabled)return;
            try{token=JsonUtility.FromJson<StoreSession>(File.ReadAllText(FilePath(".store-test-session.json"))).token;}catch(Exception){}
            if(!ValidToken(token)){token=Guid.NewGuid().ToString("N");SaveSession();}
            try{retry=JsonUtility.FromJson<StoreCommand>(File.ReadAllText(FilePath(".store-test-pending.json")));if(!ValidToken(retry.tx)||retry.token!=token)retry=null;}catch(Exception){retry=null;}
            try
            {
                var cache=JsonUtility.FromJson<StoreLocalInventory>(File.ReadAllText(FilePath(".store-test-inventory.json")));
                if(cache.account==SkinCatalog.Hash(Encoding.UTF8.GetBytes(token))){Validate(cache.snapshot);Accept(cache.snapshot,false);Status="Скачанные тестовые покупки доступны офлайн";}
            }catch(Exception){}
        }
        private void SaveSession()=>Save(".store-test-session.json",JsonUtility.ToJson(new StoreSession{token=token}));
        private void Save(string file,string text)
        {
            if(temporaryQa)return;
            string path=FilePath(file),temp=path+".tmp";File.WriteAllText(temp,text);
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
        public void Connect(string address,string invitation)
        {
            if(!Enabled||Busy||peer?.Connected==true)return;
            if(string.IsNullOrEmpty(invitation)){Status="Нужен ключ приглашения из сетевого теста";return;}
            try
            {
                var uri=new Uri(address);if(uri.Scheme!="wss"||!string.IsNullOrEmpty(uri.UserInfo))throw new ArgumentException();
                endpoint=new UriBuilder(uri){Path="/store-test",Query="",Fragment=""}.Uri.ToString();key=invitation;
                peer?.Dispose();peer=new MatchWebSocketPeer(endpoint,key);needsHello=true;deadline=Time.unscaledTime+12;Status="Подключение тестового магазина…";
            }
            catch(Exception){Status="Не удалось подключить тестовый магазин";}
        }
        private void Update()
        {
            if(peer==null)return;
            if(peer.Closed){peer.Dispose();peer=null;outstanding=null;Status="Связь потеряна. Восстановите покупки для проверки.";return;}
            if(peer.Connected&&outstanding==null&&needsHello){needsHello=false;Send(new StoreCommand{action="hello"});}
            while(peer.TryReceive(out var raw))
            {
                try
                {
                    if(raw.Length>131072)throw new InvalidDataException();
                    var s=JsonUtility.FromJson<StoreSnapshot>(Encoding.UTF8.GetString(raw));
                    if(s==null||s.schema!=1||s.mode!="test")throw new InvalidDataException();
                    if(outstanding==null||s.request!=outstanding.request)continue;
                    if(!s.ok){outstanding=null;recoveryCandidate=null;Status="Тестовый сервер отклонил операцию. Предыдущий профиль сохранён.";continue;}
                    Validate(s);string action=outstanding.action;outstanding=null;
                    if(recoveryCandidate!=null){token=recoveryCandidate;recoveryCandidate=null;retry=null;SaveSession();ClearRetry();}
                    Accept(s,true);
                    Status=s.status=="success"?"Тестовая покупка завершена · 0 ₽ списано":s.status=="cancel"?"Покупка отменена":s.status=="pending"?"Ожидает подтверждения · предмет не выдан":s.status=="error"?"Тестовая ошибка оплаты":s.status=="refunded"?"Тестовый возврат обработан":"Покупки восстановлены с сервера";
                    Debug.Log("STORE_TEST_RESPONSE action="+action+" status="+s.status+" owned="+owned.Count);
                    if(action=="purchase"){retry=null;ClearRetry();}
                    if(action=="hello"&&retry!=null)Send(retry);
                }
                catch(Exception){Status="Некорректный ответ тестового магазина";outstanding=null;recoveryCandidate=null;peer.Dispose();peer=null;return;}
            }
            if((outstanding!=null||!peer.Connected)&&Time.unscaledTime>deadline)
            {peer.Dispose();peer=null;outstanding=null;recoveryCandidate=null;Status="Ответ не получен. Результат покупки проверим восстановлением.";}
        }
        private void ClearRetry(){if(temporaryQa)return;string path=FilePath(".store-test-pending.json");if(File.Exists(path))File.Delete(path);}
        private void Accept(StoreSnapshot s,bool persist)
        {
            Snapshot=s;Catalog=s.catalog;owned.Clear();foreach(var item in s.owned)owned.Add(item);Generation++;
            if(persist)Save(".store-test-inventory.json",JsonUtility.ToJson(new StoreLocalInventory{account=SkinCatalog.Hash(Encoding.UTF8.GetBytes(token)),snapshot=s}));
        }
        public static void Validate(StoreSnapshot s)
        {
            if(s==null||s.schema!=1||s.mode!="test"||!s.ok||s.catalog==null||s.catalog.schema!=1||s.catalog.mode!="test"||s.catalog.products==null||s.catalog.products.Length>200||s.owned==null||s.owned.Length>200||s.transactions==null||s.transactions.Length>500)throw new InvalidDataException();
            var ids=new HashSet<string>();var grants=new HashSet<string>();
            foreach(var p in s.catalog.products)
            {
                if(p==null||!ValidProductId(p.id)||!ids.Add(p.id)||!ValidKind(p.kind)||string.IsNullOrEmpty(p.title)||p.title.Length>80||p.hero< -1||p.hero>12||p.priceTest<0||p.priceTest>10000||p.grants==null||p.grants.Length<1||p.grants.Length>32||(!string.IsNullOrEmpty(p.skin)&&!SkinCatalog.ValidId(p.skin)))throw new InvalidDataException();
                foreach(var g in p.grants){if(!ShopCosmetics.ValidGrant(g))throw new InvalidDataException();grants.Add(g);}
            }
            foreach(var g in s.owned)if(!grants.Contains(g))throw new InvalidDataException();
            foreach(var t in s.transactions)if(!ValidToken(t.tx)||!ids.Contains(t.product)||!(t.status=="success"||t.status=="cancel"||t.status=="error"||t.status=="pending"||t.status=="refunded"))throw new InvalidDataException();
        }
        private static bool ValidProductId(string id)=>id!=null&&System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-z0-9_.-]{1,80}\\z");
        private static bool ValidKind(string k)=>k=="skin"||k=="trail"||k=="spawn"||k=="dance"||k=="stage"||k=="emote"||k=="profile"||k=="bundle";
        private void Send(StoreCommand c)
        {
            c.token=recoveryCandidate??token;c.request=Guid.NewGuid().ToString("N");outstanding=c;deadline=Time.unscaledTime+12;
            peer.Send(Encoding.UTF8.GetBytes(JsonUtility.ToJson(c)));
        }
        public void Purchase(string product,string scenario="success",string transaction=null)
        {
            if(!Ready||Busy)return;
            var c=new StoreCommand{action="purchase",product=product,scenario=scenario,tx=transaction??Guid.NewGuid().ToString("N")};
            c.token=token;retry=c;Save(".store-test-pending.json",JsonUtility.ToJson(c));Send(c);
        }
        public void Restore()
        {
            if(Busy)return;
            if(peer?.Connected==true){if(retry!=null)Send(retry);else Send(new StoreCommand{action="restore"});}
            else Connect(endpoint,key);
        }
        public void Recovery(string code)
        {
            if(!ValidToken(code)||!Ready||Busy){Status="Код тестового профиля: 32 символа";return;}
            // Existing account only: a typo must not create a fresh account.
            recoveryCandidate=code;Send(new StoreCommand{action="restore"});
        }
        public void TestTransaction(string action,string tx){if(Ready&&!Busy&&(action=="approve"||action=="refund"))Send(new StoreCommand{action=action,tx=tx});}
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public void BeginTemporaryQa()
        {
            if(temporaryQa)throw new InvalidOperationException();
            qaOriginalToken=token;qaOriginalSnapshot=Snapshot;temporaryQa=true;
            token=Guid.NewGuid().ToString("N");retry=null;outstanding=null;Snapshot=null;owned.Clear();peer?.Dispose();peer=null;Generation++;Connect(endpoint,key);
        }
        public void EndTemporaryQa()
        {
            if(!temporaryQa)return;peer?.Dispose();peer=null;outstanding=null;retry=null;recoveryCandidate=null;
            token=qaOriginalToken;Snapshot=qaOriginalSnapshot;owned.Clear();if(Snapshot!=null)foreach(var g in Snapshot.owned)owned.Add(g);
            temporaryQa=false;qaOriginalToken=null;qaOriginalSnapshot=null;Generation++;Connect(endpoint,key);
        }
        public void ForgetInMemoryForQa(){Snapshot=null;owned.Clear();Generation++;}
#endif
        private void OnDestroy(){peer?.Dispose();}
    }
}
