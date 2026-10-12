using UnityEngine;

namespace BrainrotPlanet
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlanetActor : MonoBehaviour
    {
        public int Team { get; private set; }
        public int Character { get; private set; }
        public Vector3 Wish { get; set; }
        public float SlowSeconds;
        public float SpeedSeconds;
        public Vector3 SpawnDirection { get; private set; }
        public bool Active { get; set; }
        public bool Grounded { get; private set; }
        private Rigidbody body;
        private Animator visualAnimator;
        private float nextAnimationCheck;
        private float jumpCooldown, bumpCooldown, damageCooldown;
        private PlanetGame game;
        private Vector3 lastDirection;

        public void Configure(PlanetGame director,int team,int character,Vector3 spawn)
        {
            game=director;Team=team;Character=character;SpawnDirection=spawn.normalized;
            transform.position=PlanetSurface.Point(spawn,1.55f);
            transform.rotation=Quaternion.FromToRotation(Vector3.up,SpawnDirection);
            lastDirection=SpawnDirection;
            body=GetComponent<Rigidbody>();
            body.useGravity=false;body.mass=1.2f;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.constraints=RigidbodyConstraints.FreezeRotation;
            SphereCollider col=gameObject.AddComponent<SphereCollider>();
            col.center=new Vector3(0,.05f,0);col.radius=.66f;
            AvatarArt.Make(transform,character,Colors.Team(team));
            visualAnimator=GetComponentInChildren<Animator>();
        }
        public void Celebrate()
        {
            if(Character==4 && visualAnimator!=null &&
               visualAnimator.runtimeAnimatorController!=null)
            {
                visualAnimator.SetTrigger("Victory");
                Debug.Log("BALLERINA_VICTORY_TRIGGER team="+Team);
            }
        }
        public void Jump()
        {
            if(!Active||!Grounded||jumpCooldown>0)return;
            Vector3 normal=transform.position.normalized;
            body.AddForce(normal*6.2f,ForceMode.VelocityChange);
            jumpCooldown=.55f;
            game.Effects.Burst(transform.position,Colors.Team(Team),12,1.3f);
        }
        public void HitTrail()
        {
            if(damageCooldown>0)return;
            damageCooldown=1.6f;
            game.Effects.Burst(transform.position,Colors.Team(Team),36,3f);
            if(Team==1)game.HurtPlayer();
            Respawn();
        }
        public void Respawn()
        {
            game.Territory.ResetTrail(Team);
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            transform.position=PlanetSurface.Point(SpawnDirection,1.8f);
            transform.rotation=Quaternion.FromToRotation(Vector3.up,SpawnDirection);
            lastDirection=SpawnDirection;
            Active=true;
        }
        private void FixedUpdate()
        {
            if(body==null||!Active)return;
            float dt=Time.fixedDeltaTime;
            jumpCooldown=Mathf.Max(0,jumpCooldown-dt);
            bumpCooldown=Mathf.Max(0,bumpCooldown-dt);
            damageCooldown=Mathf.Max(0,damageCooldown-dt);
            SlowSeconds=Mathf.Max(0,SlowSeconds-dt);
            SpeedSeconds=Mathf.Max(0,SpeedSeconds-dt);
            // Keep the body outside the deformed spherical ground even on mobile
            // devices where a concave MeshCollider contact can be missed.
            if(body.position.sqrMagnitude < 1f)
            {
                body.position=PlanetSurface.Point(SpawnDirection,.85f);
                body.linearVelocity=Vector3.zero;
            }
            Vector3 n=body.position.normalized;
            float radialGround=PlanetMath.Radius+PlanetSurface.Elevation(n)+.72f;
            float currentRadius=body.position.magnitude;
            bool radialContact=currentRadius <= radialGround+.10f;
            if(currentRadius<radialGround)
            {
                body.position=n*radialGround;
                Vector3 velocity=body.linearVelocity;
                float towardCenter=Vector3.Dot(velocity,n);
                if(towardCenter<0f)body.linearVelocity=velocity-n*towardCenter;
            }
            body.AddForce(-n*23f,ForceMode.Acceleration);
            RaycastHit ground;
            bool colliderContact=Physics.Raycast(body.position+n*.15f,-n,out ground,1.15f,1<<8,QueryTriggerInteraction.Ignore);
            Grounded=radialContact||colliderContact;
            Vector3 walk=Vector3.ProjectOnPlane(Wish,n).normalized;
            float speed=(Team==1?6.5f:5.4f)*(SlowSeconds>0?.46f:1f)*(SpeedSeconds>0?1.85f:1f);
            if(Grounded)
            {
                float incline=colliderContact?Vector3.Angle(ground.normal,n):0f;
                Vector3 uphill=-Vector3.ProjectOnPlane(ground.normal,n).normalized;
                if(incline>45f && Vector3.Dot(walk,uphill)>.02f)walk=Vector3.zero;
                // Ramps / bumps cause little jumps instead of glued-to-ground movement.
                if(incline>11f&&incline<34f&&bumpCooldown<=0&&walk.sqrMagnitude>.2f&&
                   Vector3.Dot(walk,uphill)>.20f)
                {
                    body.AddForce(n*2.35f,ForceMode.VelocityChange);
                    bumpCooldown=.95f;
                }
            }
            Vector3 radial=Vector3.Project(body.linearVelocity,n);
            body.linearVelocity=walk*speed+radial;
            Vector3 facing=walk.sqrMagnitude>.1f?walk:Vector3.ProjectOnPlane(transform.forward,n).normalized;
            if(facing.sqrMagnitude>.001f)
            {
                Quaternion target=Quaternion.LookRotation(facing,n);
                body.MoveRotation(Quaternion.Slerp(body.rotation,target,dt*10f));
            }
        }
        private void Update()
        {
            if(body==null||!Active||game==null||game.State!=MatchState.Playing)return;
            if(visualAnimator!=null && visualAnimator.runtimeAnimatorController!=null)
            {
                visualAnimator.SetFloat("Speed",Wish.sqrMagnitude>.02f?1f:0f);
                visualAnimator.SetBool("Grounded",Grounded);
                if(Team==1 && Debug.isDebugBuild && Time.unscaledTime>=nextAnimationCheck)
                {
                    nextAnimationCheck=Time.unscaledTime+1.4f;
                    var clips=visualAnimator.GetCurrentAnimatorClipInfo(0);
                    string clipName=clips.Length>0 && clips[0].clip!=null ? clips[0].clip.name : "none";
                    Debug.Log("TRALALERO_ANIMATION clip="+clipName+
                        " movement="+Wish.magnitude.ToString("F2")+
                        " grounded="+Grounded);
                }
            }
            Vector3 dir=transform.position.normalized;
            if(Vector3.Angle(lastDirection,dir)>.06f)
            {
                game.Territory.TraceStep(Team,dir);lastDirection=dir;
            }
        }
    }
}
