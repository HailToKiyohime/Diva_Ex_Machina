using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PotaToon.Editor
{
    public class PotaToonMaterialFinderWindow : EditorWindow
    {
        private const string k_PotaToonShaderName = "PotaToon/Toon";
        private const string k_EyeShaderName = "PotaToon/Eye";
        private static List<Material> s_FoundMaterials = new List<Material>();
        private static List<Material> s_FoundEyeMaterials = new List<Material>();
        private Vector2 m_ScrollPosition;

        [MenuItem("PotaToon/View all materials Using PotaToon Shader in this scene")]
        public static void ShowWindow()
        {
            PotaToonMaterialFinderWindow window = GetWindow<PotaToonMaterialFinderWindow>("PotaToon Shader Material Finder");
            window.SearchMaterials(s_FoundMaterials, k_PotaToonShaderName);
            window.SearchMaterials(s_FoundEyeMaterials, k_EyeShaderName);
        }

        private void OnGUI()
        {
            GUILayout.Label($"🔍 Searching for Shader: {k_PotaToonShaderName}", EditorStyles.boldLabel);

            if (GUILayout.Button("Refresh List"))
            {
                SearchMaterials(s_FoundMaterials, k_PotaToonShaderName);
                SearchMaterials(s_FoundEyeMaterials, k_EyeShaderName);
            }

            GUILayout.Space(10);

            if (s_FoundMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox("No materials using this shader were found in the scene.", MessageType.Info);
            }

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            foreach (var mat in s_FoundMaterials)
            {
                EditorGUILayout.BeginHorizontal("box");

                float previewSize = 35f;

                EditorGUILayout.BeginVertical(GUILayout.Width(previewSize), GUILayout.Height(previewSize));
                Texture previewTexture = AssetPreview.GetAssetPreview(mat);
                if (previewTexture)
                {
                    Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
                    GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUILayout.Label("No Preview", GUILayout.Width(previewSize), GUILayout.Height(previewSize));
                }
                EditorGUILayout.EndVertical();
                
                GUILayout.Label(mat.name, GUILayout.ExpandWidth(true));
                
                EditorGUILayout.BeginHorizontal(GUILayout.Width(180));
                if (GUILayout.Button("Select", GUILayout.Width(70)))
                {
                    Selection.activeObject = mat;
                    EditorGUIUtility.PingObject(mat);
                }

                if (GUILayout.Button("Find in Project", GUILayout.Width(100)))
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(mat);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();
            }
            
            GUILayout.Label($"🔍 Searching for Shader: {k_EyeShaderName}", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            foreach (var mat in s_FoundEyeMaterials)
            {
                EditorGUILayout.BeginHorizontal("box");

                float previewSize = 35f;

                EditorGUILayout.BeginVertical(GUILayout.Width(previewSize), GUILayout.Height(previewSize));
                Texture previewTexture = AssetPreview.GetAssetPreview(mat);
                if (previewTexture)
                {
                    Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
                    GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUILayout.Label("No Preview", GUILayout.Width(previewSize), GUILayout.Height(previewSize));
                }
                EditorGUILayout.EndVertical();
                
                GUILayout.Label(mat.name, GUILayout.ExpandWidth(true));
                
                EditorGUILayout.BeginHorizontal(GUILayout.Width(180));
                if (GUILayout.Button("Select", GUILayout.Width(70)))
                {
                    Selection.activeObject = mat;
                    EditorGUIUtility.PingObject(mat);
                }

                if (GUILayout.Button("Find in Project", GUILayout.Width(100)))
                {
                    EditorGUIUtility.PingObject(mat);
                    EditorUtility.FocusProjectWindow();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void SearchMaterials(List<Material> targetMaterials, string targetShaderName)
        {
            targetMaterials.Clear();
            Shader targetShader = Shader.Find(targetShaderName);

            if (targetShader == null)
            {
                Debug.LogError($"[PotaToon] Shader '{targetShaderName}' not found!");
                return;
            }

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.shader == targetShader && !targetMaterials.Contains(mat))
                    {
                        targetMaterials.Add(mat);
                    }
                }
            }

            Debug.Log($"<color=cyan>[PotaToon] Found {targetMaterials.Count} materials using shader '{targetShaderName}'</color>");
        }
    }
}