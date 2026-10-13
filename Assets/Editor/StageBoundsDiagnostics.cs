#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
namespace BrainrotPlanet.Editor
{
    public static class StageBoundsDiagnostics
    {
        public static void Run()
        {
            foreach(int hero in new[]{0,1,4})
            {
                var root=new GameObject("Bounds "+hero);AvatarArt.Make(root.transform,hero,Color.magenta);
                root.transform.position=new Vector3(0,38,-55);
                root.transform.rotation=AvatarArt.AlignFacing(AvatarArt.LocalFacing(root.transform),Vector3.back);
                var mesh=new Mesh();
                foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.BakeMesh(mesh,false);
                    Bounds noScale=mesh.bounds;renderer.BakeMesh(mesh,true);
                    Debug.Log("BOUNDS_DIAG hero="+hero+" name="+renderer.name+" scale="+renderer.transform.lossyScale+" shared="+renderer.sharedMesh.bounds+" bakeFalse="+noScale+" bakeTrue="+mesh.bounds+" live="+renderer.bounds);
                }
                Debug.Log("BOUNDS_TOTAL hero="+hero+" "+AvatarArt.PoseBounds(root.transform));
                Object.DestroyImmediate(mesh);Object.DestroyImmediate(root);
            }
        }
        public static void Poses()
        {
            string[] names={"Tralalero","Bombardiro","Frigo","Bombombini","Ballerina","Assassino","Patapim","TungSahur","Lirili","Frulli","Boneca","Trippi","Troppa"};
            for(int hero=0;hero<names.Length;hero++)
            {
                var root=new GameObject("Pose "+hero);AvatarArt.Make(root.transform,hero,Color.magenta);
                var animator=root.GetComponentInChildren<Animator>();
                var nodes=animator.GetComponentsInChildren<Transform>();
                var rotations=new Quaternion[nodes.Length];var positions=new Vector3[nodes.Length];
                for(int n=0;n<nodes.Length;n++){rotations[n]=nodes[n].localRotation;positions[n]=nodes[n].localPosition;}
                Bounds rest=AvatarArt.PoseBounds(root.transform);
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Characters/Victory064/"+names[hero]+".anim");
                clip.SampleAnimation(animator.gameObject,0);
                Debug.Log("POSE_COMPARE hero="+names[hero]+" rest="+rest+" clip0="+AvatarArt.PoseBounds(root.transform));
                for(int n=0;n<nodes.Length;n++)
                    if(Quaternion.Angle(rotations[n],nodes[n].localRotation)>10||Vector3.Distance(positions[n],nodes[n].localPosition)>.05f)
                        Debug.Log("POSE_DELTA hero="+names[hero]+" node="+AnimationUtility.CalculateTransformPath(nodes[n],animator.transform)+" restRot="+rotations[n].eulerAngles+" clipRot="+nodes[n].localEulerAngles+" restPos="+positions[n]+" clipPos="+nodes[n].localPosition);
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
