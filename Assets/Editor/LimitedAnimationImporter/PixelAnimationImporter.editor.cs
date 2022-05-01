using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script that imports animations in a pixel-art style
/// </summary>
public class PixelAnimationImporter : EditorWindow
{
    /// <summary>
    /// The avatar to associate with the animations
    /// </summary>
    static Avatar avatar;
    /// <summary>
    /// Whether to export clips as .anim files
    /// </summary>
    static bool exportClips;
    /// <summary>
    /// Whether to delete the source asset after exporting animations
    /// </summary>
    bool deleteAfterExport;

    struct Target
    {
        /// <summary>
        /// Whether to export this clip
        /// </summary>
        public bool export;
        /// <summary>
        /// The animation clip to overwrite (leave empty to create a new clip)
        /// </summary>
        public AnimationClip clip;
        /// <summary>
        /// Whether to produce xy root motion
        /// </summary>
        public bool useRootMotion;
        /// <summary>
        /// Whether the xy root motion should be smoothly interpolated
        /// </summary>
        public bool smoothRootMotion;
        /// <summary>
        /// Whether the animation clip is a loop
        /// </summary>
        public bool loop;
    };

    ModelImporter target;
    ModelImporterClipAnimation[] clips;
    Target[] targetAnimations;
    string path;
    bool reimported;

    [MenuItem("Assets/Import Pixel Animation", true)]
    static bool ValidateImportPixelAnimation()
    {
        // check asset selected
        if (Selection.assetGUIDs.Length != 1) return false;
        try
        {
            Debug.Log((ModelImporter)ModelImporter.GetAtPath(AssetDatabase.GetAssetPath(Selection.activeObject)));
        }
        catch (System.InvalidCastException)
        {
            return false;
        }
        return true;
    }

