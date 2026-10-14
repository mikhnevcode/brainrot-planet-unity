using System.Collections.Generic;
using UnityEngine;
namespace BrainrotPlanet
{
    // Captured before assigning the Animator controller. Late network downloads
    // must use this original bind space, not the character's current dance pose.
    public sealed class SkinRestBindings : MonoBehaviour
    {
        private readonly Dictionary<string,Matrix4x4> modelToBone=new Dictionary<string,Matrix4x4>();
        public void Capture(int hero)
        {
            foreach(var bone in GetComponentsInChildren<Transform>(true))
                if(SkinRigProfile.AllowsAnchor(hero,bone.name))modelToBone[bone.name]=bone.worldToLocalMatrix*transform.localToWorldMatrix;
        }
        public bool TryGet(string anchor,out Matrix4x4 matrix)=>modelToBone.TryGetValue(anchor,out matrix);
    }
}
