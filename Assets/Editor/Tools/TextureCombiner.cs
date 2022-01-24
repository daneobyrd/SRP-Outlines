// Source: https://gist.github.com/HAliss/27d1e4105016c5e97f38db6af41c2509

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureCombiner : EditorWindow {

    //Input textures
    private Texture2D[] textures = new Texture2D[4];
    //Output texture
    private Texture2D generatedTexture;
    //Dimensions of output texture
    private Vector2Int textureDimensions;
    //The value for the channels where a texture is not provided
    private float defaultValue = 1.0f;

    //Counter of generated textures used for naming
    private int totalTextures;

    private bool hasAlpha = false;


    [MenuItem ("Tools/TextureCombiner")]
    private static void ShowWindow () {
        var window = GetWindow<TextureCombiner> ();
        window.titleContent = new GUIContent ("Texture Combiner");
        window.Show ();
    }

    private void OnGUI () {

        //Displaying the texture fields
        textures[0] = (Texture2D) EditorGUILayout.ObjectField ("Texture 1 (R)", textures[0], typeof (Texture2D), false);
        textures[1] = (Texture2D) EditorGUILayout.ObjectField ("Texture 2 (G)", textures[1], typeof (Texture2D), false);
        textures[2] = (Texture2D) EditorGUILayout.ObjectField ("Texture 3 (B)", textures[2], typeof (Texture2D), false);
        textures[3] = (Texture2D) EditorGUILayout.ObjectField ("Texture 4 (A)", textures[3], typeof (Texture2D), false);

        //Displaying the texture information
        textureDimensions = EditorGUILayout.Vector2IntField ("Dimensions", textureDimensions);
        defaultValue = EditorGUILayout.Slider ("Default value", defaultValue, 0.0f, 1.0f);

        if (GUILayout.Button ("Generate texture")) {
            GenerateTexture ();
        }

        if (GUILayout.Button ("Save texture")) {
            SaveGeneratedTexture ();
        }

        //Showing preview of the generated texture and its alpha (if it has any)
        if (generatedTexture != null) {
            EditorGUILayout.LabelField ("Generated texture preview");
            EditorGUI.DrawPreviewTexture (new Rect (50, 380, 100, 100), generatedTexture);
            if (hasAlpha) {
                EditorGUI.DrawTextureAlpha (new Rect (200, 380, 100, 100), generatedTexture);
            }
        }
    }

    private void GenerateTexture () {
        generatedTexture = null;
        if (AllTexturesAreEmpty ()) {
            Debug.LogWarning ("No textures were provided, not generating any new textures.");
        } else {
            hasAlpha = (textures[3] != null); //If the 4th texture has been assigned, the generated texture will have an alpha channel
            generatedTexture = new Texture2D (textureDimensions.x, textureDimensions.y, hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);

            for (int i = 0; i < textureDimensions.x; i++) {
                for (int j = 0; j < textureDimensions.y; j++) {
                    float x = (float) i / (float) textureDimensions.x;
                    float y = (float) j / (float) textureDimensions.y;

                    //Using GetPixelBilinear so that the size of the output texture does not depend on the size of the input textures.
                    float colR = textures[0] == null ? defaultValue : textures[0].GetPixelBilinear (x, y).r;
                    float colG = textures[1] == null ? defaultValue : textures[1].GetPixelBilinear (x, y).r;
                    float colB = textures[2] == null ? defaultValue : textures[2].GetPixelBilinear (x, y).r;
                    float colA = textures[3] == null ? defaultValue : textures[3].GetPixelBilinear (x, y).r;
                    generatedTexture.SetPixel (i, j, new Color (colR, colG, colB, colA));
                }
            }
            generatedTexture.Apply ();
        }
    }

    private void SaveGeneratedTexture () {
        GenerateTexture ();

        //If a directory called "Textures/Generated Textures" doesn't exist, create it
        if (!Directory.Exists (Application.dataPath + "Textures/Generated Textures")) {
            Directory.CreateDirectory (Application.dataPath + "/Textures/Generated Textures/");
            totalTextures = 0;
        } else {
            totalTextures = Directory.GetFiles (Application.dataPath + "/Textures/Generated Textures/").Length;
        }

        byte[] bytes = generatedTexture.EncodeToPNG ();
        while (File.Exists (Application.dataPath + "/Textures/Generated Textures/generated_texture_" + totalTextures.ToString () + ".png")) {
            totalTextures++;
        }
        File.WriteAllBytes (Application.dataPath + "/Textures/Generated Textures/generated_texture_" + totalTextures.ToString () + ".png", bytes);
        AssetDatabase.Refresh ();

        EditorUtility.FocusProjectWindow ();
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath ("Assets/Textures/Generated Textures/generated_texture_" + totalTextures.ToString () + ".png");
    }

    private bool AllTexturesAreEmpty () {
        for (int i = 0; i < 4; i++) {
            if (textures[i] != null) {
                return false;
            }
        }
        return true;
    }

}