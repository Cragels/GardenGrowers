using GardenGrowers.NPC;
using GardenGrowers.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GardenGrowers.EditorTools
{
    /// <summary>Tool 4: swaps Granny2 at the food stall in Level.unity for Gogo, who sells and talks.</summary>
    public static class LevelStallSetup
    {
        private const float PromptHeight = 2.9f;

        [MenuItem("Tools/Garden Growers/Tutorial/4. Put Gogo At The Level Stall (Level.unity only)", priority = 4)]
        public static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "Level")
            {
                EditorUtility.DisplayDialog("Gogo's Stall", "Open Level.unity first.", "OK");
                return;
            }

            if (Object.FindFirstObjectByType<GogoStall>() != null)
            {
                EditorUtility.DisplayDialog("Gogo's Stall", "Gogo is already at the stall.", "OK");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GogoBaker.PrefabPath);
            GameObject granny = FindRoot(scene, "Granny2");
            Interactable grannySell = granny != null ? granny.GetComponent<Interactable>() : null;
            if (prefab == null || grannySell == null)
            {
                EditorUtility.DisplayDialog("Gogo's Stall", "Couldn't find " + (prefab == null ? GogoBaker.PrefabPath : "Granny2 and her sell prompt") + ".", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Put Gogo at the stall");
            int undoGroup = Undo.GetCurrentGroup();

            //Gogo

            var gogo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(gogo, "Add Gogo");
            gogo.name = "Gogo";
            gogo.transform.SetPositionAndRotation(granny.transform.position, Quaternion.identity); // she turns to the player in play mode

            //Sell counter (the group's scripts, same setup as in the tutorial)

            var counter = new GameObject("SellCounter") { tag = "Interactable" };
            Undo.RegisterCreatedObjectUndo(counter, "Add sell counter");
            counter.transform.SetParent(gogo.transform, false);

            var box = counter.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.3f, 0f);
            box.size = new Vector3(1.4f, 2.6f, 1.4f);

            counter.AddComponent<global::Outline>(); // Interactable needs one
            var interactable = counter.AddComponent<Interactable>();
            interactable.message = grannySell.message;
            var sell = counter.AddComponent<SellAllInteractable>();
            SetReference(sell, "interactable", interactable);
            if (interactable.onInteraction == null) interactable.onInteraction = new UnityEngine.Events.UnityEvent();
            UnityEventTools.AddPersistentListener(interactable.onInteraction, sell.SellAll);

            SetReference(counter.AddComponent<SellIncludesHeldItem>(), "interactable", interactable);

            // Keep Granny2's own prompt, just move it above Gogo.
            GameObject prompt = new SerializedObject(grannySell).FindProperty("promptCanvas").objectReferenceValue as GameObject;
            if (prompt != null)
            {
                Undo.SetTransformParent(prompt.transform, counter.transform, "Move sell prompt");
                prompt.transform.localPosition = new Vector3(0f, PromptHeight, 0f);
                var face = prompt.GetComponent<FaceCamera>();
                if (face != null) SetVector(face, "offsetFromParent", new Vector3(0f, PromptHeight, 0f));
                SetReference(interactable, "promptCanvas", prompt);
                SetReference(interactable, "promptText", prompt.GetComponentInChildren<TextMeshProUGUI>(true));
            }

            //Talking when the player sells

            var stall = Undo.AddComponent<GogoStall>(gogo);
            SetReference(stall, "gogo", gogo.GetComponent<GrandmaController>());
            SetReference(stall, "counter", interactable);

            Undo.DestroyObjectImmediate(granny);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = gogo;

            EditorUtility.DisplayDialog("Gogo's Stall", "Gogo replaced Granny2 at the stall. Save the scene (Ctrl+S).", "OK");
        }

        //Helpers

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector(Object target, string field, Vector3 value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).vector3Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
