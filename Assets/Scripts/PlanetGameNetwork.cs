using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        public int LocalTeam { get; private set; } = 1;
        public bool DedicatedMatchServer { get; private set; }
        public bool IsNetworkReplica => matchConnection != null && !DedicatedMatchServer;
        public bool IsNetworkMatch => DedicatedMatchServer || matchConnection != null;
        private IMatchPeer matchConnection;
        private TcpListener matchListener;
        private sealed class Participant
        {
            public IMatchPeer peer;
            public bool joined, replay;
            public uint sequence;
            public float lastInput, acceptedAt;
        }
        private readonly Participant[] participants = new Participant[5];
        private readonly int[] networkHeroes = { 0, 0, 1, 2, 3 };
        private readonly float[] networkCooldown = new float[5], networkGrace = new float[5];
        private readonly byte[] ownerScratch = new byte[PlanetMath.CellCount];
        private uint networkEpoch, inputSequence;
        private float nextSnapshot, nextInput, nextLobby, lobbyStarted, networkLastPacket;
        private byte inputButtons;
        private bool networkPanel, helloSent;
        private string networkAddress = "", networkKey = "", networkMessage = "";
        private int networkWinner;

        private void SetupNetwork()
        {
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg == "-brainrot-server") DedicatedMatchServer = true;
            var config = Resources.Load<TextAsset>("MatchEndpoint");
            if (config != null) networkAddress = config.text.Trim();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            string invitePath=Path.Combine(Application.persistentDataPath,".network-test-key");
            if(File.Exists(invitePath))networkKey=File.ReadAllText(invitePath).Trim();
