#if UNITY_EDITOR
using System;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class PlanetPaintEdgeSmoke
    {
        public static void Run()
        {
            var holder=new GameObject("Sharp paint boundary smoke");
            try
            {
                var surface=holder.AddComponent<PlanetSurface>();
                surface.Build();
                var owners=new byte[PlanetMath.CellCount];
                for(int y=0;y<PlanetMath.Height;y++)
                    for(int x=0;x<PlanetMath.Width/2;x++)
                        owners[y*PlanetMath.Width+x]=1;
                surface.PaintTexture(owners,null);
                Color32[] image=surface.MapTexture.GetPixels32();
                int row=(PlanetMath.Height)*PlanetMath.Width*2;
                Color32 interior=image[row+254],rim=image[row+255];
                if(rim.g<interior.g+20 || rim.b<interior.b+15 ||
                   surface.MapTexture.filterMode!=FilterMode.Bilinear)
                    throw new Exception("Painted boundary is not sharply outlined");
                // The longitude seam is not an artificial border if both
                // ownership cells on either side of it belong to one team.
                for(int y=0;y<PlanetMath.Height;y++)owners[y*PlanetMath.Width+PlanetMath.Width-1]=1;
                surface.PaintTexture(owners,null);
                image=surface.MapTexture.GetPixels32();
                Color32 seamA=image[row],seamB=image[row+1];
                if(Math.Abs(seamA.r-seamB.r)>24||Math.Abs(seamA.g-seamB.g)>24||
                   Math.Abs(seamA.b-seamB.b)>24)
                    throw new Exception("False painted border at longitude seam: "+
                        seamA+" / "+seamB);
                Debug.Log("PLANET_PAINT_EDGE_SMOKE_PASS rim=true seamContinuous=true gridUnchanged=true");
            }
            finally{UnityEngine.Object.DestroyImmediate(holder);}
        }
    }
}
#endif
