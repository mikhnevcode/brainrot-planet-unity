#if UNITY_EDITOR
using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace BrainrotPlanet.Editor
{
    public sealed class ShortHapticsPermission : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder=>10;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string file=Path.Combine(path,"src/main/AndroidManifest.xml");
            var xml=new XmlDocument();xml.Load(file);
            const string android="http://schemas.android.com/apk/res/android";
            foreach(XmlNode node in xml.DocumentElement.SelectNodes("uses-permission"))
                if(node.Attributes["name",android]?.Value=="android.permission.VIBRATE")return;
            var permission=xml.CreateElement("uses-permission");
            permission.SetAttribute("name",android,"android.permission.VIBRATE");
            xml.DocumentElement.AppendChild(permission);xml.Save(file);
        }
    }
}
#endif
