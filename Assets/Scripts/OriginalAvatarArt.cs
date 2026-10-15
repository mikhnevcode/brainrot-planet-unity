using System;
using System.Collections.Generic;
using UnityEngine;
namespace BrainrotPlanet
{
    public static class OriginalAvatarArt
    {
        private static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>();
        private static readonly GameObject[] Models=new GameObject[OriginalHeroCatalog.Count];
        public static Material Palette(Material source)
        {
            if(source==null)throw new InvalidOperationException("Original cast material missing");
            string key=source.name;
            if(Materials.TryGetValue(key,out var existing)&&existing!=null)return existing;
            string[] tokens=key.Split(' ');
            if(tokens.Length<4||tokens[0]!="Orig26"||!ColorUtility.TryParseHtmlString(tokens[2],out var color))
                throw new InvalidOperationException("Original palette format: "+key);
            var shader=Resources.Load<Shader>("OriginalHeroSurface");
            if(shader==null)throw new InvalidOperationException("Original hero shader missing");
            var material=new Material(shader){name=key,color=color};
            string kind=tokens[3];
            int pattern=kind=="cloth"?2:kind=="dough"?3:kind=="paper"?4:kind=="metal"?5:kind=="glass"?6:kind=="leaf"?7:1;
            material.SetFloat("_Pattern",pattern);
            material.SetFloat("_Glossiness",kind=="glass"?.48f:kind=="metal"?.38f:.20f);
            material.SetFloat("_Metallic",kind=="metal"?.30f:0);
            Materials[key]=material;return material;
        }
        public static void Make(Transform root,int hero,Color territory)
        {
            var definition=OriginalHeroCatalog.Get(hero);
            var prefab=Models[hero];
            if(prefab==null)Models[hero]=prefab=Resources.Load<GameObject>(OriginalHeroCatalog.Resource(hero));
            if(prefab==null)throw new InvalidOperationException("Original model missing: "+definition.Id);
            var art=new GameObject("Original avatar").transform;art.SetParent(root,false);
            art.localPosition=new Vector3(0,.185f,0);art.localScale=Vector3.one*.5f;
            var visual=UnityEngine.Object.Instantiate(prefab,art,false);
            visual.name="Original26 "+definition.Id;
            visual.transform.localPosition=new Vector3(0,-.34f,0);
            visual.transform.localRotation=Quaternion.Euler(0,180,0);visual.transform.localScale=Vector3.one*.6f;
            visual.AddComponent<OriginalRigTag>().Hero=hero;
            visual.AddComponent<SkinRestBindings>().Capture(hero);
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var palette=renderer.sharedMaterials;
                for(int i=0;i<palette.Length;i++)palette[i]=Palette(palette[i]);
                renderer.sharedMaterials=palette;
            }
            var animator=visual.GetComponent<Animator>()??visual.AddComponent<Animator>();
            animator.runtimeAnimatorController=Resources.Load<RuntimeAnimatorController>("OriginalCast/"+definition.Id);
            if(animator.runtimeAnimatorController==null)throw new InvalidOperationException("Original controller missing: "+definition.Id);
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            Debug.Log("ORIGINAL_HERO_READY index="+hero+" id="+definition.Id+" opaque=true");
        }
    }
}
