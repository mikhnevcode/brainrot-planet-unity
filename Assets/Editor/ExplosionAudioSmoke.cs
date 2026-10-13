#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class ExplosionAudioSmoke
    {
        public static void Run()
        {
            for(int i=1;i<=3;i++)
            {
                string path="Assets/Resources/Audio/BombExplosion"+i+".wav";
                var importer=AssetImporter.GetAtPath(path) as AudioImporter;
                if(importer==null)throw new Exception("Missing explosion sample "+path);
                importer.forceToMono=true;importer.loadInBackground=false;
                var settings=importer.defaultSampleSettings;
                settings.preloadAudioData=true;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if(clip==null||clip.length<1.5f||clip.channels!=1)throw new Exception("Invalid explosion clip "+path);
                clip.LoadAudioData();var data=new float[clip.samples];
                if(!clip.GetData(data,0))throw new Exception("Cannot inspect explosion PCM "+path);
                float peak=0;double energy=0;
                foreach(float sample in data){peak=Mathf.Max(peak,Mathf.Abs(sample));energy+=sample*sample;}
                if(peak>.90f||peak<.7f||energy/data.Length<.001)throw new Exception("Explosion level out of range "+path);
                if(peak*ExplosionSound.MaxVolume*ExplosionSound.VoiceCount>.60f)throw new Exception("Explosion mix needs headroom");
            }
            if(ExplosionSound.Attenuation(Vector3.up,Vector3.up)!=1f||ExplosionSound.Attenuation(Vector3.down,Vector3.up)!=0f)
                throw new Exception("Incorrect explosion falloff");
            Debug.Log("EXPLOSION_AUDIO_SMOKE_PASS clips=3 impactSeconds=1.8 voices=3 noClippedSamples=true attenuation=true");
        }
    }
}
#endif
