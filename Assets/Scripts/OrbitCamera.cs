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
        private float victoryTime = -1f;
        private Vector3 victoryStartPosition;
        private Quaternion victoryStartRotation;
        private Vector3 victoryCameraPosition;
        private Quaternion victoryCameraRotation;
        private Vector3 victoryLookAt;
        private bool victoryDiagnosticsLogged;
        private const float VictoryFocusDuration = 1.35f;

        private void Awake()
        {
            view = GetComponent<Camera>();
        }

        public void BeginDescent(Transform actor)
        {
            Target = actor;
            victoryTime=-1f;
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

        // Smoothly turn the camera from the player toward the actual winner,
        // then remain close enough to see the victory dance on mobile.
        public void BeginVictoryFocus(Transform winner,Vector3 stageCenter)
        {
            if(winner==null)return;
            victoryStartPosition=transform.position;
            victoryStartRotation=transform.rotation;
            victoryTime=0f;
            descentTime=DescentDuration;
            Target=winner;
            Bounds bounds=AvatarArt.PoseBounds(winner);
            // Frame the real half-size artwork, not an assumed human height.
            // Keep room above/below for the fixed result panels on portrait screens.
            victoryLookAt=bounds.center+Vector3.up*(bounds.size.y*.05f);
            float aspect=view!=null?view.aspect:.5625f;
            float distance=Mathf.Max(2.65f,Mathf.Max(bounds.size.x/(.68f*aspect),bounds.size.y/.40f)/(.788f));
            victoryCameraPosition=victoryLookAt+new Vector3(0,distance*.24f,-distance);
            victoryCameraRotation=Quaternion.LookRotation(
                victoryLookAt-victoryCameraPosition,Vector3.up);
            victoryDiagnosticsLogged=false;
            Debug.Log("CAM_VICTORY_FOCUS_STARTED teamActor="+winner.name+
                " stableStage=true bounds="+bounds.size+" distance="+distance.ToString("F2"));
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            if (view == null) view = GetComponent<Camera>();

            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (dt <= 0f) return;

            // The result scene uses a fixed world-up frame. It deliberately
            // bypasses the spherical surface normal used during gameplay so a
            // winner on the far side of the planet can never flip the camera.
            if(victoryTime>=0f)
            {
                victoryTime+=dt;
                float t=Mathf.Clamp01(victoryTime/VictoryFocusDuration);
                float eased=t*t*(3f-2f*t);
                transform.position=Vector3.Lerp(
                    victoryStartPosition,victoryCameraPosition,eased);
                transform.rotation=Quaternion.Slerp(
                    victoryStartRotation,victoryCameraRotation,eased);
                view.fieldOfView=Mathf.Lerp(GameplayFov,43f,eased);
                if(t>=1f&&!victoryDiagnosticsLogged)
                {
                    victoryDiagnosticsLogged=true;
                    Vector3 facing=Vector3.ProjectOnPlane(
                        transform.position-Target.position,Vector3.up).normalized;
                    Vector3 viewport=view.WorldToViewportPoint(
                        Target.position+Vector3.up*1.2f);
                    Debug.Log(string.Format(
                        "CAM_VICTORY_STABLE upDot={0:F3} faceDot={1:F3} screen=({2:F3},{3:F3},{4:F2})",
                        Vector3.Dot(transform.up,Vector3.up),
                        Vector3.Dot(Target.TransformDirection(AvatarArt.LocalFacing(Target)),facing),
                        viewport.x,viewport.y,viewport.z));
                }
                return;
            }

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
            Vector3 direction=Target.position.normalized;
            Debug.Log(string.Format(
                "CAMQA stage={0} actorR={1:F2} camR={2:F2} actorScreen=({3:F3},{4:F3},{5:F2}) camToActor={6:F2} heading={7:F1} actorDir=({8:F3},{9:F3},{10:F3})",
                IsDescending ? "descent" : "ground",
                Target.position.magnitude, transform.position.magnitude,
                viewport.x, viewport.y, viewport.z,
                Vector3.Distance(transform.position, Target.position),
                Vector3.Angle(stableNormal, direction),
                direction.x,direction.y,direction.z));
        }

        public void ClearTarget()
        {
            Target = null;
            victoryTime = -1f;
            descentTime = DescentDuration;
        }
    }
}
