using System;
using UnityEngine;
namespace BrainrotPlanet
{
    public static class DetailedHeroMaterials
    {
        public static Material Make(Material source)
        {
            string[] tokens=source.name.Split(' ');
            if(tokens.Length<4||!ColorUtility.TryParseHtmlString(tokens[2],out var color))
                throw new InvalidOperationException("Detailed hero material: "+source.name);
            var shader=Resources.Load<Shader>("DetailedHeroSurface");
            if(shader==null)throw new InvalidOperationException("Detailed hero shader missing");
            var material=new Material(shader){name=source.name,color=color};
            string role=tokens[1],prefix=role=="main"?"Body":role=="accent"?"Roller":null;
            material.SetFloat("_Glossiness",role=="eye"||role=="iris"?.70f:role=="nose"?.56f:role=="accent"?.22f:.35f);
            if(prefix!=null)
            {
                const string folder="OriginalCastDetail/RolloBulk_v04/";
                var albedo=Resources.Load<Texture2D>(folder+prefix+"_Albedo");
                var normal=Resources.Load<Texture2D>(folder+prefix+"_Normal");
                if(albedo==null||normal==null)throw new InvalidOperationException("Detailed hero maps missing: "+prefix);
                material.SetTexture("_MainTex",albedo);material.SetTexture("_BumpMap",normal);
                material.SetColor("_BaseTint",color);
                if(prefix=="Body")
                {
                    var mask=Resources.Load<Texture2D>(folder+"Body_TintMask");
                    if(mask==null)throw new InvalidOperationException("Detailed hero palette mask missing");
                    material.SetTexture("_TintMask",mask);
                }
            }
            return material;
        }
    }
}
