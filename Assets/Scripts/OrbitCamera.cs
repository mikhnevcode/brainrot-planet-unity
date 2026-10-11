using UnityEngine;

namespace BrainrotPlanet
{
    // Landing fly-in followed by a stable local third-person camera.
    // The camera follows the ground direction, not the player's bumps or jumps.
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Transform Target { get; private set; }
        public bool IsDescending => Target != null && descentTime < DescentDuration;

        private const float DescentDuration = 3.8f;
        private const float CameraHeight = 12.5f;
        private const float CameraBack = 4.5f;
        private const float LookAhead = 0.5f;
        private const float FollowSharpness = 4.5f;
        private const float NormalSharpness = 5.0f;

        private Camera view;
        private float descentTime = DescentDuration;
        private Vector3 smoothedNormal = Vector3.up;
        private Vector3 previousNormal = Vector3.up;
        private Vector3 travelForward = Vector3.forward;
        private Vector3 landingStart;
        private Vector3 landingFocus;
        private Vector3 positionVelocity;
        private Vector3 smoothedGround;

        private void Awake()
        {
            view = GetComponent<Camera>();
        }

        public void BeginDescent(Transform actor)
        {
            Target = actor;
            Vector3 surfaceNormal = actor.position.normalized;
            if(surfaceNormal.sqrMagnitude < .1f)
                surfaceNormal = Vector3.up;

            smoothedNormal = surfaceNormal;
            previousNormal = surfaceNormal;

            // One initial tangent direction. It is transported across the sphere
            // instead of being recalculated from world-up on every frame.
            travelForward = Vector3.ProjectOnPlane(Vector3.up, surfaceNormal);
            if (travelForward.sqrMagnitude < .06f)
                travelForward = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal);
            travelForward.Normalize();

            landingStart = surfaceNormal * (PlanetMath.Radius + 82f) - travelForward * 9f;
            landingFocus = surfaceNormal * (PlanetMath.Radius * .30f);
            descentTime = 0f;
            positionVelocity = Vector3.zero;
            smoothedGround = PlanetSurface.Point(surfaceNormal, .65f);
            transform.position = landingStart;
            transform.rotation = Quaternion.LookRotation(landingFocus - landingStart, surfaceNormal);
            if (view != null) view.fieldOfView = 50f;
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            if(view == null) view = GetComponent<Camera>();

            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (dt <= 0f) return;

            Vector3 rawNormal = Target.position.normalized;
            if (rawNormal.sqrMagnitude < .1f) return;

            // Rotate the heading by the shortest rotation between two normals:
            // no sudden roll or yaw when crossing a longitude or a pole.
            float normalLerp = 1f - Mathf.Exp(-NormalSharpness * dt);
            smoothedNormal = Vector3.Slerp(smoothedNormal, rawNormal, normalLerp).normalized;
            Quaternion transport = Quaternion.FromToRotation(previousNormal, smoothedNormal);
            travelForward = transport * travelForward;
            travelForward = Vector3.ProjectOnPlane(travelForward, smoothedNormal).normalized;
            if(travelForward.sqrMagnitude < .01f)
                travelForward = Vector3.ProjectOnPlane(Vector3.forward, smoothedNormal).normalized;
            previousNormal = smoothedNormal;

            // Camera anchor is the planet surface, not the avatar's actual height.
            // Jumping and the radial physics correction cannot bounce the camera.
            Vector3 desiredGround = PlanetSurface.Point(smoothedNormal, .65f);
            smoothedGround = Vector3.Lerp(smoothedGround, desiredGround, 1f - Mathf.Exp(-3.1f * dt));
            Vector3 ground = smoothedGround;
            Vector3 targetPosition = ground + smoothedNormal * CameraHeight - travelForward * CameraBack;
            Vector3 targetFocus = ground + smoothedNormal * .55f + travelForward * LookAhead;

            if (IsDescending)
            {
                descentTime += dt;
                float t = Mathf.Clamp01(descentTime / DescentDuration);
                float ease = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(landingStart, targetPosition, ease);
                Vector3 focus = Vector3.Lerp(landingFocus, targetFocus, ease);
                transform.rotation = Quaternion.LookRotation(focus - transform.position, smoothedNormal);
                view.fieldOfView = Mathf.Lerp(50f, 57f, ease);
                if (descentTime >= DescentDuration)
                    positionVelocity = Vector3.zero;
                return;
            }

            // Smoothing is applied after ground stabilisation; player turning
            // on the spot never makes the planet revolve on the screen.
            transform.position = Vector3.SmoothDamp(
                transform.position, targetPosition, ref positionVelocity, .25f, 100f, dt);
            Quaternion ideal = Quaternion.LookRotation(targetFocus - transform.position, smoothedNormal);
            float rotate = 1f - Mathf.Exp(-FollowSharpness * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, ideal, rotate);
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, 57f, rotate);
        }

        public void ClearTarget()
        {
            Target = null;
            descentTime = DescentDuration;
            positionVelocity = Vector3.zero;
        }
    }
}