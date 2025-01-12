using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FormerlySerializedAsHelpers.Editor
{
    public class FormerlySerializedAsHelper : EditorWindow
    {
        private string targetTypeFullName = "";

        [MenuItem("Custom/"+nameof(FormerlySerializedAsHelper))]
        private static void Init()
        {
            GetWindow<FormerlySerializedAsHelper>(nameof(FormerlySerializedAsHelper));
        }

        private void OnGUI()
        {
            WrappedLabel("Update all related assets that have the target type component or field.");
            WrappedLabel("Current scenes are all saved before processing.");
            WrappedLabel("Be sure to backup your project before using.");

            void WrappedLabel(string text)
            {
                GUILayout.Label(text, EditorStyles.wordWrappedLabel);
            }

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Type Full Name: ");
            targetTypeFullName = GUILayout.TextField(targetTypeFullName);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Execute"))
            {
                SaveCurrentScenes();

                var lastScenePath  = SceneManager.GetActiveScene().path;

                UpdateScriptableObjects();
                UpdatePrefabs();
                UpdateScenes();

                EditorSceneManager.OpenScene(lastScenePath, OpenSceneMode.Single);
            }
            GUILayout.EndVertical();

            GUI.enabled = true;
        }

        private static void SaveCurrentScenes()
        {
            var sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                 EditorSceneManager.SaveScene(SceneManager.GetSceneAt(i));
            }
        }

        private void UpdateScriptableObjects()
        {
            var assetGUIDs = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (var assetGUID in assetGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                var asset     = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                if (IsTargetTypeOrHasTargetTypeField(asset.GetType()))
                {
                    EditorUtility.SetDirty(asset);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private void UpdatePrefabs()
        {
            var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");

            foreach (var prefabGUID in prefabGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                var asset     = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                var (hasTarget, components) = HasTargetTypeComponentOrTargetTypeFieldInChildren(asset);

                if (!hasTarget)
                {
                    continue;
                }

                Debug.Log($"Processing Prefab: {assetPath}");

                foreach (var component in components)
                {
                    EditorUtility.SetDirty(component);
                }

                EditorUtility.SetDirty(asset);
                PrefabUtility.SavePrefabAsset(asset);
            }
        }

        private void UpdateScenes()
        {
            var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");

            foreach (var sceneGUID in sceneGUIDs)
            {
                var scenePath       = AssetDatabase.GUIDToAssetPath(sceneGUID);
                var scene           = EditorSceneManager.OpenScene(scenePath);
                var rootGameObjects = scene.GetRootGameObjects();

                foreach (var rootGameObject in rootGameObjects)
                {
                    var (hasTarget, components) = HasTargetTypeComponentOrTargetTypeFieldInChildren(rootGameObject);

                    if (!hasTarget)
                    {
                        continue;
                    }

                    Debug.Log($"Processing scene: {scenePath}");

                    foreach (var component in components)
                    {
                        EditorUtility.SetDirty(component);
                    }

                    EditorUtility.SetDirty(rootGameObject);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }

        private bool IsTargetTypeOrHasTargetTypeField(Type type)
        {
            return type.FullName == targetTypeFullName || HasTargetTypeField(type);
        }

        private (bool, Component[]) HasTargetTypeComponentOrTargetTypeFieldInChildren(GameObject gameObject)
        {
            var components = gameObject.GetComponentsInChildren<Component>(true);
            var targets    = new List<Component>();

            foreach (var component in components)
            {
                var componentType = component.GetType();

                if (componentType.FullName == targetTypeFullName || HasTargetTypeField(componentType))
                {
                    targets.Add(component);
                }
            }

            return (0 < targets.Count, targets.ToArray());
        }

        private bool HasTargetTypeField(Type type)
        {
            return type.GetFields(BindingFlags.Instance
                                | BindingFlags.Public
                                | BindingFlags.NonPublic)
                       .Any(field => field.FieldType.FullName == targetTypeFullName);
        }
    }
}