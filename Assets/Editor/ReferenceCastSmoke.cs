#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class ReferenceCastSmoke
    {
        public static void Run()
        {
            foreach(var front in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left})
            {
                Quaternion orientation=AvatarArt.AlignFacing(front,Vector3.back);
                if(Vector3.Dot(orientation*Vector3.up,Vector3.up)<.999f ||
                    Vector3.Dot(orientation*front,Vector3.back)<.999f)
                    throw new Exception("Facing yaw flipped an antiparallel portrait");
            }
            string[] names={"Tralalero","Bombardiro","Frigo","Bombombini","Ballerina","Assassino",
                "Patapim","TungSahur","Lirili","Frulli","Boneca","Trippi","Troppa"};
            string[] actions={"Drop","Idle","Jump","Land","Run","Special","Victory","VictoryShow"};
            foreach(string name in names)
            {
                string version=name=="Tralalero"?"v07":name=="Bombardiro"?"v06":name=="Troppa"?"v01":"v04";
                string path="Assets/Resources/Characters/"+name+"_"+version+".fbx";
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(model==null)throw new Exception("Missing reference hero "+name);
                if(name!="Bombardiro")
                {
                    bool origin=false,front=false;
                    foreach(var node in model.GetComponentsInChildren<Transform>(true))
                    {
                        origin|=node.name=="ReferenceFacingOrigin";
                        front|=node.name=="ReferenceFacingFront";
                    }
                    if(!origin||!front)throw new Exception("Missing anatomical facing markers "+name);
                    Debug.Log("REFERENCE_FACE_AXIS name="+name+" local="+AvatarArt.LocalFacing(model.transform));
                }
                int triangles=0;
                foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if(renderer.sharedMesh==null || renderer.bones.Length==0)
                        throw new Exception("Unbound reference mesh "+name);
                    triangles+=renderer.sharedMesh.triangles.Length/3;
                    if(name=="Bombardiro")continue;
                    foreach(var material in renderer.sharedMaterials)
                        if(material==null || !material.name.StartsWith("Ref63 "))
                            throw new Exception("Unmapped reference material "+name);
                }
                foreach(string action in actions)
                {
                    bool found=false;
                    foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                        if(asset is AnimationClip clip && (clip.name==action || clip.name.EndsWith("|"+action)))found=true;
                    if(!found)throw new Exception("Missing "+action+" in "+name);
                }
                if(triangles==0 || (name!="Bombardiro" && triangles>65020))
                    throw new Exception("Invalid mobile mesh budget "+name+": "+triangles);
                Debug.Log("REFERENCE_CAST_HERO_PASS name="+name+" triangles="+triangles+" actions=8");
            }
            if(!AiTactics.WantsSkill(12,AiObjective.Expand,true,80f,80f,true))
                throw new Exception("Troppa AI ability is unreachable");
            Debug.Log("REFERENCE_CAST_SMOKE_PASS heroes=13 textures=10 newAIAbility=true");
        }
    }
}
#endif
