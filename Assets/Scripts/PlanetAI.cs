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
                Random.Range(difficulty.LoopFirstMin,difficulty.LoopFirstMax)),start);
            secondTurn=Accessible(PlanetMath.RandomAround(firstTurn,
                Random.Range(difficulty.LoopSecondMin,difficulty.LoopSecondMax)),firstTurn);
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
                    if(Time.time>=legDeadline || Vector3.Angle(position,goal)<6.5f)
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

            // Replanning is deliberately infrequent on mobile devices.
            if(Time.time>=evaluateAt)
            {
                Think();
                evaluateAt=Time.time+Random.Range(difficulty.DecisionMin,
                    difficulty.DecisionMax);
            }

            Vector3 normal=transform.position.normalized;
            Vector3 desired=PlanetMath.Tangent(normal,goal);
            if(desired.sqrMagnitude<.01f)
                desired=PlanetMath.Tangent(normal,home);

            // Skirt sharp rocks rather than pursuing the same impossible line.
            Vector3 probe=Vector3.Slerp(normal,goal,.10f);
            if(PlanetSurface.Elevation(probe)>2.35f)
            {
                desired=Quaternion.AngleAxis(
                    actor.Team%2==0 ? 65f : -65f,normal)*desired;
                if(objective==AiObjective.Expand)
                    legDeadline=Mathf.Min(legDeadline,Time.time+1.8f);
            }

            // Steer with a small continuous turn. Do not teleport or alter
            // rigidbody speed, preserving fair player/bot movement.
            actor.Wish=Vector3.ProjectOnPlane(desired,normal).normalized;
            if(actor.Grounded && Random.value<difficulty.JumpPerFrame)
                actor.Jump();
            lastPosition=normal;
        }
    }
}