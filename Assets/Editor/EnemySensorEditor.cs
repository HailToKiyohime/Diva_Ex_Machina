using UnityEditor;
using UnityEngine;

/// <summary>
/// EnemySensor 的 Scene 視圖把手：
///   · 黃色球面上的點：拖曳改近距離感知半徑
///   · 紅色錐尖的點：沿視線方向拖曳改視野距離
///   · 紅色錐側邊的點：左右拖曳改視野角度
/// 所有修改都支援 Undo，也能直接在 Prefab 模式裡編輯。
/// </summary>
[CustomEditor(typeof(EnemySensor))]
public class EnemySensorEditor : Editor
{
    private void OnSceneGUI()
    {
        EnemySensor sensor = (EnemySensor)target;
        serializedObject.Update();

        SerializedProperty proximityRadius = serializedObject.FindProperty("proximityRadius");
        SerializedProperty sightRange = serializedObject.FindProperty("sightRange");
        SerializedProperty sightAngle = serializedObject.FindProperty("sightAngle");

        // ── 近距離感知半徑 ──
        if (sensor.UseProximity)
        {
            Handles.color = sensor.ProximityColor;
            EditorGUI.BeginChangeCheck();
            float r = Handles.RadiusHandle(Quaternion.identity, sensor.ProximityCenter, proximityRadius.floatValue);
            if (EditorGUI.EndChangeCheck())
                proximityRadius.floatValue = Mathf.Max(0f, r);
        }

        // ── 視野錐 ──
        if (sensor.UseSight)
        {
            Handles.color = sensor.SightColor;

            Vector3 apex = sensor.SightApex;
            Quaternion rot = sensor.transform.rotation;
            Vector3 forward = rot * Vector3.forward;
            Vector3 up = rot * Vector3.up;
            Vector3 right = rot * Vector3.right;

            float range = sightRange.floatValue;
            float halfAngle = sightAngle.floatValue * 0.5f;

            // 距離：錐尖正前方的點，只能沿視線方向移動
            Vector3 tip = apex + forward * range;
            float tipSize = HandleUtility.GetHandleSize(tip) * 0.08f;
            EditorGUI.BeginChangeCheck();
            Vector3 newTip = Handles.Slider(tip, forward, tipSize, Handles.DotHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
                sightRange.floatValue = Mathf.Max(0f, Vector3.Dot(newTip - apex, forward));

            // 角度：錐的右側邊緣，在敵人的水平面上移動
            Vector3 edge = apex + (Quaternion.AngleAxis(halfAngle, up) * forward) * range;
            float edgeSize = HandleUtility.GetHandleSize(edge) * 0.08f;
            EditorGUI.BeginChangeCheck();
            Vector3 newEdge = Handles.Slider2D(edge, up, forward, right, edgeSize, Handles.DotHandleCap, Vector2.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 d = newEdge - apex;
                float half = Mathf.Atan2(Mathf.Abs(Vector3.Dot(d, right)), Vector3.Dot(d, forward)) * Mathf.Rad2Deg;
                sightAngle.floatValue = Mathf.Clamp(half * 2f, 0f, 179f);
            }

            Handles.Label(tip + up * (HandleUtility.GetHandleSize(tip) * 0.3f),
                          $"{sightRange.floatValue:0.#} m  /  {sightAngle.floatValue:0.#}°");
        }

        serializedObject.ApplyModifiedProperties();
    }
}
