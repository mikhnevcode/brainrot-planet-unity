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
        private HeroMotion heroMotion;
        private bool springLandingArmed;
        private float springEarliestLand;
        private float nextAnimationCheck;
        private float jumpCooldown, bumpCooldown, damageCooldown;
        private PlanetGame game;
        private Vector3 lastDirection;
        private bool isDropping;
        private float dropElapsed;
        private float nextDropDiagnostic;
        private Vector3 dropStart;
        private Vector3 dropLanding;
        private SphereCollider bodyCollider;
        public Collider BodyCollider => bodyCollider;

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
            bodyCollider=gameObject.AddComponent<SphereCollider>();
            bodyCollider.center=new Vector3(0,.05f,0);
            // Half-size art should not retain the original oversized contact ball.
            bodyCollider.radius=ActorCollisionRules.Radius;
            AvatarArt.Make(transform,character,Colors.Team(team));
            visualAnimator=GetComponentInChildren<Animator>();
            if(visualAnimator!=null)
            {
                heroMotion=visualAnimator.gameObject.AddComponent<HeroMotion>();
                heroMotion.Configure(this,visualAnimator);
            }
        }
        // Scripted insertion occurs only at the start of a match. Keep the
        // actor above the planet while the existing 2.9-second camera approach
        // occurs, then land with no leftover Rigidbody velocity.
        public void BeginDropFromOrbit()
        {
            if(body==null || isDropping)return;
            dropLanding=PlanetSurface.Point(SpawnDirection,.75f);
            dropStart=dropLanding + SpawnDirection*9.0f;
            dropElapsed=0f;
            nextDropDiagnostic=0f;
            isDropping=true;
            if(heroMotion!=null)heroMotion.StartDrop();
            if(visualAnimator!=null)visualAnimator.SetBool("Dropping",true);
            Grounded=false;
            Wish=Vector3.zero;
            body.linearVelocity=Vector3.zero;
            body.angularVelocity=Vector3.zero;
            body.isKinematic=true;
            if(bodyCollider!=null)bodyCollider.enabled=false;
            body.position=dropStart;
            transform.position=dropStart;
            Debug.Log("PLAYER_DROP_BEGIN altitude=9.00 duration="+
                      OrbitCamera.DescentDuration.ToString("F2")+
                      " character="+Character);
        }

        private void UpdateDrop(float dt)
        {
            dropElapsed+=dt;
            if(heroMotion!=null)heroMotion.SetDropTime(dropElapsed);
            // Delay visible fall until the approach has established the planet.
            // Then accelerate downward, leaving time to see the descent clearly.
            float progress=Mathf.Clamp01((dropElapsed-.65f)/
                (OrbitCamera.DescentDuration-.65f));
            float fall=progress*progress*(3f-2f*progress);
            body.MovePosition(Vector3.Lerp(dropStart,dropLanding,fall));
            if(dropElapsed>=nextDropDiagnostic)
            {
                nextDropDiagnostic+=.45f;
                Debug.Log("PLAYER_DROP_PROGRESS t="+dropElapsed.ToString("F2")+
                          " height="+(9f*(1f-fall)).ToString("F2"));
            }
            if(dropElapsed<OrbitCamera.DescentDuration)return;
            body.position=dropLanding;
            isDropping=false;
            if(visualAnimator!=null)visualAnimator.SetBool("Dropping",false);
            body.isKinematic=false;
            if(bodyCollider!=null)bodyCollider.enabled=true;
            // Unity resets ignored-pair state if one collider was disabled in flight.
            game.ApplyActorPassThrough("after_descent");
            body.linearVelocity=Vector3.zero;
            body.angularVelocity=Vector3.zero;
            Grounded=true;
            if(heroMotion!=null)heroMotion.Landed();
            jumpCooldown=.25f;
            lastDirection=dropLanding.normalized;
            game.Effects.Burst(dropLanding,Colors.Team(Team),36,2.1f);
            Debug.Log("PLAYER_DROP_LANDED altitude=0.00 character="+Character);
            if(Team==1)game.Announce(" ДЕСАНТ ЗАВЕРШЁН! НАЧИНАЙ ЗАХВАТ!");
        }

        public void Celebrate()
        {
            if(heroMotion!=null)heroMotion.Celebrate();
            if(visualAnimator!=null &&
               visualAnimator.runtimeAnimatorController!=null)
                visualAnimator.SetTrigger("Victory");
        }
        public void EnterVictoryStage(Vector3 position,Vector3 faceDirection)
        {
            // All authored Blender heroes use local +X as their visual front.
            // Build a world-up rotation whose right axis points at the camera.
            Vector3 flatFace=Vector3.ProjectOnPlane(faceDirection,Vector3.up).normalized;
            if(flatFace.sqrMagnitude<.01f)flatFace=Vector3.back;
            Quaternion stageRotation=Quaternion.LookRotation(
                Vector3.Cross(flatFace,Vector3.up),Vector3.up);
            Wish=Vector3.zero;
            Active=false;
            isDropping=false;
            Grounded=true;
            if(body!=null)
            {
                body.linearVelocity=Vector3.zero;
                body.angularVelocity=Vector3.zero;
                body.isKinematic=true;
                body.position=position;
                body.rotation=stageRotation;
            }
            transform.SetPositionAndRotation(position,stageRotation);
            if(bodyCollider!=null)bodyCollider.enabled=false;
            Debug.Log(string.Format(
                "HERO_VICTORY_STAGE team={0} position=({1:F2},{2:F2},{3:F2}) upDot={4:F3}",
                Team,position.x,position.y,position.z,
                Vector3.Dot(transform.up,Vector3.up)));
        }
        public void PlaySpecial()
        {
            if(heroMotion!=null)heroMotion.PerformSkill();
            if(visualAnimator!=null)visualAnimator.SetTrigger("Special");
        }
        public bool SpringJump()
        {
            if(!Active||!Grounded||springLandingArmed||body==null)return false;
            springLandingArmed=true;
            springEarliestLand=Time.time+.38f;
            Vector3 normal=transform.position.normalized;
            body.AddForce(normal*8.5f,ForceMode.VelocityChange);
            Grounded=false;
            if(heroMotion!=null)heroMotion.PerformSkill();
            Debug.Log("BONECA_SPRING_LAUNCH team="+Team);
            return true;
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
            if(Team==1)
            {
                game.HurtPlayer();
                if(game.State!=MatchState.Playing)return;
            }
            Respawn();
        }
        public void Respawn()
        {
            game.Territory.ResetTrail(Team);
            springLandingArmed=false;
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            transform.position=PlanetSurface.Point(SpawnDirection,1.8f);
            transform.rotation=Quaternion.FromToRotation(Vector3.up,SpawnDirection);
            lastDirection=SpawnDirection;
            Active=true;
            if(Team==1)game.OnPlayerRespawn();
            var ai=GetComponent<PlanetAI>();
            if(ai!=null)ai.OnRespawn();
        }
        private void FixedUpdate()
        {
            if(body==null||!Active)return;
            float dt=Time.fixedDeltaTime;
            if(isDropping)
            {
                UpdateDrop(dt);
                return;
            }
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
            if(springLandingArmed && Grounded && Time.time>=springEarliestLand)
            {
                springLandingArmed=false;
                int painted=game.Territory.ClaimDisk(Team,n,.115f);
                game.Effects.CaptureFlash(n,Colors.Team(Team),Mathf.Max(25,painted));
                game.Effects.Burst(transform.position,Colors.Team(Team),26,2.9f);
                if(heroMotion!=null)heroMotion.Celebrate();
                Debug.Log("BONECA_SPRING_LANDED team="+Team+" cells="+painted);
            }
            Vector3 walk=Vector3.ProjectOnPlane(Wish,n).normalized;
            float speed=6.5f*(SlowSeconds>0?.46f:1f)*(SpeedSeconds>0?1.85f:1f);
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
            if(isDropping)return; // Falling is not a player paint trail.
            Vector3 dir=transform.position.normalized;
            if(Vector3.Angle(lastDirection,dir)>.06f)
            {
                game.Territory.TraceStep(Team,dir);lastDirection=dir;
            }
        }
    }
}
