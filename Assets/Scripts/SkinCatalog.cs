using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BrainrotPlanet.Networking;
using UnityEngine;

namespace BrainrotPlanet
{
    [Serializable] public sealed class SkinEntry { public string id,title,titleEn,description,file,sha256,swatch; public int hero,bytes; }
    [Serializable] public sealed class SkinManifest { public int schema,revision; public SkinEntry[] skins; }
    [Serializable] public sealed class SkinMesh { public string name,color,anchor; public bool glow; public float[] vertices,normals; public int[] triangles; }
    [Serializable] public sealed class SkinPalettePatch { public string material,color; public float metallic,smoothness; public bool glow; }
    [Serializable] public sealed class SkinPack { public int schema,hero; public string id,hull,trim,accent,skin; public SkinMesh[] meshes; public SkinPalettePatch[] bindings; }

    // Cosmetic data only: authored meshes and opaque material settings. All prototype
    // entries are free. This validates content, not store receipts or ownership.
    public sealed class SkinCatalog : MonoBehaviour
    {
        public const string BaseUrl="https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/rollo083/skins/";
        public const string CatalogFile="catalog-original26-v5.json";
        public const int MaxPackBytes=2*1024*1024,MaxManifestBytes=131072,MemoryPackLimit=16;
        public SkinManifest Manifest {get;private set;}=new SkinManifest{schema=2,revision=0,skins=new SkinEntry[0]};
        public bool Busy=>refreshing||inFlight.Count>0;
        public string Status {get;private set;}="Каталог ещё не загружен";
        public int Generation {get;private set;}
        private bool refreshing;
        private readonly Dictionary<string,SkinPack> packs=new Dictionary<string,SkinPack>();
        private readonly LinkedList<string> recent=new LinkedList<string>();
        private readonly HashSet<string> inFlight=new HashSet<string>();
        private readonly Dictionary<string,float> retryAfter=new Dictionary<string,float>();
        private string CacheDir=>Path.Combine(Application.persistentDataPath,"cosmetics-v1");
        public SkinEntry Entry(string id)
        {foreach(var e in Manifest.skins)if(e.id==id)return e;return null;}
        public static string Key(SkinEntry e)=>e.id+"@"+e.sha256;
        private SkinEntry EntryForReference(string reference)
        {foreach(var e in Manifest.skins)if(Key(e)==reference)return e;return null;}
        public SkinPack Pack(string id)
        {var e=Entry(id);return e==null?null:ReferencedPack(Key(e));}
        public bool Available(string id)=>string.IsNullOrEmpty(id)||Pack(id)!=null;
        public string Reference(string id)
        {var e=Entry(id);return e!=null&&Available(id)?Key(e):"";}
        public SkinPack ReferencedPack(string reference)
        {
            if(string.IsNullOrEmpty(reference))return null;
            if(packs.TryGetValue(reference,out var p)){Touch(reference);return p;}
            var e=EntryForReference(reference);if(e==null)return null;
            string path=Path.Combine(CacheDir,e.file);
            try
            {
                if(File.Exists(path)&&new FileInfo(path).Length==e.bytes)p=ReadPack(File.ReadAllBytes(path),e);
                else
                {
                    var bundled=Resources.Load<TextAsset>("OriginalCast/Skins/"+e.id);
                    if(bundled==null)return null;
                    try{p=ReadPack(bundled.bytes,e);}finally{Resources.UnloadAsset(bundled);}
                }
                Remember(reference,p);return p;
            }
            catch(Exception){return null;}
        }
        private void Touch(string key){recent.Remove(key);recent.AddLast(key);}
        private void Remember(string key,SkinPack pack)
        {
            packs[key]=pack;Touch(key);
            while(recent.Count>MemoryPackLimit){string oldest=recent.First.Value;recent.RemoveFirst();packs.Remove(oldest);}
        }
        public static bool ValidReference(string value)=>value==""||(value!=null&&Regex.IsMatch(value,"^[a-z0-9_-]{1,48}@[a-f0-9]{64}\\z"));
        public static bool ValidId(string id)=>id!=null&&Regex.IsMatch(id,"^[a-z0-9_-]{1,48}\\z");
        public static string Hash(byte[] bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static Color ParseColor(string value)
        {if(value==null||!Regex.IsMatch(value,"^#[0-9A-Fa-f]{6}\\z")||!ColorUtility.TryParseHtmlString(value,out var c))throw new InvalidDataException("Invalid skin color");return c;}
        public static SkinManifest ReadManifest(string json)
        {
            if(json==null||Encoding.UTF8.GetByteCount(json)>MaxManifestBytes)throw new InvalidDataException("Catalog size");
            var m=JsonUtility.FromJson<SkinManifest>(json);
            if(m==null||m.schema!=2||m.revision<1||m.skins==null||m.skins.Length>256)throw new InvalidDataException("Catalog schema");
            var ids=new HashSet<string>();
            foreach(var e in m.skins)
            {
                if(e==null||!ValidId(e.id)||!ids.Add(e.id)||e.hero<0||e.hero>=SkinRigProfile.HeroCount||(m.schema==1&&e.hero!=1)||
                   string.IsNullOrEmpty(e.title)||e.title.Length>48||e.description==null||e.description.Length>160||
                   (e.titleEn!=null&&e.titleEn.Length>64)||e.sha256==null||!Regex.IsMatch(e.sha256,"^[a-f0-9]{64}\\z")||
                   e.file!=e.id+"-"+e.sha256+".json"||e.bytes<1||e.bytes>MaxPackBytes)throw new InvalidDataException("Invalid skin entry");
                if(m.schema==2)ParseColor(e.swatch);
            }
            return m;
        }
        public static SkinPack ReadPack(byte[] data,SkinEntry e)
        {
            if(data==null||data.Length!=e.bytes||data.Length>MaxPackBytes||Hash(data)!=e.sha256)throw new InvalidDataException("Skin integrity");
            var p=JsonUtility.FromJson<SkinPack>(Encoding.UTF8.GetString(data));
            if(p==null||(p.schema!=1&&p.schema!=2)||p.hero!=e.hero||p.hero<0||p.hero>=SkinRigProfile.HeroCount||
               (p.schema==1&&p.hero!=1)||p.id!=e.id||p.meshes==null||p.meshes.Length>24)throw new InvalidDataException("Skin schema");
            if(p.schema==1){ParseColor(p.hull);ParseColor(p.trim);ParseColor(p.accent);ParseColor(p.skin);}
            else
            {
                if(p.bindings==null||p.bindings.Length>32)throw new InvalidDataException("Skin palette");
                var materials=new HashSet<string>();
                foreach(var b in p.bindings)
                {
                    if(b==null||string.IsNullOrEmpty(b.material)||b.material.Length>128||!materials.Add(b.material)||
                       !Finite(b.metallic)||b.metallic<0||b.metallic>1||!Finite(b.smoothness)||b.smoothness<0||b.smoothness>.8f)
                        throw new InvalidDataException("Skin material");
                    ParseColor(b.color);
                }
            }
            int vertices=0,indices=0;
            foreach(var mesh in p.meshes)
            {
                if(mesh==null||mesh.vertices==null||mesh.triangles==null||mesh.vertices.Length%3!=0||mesh.triangles.Length%3!=0||
                   string.IsNullOrEmpty(mesh.name)||mesh.name.Length>64)throw new InvalidDataException("Skin mesh");
                if(p.schema==2&&!SkinRigProfile.AllowsAnchor(p.hero,mesh.anchor))throw new InvalidDataException("Skin anchor");
                ParseColor(mesh.color);int count=mesh.vertices.Length/3;vertices+=count;indices+=mesh.triangles.Length;
                if(count<3||vertices>12000||indices>72000)throw new InvalidDataException("Skin geometry budget");
                foreach(float x in mesh.vertices)if(!Finite(x)||Mathf.Abs(x)>5f)throw new InvalidDataException("Skin coordinate");
                foreach(int index in mesh.triangles)if(index<0||index>=count)throw new InvalidDataException("Skin index");
                if(mesh.normals!=null&&mesh.normals.Length>0)
                {
                    if(mesh.normals.Length!=mesh.vertices.Length)throw new InvalidDataException("Skin normals");
                    for(int i=0;i<mesh.normals.Length;i+=3)
                    {
                        var n=new Vector3(mesh.normals[i],mesh.normals[i+1],mesh.normals[i+2]);
                        if(!Finite(n.x)||!Finite(n.y)||!Finite(n.z)||n.sqrMagnitude<.25f||n.sqrMagnitude>1.5f)
                            throw new InvalidDataException("Skin normal value");
                    }
                }
            }
            return p;
        }
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        private void Awake()
        {
            Debug.Log("BRAINROT_BOOT_SKIN_AWAKE");
            try
            {
                string path=Path.Combine(CacheDir,CatalogFile);
                if(File.Exists(path)&&new FileInfo(path).Length<=MaxManifestBytes)
                {Manifest=ReadManifest(File.ReadAllText(path));Generation++;Status="Скачанные образы доступны";Debug.Log("SKIN_CATALOG_CACHE revision="+Manifest.revision);}
            }
            catch(Exception){Status="Используется базовый образ";}
            // A newer app bundle must not be masked by an older offline cache.
            // A newer remote catalog still takes precedence over this bundle.
            {
                var bundled=Resources.Load<TextAsset>("OriginalCast/Skins/"+Path.GetFileNameWithoutExtension(CatalogFile));
                if(bundled!=null)
                {
                    try
                    {
                        var candidate=ReadManifest(bundled.text);
                        if(candidate.revision>Manifest.revision||Manifest.skins.Length==0)
                        {Manifest=candidate;Generation++;Status="Встроенный гардероб доступен офлайн";}
                    }
                    finally{Resources.UnloadAsset(bundled);}
                }
            }
            Refresh();
        }
        public void Refresh(){if(!refreshing)StartCoroutine(Fetch());}
        public void EnsureLoaded(string id){var e=Entry(id);if(e!=null)EnsureReference(Key(e));}
        public void EnsureReference(string reference)
        {
            var e=EntryForReference(reference);
            if(e==null||ReferencedPack(reference)!=null||inFlight.Contains(reference))return;
            if(retryAfter.TryGetValue(reference,out float retry)&&Time.unscaledTime<retry)return;
            inFlight.Add(reference);StartCoroutine(Download(e));
        }
        private IEnumerator Fetch()
        {
            refreshing=true;Status="Проверка каталога…";
            using var request=new ManagedHttpRequest(BaseUrl+CatalogFile+"?refresh="+DateTimeOffset.UtcNow.ToUnixTimeSeconds(),"GET"){MaxResponseBytes=MaxManifestBytes,TimeoutSeconds=10};
            request.Send();while(!request.IsDone)yield return null;
            try
            {
                if(request.Result!=ManagedHttpRequest.RequestResult.Success)throw new IOException();
                var manifest=ReadManifest(request.Text);
                SaveAtomically(CatalogFile,Encoding.UTF8.GetBytes(JsonUtility.ToJson(manifest)));
                Manifest=manifest;Generation++;Status="Каталог обновлён · образы загружаются при выборе";
                Debug.Log("SKIN_CATALOG_READY revision="+manifest.revision+" count="+manifest.skins.Length+" lazy=true");
            }
            catch(Exception){Status="Нет обновления. Скачанные образы доступны.";}
            refreshing=false;
        }
        private IEnumerator Download(SkinEntry e)
        {
            string key=Key(e);Status="Загрузка: "+e.title;
            using var request=new ManagedHttpRequest(BaseUrl+e.file,"GET"){MaxResponseBytes=MaxPackBytes,TimeoutSeconds=15};
            request.Send();while(!request.IsDone)yield return null;
            try
            {
                if(request.Result!=ManagedHttpRequest.RequestResult.Success)throw new IOException();
                var p=ReadPack(request.Data,e);SaveAtomically(e.file,request.Data);Remember(key,p);
                Generation++;retryAfter.Remove(key);Status="Образ готов к примерке";
                Debug.Log("SKIN_PACK_READY id="+e.id+" hero="+e.hero+" bytes="+e.bytes);
            }
            catch(Exception){retryAfter[key]=Time.unscaledTime+15f;Status="Не удалось загрузить образ. Доступен классический.";}
            inFlight.Remove(key);
        }
        private void SaveAtomically(string filename,byte[] data)
        {
            Directory.CreateDirectory(CacheDir);string path=Path.Combine(CacheDir,filename),temp=path+".tmp";
            File.WriteAllBytes(temp,data);
            if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        }
    }
}
