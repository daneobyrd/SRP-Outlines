using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

[CustomEditor(typeof(CustomScript))]
public class CustomScriptEditor : Editor
{
    private VisualElement _rootElement;
    private VisualTreeAsset _visualTree;
    
    SerializedProperty colorFormat;
    
    private void OnEnable()
    {
        _rootElement = new VisualElement();
        _visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/CustomEditorWindowUXMLTemplate.uxml");
        
        colorFormat = serializedObject.FindProperty("colorFormat");
    }

    public override VisualElement CreateInspectorGUI()
    {
        _rootElement.Clear();

        InspectorElement.FillDefaultInspector(_rootElement, serializedObject, this);
        
        _visualTree.CloneTree(_rootElement);
        
        var dropdown = _rootElement.Q<DropdownField>("RenderTextureFormatDropdownField");
        var button = _rootElement.Q<Button>("LogColorFormatButton");

        var customScript = (CustomScript)serializedObject.targetObject;
        
        dropdown.RegisterValueChangedCallback(OnDropdownValueChanged);
        button.clicked += () => customScript.LogColorFormatButton();

        return _rootElement;
    }

    private void OnDropdownValueChanged(ChangeEvent<string> changeEvent)
    {
        colorFormat.enumValueIndex = (int)Enum.Parse(typeof(RenderTextureFormat), changeEvent.newValue);
        serializedObject.ApplyModifiedProperties();
    }
}
