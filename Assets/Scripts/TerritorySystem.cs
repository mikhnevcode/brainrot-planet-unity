using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>Painted cells and unfinished paths on a fully wrapped spherical grid.</summary>
    public sealed class TerritorySystem
    {
        private readonly PlanetSurface surface;
        private readonly byte[] owner = new byte[PlanetMath.CellCount];
        private readonly int[] queue = new int[PlanetMath.CellCount];
        private readonly byte[] seen = new byte[PlanetMath.CellCount];
        private readonly float[] territoryArea = new float[5];
        public readonly HashSet<int>[] Trails = new HashSet<int>[5];
        // Visible centreline is not the same thing as the thick flood-fill wall.
        private readonly HashSet<int>[] trailCenters = new HashSet<int>[5];
        private readonly List<int>[] trailOrder = new List<int>[5];
        private readonly Vector3[] lastPosition = new Vector3[5];
        private readonly bool[] hasLast = new bool[5];
        private readonly Vector3[] core = new Vector3[5];
        private bool dirty = true;
        private float renderTimer;
        public event Action<int,int,Vector3> OnCapture;
        public event Action<int,Vector3> OnTrailDestroyed;

        public TerritorySystem(PlanetSurface surface)
        {
            this.surface=surface;
            for(int i=0;i<5;i++){Trails[i]=new HashSet<int>();trailCenters[i]=new HashSet<int>();trailOrder[i]=new List<int>();}
        }
        public void Seed(int team,Vector3 direction)
        {
            core[team]=direction.normalized;
            ClaimDisk(team,direction,.21f, false);
            hasLast[team]=false;
        }
        public byte OwnerAt(Vector3 direction)=>owner[PlanetMath.ToCell(direction)];
        private bool IsCore(int team,int index)
        {
            if(team<=0 || team>4) return false;
            return Vector3.Dot(core[team],PlanetMath.CellDirection(index))>.995f;
        }
        public float Percentage(int team)
        {
            float p=0,total=0;
            for(int i=0;i<PlanetMath.CellCount;i++)
            {
                float weight=PlanetMath.CellAreaWeight(i);
                total+=weight;if(owner[i]==team)p+=weight;
            }
            return p*100f/Mathf.Max(.0001f,total);
        }
        public bool InOwn(int team,Vector3 direction)=>OwnerAt(direction)==team;
        public bool HasTrail(int team)=>trailOrder[team].Count>0;
        public IReadOnlyList<int> Path(int team)=>trailOrder[team];

        public void TraceStep(int team,Vector3 direction)
        {
            direction.Normalize();
            if(hasLast[team])
            {
                Vector3 from=lastPosition[team];
                float angle=Vector3.Angle(from,direction);
                int steps=Mathf.Clamp(Mathf.CeilToInt(angle/.42f),1,20);
                for(int j=1;j<=steps;j++) TraceCell(team,Vector3.Slerp(from,direction,(float)j/steps));
            }
            else TraceCell(team,direction);
            lastPosition[team]=direction;hasLast[team]=true;
        }
        private void TraceCell(int team,Vector3 direction)
        {
            int id=PlanetMath.ToCell(direction);
            for(int other=1;other<=4;other++)
            {
                if(other==team)continue;
                if(Trails[other].Contains(id))
                {
                    ClearTrail(other);
                    OnTrailDestroyed?.Invoke(other,direction);
                }
            }
            if(owner[id]==team)
            {
                if(trailOrder[team].Count>=3)Capture(team,direction);
                else if(trailOrder[team].Count>0)ClearTrail(team);
                return;
            }
            if(trailCenters[team].Add(id))
            {
                // Render the true path. Keep a thicker and connected digital wall
                // so diagonal steps do not leak during the 4-neighbor flood fill.
                trailOrder[team].Add(id);
                int x=id%PlanetMath.Width,y=id/PlanetMath.Width;
                Trails[team].Add(id);
                Trails[team].Add(y*PlanetMath.Width+(x+1)%PlanetMath.Width);
                Trails[team].Add(y*PlanetMath.Width+(x+PlanetMath.Width-1)%PlanetMath.Width);
                if(y>0)Trails[team].Add(id-PlanetMath.Width);
                if(y<PlanetMath.Height-1)Trails[team].Add(id+PlanetMath.Width);
                dirty=true;
                if(trailOrder[team].Count>2000)ClearTrail(team);
            }
        }
        private void ClearTrail(int team)
        {
            Trails[team].Clear();trailCenters[team].Clear();trailOrder[team].Clear();dirty=true;
        }
        public void ResetTrail(int team){ClearTrail(team);hasLast[team]=false;}

        private void Capture(int team,Vector3 hit)
        {
            if(trailOrder[team].Count<3)return;
            // A closed sphere has no external border. The painted land and this
            // unfinished path divide it; take the smaller enclosed component.
            Array.Clear(seen,0,seen.Length);
            var components=new List<List<int>>();
            var weights=new List<float>();
            for(int i=0;i<seen.Length;i++)
            {
                if(seen[i]!=0||owner[i]==team||Trails[team].Contains(i))continue;
                var region=new List<int>(256);
                int head=0,tail=0;queue[tail++]=i;seen[i]=1;
                float area=0;
                while(head<tail)
                {
                    int k=queue[head++],x=k%PlanetMath.Width,y=k/PlanetMath.Width;
                    region.Add(k);area+=PlanetMath.CellAreaWeight(k);
                    Visit(y*PlanetMath.Width+(x+1)%PlanetMath.Width,team,ref tail);
                    Visit(y*PlanetMath.Width+(x+PlanetMath.Width-1)%PlanetMath.Width,team,ref tail);
                    if(y>0)Visit(k-PlanetMath.Width,team,ref tail);
                    if(y+1<PlanetMath.Height)Visit(k+PlanetMath.Width,team,ref tail);
                }
                components.Add(region);weights.Add(area);
            }
            int selected=-1;
            if(components.Count>=2)
            {
                float best=float.MaxValue;
                for(int i=0;i<components.Count;i++)if(weights[i]<best){selected=i;best=weights[i];}
            }
            // Even with high-speed or diagonal cell transitions, a visible closed
            // loop should create a filled area. Fall back to a local tangent-plane
            // polygon if the discrete 4-neighbor wall failed to separate the sphere.
            List<int> polygonCells=PolygonFallback(team);
            List<int> enclosedCells = polygonCells!=null ? polygonCells :
                (selected>=0 ? components[selected] : null);
            int changed=0, enclosed=0;
            Vector3 effectCenter=Vector3.zero;
            foreach(int i in Trails[team])
                if(owner[i]!=team)
                {owner[i]=(byte)team;changed++;}
            if(enclosedCells!=null)
            {
                foreach(int i in enclosedCells)
                {
                    if(owner[i]!=team)
                    {
                        owner[i]=(byte)team;
                        effectCenter+=PlanetMath.CellDirection(i)*PlanetMath.CellAreaWeight(i);
                        changed++;enclosed++;
                    }
                }
            }
            ClearTrail(team);
            dirty=true;
            // Apply the full closed area in the same frame as the end of the trail.
            if(surface!=null)
            {
                surface.PaintTexture(owner,Trails);
                dirty=false;renderTimer=.18f;
            }
            Debug.Log("TERRITORY_CAPTURE team="+team+" areaCells="+enclosed+
                " totalNewCells="+changed+" selection="+
                (polygonCells!=null?"polygon":selected>=0?"flood":"none"));
            if(changed>3)
                OnCapture?.Invoke(team,changed,
                    effectCenter.sqrMagnitude>.001f ? effectCenter.normalized : hit);
        }

        // Robust local approximation for relatively short planetary loops. Unlike
        // longitude/latitude polygons, this projection has no 0/360-degree seam.
        private List<int> PolygonFallback(int team)
        {
            List<int> path=trailOrder[team];
            if(path.Count<6)return null;
            Vector3 centre=Vector3.zero;
            foreach(int id in path)centre+=PlanetMath.CellDirection(id);
            if(centre.sqrMagnitude<.00001f)return null;
            centre.Normalize();
            Vector3 east=Vector3.Cross(centre,Vector3.up);
            if(east.sqrMagnitude<.001f)east=Vector3.Cross(centre,Vector3.forward);
            east.Normalize();
            Vector3 north=Vector3.Cross(centre,east).normalized;
            var polygon=new List<Vector2>(path.Count);
            float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
            int skip=Mathf.Max(1,Mathf.CeilToInt(path.Count/260f));
            for(int index=0;index<path.Count;index+=skip)
            {
                int id=path[index];
                Vector3 normal=PlanetMath.CellDirection(id);
                float depth=Vector3.Dot(normal,centre);
                if(depth<.35f)return null;
                Vector2 point=new Vector2(Vector3.Dot(normal,east)/depth,
                                           Vector3.Dot(normal,north)/depth);
                polygon.Add(point);
                minX=Mathf.Min(minX,point.x);maxX=Mathf.Max(maxX,point.x);
                minY=Mathf.Min(minY,point.y);maxY=Mathf.Max(maxY,point.y);
            }
            if((maxX-minX)*(maxY-minY)<.00004f)return null;
            var filled=new List<int>();
            for(int id=0;id<owner.Length;id++)
            {
                if(owner[id]==team||Trails[team].Contains(id))continue;
                Vector3 normal=PlanetMath.CellDirection(id);
                float depth=Vector3.Dot(normal,centre);
                if(depth<.35f)continue;
                float x=Vector3.Dot(normal,east)/depth;
                float y=Vector3.Dot(normal,north)/depth;
                if(x<minX||x>maxX||y<minY||y>maxY)continue;
                bool inside=false;
                for(int i=0,j=polygon.Count-1;i<polygon.Count;j=i++)
                {
                    Vector2 a=polygon[i],b=polygon[j];
                    if((a.y>y)!=(b.y>y) && x<(b.x-a.x)*(y-a.y)/(b.y-a.y)+a.x)
                        inside=!inside;
                }
                if(inside)filled.Add(id);
            }
            return filled.Count>0 ? filled : null;
        }
        private void Visit(int id,int team,ref int tail)
        {
            if(seen[id]!=0||owner[id]==team||Trails[team].Contains(id))return;
            seen[id]=1;queue[tail++]=id;
        }
        private bool ProtectedByOtherCore(int team,int id)
        {
            for(int other=1;other<=4;other++)if(other!=team&&IsCore(other,id))return true;
            return false;
        }
        public int ClaimDisk(int team,Vector3 center,float angularRadius,bool fireCapture=true)
        {
            center.Normalize();int count=0;
            float d=Mathf.Cos(angularRadius);
            for(int i=0;i<owner.Length;i++)
            {
                if(ProtectedByOtherCore(team,i))continue;
                if(Vector3.Dot(PlanetMath.CellDirection(i),center)<d)continue;
                if(owner[i]==team)continue;
                owner[i]=(byte)team;count++;
            }
            if(count>0)
            {
                dirty=true;
                if(fireCapture)OnCapture?.Invoke(team,count,center);
            }
            return count;
        }
        public void Update(float dt)
        {
            renderTimer-=dt;
            if(dirty && renderTimer<=0)
            {
                surface.PaintTexture(owner,Trails);
                renderTimer=.18f;
                dirty=false;
            }
        }
    }
}
