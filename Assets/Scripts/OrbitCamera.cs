using UnityEngine;

namespace BrainrotPlanet
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Transform Target;
        private Camera view;
        private Vector3 baseAxis=Vector3.forward;
        private void Awake(){view=GetComponent<Camera>();}
        private void LateUpdate()
        {
            if(Target==null)return;
            Vector3 up=Target.position.normalized;
            Vector3 sideways=Vector3.Cross(up,Vector3.up).normalized;
            if(sideways.sqrMagnitude<.03f)sideways=Vector3.Cross(up,Vector3.forward).normalized;
            baseAxis=Vector3.Slerp(baseAxis,(up*.94f + sideways*.33f).normalized,Time.deltaTime*2.7f);
            // Entire curved world remains visible, while the player is near its foreground.
            Vector3 focus=Target.position*.25f;
            Vector3 desired=baseAxis*42f;
            transform.position=Vector3.Lerp(transform.position,desired,Time.deltaTime*3f);
            Vector3 upHint=Vector3.ProjectOnPlane(up,transform.forward).normalized;
            if(upHint.sqrMagnitude<.01f)upHint=Vector3.up;
            Quaternion rotation=Quaternion.LookRotation(focus-transform.position,upHint);
            transform.rotation=Quaternion.Slerp(transform.rotation,rotation,Time.deltaTime*5f);
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,52f,Time.deltaTime*3f);
        }
    }
}
