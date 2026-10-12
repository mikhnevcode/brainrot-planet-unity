#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class AiBalanceSmoke
    {
        [MenuItem("Brainrot Planet/10. Verify encounter balance")]
        public static void Run()
        {
            float easy=AiBalanceTuning.SpawnAngle(AiDifficulty.Easy);
            float medium=AiBalanceTuning.SpawnAngle(AiDifficulty.Medium);
            float hard=AiBalanceTuning.SpawnAngle(AiDifficulty.Hard);
            if(!(AiBalanceTuning.StartGrace(AiDifficulty.Easy)>
                 AiBalanceTuning.StartGrace(AiDifficulty.Medium) &&
                 AiBalanceTuning.StartGrace(AiDifficulty.Medium)>
                 AiBalanceTuning.StartGrace(AiDifficulty.Hard)))
                throw new Exception("Beginner landing protection must be monotonic");
            if(!(AiBalanceTuning.RespawnGrace(AiDifficulty.Easy)>
                 AiBalanceTuning.RespawnGrace(AiDifficulty.Medium) &&
                 AiBalanceTuning.RespawnGrace(AiDifficulty.Medium)>
                 AiBalanceTuning.RespawnGrace(AiDifficulty.Hard)))
                throw new Exception("Recovery grace must be monotonic");
            if(!(easy>medium && medium>hard))
                throw new Exception("Encounter distances not monotonic");
            Vector3 centre=new Vector3(.91f,.20f,.33f).normalized;
            Vector3 east=Vector3.Cross(Vector3.up,centre).normalized;
            Vector3 north=Vector3.Cross(centre,east).normalized;
            foreach(AiDifficulty level in new[]{
                AiDifficulty.Easy,AiDifficulty.Medium,AiDifficulty.Hard})
            {
                if(!AiBalanceTuning.HasSafeStart(level))
                    throw new Exception("Spawn overlap risk "+level);
                float a=AiBalanceTuning.SpawnAngle(level);
                float radial=Mathf.Tan(a*Mathf.Deg2Rad);
                Vector3[] points={centre,(centre+east*radial).normalized,
                    (centre-east*radial).normalized,
                    (centre+north*radial).normalized};
                float shortest=180f;
                for(int i=0;i<points.Length;i++)
                for(int j=i+1;j<points.Length;j++)
                    shortest=Mathf.Min(shortest,
                        Vector3.Angle(points[i],points[j]));
                if(shortest<=AiBalanceTuning.CoreRadiusDegrees*2f+2f)
                    throw new Exception("Initial home cores overlap "+level+
                        " nearest="+shortest);
                Debug.Log("AI_BALANCE_SPAWN level="+level+
                    " playerToBot="+Vector3.Angle(points[0],points[1]).ToString("F2")+
                    " minSeparation="+shortest.ToString("F2"));
            }
            Debug.Log("AI_BALANCE_SMOKE_PASS encounterSpawns=3 equalSpeed=6.5");
        }
    }
}
#endif
