using UnityEngine;

namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private string playerNickname="",nicknameDraft="";
        private readonly string[] networkNames={"","","","",""};
        private bool statisticsOpen,newPlanetRecord;
        private Vector2 statisticsScroll;

        private void SetupRecords()
        {
            playerNickname=PlayerRecords.LoadNickname();nicknameDraft=playerNickname;
            for(int team=1;team<=4;team++)networkNames[team]=PlayerRecords.BotName(team);
            networkNames[1]=playerNickname;
        }
        private void PrepareMatchNames()
        {
            if(IsNetworkMatch)return;
            networkNames[1]=playerNickname;
            for(int team=2;team<=4;team++)networkNames[team]=PlayerRecords.BotName(team);
        }
        private string PlayerLabel(int team)
        {
            if(team<1||team>4)return "ИГРОК";
            string value=networkNames[team];
            return string.IsNullOrEmpty(value)?PlayerRecords.BotName(team):value;
        }
        private void SaveNicknameDraft()
        {
            playerNickname=PlayerRecords.SaveNickname(nicknameDraft);
            nicknameDraft=playerNickname;
            if(!IsNetworkMatch)networkNames[1]=playerNickname;
            GameSound.Play(GameCue.Click);
            Debug.Log("PLAYER_NICKNAME_SAVED length="+playerNickname.Length);
        }
        private void RecordLocalResult(int winningTeam)
        {
            int rank=PlayerRecords.Rank(scores,LocalTeam);
            PlayerRecordUpdate update=PlayerRecords.Record(battlePlanet,scores[LocalTeam],rank,IsNetworkMatch);
            newRecord=update.OverallBest;newPlanetRecord=update.PlanetBest;
            Debug.Log("PLAYER_RESULT_RECORDED planet="+(int)battlePlanet+" rank="+rank+
                " area="+scores[LocalTeam].ToString("F2")+" winner="+winningTeam+
                " network="+IsNetworkMatch+" planetBest="+newPlanetRecord);
        }
        private void DrawResultRanking(float w)
        {
            int[] order=PlayerRecords.Ranking(scores);
            float top=157f,rowHeight=29f;
            Rounded(new Rect(15,top,w-30,139),new Color(.035f,.05f,.12f,.88f));
            GUI.Label(new Rect(24,top+4,w-48,20),"ИТОГОВЫЙ РЕЙТИНГ",captionStyle);
            for(int rank=0;rank<order.Length;rank++)
            {
                int team=order[rank];float y=top+25+rank*rowHeight;
                if(team==LocalTeam)Rounded(new Rect(22,y,w-44,rowHeight-2),new Color(Colors.Team(team).r,Colors.Team(team).g,Colors.Team(team).b,.36f));
                GUI.Label(new Rect(27,y,28,rowHeight),""+(rank+1),smallStyle);
                GUIStyle left=new GUIStyle(smallStyle){alignment=TextAnchor.MiddleLeft,fontStyle=team==LocalTeam?FontStyle.Bold:FontStyle.Normal};
                GUI.Label(new Rect(57,y,w-150,rowHeight),PlayerLabel(team)+(team==LocalTeam?"  •  ВЫ":""),left);
                GUIStyle right=new GUIStyle(smallStyle){alignment=TextAnchor.MiddleRight,fontStyle=FontStyle.Bold};
                right.normal.textColor=Colors.Team(team);
                GUI.Label(new Rect(w-104,y,77,rowHeight),scores[team].ToString("F1")+"%",right);
            }
        }
        private void DrawStatistics(float w,float h)
        {
            InitPolishStyles();
            float top=Mathf.Max(8,(Screen.height-Screen.safeArea.yMax)/guiScale);
            float bottom=h-Mathf.Max(10,Screen.safeArea.yMin/guiScale+5);
            Plate(new Rect(0,0,w,h),new Color(.025f,.035f,.095f));
            DrawGlow(new Rect(w-205,top-90,340,340),new Color(.36f,.18f,.82f,.24f));
            GUI.Label(new Rect(20,top+8,w-40,36),"РЕКОРДЫ И ИСТОРИЯ",headerStyle);
            GUI.Label(new Rect(22,top+47,w-44,24),playerNickname,heroNameStyle);
            GUI.Label(new Rect(22,top+73,w-44,24),"ИСТОРИЯ: МАТЧИ "+PlayerRecords.Matches+"   •   ПОБЕДЫ "+PlayerRecords.TrackedWins+"   •   ТОП-3 "+PlayerRecords.Podiums,smallStyle);
            GUI.Label(new Rect(22,top+96,w-44,24),"ВСЕГО ПОБЕД "+PlayerRecords.Wins+"   •   РЕКОРД "+PlayerRecords.BestArea.ToString("F1")+"%   •   СРЕДНИЙ "+PlayerRecords.AverageArea.ToString("F1")+"%",captionStyle);

            Rect view=new Rect(14,top+127,w-28,Mathf.Max(120,bottom-top-200));
            float contentHeight=36+PlanetBiomeRules.Count*49+38+Mathf.Max(1,PlayerRecords.HistoryCount)*43+10;
            statisticsScroll=GUI.BeginScrollView(view,statisticsScroll,new Rect(0,0,view.width-18,contentHeight));
            float y=0;
            GUIStyle left=new GUIStyle(smallStyle){alignment=TextAnchor.MiddleLeft};
            GUIStyle right=new GUIStyle(captionStyle){alignment=TextAnchor.MiddleRight};
            GUI.Label(new Rect(8,y,view.width-36,28),"ПО КАЖДОЙ ПЛАНЕТЕ",heroSkillStyle);y+=31;
            for(int i=0;i<PlanetBiomeRules.Count;i++)
            {
                BattlePlanet planet=(BattlePlanet)i;Color accent=PlanetBiomeRules.OrbitAccent(i);
                Rounded(new Rect(5,y,view.width-31,42),new Color(accent.r*.32f,accent.g*.32f,accent.b*.32f,.96f));
                GUI.Label(new Rect(14,y+2,view.width-48,20),PlanetBiomeRules.TitleOf(planet),left);
                GUI.Label(new Rect(14,y+20,view.width-48,19),"МАТЧИ "+PlayerRecords.PlanetMatches(planet)+"   •   ПОБЕДЫ "+PlayerRecords.PlanetWins(planet)+"   •   РЕКОРД "+PlayerRecords.PlanetBest(planet).ToString("F1")+"%",right);
                y+=49;
            }
            y+=4;GUI.Label(new Rect(8,y,view.width-36,28),"ПОСЛЕДНИЕ МАТЧИ",heroSkillStyle);y+=31;
            if(PlayerRecords.HistoryCount==0)
            {GUI.Label(new Rect(8,y,view.width-36,34),"История появится после первого завершённого матча.",smallStyle);}
            else for(int i=0;i<PlayerRecords.HistoryCount;i++)
            {
                PlayerMatchRecord record=PlayerRecords.History(i);
                Color accent=PlanetBiomeRules.OrbitAccent((int)record.Planet);
                Rounded(new Rect(5,y,view.width-31,36),new Color(.07f,.095f,.18f,.95f));
                GUI.Label(new Rect(14,y,view.width-158,36),PlanetBiomeRules.TitleOf(record.Planet),left);
                string mode=record.Network?"СЕТЬ":"БОТЫ";
                GUIStyle result=new GUIStyle(right){fontStyle=record.Rank==1?FontStyle.Bold:FontStyle.Normal};
                result.normal.textColor=record.Rank==1?accent:Color.white;
                GUI.Label(new Rect(view.width-174,y,143,36),record.Rank+" МЕСТО  •  "+record.Area.ToString("F1")+"%  •  "+mode,result);
                y+=43;
            }
            GUI.EndScrollView();
            if(FlatButton(new Rect(24,bottom-55,w-48,50),"НАЗАД",new Color(.18f,.20f,.33f)))statisticsOpen=false;
        }
    }
}
