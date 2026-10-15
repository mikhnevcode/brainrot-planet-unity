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
        private float jumpAnimationDiagnosticUntil;
        private float jumpCooldown, bumpCooldown, damageCooldown;
        private PlanetGame game;
        private Vector3 lastDirection;
        private Vector3 lastDriveVelocity;
        private readonly PlanetZoneMotion biomeMotion=new PlanetZoneMotion();
        private bool isDropping;
        public bool IsDropping => isDropping;
        private float dropElapsed;
        private float nextDropDiagnostic;
        private Vector3 dropStart;
        private Vector3 dropLanding;
        private SphereCollider bodyCollider;
        public Collider BodyCollider => bodyCollider;
        private bool networkReplica;
        private Vector3 networkPosition;
        private Quaternion networkRotation;
        private float trailCutUntil;
        private bool pendingTrailRespawn;
        public bool TrailCutVisible => Time.unscaledTime<trailCutUntil;
        public void SetNetworkReplica()
        {
            networkReplica=true; isDropping=false;
            body.isKinematic=true; body.detectCollisions=false;
            networkPosition=transform.position; networkRotation=transform.rotation;
        }
        public void ReceiveNetworkPose(Vector3 position,Quaternion rotation,Vector3 wish,bool active,bool grounded,float speed,float slow)
        {
            networkPosition=position; networkRotation=rotation;
            if(game.LocalTeam!=Team)Wish=wish;
            Active=active; Grounded=grounded; SpeedSeconds=speed; SlowSeconds=slow;
            if(Time.unscaledTime>=trailCutUntil &&
               (transform.position-position).sqrMagnitude>16f)
                transform.SetPositionAndRotation(position,rotation);
        }
        public void ShowNetworkTrailCut()
        {
            // The server already respawned this actor. Hold its visual at the
            // cut for the same short beat, then reveal the authoritative pose.
            trailCutUntil=Time.unscaledTime+.65f;
            Wish=Vector3.zero;
        }
        public static bool ShouldBounceOnHill(float incline,float uphillApproach)
        {
            return incline>8f&&incline<38f&&uphillApproach>.14f;
        }

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
            if(!game.DedicatedMatchServer)AvatarArt.Make(transform,character,Colors.Team(team));
            visualAnimator=GetComponentInChildren<Animator>();
            if(visualAnimator!=null)
            {
                heroMotion=visualAnimator.gameObject.AddComponent<HeroMotion>();
                heroMotion.Configure(this,visualAnimator);
            }
        }
        // Authoritative insertion stays near the world origin; the flying
        // artwork uses a non-physical copy on the client until touchdown.
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

        private void UpdateDrop()
        {
            // FixedUpdate may skip catch-up steps after first-frame asset loading.
            // Use the same clock as the camera so the visual hand-off is stable.
            dropElapsed=Mathf.Max(dropElapsed,game.DeploymentSeconds);
            if(heroMotion!=null)heroMotion.SetDropTime(dropElapsed);
            float progress=Mathf.Clamp01(dropElapsed/OrbitCamera.DescentDuration);
            float fall=.35f*progress+.65f*DeploymentFlight.Ease(progress);
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
            if(visualAnimator!=null&&visualAnimator.runtimeAnimatorController!=null)
            {visualAnimator.Play("Idle",0,0);visualAnimator.Update(0);}
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
        public void EnterVictoryStage(Vector3 position,Vector3 faceDirection,float floorY)
        {
            // Sample the rest pose before positioning; a running pose is not a floor anchor.
            if(heroMotion!=null)heroMotion.ResetStageVisual();
            if(visualAnimator!=null)
            {
                visualAnimator.SetFloat("Speed",0);visualAnimator.SetBool("Grounded",true);
                visualAnimator.SetBool("Dropping",false);visualAnimator.ResetTrigger("Special");
                visualAnimator.Play("Idle",0,0);visualAnimator.Update(0);
            }
            // Use exported facing metadata, independent of FBX axis conversion.
            Vector3 flatFace=Vector3.ProjectOnPlane(faceDirection,Vector3.up).normalized;
            if(flatFace.sqrMagnitude<.01f)flatFace=Vector3.back;
            Quaternion stageRotation=AvatarArt.AlignFacing(AvatarArt.LocalFacing(transform),flatFace);
            foreach(Transform node in GetComponentsInChildren<Transform>(true))
                if(node.name=="ReferenceFacingOrigin")
                {
                    Vector3 localOrigin=transform.InverseTransformPoint(node.position);
                    position.y=floorY-(stageRotation*localOrigin).y;
                    Debug.Log("VICTORY_FOOTING character="+Character+" floorY="+floorY.ToString("F3"));
                    break;
                }
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
            // Foot markers in old FBX files are not always at the sole. A one-off
            // baked pose also handles aircraft wheels and the fish/shrimp rigs.
            float correction=floorY-AvatarArt.PoseBounds(transform).min.y;
            position+=Vector3.up*correction;
            transform.position=position;if(body!=null)body.position=position;
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
            body.AddForce(normal*(game.Arena?.JumpImpulse??6.2f),ForceMode.VelocityChange);
            jumpCooldown=.55f;
            if(Team==1)GameSound.Play(GameCue.Jump);
            if(Team==1)Debug.Log("PLAYER_JUMP_BEGIN character="+Character);
            if(Team==1 && Debug.isDebugBuild)
            {
                jumpAnimationDiagnosticUntil=Time.unscaledTime+1.8f;
                nextAnimationCheck=Time.unscaledTime+.15f;
            }
            game.Effects.Burst(transform.position,Colors.Team(Team),12,1.3f);
        }
        public void HitTrail(bool showBreak=false,Vector3 cutDirection=default(Vector3))
        {
            if(damageCooldown>0)return;
            damageCooldown=1.6f;
            game.Effects.Burst(transform.position,Colors.Team(Team),36,3f);
            if(game.DedicatedMatchServer)
                game.NetworkActorTrailBroken(this,cutDirection.sqrMagnitude>.01f?
                    cutDirection:transform.position.normalized);
            if(showBreak)
            {
                // Freeze only the victim, keeping the rest of the match live.
                pendingTrailRespawn=true;trailCutUntil=Time.unscaledTime+.65f;
                Active=false;Wish=Vector3.zero;
                body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
                body.isKinematic=true;body.detectCollisions=false;
                Debug.Log("TRAIL_CUT_WAIT team="+Team);
            }
            else Respawn();
        }
        public void Respawn()
        {
            pendingTrailRespawn=false;trailCutUntil=0f;
            if(!networkReplica){body.isKinematic=false;body.detectCollisions=true;}
            game.Territory.ResetTrail(Team);
            springLandingArmed=false;
            Wish=Vector3.zero;
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            lastDriveVelocity=Vector3.zero;
            biomeMotion.Reset();
            Vector3 home=PlanetSurface.Point(SpawnDirection,1.8f);
            Quaternion orientation=Quaternion.FromToRotation(Vector3.up,SpawnDirection);
            body.position=home;body.rotation=orientation;
            transform.SetPositionAndRotation(home,orientation);
            lastDirection=SpawnDirection;
            Grounded=false;
            jumpCooldown=.35f;
            Active=true;
            if(Team==1)game.OnPlayerRespawn();
            else game.OnBotRespawn(Team);
            var ai=GetComponent<PlanetAI>();
            if(ai!=null)ai.OnRespawn();
            Debug.Log("HERO_RESPAWN team="+Team+" homeOwned="+
                game.Territory.InOwn(Team,SpawnDirection));
        }
        private void FixedUpdate()
        {
            if(networkReplica)return;
            if(body==null||!Active)return;
            float dt=Time.fixedDeltaTime;
            if(isDropping)
            {
                UpdateDrop();
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
            body.AddForce(-n*(game!=null&&game.Arena!=null?game.Arena.Gravity:23f),ForceMode.Acceleration);
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
            speed*=biomeMotion.Tick(game!=null?game.Arena:null,n,walk,Grounded,dt);
            if(Grounded)
            {
                float incline=colliderContact?Vector3.Angle(ground.normal,n):0f;
                Vector3 uphill=-Vector3.ProjectOnPlane(ground.normal,n).normalized;
                // Ramps / bumps cause little jumps instead of glued-to-ground movement.
                float uphillApproach=Vector3.Dot(walk,uphill);
                if(PlanetSurface.IsHillArea(n)&&
                   ShouldBounceOnHill(incline,uphillApproach)&&
                   bumpCooldown<=0&&walk.sqrMagnitude>.2f)
                {
                    body.AddForce(n*2.85f,ForceMode.VelocityChange);
                    bumpCooldown=1.05f;
                    if(Debug.isDebugBuild)
                        Debug.Log("HILL_BOUNCE team="+Team+
                            " incline="+incline.ToString("F1")+
                            " approach="+uphillApproach.ToString("F2"));
                }
            }
            // Separate slope support from jump/gravity velocity. Do not add last
            // frame's uphill lift again, or climbing turns into a rocket launch.
            Vector3 radial=Vector3.Project(body.linearVelocity-lastDriveVelocity,n);
            Vector3 drive=walk*speed;
            if(Grounded&&colliderContact&&Vector3.Dot(ground.normal,n)>.12f)
            {
                Vector3 alongGround=Vector3.ProjectOnPlane(walk,ground.normal).normalized;
                if(Vector3.Dot(alongGround,n)>0f)drive=alongGround*speed;
            }
            lastDriveVelocity=drive;
            body.linearVelocity=drive+radial;
            Vector3 facing=walk.sqrMagnitude>.1f?walk:Vector3.ProjectOnPlane(transform.forward,n).normalized;
            if(facing.sqrMagnitude>.001f)
            {
                Quaternion target=Quaternion.LookRotation(facing,n);
                body.MoveRotation(Quaternion.Slerp(body.rotation,target,dt*10f));
            }
        }
        private void Update()
        {
            if(pendingTrailRespawn && Time.unscaledTime>=trailCutUntil &&
               game!=null && game.State==MatchState.Playing)
                Respawn();
            if(networkReplica && game!=null && game.State==MatchState.Playing)
            {
                if(Time.unscaledTime<trailCutUntil)return;
                if(trailCutUntil>0f)
                {
                    transform.SetPositionAndRotation(networkPosition,networkRotation);
                    trailCutUntil=0f;
                    Debug.Log("NETWORK_TRAIL_CUT_REVEAL team="+Team);
                }
                float blend=1f-Mathf.Exp(-18f*Time.unscaledDeltaTime);
                transform.SetPositionAndRotation(Vector3.Lerp(transform.position,networkPosition,blend),
                    Quaternion.Slerp(transform.rotation,networkRotation,blend));
            }
            if(body==null||!Active||game==null||game.State!=MatchState.Playing)return;
            if(visualAnimator!=null && visualAnimator.runtimeAnimatorController!=null)
            {
                visualAnimator.SetFloat("Speed",Wish.sqrMagnitude>.02f?1f:0f);
                visualAnimator.SetBool("Grounded",Grounded);
                if(Team==1 && Debug.isDebugBuild && Time.unscaledTime>=nextAnimationCheck)
                {
                    nextAnimationCheck=Time.unscaledTime+(Time.unscaledTime<jumpAnimationDiagnosticUntil ? .12f : 1.4f);
                    var clips=visualAnimator.GetCurrentAnimatorClipInfo(0);
                    string clipName=clips.Length>0 && clips[0].clip!=null ? clips[0].clip.name : "none";
                    Debug.Log("TRALALERO_ANIMATION clip="+clipName+
                        " movement="+Wish.magnitude.ToString("F2")+
                        " grounded="+Grounded);
                }
            }
            if(isDropping||networkReplica)return; // Only the authority paints trails.
            Vector3 dir=transform.position.normalized;
            if(Vector3.Angle(lastDirection,dir)>.06f)
            {
                game.Territory.TraceStep(Team,dir);lastDirection=dir;
            }
        }
    }
}
