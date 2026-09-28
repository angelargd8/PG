#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

// Keep Unity's generated manifest and existing XR permissions; add only this normal permission.
public sealed class HeadsetVolumeAndroidManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 100;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        AddPermission(manifestPath);
    }

    public static void AddPermission(string manifestPath)
    {
        const string androidNamespace = "http://schemas.android.com/apk/res/android";
        const string permission = "android.permission.MODIFY_AUDIO_SETTINGS";
        var document = new XmlDocument();
        document.Load(manifestPath);
        XmlElement manifest = document.DocumentElement;
        foreach (XmlNode child in manifest.ChildNodes)
            if (child is XmlElement element && element.Name == "uses-permission" &&
                element.GetAttribute("name", androidNamespace) == permission) return;
        XmlElement entry = document.CreateElement("uses-permission");
        XmlAttribute name = document.CreateAttribute("android", "name", androidNamespace);
        name.Value = permission;
        entry.Attributes.Append(name);
        manifest.AppendChild(entry);
        document.Save(manifestPath);
    }
}
#endif
