using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FormerlySerializedAsHelpers.Editor
{
    public class FormerlySerializedAsHelper : EditorWindow
    {
        [MenuItem("Custom/FormerlySerializedAsHelper")]
        private static void Init()
        {
            GetWindow<FormerlySerializedAsHelper>(nameof(FormerlySerializedAsHelper));
        }

         private void OnGUI()
         {
             GUILayout.Label("All scene files are opened, saved, and then closed.");
             GUILayout.Label("Current scenes are all saved before processing.");

             if (GUILayout.Button("Execute"))
             {
                 SaveCurrentScenes();

                 var lastScenePath  = SceneManager.GetActiveScene().path;
                 var sceneFilePaths = GetAllScenePaths();

                 if (sceneFilePaths.Length == 0)
                 {
                     Debug.LogWarning("No scene files found.");
                     return;
                 }

                 foreach (var scenePath in sceneFilePaths)
                 {
                     Debug.Log($"Processing scene: {scenePath}");

                     var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                                 EditorSceneManager.MarkSceneDirty(scene);
                                 EditorSceneManager.SaveScene(scene);
                 }

                 EditorSceneManager.OpenScene(lastScenePath, OpenSceneMode.Single);
             }

             return;
         }

         private static void SaveCurrentScenes()
         {
            var sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                 EditorSceneManager.SaveScene(SceneManager.GetSceneAt(i));
            }
         }

         private string[] GetAllScenePaths()
         {
             var scenesFolderPath = Application.dataPath; // means "Assets";
             return Directory.GetFiles(scenesFolderPath, "*.unity", SearchOption.AllDirectories);
         }
    }
}