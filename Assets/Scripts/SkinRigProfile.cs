using UnityEngine;
namespace BrainrotPlanet
{
    // Stable character indices and imported rig names. A package can attach only
    // to this hero's existing skeleton, never to camera, gameplay or UI objects.
    public static class SkinRigProfile
    {
        public const int HeroCount=OriginalHeroCatalog.Count;
        public static readonly string[] Names=System.Array.ConvertAll(OriginalHeroCatalog.All,hero=>hero.Id);
        public static bool MatchesMaterial(int hero,string runtimeName,string sourceName)
            =>runtimeName==sourceName;
        public static Transform Visual(Transform avatar,int hero)
        {
            if(hero<0||hero>=HeroCount)return null;
            foreach(var tag in avatar.GetComponentsInChildren<OriginalRigTag>(true))if(tag.Hero==hero)return tag.transform;
            return null;
        }
        public static bool AllowsAnchor(int hero,string anchor)
        {
            if(hero<0||hero>=HeroCount||anchor==null)return false;
            if(anchor=="Body"||anchor=="Head"||anchor=="Tail"||anchor=="Arm_L"||anchor=="Arm_R"||anchor=="Leg_L"||anchor=="Leg_R")return true;
            if(anchor.StartsWith("Appendage_",System.StringComparison.Ordinal)&&int.TryParse(anchor.Substring(10),out int appendage)&&appendage>=0&&appendage<8)return true;
            return false;
        }
    }
}
