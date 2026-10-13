using UnityEngine;

namespace BrainrotPlanet
{
    public enum GameCue { Capture, Hit, Ready, Power, Jump, Win, Lose, Click }
    // Small original synthesized sounds: no third-party recordings or downloads.
    public sealed class GameSound : MonoBehaviour
    {
        private static GameSound instance;
        private AudioSource voice,music;
        private readonly AudioClip[] clips=new AudioClip[8];
        public static bool Enabled { get; private set; }=true;
        public static bool Haptics { get; private set; }
        private float lastHit;
        public static void SetEnabled(bool on)
        {
            Enabled=on;PlayerPrefs.SetInt("BrainrotSound",on?1:0);
            PlayerPrefs.Save();
            if(instance!=null)instance.music.mute=!on;
        }
        public static void SetHaptics(bool on){Haptics=on;PlayerPrefs.SetInt("BrainrotHaptics",on?1:0);PlayerPrefs.Save();}
        private void Awake()
        {
            instance=this;Enabled=PlayerPrefs.GetInt("BrainrotSound",1)!=0;
            Haptics=PlayerPrefs.GetInt("BrainrotHaptics",0)!=0;
            voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;
            music.loop=true;music.volume=.055f;music.mute=!Enabled;
            for(int i=0;i<clips.Length;i++)clips[i]=Tone(i);
            music.clip=Music();music.Play();
        }
        public static void Play(GameCue cue,int streak=0)
        {
            if(instance==null)return;
            if(cue==GameCue.Hit && Time.unscaledTime-instance.lastHit<.35f)return;
            if(cue==GameCue.Hit)instance.lastHit=Time.unscaledTime;
            if(Enabled){instance.voice.pitch=cue==GameCue.Capture?1f+.035f*streak:1f;
                instance.voice.PlayOneShot(instance.clips[(int)cue],cue==GameCue.Click?.16f:.34f);}
#if UNITY_ANDROID && !UNITY_EDITOR
            if(Haptics&&(cue==GameCue.Hit||cue==GameCue.Win))
            {
                // Android's duration API avoids the long default Handheld vibration.
                try{using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using(var vibrator=activity.Call<AndroidJavaObject>("getSystemService","vibrator"))
                    vibrator.Call("vibrate",(long)24);}
                catch(System.Exception){ /* Devices without a vibrator remain playable. */ }
            }
#endif
        }
        private static AudioClip Tone(int kind)
        {
            int rate=22050;float length=kind==5?.8f:kind==6?.55f:.23f;
            float[] data=new float[(int)(rate*length)];
            float phase=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate,u=t/length;
                float frequency=kind==1?Mathf.Lerp(160,55,u):kind==6?Mathf.Lerp(380,130,u):
                    kind==5?440*Mathf.Pow(2,Mathf.Floor(u*4)*.25f):
                    Mathf.Lerp(380+kind*75,850+kind*100,u);
                phase+=frequency/rate*2*Mathf.PI;
                float envelope=Mathf.Min(1,t/.008f)*Mathf.Pow(1-u,2);
                data[i]=Mathf.Sin(phase)*envelope*.65f;
            }
            var clip=AudioClip.Create("Original cue "+kind,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        private static AudioClip Music()
        {
            int rate=22050;float beat=.4f;int count=(int)(rate*beat*32);var data=new float[count];
            int[] notes={0,7,12,7,3,10,15,10,5,12,17,12,3,10,15,7};
            for(int i=0;i<count;i++)
            {
                float t=(float)i/rate;int step=(int)(t/beat);float local=t%beat;
                float freq=220*Mathf.Pow(2,notes[step%notes.Length]/12f);
                float env=Mathf.Min(1,local/.012f)*Mathf.Exp(-local*10);
                float pulse=Mathf.Sin(2*Mathf.PI*freq*local)+.22f*Mathf.Sin(4*Mathf.PI*freq*local);
                float bass=Mathf.Sin(2*Mathf.PI*55*local)*Mathf.Exp(-local*15);
                data[i]=(pulse*.22f+bass*.35f)*env;
            }
            var clip=AudioClip.Create("Original planet loop",count,1,rate,false);clip.SetData(data,0);return clip;
        }
        private void OnDestroy(){foreach(var c in clips)if(c!=null)Destroy(c);if(music!=null&&music.clip!=null)Destroy(music.clip);if(instance==this)instance=null;}
    }
}
