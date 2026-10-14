using UnityEngine;
namespace BrainrotPlanet
{
    // Stable character indices and imported rig names. A package can attach only
    // to this hero's existing skeleton, never to camera, gameplay or UI objects.
    public static class SkinRigProfile
    {
        public const int HeroCount=13;
        public static readonly string[] Names={"Tralalero","Bombardiro","Frigo","Bombombini","Ballerina","Assassino","Patapim","TungSahur","Lirili","Frulli","Boneca","Trippi","Troppa"};
        private static readonly string[] Prefixes={"Tralalero fully rigged", "Bombardiro crocodile airplane", "Blender Frigo", "Bombombini Gusini", "Ballerina Cappuccina", "Blender Assassino", "Blender Patapim", "Blender TungSahur", "Blender Lirili", "Blender Frulli", "Blender Boneca", "Blender Trippi", "Blender Troppa"};
        public static bool MatchesMaterial(int hero,string runtimeName,string sourceName)
            =>runtimeName==sourceName||(hero==1&&runtimeName=="Bombardiro / "+sourceName.ToLowerInvariant());
        public static Transform Visual(Transform avatar,int hero)
        {
            if(hero<0||hero>=HeroCount)return null;
            foreach(var node in avatar.GetComponentsInChildren<Transform>(true))if(node.name.StartsWith(Prefixes[hero],System.StringComparison.Ordinal))return node;
            return null;
        }
        public static bool AllowsAnchor(int hero,string anchor)
        {
            if(hero<0||hero>=HeroCount||anchor==null)return false;
            if(anchor=="Body")return true;
            if(hero==0)return anchor=="Tail"||anchor=="Fin_L"||anchor=="Fin_R"||anchor=="Thigh_L"||anchor=="Thigh_R"||anchor=="Thigh_Rear"||anchor=="Shin_L"||anchor=="Shin_R"||anchor=="Shin_Rear"||anchor=="Foot_L"||anchor=="Foot_R"||anchor=="Foot_Rear";
            if(hero==1)return anchor=="Jaw"||anchor=="Tail"||anchor=="Flap_L"||anchor=="Flap_R"||anchor=="Prop_L"||anchor=="Prop_R";
            if(hero==3)return anchor=="Neck"||anchor=="Beak"||anchor=="Wing_L"||anchor=="Wing_R"||anchor=="Tail"||anchor=="Foot_L"||anchor=="Foot_R"||anchor=="Engine_L"||anchor=="Engine_R";
            if(anchor=="Head"||anchor=="Arm_L"||anchor=="Arm_R"||anchor=="Leg_L"||anchor=="Leg_R")return true;
            return hero==4&&(anchor=="Skirt"||anchor=="Forearm_L"||anchor=="Forearm_R"||anchor=="Shin_L"||anchor=="Shin_R");
        }
    }
}
