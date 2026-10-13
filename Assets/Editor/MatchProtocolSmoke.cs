using System;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class MatchProtocolSmoke
    {
        private static int cases;
        private static void Check(bool ok) { if(!ok)throw new Exception("Match protocol assertion failed"); cases++; }
        private static void Reject(Action action)
        { bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}catch(EndOfStreamException){rejected=true;}Check(rejected); }
        public static void Run()
        {
            cases=0;
            for(int pattern=0;pattern<3;pattern++)
            {
                var owners=new byte[PlanetMath.CellCount];
                for(int i=0;i<owners.Length;i++)owners[i]=(byte)(pattern==0?0:pattern==1?i%5:(i/128)%5);
                var bytes=MatchProtocol.Packet(5,w=>MatchProtocol.WriteOwners(w,owners));
                using var r=MatchProtocol.Reader(bytes);Check(r.ReadByte()==5);
                var decoded=new byte[owners.Length];MatchProtocol.ReadOwners(r,decoded);MatchProtocol.End(r);
                Check(Convert.ToBase64String(decoded)==Convert.ToBase64String(owners));
                Check(bytes.Length<MatchTcpPeer.MaxPacket);
            }
            foreach(var bytes in new[]{new byte[]{0,0,1},new byte[]{2,0,1},new byte[]{1,0,5},new byte[]{1}})
                Reject(()=>{using var r=MatchProtocol.Reader(bytes);MatchProtocol.ReadOwners(r,new byte[1]);});
            foreach(float value in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,10001f})
                Reject(()=>{using var r=MatchProtocol.Reader(BitConverter.GetBytes(value));MatchProtocol.Number(r);});
            foreach(int value in new[]{0,-1,MatchTcpPeer.MaxPacket+1})Reject(()=>MatchTcpPeer.PacketLength(BitConverter.GetBytes(value)));
            Check(MatchTcpPeer.PacketLength(BitConverter.GetBytes(MatchTcpPeer.MaxPacket))==MatchTcpPeer.MaxPacket);
            Reject(()=>{using var r=MatchProtocol.Reader(new byte[16]);MatchProtocol.Rotation(r);});
            Reject(()=>{using var r=MatchProtocol.Reader(new byte[]{5});MatchProtocol.Team(r);});
            Reject(()=>{using var r=MatchProtocol.Reader(new byte[]{1});MatchProtocol.End(r);});
            Debug.Log("MATCH_PROTOCOL_SMOKE_PASS cases="+cases);
        }
    }
}
