using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowWhenAttribute))]
public class ShowWhenDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return IsVisible(property)
            ? EditorGUI.GetPropertyHeight(property, label, true)
            : -EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (IsVisible(property))
            EditorGUI.PropertyField(position, property, label, true);
    }

    private bool IsVisible(SerializedProperty property)
    {
        string boolFieldName = ((ShowWhenAttribute)attribute).BoolFieldName;
        SerializedProperty boolField = property.serializedObject.FindProperty(boolFieldName);
        return boolField == null || boolField.boolValue;
    }
}
