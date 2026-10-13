using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>
    /// Four-state territorial opponent. It expands by making real closed paths,
    /// returns to protect its exposed trail, intercepts enemy trails and defends
    /// its home area. All decisions are throttled; Rigidbody movement remains
    /// in PlanetActor, identical to the human player's physics.
    /// </summary>
    [RequireComponent(typeof(PlanetActor))]
    public sealed class PlanetAI : MonoBehaviour
    {
        private PlanetActor actor;
        private PlanetGame game;
        private Vector3 home, goal, firstTurn, secondTurn;
        private Vector3 lastPosition;
        private AiObjective objective=AiObjective.Expand;
        private float evaluateAt, nextAbility, legDeadline, nextLog, objectiveStartedAt;
        private int leg;
        private int lastTargetTeam;
        private AiDifficultyProfile difficulty;
        private float nextAggressionRoll;
        private bool pursuePermission;
        private float stuckSeconds,escapeUntil,nextEscapeAt;
        private Vector3 escapeHeading;

        public static bool GoalReached(Vector3 normal,Vector3 destination,float delta)
        {
            float arrival=Mathf.Max(.45f,12.1f*Mathf.Min(delta,.05f)*1.2f);
            return Vector3.Angle(normal,destination)*Mathf.Deg2Rad*PlanetMath.Radius<=arrival;
        }
        public static Vector3 StableHeading(Vector3 previous,Vector3 desired,Vector3 normal,float delta)
        {
            desired=Vector3.ProjectOnPlane(desired,normal).normalized;
            previous=Vector3.ProjectOnPlane(previous,normal).normalized;
            if(desired.sqrMagnitude<.01f||previous.sqrMagnitude<.01f)return desired;
            float angle=Vector3.SignedAngle(previous,desired,normal);
            return Quaternion.AngleAxis(Mathf.Clamp(angle,-240f*delta,240f*delta),normal)*previous;
        }

        public AiObjective Objective => objective;

        public void Configure(PlanetGame director,PlanetActor pawn)
        {
            game=director;actor=pawn;home=pawn.SpawnDirection;
            difficulty=AiDifficultyProfile.For(director.Difficulty);
            Debug.Log("AI_DIFFICULTY_CONFIG team="+pawn.Team+
                " level="+difficulty.Level+" decision="+
                difficulty.DecisionMin.ToString("F2")+"-"+
                difficulty.DecisionMax.ToString("F2")+
                " skill="+difficulty.SkillMin.ToString("F1")+"-"+
                difficulty.SkillMax.ToString("F1"));
            lastPosition=home;
            nextAbility=Time.time+Random.Range(difficulty.FirstSkillMin,
                difficulty.FirstSkillMax);
            ChooseTrip();
            evaluateAt=Time.time+.25f;
            nextAggressionRoll=Time.time;
        }

        private void Awake(){actor=GetComponent<PlanetActor>();}

        public void OnRespawn()
        {
            objective=AiObjective.Expand;
            objectiveStartedAt=Time.time;
            lastPosition=actor.SpawnDirection;
            stuckSeconds=0;escapeUntil=0;nextEscapeAt=Time.time+2f;
            nextAbility=Mathf.Max(nextAbility,Time.time+1.7f);
            ChooseTrip();
            Debug.Log("AI_RESPAWN_PLAN team="+actor.Team);
        }

        private void ChooseTrip()
        {
            leg=0;
            Vector3 start=transform.position.sqrMagnitude>1f
                ? transform.position.normalized : home;
            if(game!=null && !game.Territory.InOwn(actor.Team,start))
                start=home;
            firstTurn=Accessible(PlanetMath.RandomAround(start,
                Random.Range(difficulty.LoopFirstMin,difficulty.LoopFirstMax)*
                PlanetMath.TraversalScale),start);
            secondTurn=Accessible(PlanetMath.RandomAround(firstTurn,
                Random.Range(difficulty.LoopSecondMin,difficulty.LoopSecondMax)*
                PlanetMath.TraversalScale),firstTurn);
            goal=firstTurn;
            legDeadline=Time.time+9f;
        }

        private Vector3 Accessible(Vector3 desired,Vector3 fallback)
        {
            Vector3 candidate=desired;
            for(int i=0;i<8 && PlanetSurface.Elevation(candidate)>2.2f;i++)
                candidate=PlanetMath.RandomAround(fallback,.18f+Random.Range(.05f,.22f));
            return candidate.normalized;
        }

        private void NextLeg()
        {
            if(leg==0){leg=1;goal=secondTurn;legDeadline=Time.time+9f;}
            else if(leg==1){leg=2;goal=home;legDeadline=Time.time+11f;}
            else ChooseTrip();
        }

        private void Think()
        {
            Vector3 position=transform.position.normalized;
            float homeDegrees=Vector3.Angle(position,home);
            int ownTrail=game.Territory.Path(actor.Team).Count;
            float nearestOpponent=180f;
            float nearestHomeThreat=180f;
            PlanetActor intruder=null,homeIntruder=null;
            foreach(PlanetActor candidate in game.Actors)
            {
                if(candidate==null||!candidate.Active||
                   candidate.Team==actor.Team)continue;
                Vector3 target=candidate.transform.position.normalized;
                float d=Vector3.Angle(position,target);
                if(d<nearestOpponent)
                {
                    nearestOpponent=d;
                    intruder=candidate;
                }
                float toBase=Vector3.Angle(home,target);
                if(toBase<nearestHomeThreat)
                {
                    nearestHomeThreat=toBase;
                    homeIntruder=candidate;
                }
            }

            Vector3 trailGoal;
            int trailTeam;
            float trailDegrees;
            bool found=game.Territory.TryFindEnemyTrail(
                actor.Team,position,difficulty.HuntDegrees+3f,
                out trailGoal,out trailTeam,out trailDegrees);
            if(!found)trailDegrees=180f;
            bool defenderNeeded=nearestHomeThreat<difficulty.DefendDegrees;
            if(found && objective!=AiObjective.HuntTrail &&
               Time.time>=nextAggressionRoll)
            {
                nextAggressionRoll=Time.time+2.5f;
                pursuePermission=Random.value<=difficulty.TrailAggression;
            }
            if(objective!=AiObjective.HuntTrail && !pursuePermission)
                trailDegrees=180f;
            AiObjective desired=AiTactics.Choose(ownTrail>0,homeDegrees,ownTrail,
                nearestOpponent,defenderNeeded,trailDegrees,difficulty);

            // A retreat is a commitment until the exposed line is closed.
            // Also avoid repeatedly changing between hunting and expanding
            // when a trail disappears for one decision interval.
            if(objective==AiObjective.Retreat && ownTrail>0)
                desired=AiObjective.Retreat;
            if(desired!=objective && desired!=AiObjective.Retreat &&
               Time.time-objectiveStartedAt<difficulty.CommitSeconds)
                desired=objective;

            if(desired!=objective)
            {
                Debug.Log("AI_TACTIC_CHANGE team="+actor.Team+" "+objective+
                    " -> "+desired+" opponentDeg="+nearestOpponent.ToString("F1")+
                    " trailDeg="+trailDegrees.ToString("F1")+
                    " ownTrail="+ownTrail);
                objective=desired;
                objectiveStartedAt=Time.time;
                lastTargetTeam=0;
            }

            switch(objective)
            {
                case AiObjective.Retreat:
                    goal=home;
                    break;
                case AiObjective.Defend:
                    if(homeIntruder!=null)
                        goal=Accessible(homeIntruder.transform.position.normalized,home);
                    else goal=home;
                    break;
                case AiObjective.HuntTrail:
                    if(found)
                    {
                        goal=trailGoal;
                        if(trailTeam!=lastTargetTeam)
                        {
                            lastTargetTeam=trailTeam;
                            Debug.Log("AI_TRAIL_INTERCEPT team="+actor.Team+
                                " targetTeam="+trailTeam+
                                " distanceDeg="+trailDegrees.ToString("F1"));
                        }
                    }
                    else {objective=AiObjective.Expand;ChooseTrip();}
                    break;
                default:
                    if(Time.time>=legDeadline || GoalReached(position,goal,Time.deltaTime))
                        NextLeg();
                    break;
            }

            if(Time.time>=nextAbility)
            {
                bool outside=!game.Territory.InOwn(actor.Team,position);
                if(AiTactics.WantsSkill(actor.Character,objective,outside,
                    nearestOpponent,trailDegrees,actor.Grounded) &&
                   Random.value<=difficulty.SkillConfidence)
                {
                    if(game.TryUseActorSkill(actor))
                    {
                        nextAbility=Time.time+Random.Range(difficulty.SkillMin,
                            difficulty.SkillMax);
                        Debug.Log("AI_SKILL_USED team="+actor.Team+
                            " character="+actor.Character+" mode="+objective);
                    }
                    else nextAbility=Time.time+2.0f;
                }
            }

            if(Time.time>=nextLog)
            {
                nextLog=Time.time+9f;
                Debug.Log("AI_DECISION team="+actor.Team+
                    " difficulty="+difficulty.Level+" mode="+objective+
                    " ownTrail="+ownTrail+" nearestEnemyDeg="+
                    nearestOpponent.ToString("F1")+" trailDeg="+
                    trailDegrees.ToString("F1"));
            }
        }

        private void Update()
        {
            if(game==null || actor==null || !actor.Active ||
               game.State!=MatchState.Playing)return;
            if(game.IsLanding){actor.Wish=Vector3.zero;return;}
            if(Time.deltaTime<=0f)return;

            // Replanning is deliberately infrequent on mobile devices.
            if(Time.time>=evaluateAt)
            {
                Think();
                evaluateAt=Time.time+Random.Range(difficulty.DecisionMin,
                    difficulty.DecisionMax);
            }

            Vector3 normal=transform.position.normalized;
            // Arrival is checked every frame, independently of tactical think
            // intervals. Otherwise the bot overshoots a point and reverses each
            // frame until the next decision, looking like a spinning fan.
            if(GoalReached(normal,goal,Time.deltaTime))
            {
                if(objective==AiObjective.Expand)NextLeg();
                else {actor.Wish=Vector3.zero;lastPosition=normal;stuckSeconds=0;return;}
            }
            Vector3 desired=PlanetMath.Tangent(normal,goal);
            if(desired.sqrMagnitude<.01f)
                desired=PlanetMath.Tangent(normal,home);

            float moved=Vector3.Angle(lastPosition,normal)*Mathf.Deg2Rad*PlanetMath.Radius;
            if(actor.Grounded&&actor.Wish.sqrMagnitude>.2f&&moved<.012f)
                stuckSeconds+=Time.deltaTime;
            else stuckSeconds=0;
            if(stuckSeconds>1.2f&&Time.time>=nextEscapeAt)
            {
                escapeHeading=Quaternion.AngleAxis(actor.Team%2==0?55f:-55f,normal)*desired;
                escapeUntil=Time.time+.8f;nextEscapeAt=Time.time+2.5f;stuckSeconds=0;
                actor.Jump();
                Debug.Log("AI_UNSTUCK team="+actor.Team+" mode="+objective);
            }
            if(Time.time<escapeUntil)desired=escapeHeading;

            // Steer with a small continuous turn. Do not teleport or alter
            // rigidbody speed, preserving fair player/bot movement.
            actor.Wish=StableHeading(actor.Wish,desired,normal,Time.deltaTime);
            if(actor.Grounded && Random.value<difficulty.JumpPerFrame)
                actor.Jump();
            lastPosition=normal;
        }
    }
}
