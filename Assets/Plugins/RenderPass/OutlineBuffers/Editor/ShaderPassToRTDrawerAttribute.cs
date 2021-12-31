/*using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tells a ShaderPassToRTDrawer which ShaderPassToRT class is intended for the GUI inside the ShaderPassToRTDrawer class
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ShaderPassToRTDrawerAttribute : Attribute
{
    internal Type targetPassType;

    /// <summary>
    /// Indicates that the class is a Custom Pass drawer and that it replaces the default Custom Pass GUI.
    /// </summary>
    /// <param name="targetPassType">The Custom Pass Target type.</param>
    public ShaderPassToRTDrawerAttribute(Type targetPassType) => this.targetPassType = targetPassType;
}*/