    [MenuItem("Assets/Import Pixel Animation", false, 10)]
    static void ImportPixelAnimation()
    {
        PixelAnimationImporter window = (PixelAnimationImporter)EditorWindow.GetWindow(typeof(PixelAnimationImporter));
        window.path = AssetDatabase.GetAssetPath(Selection.activeObject);
        window.target = (ModelImporter)ModelImporter.GetAtPath(window.path);
        window.reimported = window.target.clipAnimations.Length != 0;
        window.clips = window.reimported ? window.target.clipAnimations : window.target.defaultClipAnimations;
        window.targetAnimations = new Target[window.clips.Length];
        for (int i = 0; i < window.clips.Length; i++)
        {
            window.targetAnimations[i] = new Target
            {
                export = true,
                clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        Path.Combine(Path.GetDirectoryName(window.path), window.clips[i].name + ".anim")),
                useRootMotion = window.reimported ? !window.clips[i].lockRootPositionXZ : true,
                smoothRootMotion = true,
                loop = window.clips[i].loop,
            };
        }
        avatar = window.target.sourceAvatar ?? avatar;
        window.ShowModalUtility();
    }

    void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        avatar = (Avatar)EditorGUILayout.ObjectField("Avatar", avatar, typeof(Avatar), false);
        exportClips = EditorGUILayout.Toggle("Export Clips", exportClips);
        if (exportClips)
        {
            deleteAfterExport = EditorGUILayout.Toggle("Delete After Export", deleteAfterExport);
            GUILayout.Label("Clips", EditorStyles.boldLabel);
            for (int i = 0; i < clips.Length; i++)
            {
                targetAnimations[i].export = EditorGUILayout.BeginToggleGroup(clips[i].name, targetAnimations[i].export);
                EditorGUI.indentLevel++;
                targetAnimations[i].clip = (AnimationClip)EditorGUILayout.ObjectField("Destination", targetAnimations[i].clip, typeof(AnimationClip), false);
                targetAnimations[i].useRootMotion = EditorGUILayout.Toggle("Use Root Motion", targetAnimations[i].useRootMotion);
                targetAnimations[i].smoothRootMotion = EditorGUILayout.Toggle("Smooth Root Motion", targetAnimations[i].smoothRootMotion);
                targetAnimations[i].loop = EditorGUILayout.Toggle("Loop Time", targetAnimations[i].loop);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndToggleGroup();
            }
        }

        if (GUILayout.Button("Apply"))
        {
            Execute();
            Close();
        }
    }

    void Execute()
    {
        // set import settings
        if (avatar != null)
        {
            target.animationType = ModelImporterAnimationType.Human;
            target.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            target.sourceAvatar = avatar;
        }

        target.animationCompression = ModelImporterAnimationCompression.Off;
        target.resampleCurves = false;

        if (!reimported)
        {
            // set these up on a fresh import but keep user edits
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
                clips[i].keepOriginalOrientation = true;
                clips[i].keepOriginalPositionY = true;
                clips[i].keepOriginalPositionXZ = true;
            }
        }
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].lockRootPositionXZ = !targetAnimations[i].useRootMotion;
            clips[i].loop = targetAnimations[i].loop;
        }
        target.clipAnimations = clips;

        target.SaveAndReimport();

        if (exportClips)
        {
            // grab imported animations
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            AnimationClip[] animations = assets.Where(c => (c as AnimationClip) != null).Cast<AnimationClip>().ToArray();
            for (int i = 0; i < clips.Length; i++)
            {
                if (!targetAnimations[i].export)
                {
                    continue;
                }
                string[] customCurveNames = clips[i].curves.Select(curve => curve.name).ToArray();
                // load the animation
                AnimationClip anim = Object.Instantiate(animations.Where(c => c.name == clips[i].name).First());
                AnimationUtility.GetCurveBindings(anim);
                // edit the keyframes
                foreach (var binding in AnimationUtility.GetCurveBindings(anim))
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(anim, binding);
                    if (customCurveNames.Contains(binding.propertyName))
                    {
                        // align keyframes
                        for (int j = 0; j < curve.keys.Length; j++)
                        {
                            Keyframe key = curve[j];
                            key.time = Mathf.Round(key.time * anim.frameRate) / anim.frameRate;
                            curve.MoveKey(j, key);
                        }
                        AnimationUtility.SetEditorCurve(anim, binding, curve);
                        continue;
                    }

                    bool isSmooth = targetAnimations[i].smoothRootMotion &&
                                    (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z");
                    // delete duplicate keys
                    bool deletedLast = false;
                    for (int j = 1; j < curve.keys.Length - 1; j++)
                    {
                        if (isSmooth && deletedLast && !Mathf.Approximately(curve[j].value, curve[j + 1].value))
                        {
                            // previous key is gone and next key is different so we need to keep this one
                        }
                        else
                        {
                            if (Mathf.Approximately(curve[j].value, curve[j - 1].value))
                            {
                                curve.RemoveKey(j--);
                                deletedLast = true;
                                continue;
                            }
                        }
                        deletedLast = false;
                    }
                    // Set keyframe interpolation
                    for (int j = 0; j < curve.keys.Length; j++)
                    {
                        if (isSmooth)
                        {
                            // smooth interpolation for root motion
                            AnimationUtility.SetKeyLeftTangentMode(curve, j, AnimationUtility.TangentMode.ClampedAuto);
                            AnimationUtility.SetKeyRightTangentMode(curve, j, AnimationUtility.TangentMode.ClampedAuto);
                        }
                        else
                        {
                            // no interpolation for everything else
                            AnimationUtility.SetKeyLeftTangentMode(curve, j, AnimationUtility.TangentMode.Constant);
                            AnimationUtility.SetKeyRightTangentMode(curve, j, AnimationUtility.TangentMode.Constant);
                        }
                    }
                    if (!targetAnimations[i].loop)
                    {
                        if (isSmooth)
                        {
                            // push all key forwards
                            for (int j = curve.keys.Length - 1; j >= 0; j--)
                            {
                                Keyframe key = curve[j];
                                key.time += 1 / anim.frameRate;
                                curve.MoveKey(j, key);
                                if (!isSmooth) break;
                            }
                        }
                        else
                        {
                            // Add final key
                            Keyframe key = curve[curve.length - 1];
                            key.time += 1 / anim.frameRate;
                            curve.AddKey(key);
                        }
                    }
                    AnimationUtility.SetEditorCurve(anim, binding, curve);
                }

                // adjust event timing
                var events = AnimationUtility.GetAnimationEvents(anim);
                for (int j = 0; j < events.Length; j++)
                {
                    events[j].time = Mathf.Round(events[j].time * anim.frameRate) / anim.frameRate;
                }
                AnimationUtility.SetAnimationEvents(anim, events);

                // write the animations
                if (targetAnimations[i].clip == null)
                {
                    // new animation
                    AssetDatabase.CreateAsset(anim, Path.Combine(Path.GetDirectoryName(path), clips[i].name + ".anim"));
                }
                else
                {
                    // overwrite animation
                    EditorUtility.CopySerialized(anim, targetAnimations[i].clip);
                }
            }

            if (deleteAfterExport)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.SaveAssets();
        }
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }
}
