using UnityEngine;

namespace BrainrotPlanet
{
    // Dedicated voices keep capture/UI pitch changes out of the explosion tail.
    // Three bounded voices also keep simultaneous impacts below the mix ceiling.
    public sealed class ExplosionSound : MonoBehaviour
    {
        public const int VoiceCount=3;
        public const float MaxVolume=.22f;
        private readonly AudioClip[] clips=new AudioClip[3];
        private readonly AudioSource[] voices=new AudioSource[VoiceCount];
        private int next,variant;
        private bool paused;

        private void Awake()
        {
            for(int i=0;i<clips.Length;i++)
            {
                clips[i]=Resources.Load<AudioClip>("Audio/BombExplosion"+(i+1));
                if(clips[i]==null)Debug.LogError("Missing bomb explosion audio "+(i+1));
            }
            for(int i=0;i<voices.Length;i++)
            {
                var source=gameObject.AddComponent<AudioSource>();
                source.playOnAwake=false;source.loop=false;source.spatialBlend=0f;
                source.dopplerLevel=0f;source.priority=80;source.mute=!GameSound.Enabled;
                voices[i]=source;
            }
        }
        public static float Attenuation(Vector3 origin,Vector3 listener)
        {
            float fade=Mathf.InverseLerp(12f,65f,Vector3.Angle(origin,listener));
            return (1f-fade)*(1f-fade);
        }
        public void Play(Vector3 at,Vector3 listener,Vector3 cameraRight)
        {
            if(paused||!GameSound.Enabled)
            {
                Debug.Log("BOMB_EXPLOSION_AUDIO muted=true");return;
            }
            float volume=MaxVolume*Attenuation(at,listener);
            if(volume<.005f)return; // Far side of the globe is inaudible.
            int selected=variant++%clips.Length;
            if(clips[selected]==null)return;
            var source=voices[next];next=(next+1)%voices.Length;
            source.Stop();source.clip=clips[selected];source.volume=volume;
            source.pitch=.97f+selected*.03f;
            source.panStereo=Mathf.Clamp(Vector3.Dot((at-listener).normalized,cameraRight)*.6f,-.6f,.6f);
            source.Play();
            Debug.Log("BOMB_EXPLOSION_AUDIO clip="+source.clip.name+" volume="+volume.ToString("F3")+" muted=false");
        }
        public void SetMuted(bool muted)
        {
            foreach(var source in voices)if(source!=null)source.mute=muted;
        }
        public void SetPaused(bool value)
        {
            paused=value;
            foreach(var source in voices)if(source!=null)
            {
                if(value)source.Pause();else source.UnPause();
            }
        }
        public void StopAll()
        {
            foreach(var source in voices)if(source!=null)source.Stop();
            paused=false;next=0;
        }
    }
}
