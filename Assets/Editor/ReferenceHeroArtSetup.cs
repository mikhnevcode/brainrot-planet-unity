#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    /// <summary>Retains shared opaque Standard normal-map variants in Android builds.</summary>
    public static class ReferenceHeroArtSetup
    {
        public static readonly string[] Kinds={"wood","fur","hide","leather","cloth","leaf","fish","elephant","rubber","feather"};
        private const string Root="Assets/Resources/Characters/ReferenceDetail/Ref63_";
        public static void Prepare()
        {
            foreach(string kind in Kinds)
            {
                foreach(string suffix in new[]{"Albedo","Normal"})
                {
                    string path=Root+kind+"_"+suffix+".png";
                    var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                    if(importer==null)throw new Exception("Missing reference texture "+path);
                    bool normal=suffix=="Normal";
                    var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                    if(importer.textureType!=type || importer.sRGBTexture!=!normal ||
                        importer.maxTextureSize!=512 || !importer.mipmapEnabled)
                    {
                        importer.textureType=type;importer.sRGBTexture=!normal;
                        importer.maxTextureSize=512;importer.mipmapEnabled=true;
                        importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
                    }
                }
                string materialPath=Root+kind+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null)
                {
                    var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/PlanetStandard.mat");
                    if(template==null)throw new Exception("Missing PlanetStandard material");
                    material=new Material(template){name="Reference detail "+kind};
                    AssetDatabase.CreateAsset(material,materialPath);
                }
                material.color=Color.white;material.SetFloat("_Mode",0f);
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+kind+"_Albedo.png");
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+kind+"_Normal.png"));
                material.SetFloat("_BumpScale",.6f);material.SetFloat("_Glossiness",.10f);
                material.SetFloat("_SpecularHighlights",0f);material.SetFloat("_GlossyReflections",0f);
                material.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();Debug.Log("REFERENCE_DETAIL_MATERIALS_READY kinds="+Kinds.Length);
        }
    }
}
#endif