#endif
            if (!DedicatedMatchServer) return;
            Application.runInBackground = true;
            int listenPort=MatchProtocol.Port;
            string configuredPort=Environment.GetEnvironmentVariable("BRAINROT_MATCH_PORT");
            if(!string.IsNullOrEmpty(configuredPort)&&(!int.TryParse(configuredPort,out listenPort)||listenPort<1024||listenPort>65535))
                throw new InvalidOperationException("Invalid private match port");
            matchListener = new TcpListener(IPAddress.Loopback, listenPort);
            matchListener.Start(4);
            Debug.Log("MATCH_SERVER_READY loopbackPort=" + listenPort + " protocol=" + MatchProtocol.Version);
        }
        private void CloseNetwork()
        {
            matchConnection?.Dispose(); matchConnection = null;
            matchListener?.Stop(); matchListener = null;
            for (int i = 1; i <= 4; i++) { participants[i]?.peer.Dispose(); participants[i] = null; }
            LocalTeam = 1; helloSent = false; inputButtons = 0; networkEpoch = 0;
        }
        private void ConnectMatch()
        {
            try
            {
                CloseNetwork();
                matchConnection = new MatchWebSocketPeer(networkAddress.Trim(), networkKey.Trim());
                networkMessage = "ПОДКЛЮЧЕНИЕ…";
                networkLastPacket = Time.unscaledTime;
                Debug.Log("MATCH_CONNECT_BEGIN protocol=" + MatchProtocol.Version);
            }
            catch (Exception e) { networkMessage = "Не удалось подключиться: " + e.GetType().Name; }
        }
        private void NetworkDisconnected(string message)
        {
            Debug.Log("MATCH_DISCONNECTED " + message);
            ReturnToMenu(); networkPanel = true; networkMessage = message;
        }
        private void PollNetwork()
        {
            float now = Time.unscaledTime;
            if (DedicatedMatchServer)
            {
                if (matchListener == null) return;
                while (matchListener.Pending())
                {
                    TcpClient socket = matchListener.AcceptTcpClient();
                    int slot = 0;
                    if (State == MatchState.Menu)
                        for (int i = 1; i <= 4; i++) if (participants[i] == null) { slot = i; break; }
                    if (slot == 0) { socket.Close(); continue; }
                    participants[slot] = new Participant { peer = new MatchTcpPeer(socket), acceptedAt = now, lastInput = now };
                }
                int connected = 0;
                bool allReplay = true;
                for (int i = 1; i <= 4; i++)
                {
                    var participant = participants[i];
                    if (participant == null) continue;
                    try
                    {
                        for (int n = 0; n < 64 && participant.peer.TryReceive(out byte[] packet); n++)
                            ReceiveCommand(i, participant, packet);
                    }
                    catch (Exception e) { Debug.Log("MATCH_BAD_COMMAND team=" + i + " error=" + e.GetType().Name); participant.peer.Dispose(); }
                    if (participant.peer.Closed || (!participant.joined && now-participant.acceptedAt > 8f))
                    {
                        participant.peer.Dispose(); participants[i] = null;
                        if (State == MatchState.Playing)
                        {
                            PlanetActor actor = players[i-1];
                            if (actor.Active && actor.GetComponent<PlanetAI>() == null)
                                actor.gameObject.AddComponent<PlanetAI>().Configure(this, actor);
                        }
                        Debug.Log("MATCH_PEER_LEFT team=" + i + " botTakeover=" + (State == MatchState.Playing));
                        continue;
                    }
                    if (!participant.joined) continue;
                    connected++; allReplay &= participant.replay;
                    if (State == MatchState.Playing && now-participant.lastInput > .6f) players[i-1].Wish = Vector3.zero;
                }
                if (connected == 0)
                {
                    lobbyStarted = 0;
                    if (State != MatchState.Menu) { if (worldRoot != null) Destroy(worldRoot.gameObject); players.Clear(); State = MatchState.Menu; }
                    return;
                }
                if (lobbyStarted == 0) lobbyStarted = now;
                if ((State == MatchState.Menu && now-lobbyStarted >= 10f) || (State == MatchState.End && allReplay))
                    StartServerMatch();
                if (now >= nextLobby && State == MatchState.Menu)
                {
                    nextLobby = now+1;
                    for (int i = 1; i <= 4; i++) if (participants[i]?.joined == true)
                        participants[i].peer.Send(MatchProtocol.Packet(MatchProtocol.Lobby, w => { w.Write((byte)i); w.Write((byte)connected); }));
                }
                if (State == MatchState.Playing)
                    for (int i = 1; i <= 4; i++) { networkCooldown[i] = Mathf.Max(0, networkCooldown[i]-Time.deltaTime); networkGrace[i] = Mathf.Max(0, networkGrace[i]-Time.deltaTime); }
                if (State != MatchState.Menu && now >= nextSnapshot)
                { nextSnapshot = now+.1f; Broadcast(WriteSnapshot()); }
                return;
            }
            if (matchConnection == null) return;
            if (matchConnection.Closed || now-networkLastPacket > 15f)
            { NetworkDisconnected("Связь с комнатой потеряна. Можно подключиться снова."); return; }
            if (matchConnection.Connected && !helloSent)
            {
                helloSent = true;
                matchConnection.Send(MatchProtocol.Packet(MatchProtocol.Hello, w => { w.Write(MatchProtocol.Version); w.Write((byte)selectedHero); w.Write(ChosenSkinReference()); }));
            }
            try
            {
                for (int i = 0; i < 64 && matchConnection != null && matchConnection.TryReceive(out byte[] packet); i++)
                { ReceiveServerPacket(packet); networkLastPacket = now; }
            }
            catch (Exception e) { NetworkDisconnected("Ошибка сетевых данных: " + e.GetType().Name); }
            if (matchConnection != null && helloSent && now >= nextInput)
            {
                nextInput = now+.05f;
                Vector3 wish = State == MatchState.Playing && !paused && Player != null ? Player.Wish : Vector3.zero;
                byte buttons = paused ? (byte)0 : inputButtons; inputButtons = 0;
                matchConnection.Send(MatchProtocol.Packet(MatchProtocol.Input, w =>
                { w.Write(networkEpoch); w.Write(++inputSequence); MatchProtocol.Vector(w, wish); w.Write(buttons); }));
            }
        }
        private void ReceiveCommand(int team, Participant p, byte[] packet)
        {
            using var r = MatchProtocol.Reader(packet);
            byte kind = r.ReadByte();
            if (kind == MatchProtocol.Hello && !p.joined)
            {
                if (r.ReadInt32() != MatchProtocol.Version) throw new InvalidDataException("Protocol mismatch");
                int hero = r.ReadByte(); if (hero >= characters.Length) throw new InvalidDataException("Invalid hero");
                string skin=r.ReadString();if(!SkinCatalog.ValidReference(skin))throw new InvalidDataException("Invalid cosmetic reference");
                MatchProtocol.End(r); networkHeroes[team] = hero; networkSkins[team]=skin; p.joined = true;
                Debug.Log("MATCH_PEER_JOINED team=" + team + " hero=" + hero); return;
            }
            if (kind != MatchProtocol.Input || !p.joined) throw new InvalidDataException("Unexpected command");
            uint epoch = r.ReadUInt32(), sequence = r.ReadUInt32();
            Vector3 wish = MatchProtocol.Vector(r, 1.01f); byte buttons = r.ReadByte(); MatchProtocol.End(r);
            if ((buttons & ~7) != 0) throw new InvalidDataException("Invalid buttons");
            if (sequence <= p.sequence) return;
            p.sequence = sequence; p.lastInput = Time.unscaledTime;
            if (epoch != networkEpoch) return;
            if (State == MatchState.End) { if ((buttons & 4) != 0) p.replay = true; return; }
            if (State != MatchState.Playing || IsLanding) return;
            var actor = players[team-1]; if (!actor.Active) return;
            actor.Wish = Vector3.ProjectOnPlane(wish, actor.transform.position.normalized).normalized;
            if ((buttons & 1) != 0) actor.Jump();
            if ((buttons & 2) != 0) TryUseActorSkill(actor);
        }
        private void StartServerMatch()
        {
            networkEpoch++; networkWinner = 0;
            for (int i = 1; i <= 4; i++)
            {
                networkCooldown[i] = 0; networkGrace[i] = OrbitCamera.DescentDuration+2f;
                if (participants[i] != null) participants[i].replay = false;
            }
            StartGame();
            for (int team = 1; team <= 4; team++) if (participants[team]?.joined == true)
                participants[team].peer.Send(MatchProtocol.Packet(MatchProtocol.Start, w =>
                { w.Write(networkEpoch); w.Write((byte)team); w.Write((byte)chosenDifficulty); MatchProtocol.WritePlanet(w,battlePlanet,planetSeed); for (int i=1;i<=4;i++) w.Write((byte)networkHeroes[i]); for(int i=1;i<=4;i++)w.Write(networkSkins[i]); }));
            nextSnapshot = Time.unscaledTime;
            Debug.Log("MATCH_SERVER_STARTED epoch=" + networkEpoch);
        }
        private void Broadcast(byte[] packet)
        { for (int i = 1; i <= 4; i++) if (participants[i]?.joined == true) participants[i].peer.Send(packet); }
        public void NetworkActorTrailBroken(PlanetActor actor,Vector3 cutDirection)
        {
            int team = actor.Team;
            if (!DedicatedMatchServer || State!=MatchState.Playing) return;
            networkGrace[team] = AiBalanceTuning.RespawnGrace(chosenDifficulty);
            EmitNetworkEffect(3, team, cutDirection);
            Debug.Log("MATCH_TRAIL_BROKEN team="+team+" shield="+networkGrace[team]);
        }
        private void RequestJump()
        { if (IsNetworkReplica) inputButtons |= 1; else Player?.Jump(); }
        private void RequestReplay()
        {
            if (IsNetworkReplica) { inputButtons |= 4; networkMessage = "ЖДЁМ ГОТОВНОСТИ ОСТАЛЬНЫХ"; }
            else StartGame();
        }
        private void DrawNetworkPanel(float w, float h)
        {
            Plate(new Rect(0,0,w,h), new Color(.025f,.035f,.095f,1));
            GUI.Label(new Rect(20,30,w-40,50), "СЕТЕВОЙ МАТЧ · ТЕСТ", headerStyle);
            GUI.Label(new Rect(25,92,w-50,68), "Одна планета · до 4 участников\nСвободные места занимают боты", smallStyle);
            GUI.Label(new Rect(25,170,w-50,30), "АДРЕС КОМНАТЫ (WSS)", smallStyle);
            networkAddress = GUI.TextField(new Rect(25,205,w-50,42), networkAddress, 240);
            GUI.Label(new Rect(25,266,w-50,30), "КОД ДОСТУПА", smallStyle);
            networkKey = GUI.PasswordField(new Rect(25,300,w-50,42), networkKey, '*', 80);
            GUI.Label(new Rect(25,368,w-50,100), networkMessage, smallStyle);
            if (matchConnection == null && FlatButton(new Rect(26,h-180,w-52,58), "ВОЙТИ В КОМНАТУ", new Color(.1f,.55f,.75f))) ConnectMatch();
            if (FlatButton(new Rect(26,h-103,w-52,50), "НАЗАД / ОТКЛЮЧИТЬСЯ", new Color(.2f,.22f,.35f)))
            { CloseNetwork(); networkPanel = false; }
        }
    }
}
