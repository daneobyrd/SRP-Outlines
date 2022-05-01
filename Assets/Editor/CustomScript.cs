using System;
using UnityEngine;

public class CustomScript : MonoBehaviour
{ 
    public string textValue;
    public float floatValue;
    [HideInInspector] public RenderTextureFormat colorFormat;

    public void LogColorFormatButton()
    {
        Debug.Log($"CustomScript - colorFormat index: {(int)colorFormat}");
    }
}