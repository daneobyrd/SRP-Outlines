// Source: https://realtimevfx.com/t/lut-gradient-generator-for-unity/15017
// unitypackage: https://drive.google.com/file/d/1GsGsOMCUqRG3AS7tLQCcUtkKCwmhuHKK/view?usp=sharing

using UnityEngine;
using UnityEditor;
using System.IO;

// Usage: Place file in an Editor folder in your project. Window > LUT Generator to access. Make a gradient. 
// Hopefully this'll save some the back and forth between photoshop :)

public class LUTGradientGenerator : EditorWindow
{
    // our lovely gradient
    [SerializeField]
    private Gradient LUTGradient = new Gradient();
   
    // basic window generation
    [MenuItem("Tools/LUT Generator")]
    public static void ShowWindow()
    {
        LUTGradientGenerator window = (LUTGradientGenerator)EditorWindow.GetWindow(typeof(LUTGradientGenerator));
        window.maxSize = new Vector2(264, 144);
        window.minSize = new Vector2(264, 144);
        window.titleContent = new GUIContent("LUT Gen");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.GradientField("Gradient", LUTGradient);

        // create a new texture so we can manipulate the gradient
        Texture2D gradTex = new Texture2D(256, 4, TextureFormat.RGBA32, false);

        // loop through and set each pixel by evaluating the gradient
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 4; j++)
                gradTex.SetPixel(i, j, LUTGradient.Evaluate((float)i / (float)256));            
        }

        // apply them pixels to the texture
        gradTex.Apply();

        // bigger gradient preview for niceness
        EditorGUI.DrawPreviewTexture(new Rect(4, 32, 256, 64), gradTex);

        // Save button
        if (GUI.Button(new Rect(4, 106, 256, 32), "Save Gradient"))
        {
            string path = EditorUtility.SaveFilePanel("Save texture as PNG", "", "LUT_.png", "png");
            if (path.Length != 0)
            {
                SaveGradTexture(gradTex, path);
            }
        }
    }

    // reading and writing window settings to prefs
    protected void OnEnable()
    {
        // Retrieve data if it exists or save the default field info
        var data = EditorPrefs.GetString("LutGeneratorGradData", JsonUtility.ToJson(this, false));
        // Then apply to this window
        JsonUtility.FromJsonOverwrite(data, this);
    }

    protected void OnDisable()
    {
        // get the Json data
        var data = JsonUtility.ToJson(this, false);
        // save it to prefs, simples
        EditorPrefs.SetString("LutGeneratorGradData", data);
    }

    // write the texture data to file
    void SaveGradTexture(Texture2D tex, string path)
    {
        var pngData = tex.EncodeToPNG();
        if (pngData != null)
            File.WriteAllBytes(path, pngData);

        AssetDatabase.Refresh();
    }
}



