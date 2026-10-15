#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class PlanetPolePaintSmoke
    {
        public static void Run()
        {
            int north=(PlanetMath.Height-1)*PlanetMath.Width,south=0;
            foreach(float longitude in new[]{-170f,-80f,0f,75f,179f})
            {
                Vector3 aroundNorth=Quaternion.AngleAxis(longitude,Vector3.up)*new Vector3(.0001f,1,0).normalized;
                Vector3 aroundSouth=Quaternion.AngleAxis(longitude,Vector3.up)*new Vector3(.0001f,-1,0).normalized;
                if(PlanetMath.ToCell(aroundNorth)!=north||PlanetMath.ToCell(aroundSouth)!=south)
                    throw new Exception("Polar coordinates did not collapse to one logical cap");
            }
            for(int x=1;x<PlanetMath.Width;x++)
            {
                if(PlanetMath.CellAreaWeight(north+x)!=0||PlanetMath.CellAreaWeight(south+x)!=0)
                    throw new Exception("Duplicate polar cell still contributes territory area");
                if(PlanetMath.CellDirection(north+x)!=Vector3.up||PlanetMath.CellDirection(south+x)!=Vector3.down)
                    throw new Exception("Polar duplicates disagree on world direction");
            }

            var holder=new GameObject("Polar paint topology smoke");
            try
            {
                var surface=holder.AddComponent<PlanetSurface>();surface.Build();
                var territory=new TerritorySystem(surface);
                int northCells=territory.ClaimDisk(1,Vector3.up,.055f,false);
                int southCells=territory.ClaimDisk(2,Vector3.down,.055f,false);
                territory.Update(1);
                if(northCells<1||southCells<1)throw new Exception("Polar cap was not claimable");
                var owners=new byte[PlanetMath.CellCount];territory.CopyOwners(owners);
                for(int x=0;x<PlanetMath.Width;x++)
                    if(owners[north+x]!=1||owners[south+x]!=2)
                        throw new Exception("Polar owner was not mirrored across the texture row");

                Color32[] basePixels=surface.MapTexture.GetPixels32();
                AssertUniformRow(basePixels,surface.MapTexture.width,surface.MapTexture.height-1,"north");
                AssertUniformRow(basePixels,surface.MapTexture.width,0,"south");

                // The ring beside a pole may legitimately contain several
                // teams. It must not split the single physical pole into UV
                // wedges after contour smoothing.
                var mixedRing=new byte[PlanetMath.CellCount];
                mixedRing[north]=1;mixedRing[south]=2;
                for(int x=0;x<PlanetMath.Width;x++)
                {
                    mixedRing[(PlanetMath.Height-2)*PlanetMath.Width+x]=(byte)(x<PlanetMath.Width/2?3:4);
                    mixedRing[PlanetMath.Width+x]=(byte)(x<PlanetMath.Width/2?4:3);
                }
                surface.PaintTexture(mixedRing,null);
                Color32[] mixedPixels=surface.MapTexture.GetPixels32();
                AssertUniformRow(mixedPixels,surface.MapTexture.width,surface.MapTexture.height-1,"mixed north");
                AssertUniformRow(mixedPixels,surface.MapTexture.width,0,"mixed south");
                surface.PaintTexture(owners,null);

                var northPath=new List<Vector3>();var southPath=new List<Vector3>();
                for(float degrees=88;degrees<=92.01f;degrees+=.25f)
                {
                    float radians=degrees*Mathf.Deg2Rad;
                    northPath.Add(new Vector3(Mathf.Cos(radians),Mathf.Sin(radians),0));
                    radians=-degrees*Mathf.Deg2Rad;
                    southPath.Add(new Vector3(Mathf.Cos(radians),Mathf.Sin(radians),0));
                }
                // AddCapturedStroke deliberately skips visual VFX while Unity is
                // in batch mode. Invoke only the texture baker so this build gate
                // still exercises the polar rasterization used on devices.
                MethodInfo bake=typeof(PlanetSurface).GetMethod("BakeCapturedStroke",
                    BindingFlags.Instance|BindingFlags.NonPublic);
                if(bake==null)throw new Exception("Captured stroke baker is missing");
                bake.Invoke(surface,new object[]{1,northPath,Vector3.zero});
                bake.Invoke(surface,new object[]{2,southPath,Vector3.zero});
                surface.PaintTexture(owners,null);
                var coverage=(byte[])typeof(PlanetSurface).GetField("strokeCoverage",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(surface);
                for(int x=0;x<surface.MapTexture.width;x++)
                    if(coverage[(surface.MapTexture.height-1)*surface.MapTexture.width+x]==0||coverage[x]==0)
                        throw new Exception("Captured stroke became a spoke at a pole");

                var trailOnly=new TerritorySystem(null);
                foreach(Vector3 direction in northPath)trailOnly.TraceStep(1,direction);
                if(!trailOnly.Trails[1].Contains(north)||!trailOnly.HasTrail(1))
                    throw new Exception("Geodesic trail disconnected at north pole");
                foreach(int cell in trailOnly.Trails[1])
                    if(!PlanetMath.IsLogicalCell(cell))throw new Exception("Trail retained a duplicate polar cell");

                Debug.Log("PLANET_POLE_PAINT_SMOKE_PASS caps=2 mirrored=true geodesicTrail=true strokeSpokes=0");
            }
            finally{UnityEngine.Object.DestroyImmediate(holder);}
        }
        private static void AssertUniformRow(Color32[] pixels,int width,int y,string label)
        {
            Color32 expected=pixels[y*width];
            for(int x=1;x<width;x++)
            {
                Color32 actual=pixels[y*width+x];
                if(Math.Abs(actual.r-expected.r)>2||Math.Abs(actual.g-expected.g)>2||Math.Abs(actual.b-expected.b)>2)
                    throw new Exception(label+" pole contains painted sectors");
            }
        }
    }
}
#endif
