using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PotaToon.Editor
{
    internal static class PotaToonGUIUtility
    {
        internal const string k_Version = "1.3.6";
        internal static readonly string[] k_Types = new string[] { "General", "Face", "Eye" };
        internal static readonly string[] k_Paths = new string[] { "PotaToon/Toon", "PotaToon/Toon", "PotaToon/Eye" };
        
        public static bool advancedSettingsUnlocked => !s_AdvancedSettingsUnlockedInitialized ? LoadAdvancedSettingUnlocked() : s_AdvancedSettingsUnlocked;
        private static bool s_AdvancedSettingsUnlocked;
        private static bool s_AdvancedSettingsUnlockedInitialized;
        private const string k_AdvancedSettingsUnlockedString = "PotaToonAdvancedSettingsUnlocked";
    
        internal static bool ChangeShader(Material material, int index, bool showNotification = true)
        {
            if (index < 0 || index >= k_Paths.Length)
                return false;

            var newShader = Shader.Find(k_Paths[index]);
            if (newShader == null)
                return false;

            if (material.GetInt("_ToonType") == index)
                return false;
            
            material.shader = newShader;
            material.SetInt("_ToonType", index);
            material.SetInt("_CharShadowType", index == 1 ? 1 : 0);
            material.SetInt("_ReceiveLightShadow", index == 1 ? 0 : 1);
            if (showNotification)
                ShowNotification($"Changed to {k_Types[index]} type.");
            return true;
        }

        internal static void ShowNotification(string text)
        {
            var win = EditorWindow.focusedWindow;
            if (win != null)
                win.ShowNotification(new GUIContent(text));
        }
        
        internal static void SaveAdvancedSettingUnlocked()
        {
            EditorPrefs.SetBool(k_AdvancedSettingsUnlockedString, s_AdvancedSettingsUnlocked);
        }
        
        internal static bool LoadAdvancedSettingUnlocked()
        {
            s_AdvancedSettingsUnlockedInitialized = true;
            s_AdvancedSettingsUnlocked = EditorPrefs.GetBool(k_AdvancedSettingsUnlockedString);
            return s_AdvancedSettingsUnlocked;
        }
        
        internal static void DrawAdvancedSettingsButton()
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 20
            };

            var buttonText = PotaToonGUIUtility.s_AdvancedSettingsUnlocked ? "Lock Advanced Settings" : "Unlock Advanced Settings";
            GUIContent buttonContent = new GUIContent(buttonText, EditorGUIUtility.IconContent(PotaToonGUIUtility.s_AdvancedSettingsUnlocked ? "LockIcon" : "LockIcon-On").image);

            if (GUILayout.Button(buttonContent, buttonStyle))
            {
                if (!PotaToonGUIUtility.s_AdvancedSettingsUnlocked)
                {
                    if (EditorUtility.DisplayDialog(
                            "[PotaToon] Unlock Advanced Settings?",
                            "Are you sure you want to unlock advanced settings?\nThese settings could cause ugly/unexpected look if you are not familiar with each feature. They require a dedicated texture or a setting to use correctly.",
                            "Yes", "No"))
                    {
                        PotaToonGUIUtility.s_AdvancedSettingsUnlocked = true;
                    }
                }
                else
                {
                    PotaToonGUIUtility.s_AdvancedSettingsUnlocked = false;
                }
            }
        }
    }
}