using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>Original primitive-built, meme-inspired silhouettes. No third-party model assets.</summary>
    public static class AvatarArt
    {
        private static Transform Part(Transform parent,string name,PrimitiveType type,Vector3 p,
                                      Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;
            go.transform.SetParent(parent,false);go.transform.localPosition=p;
            go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();if(collider)Object.Destroy(collider);
            go.GetComponent<Renderer>().sharedMaterial=mat;
            return go.transform;
        }
        private static Transform Cube(Transform parent,string name,Vector3 p,Vector3 s,Material m)
            => Part(parent,name,PrimitiveType.Cube,p,s,m);
        private static Transform Oval(Transform parent,string name,Vector3 p,Vector3 s,Material m)
            => Part(parent,name,PrimitiveType.Sphere,p,s,m);
        public static void Make(Transform root,int character,Color territoryColor)
        {
            Transform art=new GameObject("Brainrot avatar / animated").transform;
            art.SetParent(root,false);
            art.localPosition=new Vector3(0,.37f,0);
            Material white=Colors.New(new Color(.97f,.99f,1f));
            Material black=Colors.New(new Color(.06f,.08f,.17f));
            Material red=Colors.New(new Color(1f,.14f,.26f));
            Material blue=Colors.New(new Color(.18f,.80f,.94f));
            Material sand=Colors.New(new Color(.91f,.73f,.40f));
            Material olive=Colors.New(new Color(.36f,.70f,.37f));
            Material belly=Colors.New(new Color(.83f,.94f,.82f));
            Material silver=Colors.New(new Color(.72f,.82f,.92f));
            Material neon=Colors.New(territoryColor,true);
            if(character==0) // Tralalero Tralala: shark on chunky trainers.
            {
                Oval(art,"Shark body",new Vector3(0,.65f,0),new Vector3(.95f,1.15f,.9f),blue);
                Oval(art,"White belly",new Vector3(0,.57f,.42f),new Vector3(.67f,.88f,.22f),white);
                Oval(art,"Shark snout",new Vector3(0,.90f,.53f),new Vector3(.63f,.35f,.72f),blue);
                Transform fin=Cube(art,"Dorsal fin",new Vector3(0,1.30f,-.25f),new Vector3(.19f,.61f,.43f),blue);
                fin.localRotation=Quaternion.Euler(24f,0,0);
                Oval(art,"Eye L",new Vector3(-.27f,1.05f,.53f),new Vector3(.11f,.12f,.10f),black);
                Oval(art,"Eye R",new Vector3(.27f,1.05f,.53f),new Vector3(.11f,.12f,.10f),black);
                Cube(art,"Smile",new Vector3(0,.77f,.77f),new Vector3(.39f,.035f,.045f),black);
                Cube(art,"Sneaker L",new Vector3(-.35f,-.1f,.24f),new Vector3(.50f,.25f,.75f),red);
                Cube(art,"Sneaker R",new Vector3(.35f,-.1f,.24f),new Vector3(.50f,.25f,.75f),red);
                Cube(art,"Sole L",new Vector3(-.35f,-.25f,.30f),new Vector3(.53f,.11f,.76f),white);
                Cube(art,"Sole R",new Vector3(.35f,-.25f,.30f),new Vector3(.53f,.11f,.76f),white);
            }
            else if(character==1) // Bombardiro Crocodilo: crocodile-plane hybrid.
            {
                Oval(art,"Airframe",new Vector3(0,.57f,0),new Vector3(.84f,.70f,1.55f),olive);
                Cube(art,"Right wing",new Vector3(.81f,.54f,-.15f),new Vector3(1.30f,.10f,.51f),silver);
                Cube(art,"Left wing",new Vector3(-.81f,.54f,-.15f),new Vector3(1.30f,.10f,.51f),silver);
                Cube(art,"Tail fin",new Vector3(0,.97f,-.72f),new Vector3(.12f,.69f,.28f),olive);
                Oval(art,"Croc snout",new Vector3(0,.65f,1.08f),new Vector3(.82f,.35f,1.18f),olive);
                Cube(art,"Teeth",new Vector3(0,.52f,1.43f),new Vector3(.72f,.10f,.47f),white);
                Oval(art,"Eyes L",new Vector3(-.29f,.91f,.68f),new Vector3(.15f,.19f,.16f),white);
                Oval(art,"Eyes R",new Vector3(.29f,.91f,.68f),new Vector3(.15f,.19f,.16f),white);
                Oval(art,"Pupil L",new Vector3(-.29f,.92f,.79f),new Vector3(.06f,.09f,.07f),black);
                Oval(art,"Pupil R",new Vector3(.29f,.92f,.79f),new Vector3(.06f,.09f,.07f),black);
                Cube(art,"Paint tank",new Vector3(0,1f,-.14f),new Vector3(.37f,.31f,.52f),neon);
            }
            else // Frigo Camelo: desert camel / refrigerator hybrid.
            {
                Cube(art,"Fridge torso",new Vector3(0,.65f,-.06f),new Vector3(.93f,1.10f,.82f),white);
                Cube(art,"Fridge rim",new Vector3(0,.65f,.38f),new Vector3(.92f,1.04f,.075f),silver);
                Cube(art,"Fridge door",new Vector3(0,.65f,.43f),new Vector3(.77f,.94f,.045f),white);
                Cube(art,"Handle",new Vector3(.27f,.70f,.49f),new Vector3(.06f,.32f,.07f),black);
                Oval(art,"Hump",new Vector3(0,1.24f,-.35f),new Vector3(.78f,.65f,.73f),sand);
                Cube(art,"Camel neck",new Vector3(0,1.36f,.43f),new Vector3(.40f,.90f,.36f),sand);
                Oval(art,"Camel head",new Vector3(0,1.82f,.57f),new Vector3(.57f,.37f,.60f),sand);
                Oval(art,"Camel nose",new Vector3(0,1.74f,.89f),new Vector3(.51f,.25f,.44f),belly);
                Oval(art,"Eye left",new Vector3(-.25f,1.93f,.71f),new Vector3(.08f,.10f,.08f),black);
                Oval(art,"Eye right",new Vector3(.25f,1.93f,.71f),new Vector3(.08f,.10f,.08f),black);
                Cube(art,"Front left hoof",new Vector3(-.35f,-.14f,.24f),new Vector3(.23f,.44f,.27f),sand);
                Cube(art,"Front right hoof",new Vector3(.35f,-.14f,.24f),new Vector3(.23f,.44f,.27f),sand);
                Cube(art,"Back left hoof",new Vector3(-.35f,-.14f,-.35f),new Vector3(.23f,.44f,.27f),sand);
                Cube(art,"Back right hoof",new Vector3(.35f,-.14f,-.35f),new Vector3(.23f,.44f,.27f),sand);
            }
            Oval(root,"Paint aura",new Vector3(0,-.42f,0),new Vector3(1.25f,.09f,1.25f),neon);
        }
    }
}
