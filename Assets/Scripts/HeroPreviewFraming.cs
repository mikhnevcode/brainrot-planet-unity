using UnityEngine;
namespace BrainrotPlanet
{
    // Shared by the menu and its framing checks: fit all eight corners, not just height.
    public static class HeroPreviewFraming
    {
        public static void SampleIdle(Transform root)
        {
            foreach(var animator in root.GetComponentsInChildren<Animator>())
            {
                if(animator.runtimeAnimatorController!=null)
                {
                    animator.SetFloat("Speed",0);animator.SetBool("Grounded",true);
                    animator.SetBool("Dropping",false);animator.Play("Idle",0,0);animator.Update(0);
                }
                // Only the portrait freezes; gameplay animators are untouched. Shop
                // dances still animate the bones through CosmeticVictoryMotion.
                animator.enabled=false;
            }
        }
        public static Quaternion ViewRotation(float yaw,float pitch)
            =>Quaternion.Euler(Mathf.Clamp(pitch,2f,18f),Mathf.Clamp(yaw,-80f,80f),0);
        // GUI deltas use down-positive Y. The camera orbit must follow the
        // pointer so the displayed hero turns/tilts in the dragged direction.
        public static Vector2 DragAngles(Vector2 angles,Vector2 guiDelta)
            =>new Vector2(Mathf.Clamp(angles.x+guiDelta.x*.55f,-80f,80f),
                          Mathf.Clamp(angles.y+guiDelta.y*.12f,2f,18f));
        public static void Place(Camera camera,Bounds bounds,float yaw,float pitch,float zoom)
        {
            Quaternion rotation=ViewRotation(yaw,pitch);
            Quaternion inverse=Quaternion.Inverse(rotation);
            float tanY=Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);
            float tanX=tanY*camera.aspect;
            float distance=.1f;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                Vector3 p=inverse*Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                distance=Mathf.Max(distance,Mathf.Max(Mathf.Abs(p.x)/tanX,Mathf.Abs(p.y)/tanY)*1.14f-p.z);
                distance=Mathf.Max(distance,camera.nearClipPlane*2-p.z);
            }
            distance*=Mathf.Clamp(zoom,1f,1.4f);
            camera.transform.SetPositionAndRotation(bounds.center-rotation*Vector3.forward*distance,rotation);
            camera.farClipPlane=Mathf.Max(30f,distance+bounds.size.magnitude+1f);
        }
    }
}
