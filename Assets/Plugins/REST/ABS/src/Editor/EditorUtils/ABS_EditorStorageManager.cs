//*********************************************************************
//  Dependencies: System
using System.IO;

//  Dependencies: Unity
using UnityEngine;
using UnityEditor;

//  Dependencies: REST
using REST.Utils;

//*********************************************************************


namespace REST.AdvancedBuildSystem.Editor
{

    internal class ABS_EditorStorageManager
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Nested class
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public enum ErrorCode
        {
            Success,

            Error_Unkown,
            Error_NotImplemented,

            Error_File_DoesNotExists,

            Error_String_NullOrEmpty,

            Error_GameObeject_NullPtr,
            Error_GameObeject_IsAlreadyPrefab,
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  static properties
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static readonly string s_Extension_Prefab = "prefab";
        public static readonly string s_Extension_Asset = "asset";
        public static readonly string s_Extension_json = "json";
        public static readonly string s_Extension_CS = "cs";

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  text files
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static ErrorCode ReadPersistedDataFile(ref string p_PersistedData, ref string p_PersistedDataPath)
        {
            string path = EditorUtility.OpenFilePanel("Select a File", "", "");

            if (string.IsNullOrEmpty(path))
            {
                return ErrorCode.Error_String_NullOrEmpty;
            }

            if (!File.Exists(path))
            {
                return ErrorCode.Error_File_DoesNotExists;
            }

            p_PersistedDataPath = path;
            p_PersistedData = File.ReadAllText(path);
            return ErrorCode.Success;
        }

        public static ErrorCode SavePersistedDataFile(string p_PersistedDataFileName, string p_PersistedData)
        {
            string filePath = EditorUtility.SaveFilePanel("Save File", Application.dataPath, p_PersistedDataFileName, "");

            if (string.IsNullOrEmpty(filePath))
            {
                return ErrorCode.Error_String_NullOrEmpty;
            }

            try
            {
                File.WriteAllText($"{filePath}.{s_Extension_json}", p_PersistedData);
                REST_Logging.Info("ABS_BuildingEditor", "AddSaveToFileButton", $"Data saved successfully to: {filePath}.{s_Extension_json}");
            }
            catch (IOException e)
            {
                REST_Logging.Error("ABS_BuildingEditor", "AddSaveToFileButton", "Error saving file: " + e.Message);
                return ErrorCode.Error_Unkown;
            }
            return ErrorCode.Success;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Prefab
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static ErrorCode SaveObjectAsPrefab(GameObject p_GameObject, string p_FileName)
        {
            if (string.IsNullOrEmpty(p_FileName))
            {
                return ErrorCode.Error_String_NullOrEmpty;
            }

            if (p_GameObject == null)
            {
                return ErrorCode.Error_GameObeject_NullPtr;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(p_GameObject))
            {
                return ErrorCode.Error_GameObeject_IsAlreadyPrefab;
            }

            string prefabPath = EditorUtility.SaveFilePanelInProject("Save Prefab", p_FileName, s_Extension_Prefab, "Select a path for the new Prefab");
            return SaveObjectAsPrefabWithPathImpl(p_GameObject, prefabPath);
        }

        public static ErrorCode SaveObjectAsPrefabWithPath(GameObject p_GameObject, string p_Path)
        {
            if (string.IsNullOrEmpty(p_Path))
            {
                return ErrorCode.Error_String_NullOrEmpty;
            }

            if (p_GameObject == null)
            {
                return ErrorCode.Error_GameObeject_NullPtr;
            }

            return SaveObjectAsPrefabWithPathImpl(p_GameObject, p_Path);
        }

        private static ErrorCode SaveObjectAsPrefabWithPathImpl(GameObject p_GameObject, string p_Path)
        {
            bool result = false;
            PrefabUtility.SaveAsPrefabAsset(p_GameObject, p_Path, out result);
            return result ? ErrorCode.Success : ErrorCode.Error_Unkown;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  ScriptableObject
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static T SaveScriptableObject<T>(string p_Title, string p_FileName) where T : ScriptableObject
        {
            T myInstance = ScriptableObject.CreateInstance<T>();
            SaveScriptableObject(myInstance, p_Title, p_FileName);
            return myInstance;
        }

        public static T SaveScriptableObjectWithPath<T>(string p_path) where T : ScriptableObject
        {
            T myInstance = ScriptableObject.CreateInstance<T>();
            SaveScriptableObjectWithPath(myInstance, p_path);
            return myInstance;
        }

        public static ErrorCode SaveScriptableObject(ScriptableObject p_Object, string p_Title, string p_FileName)
        {
            string prefabPath = EditorUtility.SaveFilePanelInProject(
                p_Title,
                p_FileName,
                s_Extension_Asset,
                "Select a path");

            if (string.IsNullOrEmpty(prefabPath))
            {
                return ErrorCode.Error_String_NullOrEmpty;
            }

            AssetDatabase.CreateAsset(p_Object, prefabPath);
            AssetDatabase.Refresh();

            return ErrorCode.Success;
        }  
        
        public static ErrorCode SaveScriptableObjectWithPath(ScriptableObject p_Object, string p_Path)
        {
            if (string.IsNullOrEmpty(p_Path))
            {
                return ErrorCode.Error_String_NullOrEmpty;
            }

            AssetDatabase.CreateAsset(p_Object, p_Path);
            AssetDatabase.Refresh();

            return ErrorCode.Success;
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Image
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static Texture2D GetImage(in string p_Name)
        {
            string[] assetPaths = AssetDatabase.FindAssets($"{p_Name}*", null);
            if (assetPaths.Length > 0)
            {
                string fullPath = AssetDatabase.GUIDToAssetPath(assetPaths[0]);
                return (Texture2D)AssetDatabase.LoadAssetAtPath(fullPath, typeof(Texture2D));
            }
            return null;
        }

        public static Texture2D GetPreviewImage(in GameObject p_Object)
        {
            return AssetPreview.GetAssetPreview(p_Object);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //  Utils
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        public static ErrorCode GetSaveFilePath(out string p_Path)
        {
            string prefabPath = EditorUtility.OpenFolderPanel("Select Save Directory", "", "");

            if (string.IsNullOrEmpty(prefabPath))
            {
                p_Path = null;
                return ErrorCode.Error_String_NullOrEmpty;
            }

            p_Path = prefabPath;
            return ErrorCode.Success;
        }

        public static string GetRelativePathFromAssets(string p_PullPath)
        {
            // Find the index of "Assets" in the full path
            int assetsIndex = p_PullPath.IndexOf("Assets");
            if (assetsIndex == -1)
            {
                Debug.LogError("Path does not contain 'Assets'");
                return null;
            }

            // Extract the part after "Assets"
            string relativePath = p_PullPath.Substring(assetsIndex);

            // Ensure the path starts with "Assets/"
            if (!relativePath.StartsWith("Assets/"))
            {
                relativePath = "Assets/" + relativePath;
            }

            return relativePath;
        }
    }
}
