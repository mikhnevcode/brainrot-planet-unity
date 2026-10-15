#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class TralaleroShoeSmoke
    {
        private const string ModelPath="Assets/Resources/Characters/Tralalero_v07.fbx";

        public static void Run()
        {
            GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if(model==null)throw new Exception("Missing corrected Tralalero v07 model");
            int shoes=0;
            foreach(string side in new[]{"L","R","Rear"})
            {
                string foot="Foot_"+side,meshName="Combined_"+foot;
                SkinnedMeshRenderer shoe=null;
                foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if(renderer.name==meshName)shoe=renderer;
                if(shoe==null||shoe.sharedMesh==null)throw new Exception("Missing fused shoe "+meshName);
                if(shoe.sharedMesh.vertexCount>2200)
                    throw new Exception("Free-standing shoe shells returned "+meshName+" vertices="+shoe.sharedMesh.vertexCount);
                int footIndex=-1;
                for(int i=0;i<shoe.bones.Length;i++)if(shoe.bones[i]!=null&&shoe.bones[i].name==foot)footIndex=i;
                if(footIndex<0)throw new Exception("Shoe is not bound to "+foot);
                foreach(var weight in shoe.sharedMesh.boneWeights)
                {
                    float correct=0f,wrong=0f;
                    if(weight.weight0>0f){if(weight.boneIndex0==footIndex)correct+=weight.weight0;else wrong+=weight.weight0;}
                    if(weight.weight1>0f){if(weight.boneIndex1==footIndex)correct+=weight.weight1;else wrong+=weight.weight1;}
                    if(weight.weight2>0f){if(weight.boneIndex2==footIndex)correct+=weight.weight2;else wrong+=weight.weight2;}
                    if(weight.weight3>0f){if(weight.boneIndex3==footIndex)correct+=weight.weight3;else wrong+=weight.weight3;}
                    if(correct<.999f||wrong>.001f)throw new Exception("Non-rigid shoe weight "+meshName);
                }
                shoes++;
            }

            var manifest=SkinCatalog.ReadManifest(File.ReadAllText("Content/Skins/catalog-cast-v1.json"));
            int skins=0;
            foreach(var entry in manifest.skins)
            {
                if(entry.hero!=0)continue;
                string path="Content/Skins/Cast/"+entry.file;
                var pack=SkinCatalog.ReadPack(File.ReadAllBytes(path),entry);
                foreach(var part in pack.meshes)
                    if(part.anchor.StartsWith("Foot_",StringComparison.Ordinal))
                        throw new Exception("Detached shoe overlay returned in "+entry.id+" / "+part.name);
                skins++;
            }
            if(shoes!=3||skins!=5)throw new Exception("Incomplete Tralalero shoe validation");
            Debug.Log("TRALALERO_SHOE_SMOKE_PASS shoes=3 skins=5 rigidFootWeights=true floatingOverlays=0 vertexBudget=true");
        }
    }
}
#endif
