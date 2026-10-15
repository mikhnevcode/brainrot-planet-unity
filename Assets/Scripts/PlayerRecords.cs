using System;
using UnityEngine;

namespace BrainrotPlanet
{
    public readonly struct PlayerMatchRecord
    {
        public readonly BattlePlanet Planet;
        public readonly float Area;
        public readonly int Rank;
        public readonly bool Network;
        public PlayerMatchRecord(BattlePlanet planet,float area,int rank,bool network)
        {Planet=planet;Area=area;Rank=rank;Network=network;}
    }

    public readonly struct PlayerRecordUpdate
    {
        public readonly bool OverallBest,PlanetBest;
        public PlayerRecordUpdate(bool overallBest,bool planetBest)
        {OverallBest=overallBest;PlanetBest=planetBest;}
    }

    public static class PlayerRecords
    {
        public const int MaxNicknameLength=14,HistoryLimit=8;
        private const string NicknameKey="BrainrotNickname";
        private static readonly string[] BotNames={"","БОТ ИСКРА","БОТ ВИХРЬ","БОТ КОМЕТА","БОТ РАКЕТА"};

        public static string LoadNickname()
        {
            string saved=PlayerPrefs.GetString(NicknameKey,"");
            if(!string.IsNullOrEmpty(saved))return SanitizeNickname(saved);
            uint hash=unchecked((uint)SystemInfo.deviceUniqueIdentifier.GetHashCode());
            string generated="ИГРОК "+(1000+hash%9000);
            PlayerPrefs.SetString(NicknameKey,generated);PlayerPrefs.Save();
            return generated;
        }
        public static string SaveNickname(string value)
        {
            string clean=SanitizeNickname(value);
            PlayerPrefs.SetString(NicknameKey,clean);PlayerPrefs.Save();return clean;
        }
        public static string SanitizeNickname(string value)
        {
            if(value==null)value="";
            char[] buffer=new char[Math.Min(value.Length,MaxNicknameLength)];
            int count=0;bool space=false;
            foreach(char c in value)
            {
                if(count>=MaxNicknameLength)break;
                if(char.IsLetterOrDigit(c)||c=='_'||c=='-')
                {buffer[count++]=c;space=false;continue;}
                if(char.IsWhiteSpace(c)&&count>0&&!space)
                {buffer[count++]=' ';space=true;}
            }
            string clean=new string(buffer,0,count).Trim();
            return clean.Length>0?clean:"ИГРОК";
        }
        public static bool ValidNickname(string value)
        {return !string.IsNullOrEmpty(value)&&value.Length<=MaxNicknameLength&&SanitizeNickname(value)==value;}
        public static string BotName(int team)=>team>=1&&team<=4?BotNames[team]:"БОТ";

        public static int[] Ranking(float[] scores)
        {
            if(scores==null||scores.Length<5)throw new ArgumentException("Four team scores required");
            int[] order={1,2,3,4};
            for(int i=1;i<order.Length;i++)
            {
                int team=order[i],j=i-1;
                while(j>=0&&(scores[team]>scores[order[j]]+.0001f||
                      (Mathf.Abs(scores[team]-scores[order[j]])<=.0001f&&team<order[j])))
                {order[j+1]=order[j];j--;}
                order[j+1]=team;
            }
            return order;
        }
        public static int Rank(float[] scores,int team)
        {
            int[] order=Ranking(scores);
            for(int i=0;i<order.Length;i++)if(order[i]==team)return i+1;
            throw new ArgumentOutOfRangeException(nameof(team));
        }

