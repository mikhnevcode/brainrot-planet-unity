using System;
using System.IO;
using UnityEngine;

namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private byte[] WriteSnapshot()
        {
            return MatchProtocol.Packet(MatchProtocol.Snapshot, w =>
            {
                w.Write(networkEpoch); w.Write(State == MatchState.End); w.Write((byte)networkWinner);
                w.Write(Mathf.Max(0, matchTime));
                for (int team=1;team<=4;team++)
                {
                    var actor=players[team-1];
                    MatchProtocol.Vector(w,actor.transform.position); MatchProtocol.Rotation(w,actor.transform.rotation);
                    MatchProtocol.Vector(w,actor.Wish); w.Write(actor.Active); w.Write(actor.Grounded);
                    w.Write((byte)networkLives[team]); w.Write(networkCooldown[team]); w.Write(networkGrace[team]);
                    w.Write(actor.SpeedSeconds); w.Write(actor.SlowSeconds); w.Write(scores[team]);
                }
                Territory.CopyOwners(ownerScratch); MatchProtocol.WriteOwners(w,ownerScratch);
                for(int team=1;team<=4;team++)
                {
                    var path=Territory.VisualPath(team); int count=Mathf.Min(path.Count,MatchProtocol.MaxTrail);
                    w.Write((ushort)count);
                    for(int i=0;i<count;i++)
                        MatchProtocol.Vector(w,path[count<=1?0:Mathf.RoundToInt(i*(path.Count-1f)/(count-1))]);
                }
            });
        }
        private void ReceiveServerPacket(byte[] packet)
        {
            using var r=MatchProtocol.Reader(packet);
            byte kind=r.ReadByte();
            if(kind==MatchProtocol.Lobby)
            {
                LocalTeam=MatchProtocol.Team(r); int count=r.ReadByte(); MatchProtocol.End(r);
                networkMessage="УЧАСТНИКОВ: "+count+" / 4\nОжидаем старт комнаты…";
                return;
            }
            if(kind==MatchProtocol.Start)
            {
                uint epoch=r.ReadUInt32(); if(epoch<=networkEpoch)throw new InvalidDataException("Stale match start");
                int team=MatchProtocol.Team(r), difficulty=r.ReadByte();
                if(difficulty>2)throw new InvalidDataException("Invalid difficulty");
                MatchProtocol.ReadPlanet(r,out BattlePlanet incomingPlanet,out int incomingSeed);
                for(int i=1;i<=4;i++)
                { networkHeroes[i]=r.ReadByte(); if(networkHeroes[i]>=characters.Length)throw new InvalidDataException("Invalid hero"); }
                for(int i=1;i<=4;i++){networkSkins[i]=r.ReadString();if(!SkinCatalog.ValidReference(networkSkins[i]))throw new InvalidDataException("Invalid cosmetic reference");}
                MatchProtocol.End(r); networkEpoch=epoch; LocalTeam=team;
                chosenDifficulty=(AiDifficulty)difficulty; selectedHero=networkHeroes[team];
                battlePlanet=incomingPlanet;planetSeed=incomingSeed;
                networkWinner=0; networkMessage=""; inputButtons=0;
                StartGame(); networkPanel=false;
                Debug.Log("MATCH_CLIENT_STARTED epoch="+networkEpoch+" team="+LocalTeam);
                return;
            }
            if(kind==MatchProtocol.Effect) { ReadNetworkEffect(r); return; }
            if(kind!=MatchProtocol.Snapshot)throw new InvalidDataException("Unknown server packet");
            uint stateEpoch=r.ReadUInt32(); if(stateEpoch!=networkEpoch)throw new InvalidDataException("Wrong match epoch");
            bool ended=r.ReadBoolean(); int winner=r.ReadByte();
            if((ended && (winner<1||winner>4))||(!ended&&winner!=0))throw new InvalidDataException("Invalid winner");
            matchTime=MatchProtocol.Number(r,145f);
            if(players.Count!=4)throw new InvalidDataException("Snapshot before match");
            for(int team=1;team<=4;team++)
            {
                Vector3 position=MatchProtocol.Vector(r,100f); Quaternion rotation=MatchProtocol.Rotation(r);
                Vector3 wish=MatchProtocol.Vector(r,1.01f); bool active=r.ReadBoolean(), grounded=r.ReadBoolean();
                int remaining=r.ReadByte(); if(remaining>3)throw new InvalidDataException("Invalid life count");
                networkLives[team]=remaining; networkCooldown[team]=MatchProtocol.Number(r,12.01f);
                networkGrace[team]=MatchProtocol.Number(r,30f);
                float speed=MatchProtocol.Number(r,30),slow=MatchProtocol.Number(r,30);
                scores[team]=MatchProtocol.Number(r,100.01f);
                var actor=players[team-1];
                if(State!=MatchState.End && !ended) actor.ReceiveNetworkPose(position,rotation,wish,active,grounded,speed,slow);
            }
            MatchProtocol.ReadOwners(r,ownerScratch); Territory.ApplyNetworkOwners(ownerScratch);
            for(int team=1;team<=4;team++)
            {
                int count=r.ReadUInt16(); if(count>MatchProtocol.MaxTrail)throw new InvalidDataException("Trail exceeds limit");
                var path=networkPaths[team]; path.Clear();
                for(int i=0;i<count;i++)path.Add(MatchProtocol.Vector(r,1.01f).normalized);
                Territory.ApplyNetworkTrail(team,path);
            }
            MatchProtocol.End(r);
            lives=networkLives[LocalTeam]; skillCooldown=networkCooldown[LocalTeam]; playerLandingGrace=networkGrace[LocalTeam];
            if(ended && State!=MatchState.End) { networkWinner=winner; finishReason="СЕТЕВОЙ МАТЧ ЗАВЕРШЁН"; Finish(); }
            if(Time.unscaledTime>=nextNetworkDiagnostic)
            {
                nextNetworkDiagnostic=Time.unscaledTime+5;
                Debug.Log("MATCH_SYNC epoch="+networkEpoch+" localTeam="+LocalTeam+" time="+matchTime.ToString("F1")+
                    " area="+scores[LocalTeam].ToString("F2")+" bytes="+packet.Length+" winner="+winner);
            }
        }
        private readonly System.Collections.Generic.List<Vector3>[] networkPaths = {
            new System.Collections.Generic.List<Vector3>(),new System.Collections.Generic.List<Vector3>(280),
            new System.Collections.Generic.List<Vector3>(280),new System.Collections.Generic.List<Vector3>(280),
            new System.Collections.Generic.List<Vector3>(280)};
        private float nextNetworkDiagnostic;
        private void UpdateNetworkReplica()
        {
            if(State!=MatchState.Playing)return;
            damageFlash=Mathf.Max(0,damageFlash-Time.unscaledDeltaTime*1.7f);
            captureRewardTime=Mathf.Max(0,captureRewardTime-Time.unscaledDeltaTime);
            annTime=Mathf.Max(0,annTime-Time.unscaledDeltaTime);
            threatTick-=Time.unscaledDeltaTime;
            if(threatTick<=0 && Player!=null)
            {
                threatTick=.2f; nearestEnemyDegrees=180f;
                foreach(var actor in players)
                    if(actor!=Player && actor.Active)
                        nearestEnemyDegrees=Mathf.Min(nearestEnemyDegrees,
                            Vector3.Angle(Player.transform.position.normalized,actor.transform.position.normalized));
            }
            if(!paused && !IsLanding && Player!=null && Player.Active)HandleInput();
            Territory.Update(Time.unscaledDeltaTime);
            pathUpdate-=Time.unscaledDeltaTime;
            if(pathUpdate<=0){pathUpdate=.08f;RenderPaths();}
        }
        public void EmitNetworkEffect(byte kind,int team,Vector3 direction,int value=0)
        {
            if(!DedicatedMatchServer)return;
            Broadcast(MatchProtocol.Packet(MatchProtocol.Effect,w=>
            { w.Write(networkEpoch); w.Write(kind); w.Write((byte)team); MatchProtocol.Vector(w,direction); w.Write(value); }));
        }
        private void ReadNetworkEffect(BinaryReader r)
        {
            uint epoch=r.ReadUInt32(); byte kind=r.ReadByte(); int team=MatchProtocol.Team(r);
            Vector3 direction=MatchProtocol.Vector(r,1.01f).normalized; int value=r.ReadInt32(); MatchProtocol.End(r);
            if(epoch!=networkEpoch || State!=MatchState.Playing)return;
            Vector3 at=PlanetSurface.Point(direction,1f);
            if(kind==0)
            {
                var actor=players[team-1]; actor.PlaySpecial(); Effects.HeroAbility(actor.Character,direction,Colors.Team(team));
                if(team==LocalTeam && actor.Character!=1)GameSound.Play(GameCue.Power);
            }
            else if(kind==1)
            {
                Effects.Burst(at,Colors.Team(team),62,6f);
                if(Player!=null)GameSound.Explosion(at,Player.transform.position,worldCamera.transform.right);
            }
            else if(kind==2)
            {
                Effects.CaptureFlash(direction,Colors.Team(team),Mathf.Clamp(value,0,PlanetMath.CellCount));
                if(team==LocalTeam) { capturesThisMatch++; tutorialStep=2; GameSound.Play(GameCue.Capture); }
            }
            else if(kind==3 && team==LocalTeam)
            {
                autoMoving=false; savedSteering=movement=Vector2.zero; inputButtons=0; Player.Wish=Vector3.zero;
                damageFlash=1f; GameSound.Play(GameCue.Hit);
                Announce(value>0?"ПОТЕРЯН СЛЕД! СДЕЛАЙ НОВЫЙ СВАЙП":"ТЫ ВЫБЫЛ. ДОЖДИСЬ ИТОГОВ МАТЧА");
            }
        }
        private void RenderPaths()
        {
            for(int team=1;team<=4;team++)
            {
                var path=Territory.VisualPath(team); var renderer=pathRenderers[team]; if(renderer==null)continue;
                int skip=Mathf.Max(1,path.Count/280),count=(path.Count+skip-1)/skip;
                renderer.positionCount=count;
                for(int i=0;i<count;i++)renderer.SetPosition(i,PlanetSurface.Point(path[Mathf.Min(i*skip,path.Count-1)],.56f));
            }
        }
    }
}
