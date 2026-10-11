#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    // Resources materials keep required shaders in Android builds.
    public static class RuntimeShaderAssets
    {
        public static void Ensure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            Create("PlanetStandard", "Standard");
            Create("PlanetParticles", "Particles/Standard Unlit");
            Create("PlanetSprite", "Sprites/Default");
            AssetDatabase.SaveAssets();
        }

        private static void Create(string assetName, string shaderName)
        {
            string path = "Assets/Resources/" + assetName + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                return;

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new Exception("Required Unity shader not found in Editor: " + shaderName);

            var material = new Material(shader) { name = assetName };
            AssetDatabase.CreateAsset(material, path);
            Debug.Log("BRAINROT_MATERIAL_CREATED=" + path);
        }
    }
}
#endif
