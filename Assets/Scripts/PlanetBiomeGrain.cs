using UnityEngine;
namespace BrainrotPlanet
{
    // Tiny seamless, mipmapped surface patterns. Cached for ten maps (~0.85 MiB)
    // and sampled by the existing triplanar shader, also beneath captured paint.
    public static class PlanetBiomeGrain
    {
        private static readonly Texture2D[] textures=new Texture2D[PlanetBiomeRules.Count];
        private static float Hash(int x,int y,int style)
        {uint a=unchecked((uint)(x*73856093)^((uint)y*19349663u)^((uint)style*83492791u));a^=a>>13;a*=1274126177u;return (a&65535)/65535f;}
        private static Vector2 Cell(float u,float v,int scale,int style)
        {
            int x=Mathf.FloorToInt(u*scale),y=Mathf.FloorToInt(v*scale);
            return new Vector2(u*scale-x-.5f+(Hash(x,y,style)-.5f)*.34f,
                v*scale-y-.5f+(Hash(y,x,style+13)-.5f)*.34f);
        }
        public static Texture2D Get(BattlePlanet kind)
        {
            int style=(int)kind;if(textures[style]!=null)return textures[style];
            const int size=128;var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                float grain=.50f+.065f*Mathf.Sin(u*Mathf.PI*74)*Mathf.Sin(v*Mathf.PI*62);
                Vector2 cell=Cell(u,v,8,style);float d=cell.magnitude;
                switch(kind)
                {
                    case BattlePlanet.Tropical:grain+=.15f*Mathf.Clamp01(1-d*3)-.08f*Mathf.Sin(u*Mathf.PI*22+v*Mathf.PI*18);break;
                    case BattlePlanet.Desert:grain+=.21f*Mathf.Sin(u*Mathf.PI*36+.8f*Mathf.Sin(v*Mathf.PI*8));break;
                    case BattlePlanet.Moon:
                        grain-=.20f*Mathf.Clamp01(1-d/.24f);
                        grain+=.23f*Mathf.Clamp01(1-Mathf.Abs(d-.27f)/.07f);break;
                    case BattlePlanet.Frost:
                        grain-=.25f*Mathf.Clamp01(1-Mathf.Abs(cell.x-cell.y*.55f)/.038f);
                        grain+=.14f*Mathf.Abs(Mathf.Sin(v*Mathf.PI*12));break;
                    case BattlePlanet.Volcanic:
                        grain+=.33f*Mathf.Clamp01(1-Mathf.Min(Mathf.Abs(cell.x),Mathf.Abs(cell.y))/.045f);
                        grain-=.11f*Mathf.Clamp01(1-d*2.5f);break;
                    case BattlePlanet.Oceanic:grain+=.18f*Mathf.Sin(v*Mathf.PI*28+.4f*Mathf.Sin(u*Mathf.PI*12));break;
                    case BattlePlanet.Crystal:
                        grain+=.16f*Mathf.Sin((u+v)*Mathf.PI*16)*Mathf.Sin((u-v)*Mathf.PI*16);break;
                    case BattlePlanet.Storm:grain-=.13f*Mathf.Abs(Mathf.Sin(u*Mathf.PI*30+v*Mathf.PI*42));break;
                    case BattlePlanet.Amber:grain+=.10f*Mathf.Sin(v*Mathf.PI*14)-.24f*Mathf.Clamp01(1-d/.13f);break;
                    case BattlePlanet.Candy:
                        grain+=.30f*Mathf.Clamp01(1-Mathf.Sqrt(cell.x*cell.x*3+cell.y*cell.y*.45f)/.14f);break;
                }
                byte shade=(byte)Mathf.RoundToInt(Mathf.Clamp(grain,.14f,.9f)*255);
                pixels[y*size+x]=new Color32(shade,shade,shade,255);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGB24,true){
                name=kind+" seamless surface detail",wrapMode=TextureWrapMode.Repeat,
                filterMode=FilterMode.Trilinear,anisoLevel=2};
            texture.SetPixels32(pixels);texture.Apply(true,true);textures[style]=texture;return texture;
        }
    }
}
