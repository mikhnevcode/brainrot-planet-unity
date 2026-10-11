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
            for(int i=0;i<5;i++){Trails[i]=new HashSet<int>();trailOrder[i]=new List<int>();}
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
            if(Trails[team].Add(id))
            {
                // Render only centerline, but use a thicker connected digital wall
                // so diagonal steps do not leak during the 4-neighbor flood fill.
                trailOrder[team].Add(id);
                int x=id%PlanetMath.Width,y=id/PlanetMath.Width;
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
            Trails[team].Clear();trailOrder[team].Clear();dirty=true;
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
            int changed=0;
            foreach(int i in Trails[team])if(!ProtectedByOtherCore(team,i)&&owner[i]!=team){owner[i]=(byte)team;changed++;}
            if(selected>=0)
            {
                foreach(int i in components[selected])
                {
                    if(ProtectedByOtherCore(team,i))continue;
                    if(owner[i]!=team){owner[i]=(byte)team;changed++;}
                }
            }
            ClearTrail(team);dirty=true;
            if(changed>3) OnCapture?.Invoke(team,changed,hit);
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
