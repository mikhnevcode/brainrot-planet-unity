#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class PlanetScaleSmoke
    {
        [MenuItem("Brainrot Planet/12. Verify enlarged planet")]
        public static void Run()
        {
            float oldCircumference=2f*Mathf.PI*PlanetMath.PreviousRadius;
            float circumference=2f*Mathf.PI*PlanetMath.Radius;
            if(PlanetMath.Radius<PlanetMath.PreviousRadius*1.65f)
                throw new Exception("Planet enlargement is too small");
            if(Mathf.Abs(PlanetMath.TraversalScale-
                PlanetMath.PreviousRadius/PlanetMath.Radius)>0.0001f)
                throw new Exception("AI waypoint scale inconsistent");
            Vector3 pole=Vector3.up;
            Vector3 centre=PlanetSurface.Point(pole);
            if(Mathf.Abs(centre.magnitude-
                (PlanetMath.Radius+PlanetSurface.Elevation(pole)))>.002f)
                throw new Exception("Spherical surface does not match new radius");
            float boostedSpeed=6.5f*1.85f;
            float boostSeconds=4f;
            float oldShare=boostedSpeed*boostSeconds/oldCircumference;
            float newShare=boostedSpeed*boostSeconds/circumference;
            if(!(newShare<oldShare*.65f && newShare<.35f))
                throw new Exception("Boost travels too much of planet");
            // AI waypoint distances should be comparable in physical units.
            float oldRoute=.32f*PlanetMath.PreviousRadius;
            float newRoute=.32f*PlanetMath.TraversalScale*PlanetMath.Radius;
            if(Mathf.Abs(newRoute-oldRoute)>.001f)
                throw new Exception("AI path lengths changed unexpectedly");
            if(ActorCollisionRules.Radius>.50f)
                throw new Exception("Player capsule remains oversized");
            Debug.Log("PLANET_SCALE_SMOKE_PASS oldRadius="+
                PlanetMath.PreviousRadius+" radius="+PlanetMath.Radius+
                " circumference="+circumference.ToString("F2")+
                " boostShare="+newShare.ToString("F3")+
                " aiRoute="+newRoute.ToString("F2")+
                " collisionRadius="+ActorCollisionRules.Radius.ToString("F2"));
        }
    }
}
#endif