        private static string PlanetKey(string field,BattlePlanet planet)=>"BrainrotPlanet"+field+"_"+(int)planet;
        private static string HistoryKey(string field,int index)=>"BrainrotHistory"+field+"_"+index;
        public static int Matches=>PlayerPrefs.GetInt("BrainrotMatches",0);
        public static int Wins=>PlayerPrefs.GetInt("BrainrotWins",0);
        public static int TrackedWins=>PlayerPrefs.GetInt("BrainrotTrackedWins",0);
        public static int Podiums=>PlayerPrefs.GetInt("BrainrotPodiums",0);
        public static float BestArea=>PlayerPrefs.GetFloat("BrainrotBestArea",0f);
        public static float AverageArea=>Matches>0?PlayerPrefs.GetFloat("BrainrotAreaTotal",0f)/Matches:0f;
        public static int PlanetMatches(BattlePlanet planet)=>PlayerPrefs.GetInt(PlanetKey("Matches",planet),0);
        public static int PlanetWins(BattlePlanet planet)=>PlayerPrefs.GetInt(PlanetKey("Wins",planet),0);
        public static float PlanetBest(BattlePlanet planet)=>PlayerPrefs.GetFloat(PlanetKey("Best",planet),0f);
        public static int HistoryCount=>Mathf.Clamp(PlayerPrefs.GetInt("BrainrotHistoryCount",0),0,HistoryLimit);

        public static PlayerRecordUpdate Record(BattlePlanet planet,float area,int rank,bool network)
        {
            if((int)planet<0||(int)planet>=PlanetBiomeRules.Count)throw new ArgumentOutOfRangeException(nameof(planet));
            area=Mathf.Clamp(area,0f,100f);rank=Mathf.Clamp(rank,1,4);
            float oldOverall=BestArea,oldPlanet=PlanetBest(planet);
            bool overallBest=area>oldOverall+.01f,planetBest=area>oldPlanet+.01f;
            PlayerPrefs.SetInt("BrainrotMatches",Matches+1);
            PlayerPrefs.SetFloat("BrainrotAreaTotal",PlayerPrefs.GetFloat("BrainrotAreaTotal",0f)+area);
            if(rank==1)
            {
                PlayerPrefs.SetInt("BrainrotWins",Wins+1);
                PlayerPrefs.SetInt("BrainrotTrackedWins",TrackedWins+1);
            }
            if(rank<=3)PlayerPrefs.SetInt("BrainrotPodiums",Podiums+1);
            if(overallBest)PlayerPrefs.SetFloat("BrainrotBestArea",area);
            PlayerPrefs.SetInt(PlanetKey("Matches",planet),PlanetMatches(planet)+1);
            if(rank==1)PlayerPrefs.SetInt(PlanetKey("Wins",planet),PlanetWins(planet)+1);
            if(planetBest)PlayerPrefs.SetFloat(PlanetKey("Best",planet),area);

            int count=HistoryCount;
            for(int i=Mathf.Min(count,HistoryLimit-1);i>0;i--)
            {
                PlayerPrefs.SetInt(HistoryKey("Planet",i),PlayerPrefs.GetInt(HistoryKey("Planet",i-1),0));
                PlayerPrefs.SetFloat(HistoryKey("Area",i),PlayerPrefs.GetFloat(HistoryKey("Area",i-1),0f));
                PlayerPrefs.SetInt(HistoryKey("Rank",i),PlayerPrefs.GetInt(HistoryKey("Rank",i-1),4));
                PlayerPrefs.SetInt(HistoryKey("Network",i),PlayerPrefs.GetInt(HistoryKey("Network",i-1),0));
            }
            PlayerPrefs.SetInt(HistoryKey("Planet",0),(int)planet);
            PlayerPrefs.SetFloat(HistoryKey("Area",0),area);
            PlayerPrefs.SetInt(HistoryKey("Rank",0),rank);
            PlayerPrefs.SetInt(HistoryKey("Network",0),network?1:0);
            PlayerPrefs.SetInt("BrainrotHistoryCount",Mathf.Min(count+1,HistoryLimit));
            PlayerPrefs.Save();
            return new PlayerRecordUpdate(overallBest,planetBest);
        }
        public static PlayerMatchRecord History(int index)
        {
            if(index<0||index>=HistoryCount)throw new ArgumentOutOfRangeException(nameof(index));
            BattlePlanet planet=(BattlePlanet)Mathf.Clamp(PlayerPrefs.GetInt(HistoryKey("Planet",index),0),0,PlanetBiomeRules.Count-1);
            return new PlayerMatchRecord(planet,
                PlayerPrefs.GetFloat(HistoryKey("Area",index),0f),
                Mathf.Clamp(PlayerPrefs.GetInt(HistoryKey("Rank",index),4),1,4),
                PlayerPrefs.GetInt(HistoryKey("Network",index),0)!=0);
        }
    }
}
