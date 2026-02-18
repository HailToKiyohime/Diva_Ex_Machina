using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BoolMatrix))]
public class BoolMatrixDrawer : PropertyDrawer
{
    private const int CellSize = 20;
    private const int Padding = 2;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var widthProp = property.FindPropertyRelative("width");
        var heightProp = property.FindPropertyRelative("height");
        var cellsProp = property.FindPropertyRelative("cells");

        Rect header = new Rect(position.x, position.y, position.width, 18);
        EditorGUI.LabelField(header, label);

        Rect sizeRect = new Rect(position.x, position.y + 20, position.width, 18);

        float half = sizeRect.width / 2f;

        widthProp.intValue = EditorGUI.IntField(new Rect(sizeRect.x, sizeRect.y, half - 5, 18), "W", widthProp.intValue);
        heightProp.intValue = EditorGUI.IntField(new Rect(sizeRect.x + half, sizeRect.y, half - 5, 18), "H", heightProp.intValue);

        int width = Mathf.Max(0, widthProp.intValue);
        int height = Mathf.Max(0, heightProp.intValue);

        int neededSize = width * height;

        if (cellsProp.arraySize != neededSize)
        {
            cellsProp.arraySize = neededSize;
        }

        Rect gridStart = new Rect(position.x, position.y + 45, CellSize, CellSize);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                var cell = cellsProp.GetArrayElementAtIndex(index);

                Rect cellRect = new Rect(
                    gridStart.x + x * (CellSize + Padding),
                    gridStart.y + y * (CellSize + Padding),
                    CellSize,
                    CellSize
                );

                cell.boolValue = EditorGUI.Toggle(cellRect, cell.boolValue);
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int h = property.FindPropertyRelative("height").intValue;
        return 50 + h * (CellSize + Padding);
    }
}
