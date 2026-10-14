#if UNITY_EDITOR
using System;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class CapturedPaintStrokeSmoke
    {
        public static void Run()
        {
            var path=new[] {
                new Vector3(1,.08f,.02f).normalized,
                new Vector3(1,.13f,.11f).normalized,
                new Vector3(1,.18f,.20f).normalized,
                new Vector3(1,.22f,.28f).normalized
            };
            Mesh mesh=CapturedPaintStroke.BuildMesh(path,new Vector3(1,.24f,.32f).normalized);
            try
            {
                if(mesh==null||mesh.vertexCount<16||mesh.vertexCount%4!=0||
                   mesh.triangles.Length!=(mesh.vertexCount/4-1)*18)
                    throw new Exception("Captured paint bridge has invalid triangle strip");
                Color32[] colors=mesh.colors32;
                Vector3[] vertices=mesh.vertices;
                for(int i=0;i<mesh.vertexCount;i++)
                {
                    Vector3 v=vertices[i];
                    if(float.IsNaN(v.x)||float.IsInfinity(v.x)||
                       float.IsNaN(v.y)||float.IsInfinity(v.y)||
                       float.IsNaN(v.z)||float.IsInfinity(v.z)||
                       v.magnitude<PlanetSurface.Point(v.normalized).magnitude+.10f)
                        throw new Exception("Captured paint bridge does not follow relief above ground");
                    if(i%4==0||i%4==3)
                        if(colors[i].a!=0)throw new Exception("Brush outer edge is not feathered");
                }
                Debug.Log("CAPTURED_PAINT_STROKE_SMOKE_PASS vertices="+mesh.vertexCount+
                    " relief=true feather=true closure=true");
            }
            finally {if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);}
        }
    }
}
#endif
