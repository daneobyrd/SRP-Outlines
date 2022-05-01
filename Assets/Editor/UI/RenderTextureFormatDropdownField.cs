using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class RenderTextureFormatDropdownField : DropdownField
{
    public new class UxmlFactory : UxmlFactory<RenderTextureFormatDropdownField, UxmlTraits>
    {
    }
    
    public new class UxmlTraits : BaseField<string>.UxmlTraits
    {
        public override void Init(VisualElement visualElement, IUxmlAttributes attributes, CreationContext context)
        {
            base.Init(visualElement, attributes, context);
            var dropdownField = (RenderTextureFormatDropdownField) visualElement;

            var choices = Enum.GetNames(typeof(RenderTextureFormat)).ToList();
            choices.Remove("Depth");
            choices.Remove("Shadowmap");
            
            dropdownField.choices = choices;
            dropdownField.index = 0;

            Enum.Parse(typeof(RenderTextureFormat), dropdownField.value);
        }
        
        public UxmlTraits()
        {
            
        }
    }
}
