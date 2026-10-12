using System.Collections.Generic;
using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>
    /// Heroes cannot physically push or stop one another. TerritorySystem
    /// continues to handle exposed-trail intersections independently.
    /// Terrain, obstacles, bombs and pickups keep their own collision rules.
    /// </summary>
    public static class ActorCollisionRules
    {
        public const float Radius = .46f;

        public static bool IgnoreContacts(Collider a, Collider b)
        {
            if(a==null || b==null || !a.enabled || !b.enabled)return false;
            Physics.IgnoreCollision(a,b,true);
            return Physics.GetIgnoreCollision(a,b);
        }

        public static int Apply(IReadOnlyList<PlanetActor> actors)
        {
            if(actors==null)return 0;
            int count=0;
            for(int i=0;i<actors.Count;i++)
            {
                Collider a=actors[i]!=null?actors[i].BodyCollider:null;
                if(a==null || !a.enabled)continue;
                for(int j=i+1;j<actors.Count;j++)
                {
                    Collider b=actors[j]!=null?actors[j].BodyCollider:null;
                    if(b==null || !b.enabled)continue;
                    if(IgnoreContacts(a,b))count++;
                }
            }
            return count;
        }

        public static int CountIgnoredPairs(IReadOnlyList<PlanetActor> actors)
        {
            if(actors==null)return 0;
            int count=0;
            for(int i=0;i<actors.Count;i++)
            {
                Collider a=actors[i]!=null?actors[i].BodyCollider:null;
                if(a==null||!a.enabled)continue;
                for(int j=i+1;j<actors.Count;j++)
                {
                    Collider b=actors[j]!=null?actors[j].BodyCollider:null;
                    if(b!=null&&b.enabled&&Physics.GetIgnoreCollision(a,b))count++;
                }
            }
            return count;
        }

        public static int PairCount(int actorCount) =>
            actorCount*(actorCount-1)/2;
    }
}
