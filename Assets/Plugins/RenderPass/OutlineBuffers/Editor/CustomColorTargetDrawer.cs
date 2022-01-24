/*using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(CustomColorTarget))]
public class CustomColorTargetDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // Create property container element.
        var container = new VisualElement();

        // Create property fields.
        var enabled = new PropertyField(property.FindPropertyRelative("enabled"));
        var textureName = new PropertyField(property.FindPropertyRelative("textureName"), "Texture Name");
        var lightModeTags = new PropertyField(property.FindPropertyRelative("lightModeTags"));

        var overrideColorFormat = new PropertyField(property.FindPropertyRelative("overrideColorFormat"));
        var colorFormat = new PropertyField(property.FindPropertyRelative("colorFormat"));
        // var displayFormat = new EnumFlagsField("colorFormat").choicesMasks;

        // Add fields to the container.
        container.Add(enabled);
        container.Add(textureName);
        container.Add(lightModeTags);

        return container;
    }
}*/