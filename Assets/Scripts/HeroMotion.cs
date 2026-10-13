using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>
    /// Cheap per-character procedural secondary motion, layered AFTER imported
    /// Animator clips. It never changes Rigidbody, collider, planet camera or
    /// territory logic. Shared by all 13 heroes, including non-humanoid rigs.
    /// </summary>
    public sealed class HeroMotion : MonoBehaviour
    {
        private PlanetActor actor;
        private Animator animator;
        private int hero;
        private Transform torso, head, armL, armR, legL, legR, tail, accessoryL, accessoryR;
        private Quaternion restRotation;
        private Vector3 restPosition;
        private float victoryStart = -100f, skillStart = -100f;
        private float victoryCooldown = -100f;
        private bool dropping;
        private float dropTime;
        private float motionOffset;
        private float nextDebugLog;
        private const float VictoryLength = 2.55f;
        private const float SkillLength = 1.1f;

        public void Configure(PlanetActor owner, Animator source)
        {
            actor = owner;
            animator = source;
            hero = Mathf.Clamp(owner.Character,0,12);
            restRotation = transform.localRotation;
            restPosition = transform.localPosition;
            motionOffset = hero*.53f;
            var descendants = GetComponentsInChildren<Transform>(true);
            torso = Find(descendants,"Body","Torso","Spine");
            head = Find(descendants,"Head","Neck","Jaw","Beak");
            armL = Find(descendants,"Arm_L","Wing_L","Fin_L","Flap_L","Prop_L");
            armR = Find(descendants,"Arm_R","Wing_R","Fin_R","Flap_R","Prop_R");
            legL = Find(descendants,"Leg_L","Thigh_L","Foot_L");
            legR = Find(descendants,"Leg_R","Thigh_R","Foot_R");
            tail = Find(descendants,"Tail","Skirt");
            accessoryL = Find(descendants,"Forearm_L","Engine_L","Shin_L");
            accessoryR = Find(descendants,"Forearm_R","Engine_R","Shin_R");

            Debug.Log("HERO_MOTION_RIG hero="+hero+
                      " torso="+(torso!=null)+" head="+(head!=null)+
                      " arms="+(armL!=null)+"/"+(armR!=null)+
                      " legs="+(legL!=null)+"/"+(legR!=null));
        }

        private static Transform Find(Transform[] all,params string[] choices)
        {
            // Exact bone names win over mesh or Blender collection prefixes.
            foreach(string choice in choices)
                foreach(Transform candidate in all)
                    if(candidate.name.Equals(choice,System.StringComparison.OrdinalIgnoreCase))
                        return candidate;
            foreach(string choice in choices)
                foreach(Transform candidate in all)
                    if(candidate.name.EndsWith("|"+choice,
                        System.StringComparison.OrdinalIgnoreCase) ||
                       candidate.name.StartsWith(choice+".",
                        System.StringComparison.OrdinalIgnoreCase))
                        return candidate;
            return null;
        }

        public void StartDrop()
        {
            dropping=true;
            dropTime=0f;
            // Stop a previous celebration when a new match starts.
            victoryStart=-100f;skillStart=-100f;
            Debug.Log("HERO_MOTION_DROP hero="+hero);
        }
        public void SetDropTime(float seconds){dropTime=seconds;}
        public void Landed()
        {
            dropping=false;
            skillStart=Time.time-.30f; // brief bounce and shake on landing
            Debug.Log("HERO_MOTION_TOUCHDOWN hero="+hero);
        }
        public void PerformSkill()
        {
            skillStart=Time.time;
            Debug.Log("HERO_MOTION_SKILL hero="+hero);
        }
        public void Celebrate()
        {
            if(Time.time-victoryCooldown<2.2f)return;
            victoryCooldown=Time.time;
            victoryStart=Time.time;
            Debug.Log("HERO_MOTION_VICTORY hero="+hero);
        }

        private void LateUpdate()
        {
            if(actor==null || animator==null)return;
            float now=Time.time;
            float t=now+motionOffset;
            bool moving=actor.Wish.sqrMagnitude>.02f && actor.Grounded && !dropping;
            bool celebrating=now-victoryStart>=0f && now-victoryStart<VictoryLength;
            bool special=now-skillStart>=0f && now-skillStart<SkillLength;
            AnimatorStateInfo authoredState=animator.GetCurrentAnimatorStateInfo(0);
            bool authoredDrop=authoredState.IsName("Drop");
            bool authoredSpecial=authoredState.IsName("Special");
            bool authoredVictory=authoredState.IsName("Victory");
            float gait=moving ? (1f+.09f*(hero%5)) : 0f;
            float pace=t*(moving?13f+hero*.27f:2.0f);
            float step=Mathf.Sin(pace),other=Mathf.Sin(pace+Mathf.PI);
            float breathe=Mathf.Sin(t*2.5f);
            float idleAccent=Mathf.Pow(Mathf.Max(0f,Mathf.Sin(t*.68f)),8f);
            float bodyPitch=.9f*breathe;
            float bodyRoll=1.7f*breathe;
            float bodyYaw=0f;
            float tiltHead=2f*Mathf.Sin(t*1.8f);
            float lArm=7f*idleAccent, rArm=-7f*idleAccent;
            float lLeg=0,rLeg=0;
            float shoulderFlap=0f,tailSway=4f*breathe;
            float bounce=.012f*breathe,leanYaw=0f;

            if(moving)
            {
                float gaitAmp=13f;
                switch(hero)
                {
                    case 0: gaitAmp=21f;bodyRoll+=7f*step;bodyPitch+=4f*step;break; // Three-leg shark
                    case 1: gaitAmp=4f;bodyRoll+=8f*step;bodyPitch+=3f*step;break; // Airplane
                    case 2: gaitAmp=12f;bodyRoll+=3f*step;bodyPitch+=4f*step;break; // Refrigerator
                    case 3: gaitAmp=6f;bodyRoll+=9f*step;bodyPitch+=5f*step;break; // Goose aircraft
                    case 4: gaitAmp=15f;bodyRoll+=4f*step;bodyPitch+=2f*step;break; // Ballerina
                    case 5: gaitAmp=17f;bodyRoll+=2f*step;bodyPitch-=8f;break; // Ninja
                    case 6: gaitAmp=16f;bodyRoll+=7f*step;bodyPitch+=5f*step;break; // Tree monkey
                    case 7: gaitAmp=15f;bodyRoll+=3f*step;bodyPitch+=5f*step;break; // Wood drum
                    case 8: gaitAmp=12f;bodyRoll+=5f*step;bodyPitch+=4f*step;break; // Cactus elephant
                    case 9: gaitAmp=9f;bodyRoll+=5f*step;bodyPitch+=3f*step;break; // Bird
                    case 10:gaitAmp=18f;bodyRoll+=11f*step;bodyPitch+=3f*step;break; // Wheel frog
                    case 11:gaitAmp=20f;bodyRoll+=7f*step;bodyPitch+=3f*step;break; // Shrimp cat
                    case 12:gaitAmp=13f;bodyRoll+=8f*step;bodyPitch+=4f*step;break; // Fish bear
                }
                lArm+=gaitAmp*step;
                rArm+=gaitAmp*other;
                lLeg+=gaitAmp*other*.85f;
                rLeg+=gaitAmp*step*.85f;
                tailSway+=12f*step;
                shoulderFlap=12f*Mathf.Abs(step);
                tiltHead+=3f*step;
                bounce+=.032f*Mathf.Abs(step)*gait;
                if(hero==1||hero==3||hero==9)
                {
                    lArm=-23f-24f*Mathf.Sin(pace*.75f);
                    rArm=23f+24f*Mathf.Sin(pace*.75f);
                    bounce+=.018f*Mathf.Sin(pace*.80f);
                }
                if(hero==4)
                {
                    bodyYaw=7f*step;
                    lArm=-16f+gaitAmp*step;
                    rArm=-16f-gaitAmp*step;
                }
                if(hero==7)
                {
                    rArm+=22f*Mathf.Max(0,Mathf.Sin(pace*2f));
                }
                if(hero==10){bounce+=.055f*Mathf.Abs(step);bodyRoll+=12f*step;}
            }

            if(dropping)
            {
                // Comic airborne scramble: kick legs rapidly, flap/throw arms,
                // twist the torso and hunt for a foothold on the approaching planet.
                float f=dropTime;
                bodyPitch=13f+13f*Mathf.Sin(f*9.3f+hero);
                bodyRoll=15f*Mathf.Sin(f*8.1f+hero*.7f);
                bodyYaw=10f*Mathf.Cos(f*5.4f+hero);
                lArm=-44f+45f*Mathf.Sin(f*17f+hero*.33f);
                rArm=44f-45f*Mathf.Sin(f*19.2f+hero*.41f);
                lLeg=44f*Mathf.Sin(f*15.8f);
                rLeg=-44f*Mathf.Sin(f*15.8f+1.4f);
                tiltHead=11f*Mathf.Sin(f*13.7f+hero);
                tailSway=20f*Mathf.Sin(f*17f);
                shoulderFlap=32f*Mathf.Sin(f*14.5f);
                bounce=.015f*Mathf.Sin(f*16f);
                if(hero==0){lArm*=1.25f;rArm*=1.25f;}
                if(hero==1||hero==3||hero==9)
                {
                    // Wings panic-flutter; planes rock their wings, not legs.
                    lArm=-35f+35f*Mathf.Sin(f*19.5f);
                    rArm=35f-35f*Mathf.Sin(f*19.5f);
                    bodyRoll+=10f*Mathf.Sin(f*9f);
                }
                if(hero==4){bodyYaw+=25f*Mathf.Sin(f*4f);lLeg*=.65f;rLeg*=.65f;}
                if(hero==10){bodyPitch+=12f*Mathf.Cos(f*11f);}
            }
            else if(celebrating)
            {
                float u=Mathf.Clamp01((now-victoryStart)/VictoryLength);
                float a=(now-victoryStart)*Mathf.PI*2f;
                float pop=Mathf.Sin(u*Mathf.PI);
                // Styles range from freestyle, drum-step and breakdance to
                // sword salute, ballet pirouette and rolling frog wheel.
                switch(hero)
                {
                    case 0: bodyRoll=15f*Mathf.Sin(a*2.3f);bodyYaw=115f*u;
                        bodyPitch=11f*Mathf.Sin(a);lArm=55f*Mathf.Sin(a*2f);
                        rArm=-lArm;lLeg=35f*Mathf.Sin(a*2f);rLeg=-lLeg;break;
                    case 1: bodyRoll=360f*u;bodyPitch=10f*Mathf.Sin(a);
                        lArm=-38f;rArm=38f;break;
                    case 2: bodyYaw=12f*Mathf.Sin(a);bodyRoll=19f*Mathf.Sin(a*2);
                        lArm=58f*Mathf.Sin(a*2);rArm=-lArm;break;
                    case 3: bodyYaw=360f*u;bodyRoll=16f*Mathf.Sin(a*2);
                        lArm=-48f+25f*Mathf.Sin(a*5);rArm=-lArm;break;
                    case 4: bodyYaw=720f*u;bodyPitch=10f;bodyRoll=5f*Mathf.Sin(a);
                        lArm=-63f;rArm=-63f;lLeg=20f;rLeg=-24f;break;
                    case 5: bodyYaw=360f*u;bodyPitch=-17f;
                        lArm=-52f+28f*Mathf.Sin(a*3);rArm=52f-28f*Mathf.Sin(a*3);break;
                    case 6: bodyYaw=28f*Mathf.Sin(a);bodyRoll=20f*Mathf.Sin(a);
                        lArm=70f*Mathf.Sin(a*2.0f);rArm=70f*Mathf.Sin(a*2+.7f);break;
                    case 7: bodyYaw=30f*Mathf.Sin(a);bodyRoll=7f*Mathf.Sin(a*4);
                        lArm=48f*Mathf.Sin(a*4);rArm=65f*Mathf.Sin(a*4+Mathf.PI);
                        lLeg=22f*Mathf.Sin(a*4);rLeg=-lLeg;break;
                    case 8: bodyYaw=360f*u;headSway(ref tiltHead,a);
                        bodyRoll=14f*Mathf.Sin(a*2);lArm=35f*Mathf.Sin(a);rArm=-lArm;break;
                    case 9: bodyYaw=360f*u;bodyPitch=-6f;bodyRoll=17f*Mathf.Sin(a);
                        lArm=-65f+25f*Mathf.Sin(a*4);
                        rArm=65f-25f*Mathf.Sin(a*4);break;
                    case 10:bodyRoll=360f*u;bodyPitch=11f*Mathf.Sin(a*2);
                        lLeg=36f*Mathf.Sin(a*3);rLeg=-lLeg;break;
                    case 11:bodyYaw=360f*u;bodyRoll=22f*Mathf.Sin(a*3);
                        lArm=38f*Mathf.Sin(a*4);rArm=-lArm;
                        lLeg=25f*Mathf.Sin(a*4);rLeg=-lLeg;break;
                }
                bounce=.08f*Mathf.Abs(Mathf.Sin(a*2f))*pop;
                tailSway=25f*Mathf.Sin(a*2f);
                shoulderFlap=30f*Mathf.Sin(a*3f);
            }
            else if(special)
            {
                float u=Mathf.Clamp01((now-skillStart)/SkillLength);
                float strike=Mathf.Sin(u*Mathf.PI);
                bodyPitch-=18f*strike;
                bodyRoll+=8f*Mathf.Sin(u*Mathf.PI*4f);
                bodyYaw+=hero==4 ? 420f*u : (hero==3||hero==9 ? 120f*u : 0f);
                lArm+=45f*strike;
                rArm-=45f*strike;
                lLeg-=24f*strike;
                rLeg+=24f*strike;
                tailSway+=20f*strike;
                bounce+=.07f*strike;
                if(hero==7){rArm=70f*Mathf.Sin(u*Mathf.PI*5f);}
                if(hero==10){bodyPitch=28f*strike;bounce+=.10f*strike;}
            }

            // Drop/Special/Victory are now fully authored Blender actions.
            // Retain only a restrained secondary accent while those clips play;
            // applying the old full-body overlay caused double spins and a hard
            // pose reset when an authored action ended.
            float authoredOverlay=1f;
            if(dropping && authoredDrop)
            {
                float u=Mathf.Clamp01(dropTime/OrbitCamera.DescentDuration);
                authoredOverlay=Mathf.Lerp(.12f,.34f,Mathf.Sin(u*Mathf.PI));
            }
            else if(special && authoredSpecial)
            {
                float u=Mathf.Clamp01((now-skillStart)/SkillLength);
                authoredOverlay=.30f*Mathf.Sin(u*Mathf.PI);
            }
            else if(celebrating && authoredVictory)
            {
                float u=Mathf.Clamp01((now-victoryStart)/VictoryLength);
                authoredOverlay=.22f*Mathf.Sin(u*Mathf.PI);
            }
            bodyPitch*=authoredOverlay;bodyRoll*=authoredOverlay;bodyYaw*=authoredOverlay;
            tiltHead*=authoredOverlay;lArm*=authoredOverlay;rArm*=authoredOverlay;
            lLeg*=authoredOverlay;rLeg*=authoredOverlay;shoulderFlap*=authoredOverlay;
            tailSway*=authoredOverlay;bounce*=authoredOverlay;leanYaw*=authoredOverlay;

            float totalTilt=bodyRoll;
            transform.localPosition=restPosition+Vector3.up*bounce;
            transform.localRotation=restRotation*Quaternion.Euler(bodyPitch*.21f,
                bodyYaw,Mathf.Clamp(totalTilt,-360f,360f)*.36f);
            Rotate(torso,bodyPitch,bodyYaw*.12f,totalTilt);
            Rotate(head,tiltHead,0f,4f*Mathf.Sin(pace*.37f));
            Rotate(armL,lArm,0f,shoulderFlap);
            Rotate(armR,rArm,0f,-shoulderFlap);
            Rotate(legL,lLeg,0f,5f*Mathf.Sin(pace));
            Rotate(legR,rLeg,0f,-5f*Mathf.Sin(pace));
            Rotate(tail,0f,tailSway,tailSway*.34f);
            Rotate(accessoryL,lArm*.35f,0f,shoulderFlap*.50f);
            Rotate(accessoryR,rArm*.35f,0f,-shoulderFlap*.50f);

            if(Debug.isDebugBuild && actor.Team==1 && now>=nextDebugLog)
            {
                nextDebugLog=now+1.8f;
                string state=dropping?"drop":celebrating?"victory":special?"skill":moving?"run":"idle";
                Debug.Log("HERO_MOTION_ACTIVE hero="+hero+" state="+state+
                          " authored="+authoredState.shortNameHash+
                          " overlay="+authoredOverlay.ToString("F2")+
                          " armL="+(armL!=null)+" armR="+(armR!=null));
            }
        }

        private static void headSway(ref float tilt,float phase)
        {
            tilt=18f*Mathf.Sin(phase*3f);
        }

        private static void Rotate(Transform bone,float x,float y,float z)
        {
            if(bone==null)return;
            bone.localRotation=bone.localRotation*Quaternion.Euler(x,y,z);
        }
    }
}
