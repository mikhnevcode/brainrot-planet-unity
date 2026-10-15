#if UNITY_EDITOR
using System;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class PlayerRecordsSmoke
    {
        private static int cases;
        private static void Check(bool value,string reason)
        {if(!value)throw new Exception("Player records: "+reason);cases++;}
        public static void Run()
        {
            cases=0;
            Check(PlayerRecords.SanitizeNickname(" A<>Б\t В!!")=="AБ В","nickname filtering");
            Check(PlayerRecords.SanitizeNickname("abcdefghijklmnop").Length==PlayerRecords.MaxNicknameLength,"nickname length");
            Check(PlayerRecords.ValidNickname("Игрок-77"),"valid nickname");
            Check(!PlayerRecords.ValidNickname("Игрок/77"),"invalid nickname");
            float[] scores={0,12,25,25,4};int[] order=PlayerRecords.Ranking(scores);
            Check(order[0]==2&&order[1]==3&&order[2]==1&&order[3]==4,"stable score ranking");
            Check(PlayerRecords.Rank(scores,1)==3,"local rank");
            var titles=new System.Collections.Generic.HashSet<string>();
            foreach(BattlePlanet planet in Enum.GetValues(typeof(BattlePlanet)))
                Check(titles.Add(PlanetBiomeRules.TitleOf(planet)),"unique planet title");
            byte[] hello=MatchProtocol.Packet(MatchProtocol.Hello,w=>
            {w.Write(MatchProtocol.Version);w.Write((byte)3);w.Write("");w.Write("Игрок-77");});
            using(var reader=MatchProtocol.Reader(hello))
            {
                Check(reader.ReadByte()==MatchProtocol.Hello&&reader.ReadInt32()==MatchProtocol.Version,"hello version");
                Check(reader.ReadByte()==3&&reader.ReadString()==""&&PlayerRecords.ValidNickname(reader.ReadString()),"nickname round trip");
                MatchProtocol.End(reader);
            }
            Debug.Log("PLAYER_RECORDS_SMOKE_PASS cases="+cases+" planets="+PlanetBiomeRules.Count+" protocol="+MatchProtocol.Version);
        }
    }
}
#endif
