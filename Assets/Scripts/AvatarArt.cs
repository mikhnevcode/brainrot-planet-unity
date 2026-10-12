using UnityEngine;
using System.Collections.Generic;

namespace BrainrotPlanet
{
    /// <summary>Original primitive-built, meme-inspired silhouettes. No third-party model assets.</summary>
    public static class AvatarArt
    {
        private static Transform Part(Transform parent,string name,PrimitiveType type,Vector3 p,
                                      Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;
            go.transform.SetParent(parent,false);go.transform.localPosition=p;
            go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();if(collider)Object.Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial=mat;
            return go.transform;
        }
        private static Transform Cube(Transform parent,string name,Vector3 p,Vector3 s,Material m)
            => Part(parent,name,PrimitiveType.Cube,p,s,m);
        private static Transform Oval(Transform parent,string name,Vector3 p,Vector3 s,Material m)
            => Part(parent,name,PrimitiveType.Sphere,p,s,m);
        private static readonly Dictionary<string,Material> SharkMaterials=new Dictionary<string,Material>();
        private static Material Palette(Material source,string fallback)
        {
            string n=(source!=null?source.name:fallback).ToLowerInvariant();
            if(SharkMaterials.TryGetValue(n,out Material cached))return cached;
            Color c;
            if(n.Contains("deep dorsal"))c=new Color(.065f,.27f,.42f);
            else if(n.Contains("blue grey")||n.Contains("shark skin"))c=new Color(.16f,.49f,.70f);
            else if(n.Contains("pale belly")||n.Contains("underbelly"))c=new Color(.85f,.90f,.85f);
            else if(n.Contains("gill shadow"))c=new Color(.025f,.12f,.18f);
            else if(n.Contains("pupil")||n.Contains("mouth"))c=new Color(.028f,.035f,.045f);
            else if(n.Contains("iris"))c=new Color(.56f,.35f,.15f);
            else if(n.Contains("eye wet"))c=new Color(.62f,.76f,.78f);
            else if(n.Contains("blue sneaker"))c=new Color(.05f,.55f,.98f);
            else if(n.Contains("blue mesh"))c=new Color(.035f,.30f,.65f);
            else if(n.Contains("white sneaker"))c=new Color(.96f,.96f,.93f);
            else if(n.Contains("dark outsole"))c=new Color(.05f,.10f,.15f);
            else if(n.Contains("light cyan")||n.Contains("white detail"))c=new Color(.83f,.94f,1f);
            else if(n.Contains("teeth"))c=new Color(.96f,.95f,.84f);
            else c=new Color(.22f,.52f,.69f);
            Material m=Colors.New(c);
            // Shark skin and eyes must be matte; the Standard shader previously
            // produced a distracting white hotspot on the top of the snout.
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.035f);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
            if(m.HasProperty("_SpecColor"))m.SetColor("_SpecColor",new Color(.025f,.025f,.025f));
            if(m.HasProperty("_SpecularHighlights"))m.SetFloat("_SpecularHighlights",0f);
            if(m.HasProperty("_GlossyReflections"))m.SetFloat("_GlossyReflections",0f);
            m.name="Tralalero / "+n;
            SharkMaterials.Add(n,m);
            return m;
        }
        private static readonly Dictionary<string,Material> AircraftMaterials=new Dictionary<string,Material>();
        private static Material AircraftPalette(Material source)
        {
            string n=(source!=null?source.name:"unknown").ToLowerInvariant();
            if(AircraftMaterials.TryGetValue(n,out Material cached))return cached;
            Color c;
            if(n.Contains("crocodile dark"))c=new Color(.12f,.34f,.19f);
            else if(n.Contains("crocodile nose"))c=new Color(.22f,.46f,.24f);
            else if(n.Contains("underjaw"))c=new Color(.51f,.57f,.39f);
            else if(n.Contains("inner mouth"))c=new Color(.22f,.05f,.07f);
            else if(n.Contains("ivory teeth"))c=new Color(.93f,.89f,.74f);
            else if(n.Contains("yellow reptile"))c=new Color(.9f,.69f,.14f);
            else if(n.Contains("black slit"))c=new Color(.012f,.018f,.012f);
            else if(n.Contains("brushed silver"))c=new Color(.46f,.50f,.54f);
            else if(n.Contains("metal dark"))c=new Color(.24f,.31f,.34f);
            else if(n.Contains("silver edge"))c=new Color(.70f,.74f,.75f);
            else if(n.Contains("propeller nearly"))c=new Color(.045f,.065f,.085f);
            else if(n.Contains("propeller yellow"))c=new Color(.93f,.71f,.15f);
            else if(n.Contains("cockpit"))c=new Color(.17f,.40f,.55f);
            else if(n.Contains("scale crevice"))c=new Color(.04f,.13f,.07f);
            else if(n.Contains("paint tank"))c=new Color(.03f,.74f,.82f);
            else c=new Color(.35f,.40f,.43f);
            Material m=Colors.New(c);
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.12f);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
            if(m.HasProperty("_SpecColor"))m.SetColor("_SpecColor",new Color(.04f,.04f,.04f));
            if(m.HasProperty("_SpecularHighlights"))m.SetFloat("_SpecularHighlights",0f);
            if(m.HasProperty("_GlossyReflections"))m.SetFloat("_GlossyReflections",0f);
            m.name="Bombardiro / "+n;
            AircraftMaterials.Add(n,m);
            return m;
        }
        private static void RecolorAircraft(Transform root)
        {
            int count=0;
            foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)mats[i]=AircraftPalette(mats[i]);
                renderer.sharedMaterials=mats;
                count++;
            }
            Debug.Log("BOMBARDIRO_COLORED_RENDERERS="+count);
        }
        private static readonly Dictionary<string,Material> GooseMaterials=new Dictionary<string,Material>();
        private static Material GoosePalette(Material source)
        {
            string n=(source!=null?source.name:"unknown").ToLowerInvariant();
            if(GooseMaterials.TryGetValue(n,out Material old))return old;
            Color c;
            if(n.Contains("pearl white")||n.Contains("feathers"))c=new Color(.88f,.89f,.84f);
            else if(n.Contains("grey wing"))c=new Color(.57f,.66f,.69f);
            else if(n.Contains("neck ivory"))c=new Color(.96f,.91f,.78f);
            else if(n.Contains("coral orange")||n.Contains("webbed foot"))c=new Color(.95f,.39f,.14f);
            else if(n.Contains("dark beak"))c=new Color(.20f,.11f,.08f);
            else if(n.Contains("amber"))c=new Color(.68f,.41f,.14f);
            else if(n.Contains("black pupil"))c=new Color(.02f,.03f,.04f);
            else if(n.Contains("eye highlights"))c=new Color(.92f,.96f,.93f);
            else if(n.Contains("satin dark silver"))c=new Color(.36f,.43f,.50f);
            else if(n.Contains("pale aluminium"))c=new Color(.67f,.74f,.76f);
            else if(n.Contains("rivet seams"))c=new Color(.26f,.29f,.32f);
            else if(n.Contains("engine gunmetal"))c=new Color(.19f,.25f,.30f);
            else if(n.Contains("turbine cyan"))c=new Color(.08f,.68f,.88f);
            else if(n.Contains("magenta"))c=new Color(.83f,.20f,.49f);
            else c=new Color(.70f,.76f,.76f);
            Material m=Colors.New(c);
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.08f);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
            if(m.HasProperty("_SpecularHighlights"))m.SetFloat("_SpecularHighlights",0f);
            m.name="Bombombini / "+n;GooseMaterials[n]=m;
            return m;
        }
        private static void RecolorGoose(Transform root)
        {
            int count=0;
            foreach(Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats=r.sharedMaterials;
                for(int i=0;i<mats.Length;i++)mats[i]=GoosePalette(mats[i]);
                r.sharedMaterials=mats;count++;
            }
            Debug.Log("BOMBOMBINI_COLORED_RENDERERS="+count);
        }
        private static void Recolor(Transform imported)
        {
            int count=0;
            foreach(Renderer renderer in imported.GetComponentsInChildren<Renderer>(true))
            {
                // The former separate pale jaw ellipsoid intersected the head,
                // creating a bright white sphere on the dorsal side in Unity.
                // Keep the original belly material on the main torso.
                if(renderer.name.IndexOf("Underjaw pale underside",
                    System.StringComparison.OrdinalIgnoreCase)>=0)
                {
                    renderer.enabled=false;
                    Debug.Log("TRALALERO_HIDDEN_JAW_ELLIPSOID="+renderer.name);
                    continue;
                }
                Material[] mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    string materialName=mats[i]!=null ? mats[i].name.ToLowerInvariant() : "";
                    // A flipped FBX belly material slot painted an obvious white
                    // oval across the upper shark head. The torso is now one
                    // continuous blue skin; separate small facial details remain.
                    bool bodyBelly=renderer.name.IndexOf("Shark organic smoothed torso",
                        System.StringComparison.OrdinalIgnoreCase)>=0 &&
                        materialName.Contains("pale belly");
                    mats[i]=bodyBelly ? Palette(null,"Shark skin") :
                        Palette(mats[i],renderer.name);
                    if(bodyBelly)Debug.Log("TRALALERO_CORRECTED_TORSO_BELLY_SLOT");
                }
                renderer.sharedMaterials=mats;
                count++;
            }
            Debug.Log("TRALALERO_COLORED_RENDERERS="+count);
        }
        public static void Make(Transform root,int character,Color territoryColor)
        {
            Transform art=new GameObject("Brainrot avatar / animated").transform;
            art.SetParent(root,false);
            art.localPosition=new Vector3(0,.37f,0);
            Material white=Colors.New(new Color(.97f,.99f,1f));
            Material black=Colors.New(new Color(.06f,.08f,.17f));
            Material red=Colors.New(new Color(1f,.14f,.26f));
            Material blue=Colors.New(new Color(.18f,.80f,.94f));
            Material sand=Colors.New(new Color(.91f,.73f,.40f));
            Material olive=Colors.New(new Color(.36f,.70f,.37f));
            Material belly=Colors.New(new Color(.83f,.94f,.82f));
            Material silver=Colors.New(new Color(.72f,.82f,.92f));
            Material neon=Colors.New(territoryColor,true);
            if(character==0)
            {
                // Authored in Blender with an actual three-legged skeleton and
                // imported Animator clips. Retain old geometry as a fallback.
                GameObject prefab=Resources.Load<GameObject>("Characters/Tralalero_v02");
                if(prefab!=null)
                {
                    GameObject visual=Object.Instantiate(prefab,art,false);
                    visual.name="Tralalero fully rigged Blender character";
                    visual.transform.localPosition=new Vector3(0,-.34f,0);
                    visual.transform.localRotation=Quaternion.Euler(0f,180f,0f);
                    visual.transform.localScale=Vector3.one*.60f;
                    Recolor(visual.transform);
                    foreach(Collider collider in visual.GetComponentsInChildren<Collider>(true))
                        Object.Destroy(collider);
                    Animator anim=visual.GetComponent<Animator>();
                    if(anim==null)anim=visual.AddComponent<Animator>();
                    anim.runtimeAnimatorController=
                        Resources.Load<RuntimeAnimatorController>("Characters/Tralalero");
                    anim.applyRootMotion=false;
                    anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    Debug.Log("TRALALERO_RIG_LOADED animator="+
                        (anim.runtimeAnimatorController!=null));
                    Oval(root,"Paint aura",new Vector3(0,-.42f,0),
                         new Vector3(1.25f,.09f,1.25f),neon);
                    return;
                }
                Debug.LogWarning("TRALALERO_FBX_MISSING: falling back to primitives");
            }
            if(character==1)
            {
                GameObject prefab=Resources.Load<GameObject>("Characters/Bombardiro_v01");
                if(prefab!=null)
                {
                    GameObject visual=Object.Instantiate(prefab,art,false);
                    visual.name="Bombardiro crocodile airplane / Blender skeletal rig";
                    visual.transform.localPosition=new Vector3(0,-.32f,0);
                    visual.transform.localRotation=Quaternion.Euler(0f,180f,0f);
                    visual.transform.localScale=Vector3.one*.45f;
                    RecolorAircraft(visual.transform);
                    foreach(Collider collider in visual.GetComponentsInChildren<Collider>(true))
                        Object.Destroy(collider);
                    Animator anim=visual.GetComponent<Animator>();
                    if(anim==null)anim=visual.AddComponent<Animator>();
                    anim.runtimeAnimatorController=
                        Resources.Load<RuntimeAnimatorController>("Characters/Bombardiro");
                    anim.applyRootMotion=false;
                    anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    Debug.Log("BOMBARDIRO_RIG_LOADED animator="+
                        (anim.runtimeAnimatorController!=null));
                    Oval(root,"Paint aura",new Vector3(0,-.42f,0),
                         new Vector3(1.25f,.09f,1.25f),neon);
                    return;
                }
                Debug.LogWarning("BOMBARDIRO_FBX_MISSING: primitives remain in use");
            }
            if(character==3)
            {
                GameObject prefab=Resources.Load<GameObject>("Characters/Bombombini_v01");
                if(prefab!=null)
                {
                    GameObject visual=Object.Instantiate(prefab,art,false);
                    visual.name="Bombombini Gusini / rigged goose aircraft";
                    visual.transform.localPosition=new Vector3(0,-.30f,0);
                    visual.transform.localRotation=Quaternion.Euler(0f,180f,0f);
                    visual.transform.localScale=Vector3.one*.48f;
                    RecolorGoose(visual.transform);
                    foreach(Collider c in visual.GetComponentsInChildren<Collider>(true))
                        Object.Destroy(c);
                    Animator anim=visual.GetComponent<Animator>();
                    if(anim==null)anim=visual.AddComponent<Animator>();
                    anim.runtimeAnimatorController=
                        Resources.Load<RuntimeAnimatorController>("Characters/Bombombini");
                    anim.applyRootMotion=false;
                    anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    Debug.Log("BOMBOMBINI_RIG_LOADED animator="+
                        (anim.runtimeAnimatorController!=null));
                    Oval(root,"Paint aura",new Vector3(0,-.42f,0),
                         new Vector3(1.25f,.09f,1.25f),neon);
                    return;
                }
                Debug.LogWarning("BOMBOMBINI_FBX_MISSING: using simple fallback");
            }
            if(character==0) // Tralalero fallback if FBX is unavailable.
            {
                Oval(art,"Shark body",new Vector3(0,.65f,0),new Vector3(.95f,1.15f,.9f),blue);
                Oval(art,"White belly",new Vector3(0,.57f,.42f),new Vector3(.67f,.88f,.22f),white);
                Oval(art,"Shark snout",new Vector3(0,.90f,.53f),new Vector3(.63f,.35f,.72f),blue);
                Transform fin=Cube(art,"Dorsal fin",new Vector3(0,1.30f,-.25f),new Vector3(.19f,.61f,.43f),blue);
                fin.localRotation=Quaternion.Euler(24f,0,0);
                Oval(art,"Eye L",new Vector3(-.27f,1.05f,.53f),new Vector3(.11f,.12f,.10f),black);
                Oval(art,"Eye R",new Vector3(.27f,1.05f,.53f),new Vector3(.11f,.12f,.10f),black);
                Cube(art,"Smile",new Vector3(0,.77f,.77f),new Vector3(.39f,.035f,.045f),black);
                Cube(art,"Sneaker L",new Vector3(-.35f,-.1f,.24f),new Vector3(.50f,.25f,.75f),red);
                Cube(art,"Sneaker R",new Vector3(.35f,-.1f,.24f),new Vector3(.50f,.25f,.75f),red);
                Cube(art,"Sole L",new Vector3(-.35f,-.25f,.30f),new Vector3(.53f,.11f,.76f),white);
                Cube(art,"Sole R",new Vector3(.35f,-.25f,.30f),new Vector3(.53f,.11f,.76f),white);
            }
            else if(character==1) // Bombardiro Crocodilo: crocodile-plane hybrid.
            {
                Oval(art,"Airframe",new Vector3(0,.57f,0),new Vector3(.84f,.70f,1.55f),olive);
                Cube(art,"Right wing",new Vector3(.81f,.54f,-.15f),new Vector3(1.30f,.10f,.51f),silver);
                Cube(art,"Left wing",new Vector3(-.81f,.54f,-.15f),new Vector3(1.30f,.10f,.51f),silver);
                Cube(art,"Tail fin",new Vector3(0,.97f,-.72f),new Vector3(.12f,.69f,.28f),olive);
                Oval(art,"Croc snout",new Vector3(0,.65f,1.08f),new Vector3(.82f,.35f,1.18f),olive);
                Cube(art,"Teeth",new Vector3(0,.52f,1.43f),new Vector3(.72f,.10f,.47f),white);
                Oval(art,"Eyes L",new Vector3(-.29f,.91f,.68f),new Vector3(.15f,.19f,.16f),white);
                Oval(art,"Eyes R",new Vector3(.29f,.91f,.68f),new Vector3(.15f,.19f,.16f),white);
                Oval(art,"Pupil L",new Vector3(-.29f,.92f,.79f),new Vector3(.06f,.09f,.07f),black);
                Oval(art,"Pupil R",new Vector3(.29f,.92f,.79f),new Vector3(.06f,.09f,.07f),black);
                Cube(art,"Paint tank",new Vector3(0,1f,-.14f),new Vector3(.37f,.31f,.52f),neon);
            }
            else if(character==2) // Frigo Camelo: desert camel / refrigerator hybrid.
            {
                Cube(art,"Fridge torso",new Vector3(0,.65f,-.06f),new Vector3(.93f,1.10f,.82f),white);
                Cube(art,"Fridge rim",new Vector3(0,.65f,.38f),new Vector3(.92f,1.04f,.075f),silver);
                Cube(art,"Fridge door",new Vector3(0,.65f,.43f),new Vector3(.77f,.94f,.045f),white);
                Cube(art,"Handle",new Vector3(.27f,.70f,.49f),new Vector3(.06f,.32f,.07f),black);
                Oval(art,"Hump",new Vector3(0,1.24f,-.35f),new Vector3(.78f,.65f,.73f),sand);
                Cube(art,"Camel neck",new Vector3(0,1.36f,.43f),new Vector3(.40f,.90f,.36f),sand);
                Oval(art,"Camel head",new Vector3(0,1.82f,.57f),new Vector3(.57f,.37f,.60f),sand);
                Oval(art,"Camel nose",new Vector3(0,1.74f,.89f),new Vector3(.51f,.25f,.44f),belly);
                Oval(art,"Eye left",new Vector3(-.25f,1.93f,.71f),new Vector3(.08f,.10f,.08f),black);
                Oval(art,"Eye right",new Vector3(.25f,1.93f,.71f),new Vector3(.08f,.10f,.08f),black);
                Cube(art,"Front left hoof",new Vector3(-.35f,-.14f,.24f),new Vector3(.23f,.44f,.27f),sand);
                Cube(art,"Front right hoof",new Vector3(.35f,-.14f,.24f),new Vector3(.23f,.44f,.27f),sand);
                Cube(art,"Back left hoof",new Vector3(-.35f,-.14f,-.35f),new Vector3(.23f,.44f,.27f),sand);
                Cube(art,"Back right hoof",new Vector3(.35f,-.14f,-.35f),new Vector3(.23f,.44f,.27f),sand);
            }
            if(character==3)
            {
                Oval(art,"Goose fallback white body",new Vector3(0,.77f,0),new Vector3(.8f,.75f,1.4f),white);
                Cube(art,"Goose fallback aircraft wing",new Vector3(0,.75f,-.3f),new Vector3(2.9f,.10f,.65f),silver);
                Oval(art,"Goose fallback neck",new Vector3(0,1.28f,.65f),new Vector3(.32f,.78f,.31f),white);
                Oval(art,"Goose fallback beak",new Vector3(0,1.68f,.97f),new Vector3(.46f,.13f,.40f),sand);
            }
            Oval(root,"Paint aura",new Vector3(0,-.42f,0),new Vector3(1.25f,.09f,1.25f),neon);
        }
    }
}
