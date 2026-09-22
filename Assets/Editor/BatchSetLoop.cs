using UnityEditor;
using UnityEngine;

public class BatchSetLoop
{
    [MenuItem("Tools/Animation/Set All Clips Loop Time ON")]
    static void SetLoopOn() { Apply(true); }

    [MenuItem("Tools/Animation/Set All Clips Loop Time OFF")]
    static void SetLoopOff() { Apply(false); }

    static void Apply(bool loop)
    {
        int files = 0, clips = 0;

        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            // 已自訂過就沿用，否則從匯入器預設值起手
            ModelImporterClipAnimation[] anims = importer.clipAnimations;
            if (anims == null || anims.Length == 0)
                anims = importer.defaultClipAnimations;

            foreach (var a in anims)
            {
                a.loopTime = loop;
                clips++;
            }

            importer.clipAnimations = anims;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            files++;
        }

        Debug.Log($"Loop Time = {loop} → {files} 個檔案、{clips} 個 clip");
    }
}