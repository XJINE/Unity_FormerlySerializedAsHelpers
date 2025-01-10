using System;
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

        [MenuItem("Custom/FormerlySerializedAsHelper")]
        private static void Init()
        {
            GetWindow<FormerlySerializedAsHelper>(nameof(FormerlySerializedAsHelper));
        }

        private void OnGUI()
        {
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

            GUILayout.Label("Current scenes are all saved before processing.");
            GUILayout.Label("Corresponding asset is being updated.");

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

                if (HasTargetTypeComponentOrTargetTypeField(asset))
                {
                    Debug.Log($"Processing Prefab: {assetPath}");
                    EditorUtility.SetDirty(asset);
                    PrefabUtility.SavePrefabAsset(asset);
                }
            }

            // DANGER:
            // Use SavePrefabAsset instead of SaveAssets to preserve the values of existing instances.
            // AssetDatabase.SaveAssets();
        }

        private void UpdateScenes()
        {
            var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");

            foreach (var sceneGUID in sceneGUIDs)
            {
                var scenePath       = AssetDatabase.GUIDToAssetPath(sceneGUID);
                var scene           = EditorSceneManager.OpenScene(scenePath);
                var rootGameObjects = scene.GetRootGameObjects();

                foreach (var gameObject in rootGameObjects)
                {
                    if (HasTargetTypeComponentOrTargetTypeField(gameObject))
                    {
                        Debug.Log($"Processing scene: {scenePath}");
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        break;
                    }
                }
            }
        }

        private bool IsTargetTypeOrHasTargetTypeField(Type type)
        {
            return type.FullName == targetTypeFullName || HasTargetTypeField(type);
        }

        private bool HasTargetTypeComponentOrTargetTypeField(GameObject gameObject)
        {
            var components = gameObject.GetComponentsInChildren<Component>(true);
            
            foreach (var component in components)
            {
                var componentType = component.GetType();

                if (componentType.FullName == targetTypeFullName || HasTargetTypeField(componentType))
                {
                    return true;
                }
            }

            return false;
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