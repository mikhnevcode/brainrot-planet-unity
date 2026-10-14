using System.Collections.Generic;
using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>
    /// A short-lived, terrain-following brush stroke bridges the precise
    /// unfinished trail to the lower-resolution territory texture on capture.
    /// It is visual only; ownership and collision stay on the original grid.
    /// </summary>
    public sealed class CapturedPaintStroke : MonoBehaviour
    {
        private const float HoldSeconds=1.1f;
        private const float Lifetime=4.1f;
        private const float Height=.16f;
        private static readonly float[] Offsets={-.58f,-.30f,.30f,.58f};
        private static readonly byte[] Alphas={0,235,235,0};
        private Mesh strokeMesh;
        private Material strokeMaterial;
        private Color teamColor;
        private float age;

        public static CapturedPaintStroke Create(Transform parent,int team,
            IReadOnlyList<Vector3> path,Vector3 closure)
        {
            Mesh mesh=BuildMesh(path,closure);
            if(mesh==null)return null;
            Shader shader=Resources.Load<Shader>("PlanetPaintStroke");
            if(shader==null){Destroy(mesh);return null;}
            var go=new GameObject("Captured team "+team+" paint bridge");
            go.transform.SetParent(parent,false);
            var component=go.AddComponent<CapturedPaintStroke>();
            component.strokeMesh=mesh;
            component.teamColor=Colors.Team(team);
            component.strokeMaterial=new Material(shader){name="Captured paint brush"};
            component.strokeMaterial.color=component.teamColor;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=component.strokeMaterial;
            return component;
        }

        public static Mesh BuildMesh(IReadOnlyList<Vector3> path,Vector3 closure)
        {
            if(path==null||path.Count<2)return null;
            var waypoints=new List<Vector3>(Mathf.Min(path.Count,352));
            int stride=Mathf.Max(1,(path.Count-1)/350);
            for(int i=0;i<path.Count-1;i+=stride)waypoints.Add(path[i].normalized);
            waypoints.Add(path[path.Count-1].normalized);
            if(waypoints.Count<2)return null;
            Vector3 first=waypoints[0],second=waypoints[1];
            Vector3 before=Extend(first,first-second,.022f);
            var centers=new List<Vector3>(waypoints.Count*2+3){before};
            for(int i=0;i<waypoints.Count-1;i++)
            {
                Vector3 a=waypoints[i],b=waypoints[i+1];
                int steps=Mathf.Clamp(Mathf.CeilToInt(Vector3.Angle(a,b)*Mathf.Deg2Rad/.012f),1,8);
                for(int j=0;j<steps;j++)centers.Add(Vector3.Slerp(a,b,(float)j/steps).normalized);
            }
            Vector3 last=waypoints[waypoints.Count-1];
            centers.Add(last);
            if(closure.sqrMagnitude>.5f && Vector3.Angle(last,closure)<8f)
            {
                Vector3 hit=closure.normalized;
                centers.Add(hit);
                centers.Add(Extend(hit,hit-last,.022f));
            }
            else centers.Add(Extend(last,last-waypoints[waypoints.Count-2],.022f));

            var vertices=new List<Vector3>(centers.Count*4);
            var colors=new List<Color32>(centers.Count*4);
            var triangles=new List<int>((centers.Count-1)*18);
            for(int i=0;i<centers.Count;i++)
            {
                Vector3 n=centers[i];
                Vector3 along=Vector3.ProjectOnPlane(
                    centers[Mathf.Min(i+1,centers.Count-1)]-
                    centers[Mathf.Max(i-1,0)],n);
                if(along.sqrMagnitude<.000001f)along=Vector3.Cross(n,Vector3.up);
                if(along.sqrMagnitude<.000001f)along=Vector3.Cross(n,Vector3.right);
                Vector3 side=Vector3.Cross(n,along.normalized).normalized;
                byte tip=(byte)(i==0||i==centers.Count-1?0:255);
                for(int lane=0;lane<4;lane++)
                {
                    Vector3 direction=(n+side*(Offsets[lane]/PlanetMath.Radius)).normalized;
                    vertices.Add(PlanetSurface.Point(direction,Height));
                    colors.Add(new Color32(255,255,255,(byte)(Alphas[lane]*tip/255)));
                }
                if(i==0)continue;
                int prev=(i-1)*4,next=i*4;
                for(int lane=0;lane<3;lane++)
                {
                    int a=prev+lane,b=prev+lane+1,c=next+lane,d=next+lane+1;
                    triangles.Add(a);triangles.Add(c);triangles.Add(b);
                    triangles.Add(b);triangles.Add(c);triangles.Add(d);
                }
            }
            var mesh=new Mesh {name="Terrain-following captured paint stroke"};
            mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 Extend(Vector3 at,Vector3 tangent,float radians)
        {
            Vector3 along=Vector3.ProjectOnPlane(tangent,at);
            return along.sqrMagnitude<.000001f?at:(at+along.normalized*radians).normalized;
        }

        private void Update()
        {
            age+=Time.deltaTime;
            if(age>=Lifetime){Destroy(gameObject);return;}
            if(age>HoldSeconds && strokeMaterial!=null)
            {
                float alpha=1f-(age-HoldSeconds)/(Lifetime-HoldSeconds);
                Color c=teamColor;c.a=Mathf.Clamp01(alpha);
                strokeMaterial.color=c;
            }
        }

        private void OnDestroy()
        {
            if(strokeMesh!=null)Destroy(strokeMesh);
            if(strokeMaterial!=null)Destroy(strokeMaterial);
        }
    }
}
