using System;
using System.IO;
using UnityEngine;

namespace BrainrotPlanet
{
    public static class MatchProtocol
    {
        public const int Version = 7, Port = 27766, MaxTrail = 280;
        public const byte Hello = 1, Lobby = 2, Start = 3, Input = 4, Snapshot = 5, Effect = 6,
                          RoomInfo = 7;
        public static bool ValidRoomCode(string code)
        {
            if(code==null||code.Length!=6)return false;
            const string alphabet="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            foreach(char letter in code)if(alphabet.IndexOf(letter)<0)return false;
            return true;
        }
        public static byte[] Packet(byte kind, Action<BinaryWriter> write)
        {
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            {
                writer.Write(kind); write(writer); return memory.ToArray();
            }
        }
        public static BinaryReader Reader(byte[] bytes) => new BinaryReader(new MemoryStream(bytes, false));
        public static void WritePlanet(BinaryWriter writer,BattlePlanet kind,int seed)
        {PlanetBiomeRules.Validate(kind,seed);writer.Write((byte)kind);writer.Write(seed);}
        public static void ReadPlanet(BinaryReader reader,out BattlePlanet kind,out int seed)
        {kind=(BattlePlanet)reader.ReadByte();seed=reader.ReadInt32();PlanetBiomeRules.Validate(kind,seed);}
        public static void End(BinaryReader reader)
        {
            if (reader.BaseStream.Position != reader.BaseStream.Length)
                throw new InvalidDataException("Unexpected LAN payload");
        }
        public static float Number(BinaryReader r, float limit = 10000f)
        {
            float x = r.ReadSingle();
            if (float.IsNaN(x) || float.IsInfinity(x) || Mathf.Abs(x) > limit)
                throw new InvalidDataException("Invalid LAN number");
            return x;
        }
        public static void Vector(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        public static Vector3 Vector(BinaryReader r, float limit = 10000f) => new Vector3(Number(r, limit), Number(r, limit), Number(r, limit));
        public static void Rotation(BinaryWriter w, Quaternion q) { w.Write(q.x); w.Write(q.y); w.Write(q.z); w.Write(q.w); }
        public static Quaternion Rotation(BinaryReader r)
        {
            var q = new Quaternion(Number(r, 1.01f), Number(r, 1.01f), Number(r, 1.01f), Number(r, 1.01f));
            float length = q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
            if (length < .8f || length > 1.2f) throw new InvalidDataException("Invalid rotation");
            return q.normalized;
        }
        public static int Team(BinaryReader r)
        {
            int team = r.ReadByte();
            if (team < 1 || team > 4) throw new InvalidDataException("Invalid team");
            return team;
        }
        // Complete RLE baseline each snapshot: recoverable without delta history.
        // Later WAN transport can replace this with acknowledged cell deltas.
        public static void WriteOwners(BinaryWriter w, byte[] owners)
        {
            for (int i = 0; i < owners.Length;)
            {
                int end = i + 1;
                while (end < owners.Length && owners[end] == owners[i] && end-i < ushort.MaxValue) end++;
                w.Write((ushort)(end-i)); w.Write(owners[i]); i=end;
            }
        }
        public static void ReadOwners(BinaryReader r, byte[] owners)
        {
            for (int i = 0; i < owners.Length;)
            {
                int count = r.ReadUInt16(); byte owner = r.ReadByte();
                if (count == 0 || count > owners.Length-i || owner > 4) throw new InvalidDataException("Invalid territory run");
                for (int end=i+count;i<end;i++) owners[i]=owner;
            }
        }
    }
}
