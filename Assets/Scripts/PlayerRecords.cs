using System;
using System.Text;
using UnityEngine;

namespace BrainrotPlanet
{
    public enum NicknameIssue { None, Empty, TooShort, TooLong, Reserved, Inappropriate }

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
        public const int MinNicknameLength=2,MaxNicknameLength=14,HistoryLimit=8;
        private const string NicknameKey="BrainrotNickname";
        private const string NicknameSetupKey="BrainrotNicknameSetup";
        private static readonly string[] BotNames={"","БОТ ИСКРА","БОТ ВИХРЬ","БОТ КОМЕТА","БОТ РАКЕТА"};
        private static readonly string[] BlockedRoots={
            "хуй","хуе","хуя","пизд","ебан","ебат","ебло","бляд","мудак","говно",
            "fuck","fuk","fack","bitch","nigger","nigga"
        };
        private static readonly string[] BlockedWords={"сука","shit","cunt","whore"};

        public static bool NicknameSetupComplete
        {
            get
            {
                if(PlayerPrefs.GetInt(NicknameSetupKey,0)!=0)return true;
                string saved=PlayerPrefs.GetString(NicknameKey,"");
                return saved.Length>0&&!LooksGeneratedNickname(saved);
            }
        }

        public static string LoadNickname()
        {
            string saved=PlayerPrefs.GetString(NicknameKey,"");
            if(ValidNickname(saved))return saved;
            uint hash=unchecked((uint)SystemInfo.deviceUniqueIdentifier.GetHashCode());
            string generated="ИГРОК "+(1000+hash%9000);
            PlayerPrefs.SetString(NicknameKey,generated);PlayerPrefs.Save();
            return generated;
        }
        public static string SaveNickname(string value)
        {
            return TrySaveNickname(value,out string clean,out _)?clean:LoadNickname();
        }
        public static bool TrySaveNickname(string value,out string clean,out NicknameIssue issue)
        {
            issue=ValidateNicknameInput(value,out clean);
            if(issue!=NicknameIssue.None)return false;
            PlayerPrefs.SetString(NicknameKey,clean);
            PlayerPrefs.SetInt(NicknameSetupKey,1);
            PlayerPrefs.Save();
            return true;
        }
        public static void AcceptGeneratedNickname()
        {
            LoadNickname();
            PlayerPrefs.SetInt(NicknameSetupKey,1);PlayerPrefs.Save();
        }
        public static string SanitizeNickname(string value)
        {
            string clean=FilterNickname(value);
            return clean.Length>0?clean:"ИГРОК";
        }
        private static string FilterNickname(string value)
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
            return new string(buffer,0,count).Trim();
        }
        public static NicknameIssue ValidateNicknameInput(string value,out string clean)
        {
            clean=FilterNickname(value);
            if(string.IsNullOrWhiteSpace(value)||clean.Length==0)return NicknameIssue.Empty;
            if(value.Length>MaxNicknameLength)return NicknameIssue.TooLong;
            if(clean.Length<MinNicknameLength)return NicknameIssue.TooShort;
            if(IsReservedNickname(clean))return NicknameIssue.Reserved;
            if(ContainsBlockedLanguage(clean))return NicknameIssue.Inappropriate;
            return NicknameIssue.None;
        }
        public static bool ValidNickname(string value)
        {
            NicknameIssue issue=ValidateNicknameInput(value,out string clean);
            return issue==NicknameIssue.None&&clean==value;
        }
        public static bool ValidPlayerLabel(string value)
        {
            // Protocol 9 clients that predate local moderation may still have a
            // structurally valid one-character or now-blocked guest name. The
            // local input remains strict, while an already accepted roster must
            // stay readable instead of disconnecting the whole match.
            return !string.IsNullOrEmpty(value)&&value.Length<=MaxNicknameLength&&
                   FilterNickname(value)==value;
        }
        public static string NicknameIssueMessage(NicknameIssue issue)
        {
            switch(issue)
            {
                case NicknameIssue.Empty:return "ВВЕДИТЕ ИГРОВОЙ НИК";
                case NicknameIssue.TooShort:return "НИК ДОЛЖЕН БЫТЬ НЕ КОРОЧЕ 2 СИМВОЛОВ";
                case NicknameIssue.TooLong:return "НИК ДОЛЖЕН БЫТЬ НЕ ДЛИННЕЕ 14 СИМВОЛОВ";
                case NicknameIssue.Reserved:return "ЭТО ИМЯ ЗАРЕЗЕРВИРОВАНО ИГРОЙ";
                case NicknameIssue.Inappropriate:return "ВЫБЕРИТЕ ДРУГОЙ ИГРОВОЙ НИК";
                default:return "НИК СОХРАНЁН";
            }
        }
        private static bool LooksGeneratedNickname(string value)
        {
            if(value==null||value.Length!=10||!value.StartsWith("ИГРОК ",StringComparison.Ordinal))return false;
            for(int i=6;i<10;i++)if(!char.IsDigit(value[i]))return false;
            return true;
        }
        private static bool IsReservedNickname(string value)
        {
            string lower=value.Trim().ToLowerInvariant();
            if(lower=="бот"||lower.StartsWith("бот ",StringComparison.Ordinal)||
               lower=="bot"||lower.StartsWith("bot ",StringComparison.Ordinal))return true;
            return lower=="server"||lower=="сервер"||lower=="admin"||lower=="админ"||
                   lower=="moderator"||lower=="модератор";
        }
        private static bool ContainsBlockedLanguage(string value)
        {
            string lower=value.ToLowerInvariant().Replace('ё','е');
            var compact=new StringBuilder(lower.Length);
            foreach(char raw in lower)
            {
                char c=raw;
                if(c=='0')c='o';else if(c=='1')c='i';else if(c=='3')c='e';
                else if(c=='4')c='a';else if(c=='5')c='s';else if(c=='7')c='t';
                if(char.IsLetterOrDigit(c))compact.Append(c);
            }
            string joined=compact.ToString();
            foreach(string root in BlockedRoots)if(joined.Contains(root))return true;
            string[] words=lower.Split(new[]{' ','_','-'},StringSplitOptions.RemoveEmptyEntries);
            foreach(string word in words)foreach(string blocked in BlockedWords)if(word==blocked)return true;
            return false;
        }
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
