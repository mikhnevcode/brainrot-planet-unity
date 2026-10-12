using UnityEngine;

namespace BrainrotPlanet
{
    // The world follows the character without positional lag.
    // Only the surface orientation is smoothed so rapid turns do not rotate the view abruptly.
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Transform Target { get; private set; }
        public bool IsDescending => Target != null && descentTime < DescentDuration;

        public const float DescentDuration = 2.9f;
        private const float CameraHeight = 13.2f;
        private const float CameraBack = 4.1f;
        private const float FollowNormalSharpness = 8.0f;
        private const float GameplayFov = 51f;

        private Camera view;
        private Vector3 stableNormal = Vector3.up;
        private Vector3 lastNormal = Vector3.up;
        private Vector3 tangentUp = Vector3.forward;
        private Vector3 startPosition;
        private Vector3 startFocus;
        private float descentTime = DescentDuration;
        private float nextDiagnostics;

        private void Awake()
        {
            view = GetComponent<Camera>();
        }

        public void BeginDescent(Transform actor)
        {
            Target = actor;
            Vector3 normal = actor.position.normalized;
            if (normal.sqrMagnitude < 0.1f) normal = Vector3.up;
            stableNormal = normal;
            lastNormal = normal;

            tangentUp = Vector3.ProjectOnPlane(Vector3.up, normal);
            if (tangentUp.sqrMagnitude < .05f)
                tangentUp = Vector3.ProjectOnPlane(Vector3.forward, normal);
            tangentUp.Normalize();

            startPosition = normal * (PlanetMath.Radius + 82f) - tangentUp * 9f;
            startFocus = normal * (PlanetMath.Radius * .30f);
            descentTime = 0f;
            transform.position = startPosition;
            transform.rotation = Quaternion.LookRotation(startFocus - startPosition, tangentUp);
            if (view != null) view.fieldOfView = 50f;
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            if (view == null) view = GetComponent<Camera>();

            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (dt <= 0f) return;

            Vector3 normal = Target.position.normalized;
            if (normal.sqrMagnitude < 0.1f) return;

            stableNormal = Vector3.Slerp(stableNormal, normal,
                1f - Mathf.Exp(-FollowNormalSharpness * dt)).normalized;

            Quaternion surfaceTransport = Quaternion.FromToRotation(lastNormal, stableNormal);
            tangentUp = Vector3.ProjectOnPlane(surfaceTransport * tangentUp, stableNormal);
            if (tangentUp.sqrMagnitude < .02f)
                tangentUp = Vector3.ProjectOnPlane(Vector3.forward, stableNormal);
            tangentUp.Normalize();
            lastNormal = stableNormal;

            // Position is locked to the actual character, including jumps.
            // Both look-at and position reference the SAME target point,
            // so the hero stays at viewport centre (0.5, 0.5) without lag.
            // During deployment keep the landing spot in view, so the hero
            // visibly moves toward the surface instead of staying centred.
            Vector3 anchor = IsDescending
                ? PlanetSurface.Point(normal,.72f) + normal * .45f
                : Target.position + normal * .45f;
            Vector3 gameplayPosition =
                anchor + stableNormal * CameraHeight - tangentUp * CameraBack;
            Quaternion gameplayRotation =
                Quaternion.LookRotation(anchor - gameplayPosition, tangentUp);

            if (IsDescending)
            {
                descentTime += dt;
                float t = Mathf.Clamp01(descentTime / DescentDuration);
                float smooth = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(startPosition, gameplayPosition, smooth);
                Vector3 focus = Vector3.Lerp(startFocus, anchor, smooth);
                transform.rotation = Quaternion.LookRotation(
                    focus - transform.position, tangentUp);
                view.fieldOfView = Mathf.Lerp(50f, GameplayFov, smooth);
            }
            else
            {
                transform.position = gameplayPosition;
                transform.rotation = gameplayRotation;
                view.fieldOfView = GameplayFov;
            }
            LogDiagnostics();
        }

        private void LogDiagnostics()
        {
            if (!Debug.isDebugBuild || Time.unscaledTime < nextDiagnostics || Target == null)
                return;
            nextDiagnostics = Time.unscaledTime + 1.0f;
            Vector3 viewport = view.WorldToViewportPoint(
                Target.position + Target.position.normalized * .45f);
            Debug.Log(string.Format(
                "CAMQA stage={0} actorR={1:F2} camR={2:F2} actorScreen=({3:F3},{4:F3},{5:F2}) camToActor={6:F2} heading={7:F1}",
                IsDescending ? "descent" : "ground",
                Target.position.magnitude, transform.position.magnitude,
                viewport.x, viewport.y, viewport.z,
                Vector3.Distance(transform.position, Target.position),
                Vector3.Angle(stableNormal, Target.position.normalized)));
        }

        public void ClearTarget()
        {
            Target = null;
            descentTime = DescentDuration;
        }
    }
}