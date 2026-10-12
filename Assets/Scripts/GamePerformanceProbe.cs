using System;
using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>
    /// Lightweight development-build frame pacing measurements on Android.
    /// Reports five-second samples without creating objects in Update().
    /// </summary>
    public sealed class GamePerformanceProbe : MonoBehaviour
    {
        private static readonly float[] EdgesMs =
            {9f,13f,17f,22f,28f,34f,50f,67f,100f,160f,250f};
        private readonly int[] distribution = new int[12];
        private PlanetGame game;
        private float elapsed;
        private float maxFrameMs;
        private int frames;
        private int windows;
        private float totalFrames;
        private float totalSeconds;
        private int lastGc0,lastGc1,lastGc2;

        private void Awake()
        {
            game=GetComponent<PlanetGame>();
            lastGc0=GC.CollectionCount(0);
            lastGc1=GC.CollectionCount(1);
            lastGc2=GC.CollectionCount(2);
            Debug.Log("PERF_PROBE_READY device="+SystemInfo.deviceModel+
                " processor="+SystemInfo.processorType+
                " graphics="+SystemInfo.graphicsDeviceName+
                " targetFps="+Application.targetFrameRate);
        }

        private void Update()
        {
            float delta=Time.unscaledDeltaTime;
            if(delta<.00001f)return;
            elapsed+=delta;
            frames++;
            float ms=delta*1000f;
            if(ms>maxFrameMs)maxFrameMs=ms;
            int bucket=0;
            while(bucket<EdgesMs.Length && ms>EdgesMs[bucket])bucket++;
            distribution[bucket]++;
            if(elapsed<5.0f)return;
            int target=Mathf.CeilToInt(frames*.95f);
            int running=0,percentile=distribution.Length-1;
            for(int i=0;i<distribution.Length;i++)
            {
                running+=distribution[i];
                if(running>=target){percentile=i;break;}
            }
            string stage=game==null?"unknown":
                game.State==MatchState.Menu?"menu":
                game.State==MatchState.End?"end":
                game.IsLanding?"landing":"game";
            float p95=percentile<EdgesMs.Length?EdgesMs[percentile]:300f;
            float fps=frames/Mathf.Max(.01f,elapsed);
            int gc0=GC.CollectionCount(0),gc1=GC.CollectionCount(1),gc2=GC.CollectionCount(2);
            long managedBytes=GC.GetTotalMemory(false);
            totalFrames+=frames;totalSeconds+=elapsed;
            Debug.Log("PERF_SAMPLE stage="+stage+
                " fps="+fps.ToString("F2")+
                " p95ms="+p95.ToString("F1")+
                " maxms="+maxFrameMs.ToString("F1")+
                " samples="+frames+
                " avgTotalFps="+(totalFrames/Mathf.Max(.01f,totalSeconds)).ToString("F2")+
                " managedMB="+(managedBytes/(1024f*1024f)).ToString("F2")+
                " gcDelta="+(gc0-lastGc0)+"/"+(gc1-lastGc1)+"/"+(gc2-lastGc2)+
                " window="+(++windows));
            lastGc0=gc0;lastGc1=gc1;lastGc2=gc2;
            frames=0;elapsed=0;maxFrameMs=0;
            System.Array.Clear(distribution,0,distribution.Length);
        }
    }
}
