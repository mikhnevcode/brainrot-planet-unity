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
    [Serializable] public sealed class SkinEntry { public string id,title,description,file,sha256; public int hero,bytes; }
    [Serializable] public sealed class SkinManifest { public int schema,revision; public SkinEntry[] skins; }
    [Serializable] public sealed class SkinMesh { public string name,color; public bool glow; public float[] vertices; public int[] triangles; }
    [Serializable] public sealed class SkinPack { public int schema,hero; public string id,hull,trim,accent,skin; public SkinMesh[] meshes; }

    // Deliberately data-only cosmetic schema. No downloaded code, prefabs, colliders,
    // shaders or game rules. Every alpha entry is free; this is NOT an entitlement service.
    public sealed class SkinCatalog : MonoBehaviour
    {
        public const string BaseUrl="https://loadfiles.testmkn.ru/downloads/brainrot-planet/cast/skins/";
        public const int MaxPackBytes=2*1024*1024;
        public SkinManifest Manifest {get;private set;}=new SkinManifest{schema=1,revision=0,skins=new SkinEntry[0]};
        public bool Busy {get;private set;}
        public string Status {get;private set;}="Каталог ещё не загружен";
        public int Generation {get;private set;}
        private readonly Dictionary<string,SkinPack> packs=new Dictionary<string,SkinPack>();
        private string CacheDir=>Path.Combine(Application.persistentDataPath,"cosmetics-v1");
        public SkinPack Pack(string id)=>!string.IsNullOrEmpty(id)&&packs.TryGetValue(id,out var p)?p:null;
        public bool Available(string id)=>string.IsNullOrEmpty(id)||Pack(id)!=null;
        public static bool ValidReference(string value)=>value==""||(value!=null&&Regex.IsMatch(value,"^[a-z0-9_-]{1,48}@[a-f0-9]{64}$"));
        public string Reference(string id)
        {foreach(var e in Manifest.skins)if(e.id==id && Available(id))return id+"@"+e.sha256;return "";}
        public SkinPack ReferencedPack(string value)
        {foreach(var e in Manifest.skins)if(e.id+"@"+e.sha256==value)return Pack(e.id);return null;}
        public static bool ValidId(string id)=>id!=null&&Regex.IsMatch(id,"^[a-z0-9_-]{1,48}$");
        public static string Hash(byte[] bytes) { using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        public static Color ParseColor(string value)
        {if(value==null||!Regex.IsMatch(value,"^#[0-9A-Fa-f]{6}$")||!ColorUtility.TryParseHtmlString(value,out var c))throw new InvalidDataException("Invalid skin color");return c;}
        public static SkinManifest ReadManifest(string json)
        {
            if(json==null||json.Length>65536)throw new InvalidDataException("Catalog size");
            var m=JsonUtility.FromJson<SkinManifest>(json);
            if(m==null||m.schema!=1||m.revision<1||m.skins==null||m.skins.Length>24)throw new InvalidDataException("Catalog schema");
            var ids=new HashSet<string>();
            foreach(var e in m.skins)
                if(e==null||!ValidId(e.id)||!ids.Add(e.id)||e.hero!=1||string.IsNullOrEmpty(e.title)||e.title.Length>48||
                   e.description==null||e.description.Length>160||e.sha256==null||!Regex.IsMatch(e.sha256,"^[a-f0-9]{64}$")||
                   e.file!=e.id+"-"+e.sha256+".json"||e.bytes<1||e.bytes>MaxPackBytes)
                    throw new InvalidDataException("Invalid skin entry");
            return m;
        }
        public static SkinPack ReadPack(byte[] data,SkinEntry e)
        {
            if(data.Length!=e.bytes||data.Length>MaxPackBytes||Hash(data)!=e.sha256)throw new InvalidDataException("Skin integrity");
            var p=JsonUtility.FromJson<SkinPack>(Encoding.UTF8.GetString(data));
            if(p==null||p.schema!=1||p.hero!=1||p.id!=e.id||p.meshes==null||p.meshes.Length>16)throw new InvalidDataException("Skin schema");
            ParseColor(p.hull);ParseColor(p.trim);ParseColor(p.accent);ParseColor(p.skin);
            int vertices=0,indices=0;
            foreach(var mesh in p.meshes)
            {
                if(mesh==null||mesh.vertices==null||mesh.triangles==null||mesh.vertices.Length%3!=0||mesh.triangles.Length%3!=0||
                    string.IsNullOrEmpty(mesh.name)||mesh.name.Length>64)throw new InvalidDataException("Skin mesh");
                ParseColor(mesh.color);int count=mesh.vertices.Length/3;vertices+=count;indices+=mesh.triangles.Length;
                if(count<3||vertices>12000||indices>72000)throw new InvalidDataException("Skin geometry budget");
                foreach(float x in mesh.vertices)if(float.IsNaN(x)||float.IsInfinity(x)||Mathf.Abs(x)>5f)throw new InvalidDataException("Skin coordinate");
                foreach(int index in mesh.triangles)if(index<0||index>=count)throw new InvalidDataException("Skin index");
            }
            return p;
        }
        private void Awake()
        {
            try
            {
                string path=Path.Combine(CacheDir,"catalog.json");
                if(File.Exists(path))
                {
                    var m=ReadManifest(File.ReadAllText(path));var restored=new Dictionary<string,SkinPack>();
                    foreach(var e in m.skins)restored[e.id]=ReadPack(File.ReadAllBytes(Path.Combine(CacheDir,e.file)),e);
                    Commit(m,restored);Status="Каталог из кеша · "+m.revision;
                }
            }
            catch(Exception){Status="Используется базовый образ";}
            Refresh();
        }
        public void Refresh(){if(!Busy)StartCoroutine(Fetch());}
        private void Commit(SkinManifest manifest,Dictionary<string,SkinPack> ready)
        {packs.Clear();foreach(var pair in ready)packs[pair.Key]=pair.Value;Manifest=manifest;Generation++;}
        private IEnumerator Fetch()
        {
            Busy=true;Status="Проверка каталога…";
            using var request=new ManagedHttpRequest(BaseUrl+"catalog.json?refresh="+DateTimeOffset.UtcNow.ToUnixTimeSeconds(),"GET"){MaxResponseBytes=65536,TimeoutSeconds=10};
            request.Send();while(!request.IsDone)yield return null;
            SkinManifest manifest=null;
            try {if(request.Result!=ManagedHttpRequest.RequestResult.Success)throw new IOException();manifest=ReadManifest(request.Text);}
            catch(Exception){Status="Нет обновления. Скачанные образы доступны.";Busy=false;yield break;}
            var ready=new Dictionary<string,SkinPack>();
            foreach(var e in manifest.skins)
            {
                Status="Загрузка: "+e.title;
                string path=Path.Combine(CacheDir,e.file);byte[] data=null;
                try{if(File.Exists(path)){data=File.ReadAllBytes(path);ReadPack(data,e);}}catch(Exception){data=null;}
                if(data==null)
                {
                    using var download=new ManagedHttpRequest(BaseUrl+e.file,"GET"){MaxResponseBytes=MaxPackBytes,TimeoutSeconds=15};
                    download.Send();while(!download.IsDone)yield return null;
                    if(download.Result==ManagedHttpRequest.RequestResult.Success)data=download.Data;
                }
                try
                {
                    if(data==null)throw new IOException();ready[e.id]=ReadPack(data,e);
                    Directory.CreateDirectory(CacheDir);File.WriteAllBytes(path,data);
                }
                catch(Exception){Status="Обновление отклонено. Предыдущий каталог сохранён.";Busy=false;yield break;}
                yield return null;
            }
            // All packages validate before activating the new catalog. Crash-safe last-good replacement.
            try
            {
                Directory.CreateDirectory(CacheDir);string path=Path.Combine(CacheDir,"catalog.json"),temp=path+".tmp";
                File.WriteAllText(temp,JsonUtility.ToJson(manifest),new UTF8Encoding(false));
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
                Commit(manifest,ready);Status="Каталог "+manifest.revision+" · бесплатная примерка";
                Debug.Log("SKIN_CATALOG_READY revision="+manifest.revision+" count="+manifest.skins.Length);
            }
            catch(Exception){Status="Не удалось сохранить каталог. Старые образы доступны.";}
            Busy=false;
        }
    }
}
