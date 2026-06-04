//*********************************************************************
//  Dependencies: System

//  Dependencies: Unity
using REST.Utils;
using UnityEditor;
using UnityEngine;

//  Dependencies: REST

//*********************************************************************

namespace REST.AdvancedBuildSystem
{
    public class ABS_ProjectSettingsGetter
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR
        private static string s_CustomSettingsPath = "Assets/REST/ProjectSettings/ABS_ProjectSettings.asset";
        private static string s_CustomSettingsFolderPath = "Assets/REST/ProjectSettings";

        private static ABS_ProjectSettings s_Settings = null;
#endif

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Properties
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region Implementation
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR
        public static ABS_ProjectSettings GetSettings()
        {
            if (s_Settings == null)
            {
                s_Settings = GetOrCreateSettings();
                if (s_Settings == null)
                {
                    REST_Logging.Error($"ABS_ProjectSettingsGetter", "Can not find the Project Settings!");
                }
            }
            return s_Settings;
        }
#endif

#if UNITY_EDITOR
        private static ABS_ProjectSettings GetOrCreateSettings()
        {
            if (!System.IO.Directory.Exists(s_CustomSettingsFolderPath))
            {
                System.IO.Directory.CreateDirectory(s_CustomSettingsFolderPath);
            }

            ABS_ProjectSettings settings = AssetDatabase.LoadAssetAtPath<ABS_ProjectSettings>(s_CustomSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ABS_ProjectSettings>();
                AssetDatabase.CreateAsset(settings, s_CustomSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
#endif

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion // Implementation
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}