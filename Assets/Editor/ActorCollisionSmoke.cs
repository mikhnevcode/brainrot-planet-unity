#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    /// <summary>Validate physical pass-through independently of territory logic.</summary>
    public static class ActorCollisionSmoke
    {
        [MenuItem("Brainrot Planet/11. Verify body pass-through")]
        public static void Run()
        {
            GameObject[] heroes=new GameObject[4];
            Collider[] bodies=new Collider[4];
            GameObject terrain=null;
            try
            {
                for(int i=0;i<heroes.Length;i++)
                {
                    heroes[i]=new GameObject("Collision QA hero "+i);
                    var sphere=heroes[i].AddComponent<SphereCollider>();
                    sphere.radius=ActorCollisionRules.Radius;
                    bodies[i]=sphere;
                    if(sphere.isTrigger || sphere.radius>.50f)
                        throw new Exception("Oversized or trigger-only hero body");
                }

                terrain=new GameObject("Collision QA terrain");
                var ground=terrain.AddComponent<BoxCollider>();
                int pairCount=0;
                for(int i=0;i<bodies.Length;i++)
                    for(int j=i+1;j<bodies.Length;j++)
                    {
                        if(!ActorCollisionRules.IgnoreContacts(bodies[i],bodies[j]) ||
                           !Physics.GetIgnoreCollision(bodies[i],bodies[j]))
                            throw new Exception("Actor physical pass-through failed");
                        pairCount++;
                    }
                if(pairCount!=6 || ActorCollisionRules.PairCount(4)!=6)
                    throw new Exception("Expected all six actor pairs");
                for(int i=0;i<bodies.Length;i++)
                    if(Physics.GetIgnoreCollision(bodies[i],ground))
                        throw new Exception("Hero also ignores the terrain");

                // Player's collider is toggled during the orbital descent.
                // Explicitly refresh pair ignore rules upon landing.
                bodies[0].enabled=false;
                bodies[0].enabled=true;
                for(int i=1;i<bodies.Length;i++)
                    if(!ActorCollisionRules.IgnoreContacts(bodies[0],bodies[i]))
                        throw new Exception("Landing collision refresh failed");
                Debug.Log("ACTOR_COLLISION_SMOKE_PASS ignoredPairs=6"+
                    " terrainContact=true radius="+ActorCollisionRules.Radius.ToString("F2")+
                    " landingRefresh=true");
            }
            finally
            {
                for(int i=0;i<heroes.Length;i++)
                    if(heroes[i]!=null)UnityEngine.Object.DestroyImmediate(heroes[i]);
                if(terrain!=null)UnityEngine.Object.DestroyImmediate(terrain);
            }
        }
    }
}
#endif
