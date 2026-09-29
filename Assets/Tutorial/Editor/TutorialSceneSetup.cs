using System.Collections.Generic;
using System.Linq;
using GardenGrowers.CameraSystems;
using GardenGrowers.Core;
using GardenGrowers.Dialogue;
using GardenGrowers.FX;
using GardenGrowers.NPC;
using GardenGrowers.Tutorial;
using GardenGrowers.UI;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GardenGrowers.EditorTools
{
    /// <summary>
    /// Tool 3: adds the tutorial, the HUD and Gogo to the open scene. Only works on Tutorial.unity.
    /// Safe to run again, it reuses what's already there.
    /// </summary>
    public static class TutorialSceneSetup
    {
        private const string RootName = "--- TUTORIAL ---";

        [MenuItem("Tools/Garden Growers/Tutorial/3. Set Up Open Scene (Tutorial.unity only)", priority = 3)]
        public static void SetupFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!string.Equals(scene.name, "Tutorial", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Tutorial Setup",
                    "Open Tutorial.unity first.\n\nThis tool only edits a scene called 'Tutorial', so Level.unity stays safe for the group.", "OK");
                return;
            }

            var gogoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GogoBaker.PrefabPath);
            var sequence = AssetDatabase.LoadAssetAtPath<TutorialSequence>(TutorialContentBuilder.SequencePath);
            if (gogoPrefab == null || sequence == null)
            {
                EditorUtility.DisplayDialog("Tutorial Setup", "Run steps 1 and 2 in Tools > Garden Growers > Tutorial first.", "OK");
                return;
            }

            var done = new List<string>();
            var warnings = new List<string>();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Set Up Tutorial");
            Physics.SyncTransforms();

            // Systems and HUD.
            GameObject root = FindOrCreate(RootName, null);
            GameObject systems = FindOrCreate("Tutorial Systems", root.transform);
            Ensure<PlayerControlLock>(systems);
            Ensure<CameraFocus>(systems);
            Ensure<DialogueInput>(systems);
            var fx = Ensure<TutorialFX>(systems);
            var manager = Ensure<TutorialManager>(systems);

            GameObject hudObject = FindOrCreate("Tutorial HUD", root.transform);
            var hud = Ensure<TutorialHUD>(hudObject);
            Undo.RecordObject(hud, "Style HUD");
            hud.Configure(BuildStyle());
            EditorUtility.SetDirty(hud);
            done.Add("Tutorial systems and HUD");

            // Targets on existing objects.
            Transform player = Object.FindFirstObjectByType<FirstPersonController>()?.transform;
            GameObject spade = FindInScene(scene, "ShovelPickup");
            GameObject can = FindInScene(scene, "wateringcan Pick up");
            GameObject bed = FindInScene(scene, "RestInteract");
            GameObject stall = FindInScene(scene, "FoodStall");
            GameObject seeds = FindInScene(scene, "Tomato Seeds");
            GameObject granny = FindInScene(scene, "Granny2");

            Tag(spade, "spade", 1.0f, 3f, "ShovelPickup", done, warnings);
            Tag(can, "wateringCan", 1.0f, 3f, "wateringcan Pick up", done, warnings);
            Tag(bed, "bed", 1.3f, 3.5f, "RestInteract", done, warnings);
            Tag(stall, "stall", 2.8f, 5f, "FoodStall", done, warnings);
            Tag(seeds, "seeds", 1.2f, 4f, "Tomato Seeds", done, warnings);

            Vector3 reference = spade != null ? spade.transform.position : player != null ? player.position : Vector3.zero;
            PlotScript plot = Object.FindObjectsByType<PlotScript>(FindObjectsSortMode.None)
                .OrderBy(p => (p.transform.position - reference).sqrMagnitude).FirstOrDefault();
            Tag(plot != null ? plot.gameObject : null, "plot", 1.6f, 3f, "a Plant Spot", done, warnings);

            // Gogo runs the stall, so hide the old stall granny (before measuring the ground there).
            if (granny != null && granny.activeSelf)
            {
                Undo.RecordObject(granny, "Hide stall granny");
                granny.SetActive(false);
                Physics.SyncTransforms();
                done.Add("Hid the old stall granny (Gogo runs the stall in this scene)");
            }

            // Anchor points (move these in the Scene view if they look wrong).
            GameObject anchors = FindOrCreate("Tutorial Targets", root.transform);
            Vector3 playerPos = player != null ? player.position : Vector3.zero;
            Vector3 playerForward = player != null ? Flat(player.forward).normalized : Vector3.forward;
            if (playerForward == Vector3.zero) playerForward = Vector3.forward;

            // Gogo starts in front of the player, so the first thing they see is her walking over.
            Vector3 gogoStart = Ground(playerPos + playerForward * 8f);
            // "Home" for the ending is where the player woke up.
            Vector3 porch = Ground(playerPos + playerForward * 2f);
            Vector3 stallSpot = granny != null ? Ground(granny.transform.position)
                : stall != null ? Ground(stall.transform.position - stall.transform.right * 2f) : Ground(playerPos + playerForward * 20f);
            Vector3 plotPos = plot != null ? plot.transform.position : playerPos;
            Vector3 away = Flat(plotPos - playerPos).normalized;
            if (away == Vector3.zero) away = Vector3.forward;
            Vector3 birdSpawn = plotPos + away * 12f + Vector3.up * 4f;

            Anchor(anchors.transform, "gogoStart", gogoStart, done);
            Anchor(anchors.transform, "porch", porch, done);
            Anchor(anchors.transform, "stallSpot", stallSpot, done);
            Anchor(anchors.transform, "birdSpawn", birdSpawn, done);

            // Gogo herself.
            GrandmaController gogo = Object.FindFirstObjectByType<GrandmaController>(FindObjectsInactive.Include);
            if (gogo == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(gogoPrefab, scene);
                Undo.RegisterCreatedObjectUndo(instance, "Add Gogo");
                instance.transform.SetParent(root.transform, true);
                instance.transform.position = gogoStart;
                Vector3 look = Flat(playerPos - gogoStart);
                if (look != Vector3.zero) instance.transform.rotation = Quaternion.LookRotation(look);
                gogo = instance.GetComponent<GrandmaController>();
                done.Add("Gogo placed at gogoStart");
            }

            SetUpSellCounter(gogo, granny, done, warnings);

            // NavMesh, so Gogo walks around fences. Bake it by hand afterwards.
            GameObject navObject = FindOrCreate("Tutorial NavMesh", root.transform);
            var surface = Ensure<NavMeshSurface>(navObject);
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            EditorUtility.SetDirty(surface);
            if (player != null)
            {
                var modifier = Ensure<NavMeshModifier>(player.gameObject);
                modifier.ignoreFromBuild = true;
                EditorUtility.SetDirty(modifier);
            }

            // Keep Gogo off the garden beds.
            int beds = MarkBedsNotWalkable(scene);
            if (beds > 0) done.Add(beds + " garden beds marked as not walkable for Gogo");

            // Wire the manager.
            SetReference(manager, "sequence", sequence);
            SetReference(manager, "gogo", gogo);
            SetReference(manager, "hud", hud);
            SetReference(manager, "fx", fx);
            SetSeedCatalog(manager);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = navObject;

            string message = "Done:\n- " + string.Join("\n- ", done);
            if (warnings.Count > 0) message += "\n\nCheck these:\n- " + string.Join("\n- ", warnings);
            message += "\n\nNext: with 'Tutorial NavMesh' selected, click Bake in the Inspector. Then save the scene (Ctrl+S).";
            Debug.Log("[Tutorial Setup]\n" + message);
            EditorUtility.DisplayDialog("Tutorial Setup", message, "OK");
        }

        //Gogo's stall

        private static void SetUpSellCounter(GrandmaController gogo, GameObject granny, List<string> done, List<string> warnings)
        {
            if (gogo == null) return;
            Transform existing = gogo.transform.Find("SellCounter");
            if (existing != null)
            {
                // Counters made before this fix can't sell what the player is holding.
                if (existing.GetComponent<SellIncludesHeldItem>() == null)
                {
                    var helper = Undo.AddComponent<SellIncludesHeldItem>(existing.gameObject);
                    SetReference(helper, "interactable", existing.GetComponent<Interactable>());
                    done.Add("Gogo's sell counter now also sells what you're holding");
                }
                else
                {
                    done.Add("Gogo's sell counter already set up");
                }
                return;
            }

            var counter = new GameObject("SellCounter");
            Undo.RegisterCreatedObjectUndo(counter, "Add sell counter");
            counter.transform.SetParent(gogo.transform, false);
            counter.tag = "Interactable";

            var box = counter.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.3f, 0f);
            box.size = new Vector3(1.4f, 2.6f, 1.4f);

            counter.AddComponent<global::Outline>(); // Interactable needs one
            var interactable = counter.AddComponent<Interactable>();
            interactable.message = "E to Sell All";
            var sell = counter.AddComponent<SellAllInteractable>();
            SetReference(sell, "interactable", interactable);
            // Added from code, so the UnityEvent doesn't exist yet.
            if (interactable.onInteraction == null) interactable.onInteraction = new UnityEngine.Events.UnityEvent();
            UnityEventTools.AddPersistentListener(interactable.onInteraction, sell.SellAll);
            EditorUtility.SetDirty(interactable);

            // The group's SellAll skips whatever is in the player's hand.
            var heldToo = counter.AddComponent<SellIncludesHeldItem>();
            SetReference(heldToo, "interactable", interactable);

            GameObject prompt = CopyGrannyPrompt(granny, counter.transform) ?? BuildPrompt(counter.transform);
            SetReference(interactable, "promptCanvas", prompt);
            SetReference(interactable, "promptText", prompt.GetComponentInChildren<TextMeshProUGUI>(true));

            var target = counter.AddComponent<TutorialTarget>();
            target.Configure("sellCounter", 3f, 3f);

            counter.SetActive(false);
            done.Add("Gogo's sell counter (the group's Interactable + SellAllInteractable, switched on in the selling step)");
            if (granny == null) warnings.Add("Couldn't find Granny2, so I built a new sell prompt");
        }

        private static GameObject CopyGrannyPrompt(GameObject granny, Transform parent)
        {
            if (granny == null) return null;
            var grannyInteractable = granny.GetComponent<Interactable>();
            if (grannyInteractable == null) return null;

            var serialized = new SerializedObject(grannyInteractable);
            var source = serialized.FindProperty("promptCanvas").objectReferenceValue as GameObject;
            if (source == null) return null;

            GameObject copy = Object.Instantiate(source);
            Undo.RegisterCreatedObjectUndo(copy, "Copy sell prompt");
            copy.name = "SellPrompt";
            copy.transform.SetParent(parent, true);
            copy.transform.localPosition = new Vector3(0f, 2.9f, 0f);

            var face = copy.GetComponent<FaceCamera>();
            if (face == null) face = copy.AddComponent<FaceCamera>();
            SetVector(face, "offsetFromParent", new Vector3(0f, 2.9f, 0f));
            return copy;
        }

        private static GameObject BuildPrompt(Transform parent)
        {
            var go = new GameObject("SellPrompt", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Build sell prompt");
            go.layer = 5;
            go.transform.SetParent(parent, false);
            go.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)go.transform).sizeDelta = new Vector2(3f, 1f);

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.layer = 5;
            textObject.transform.SetParent(go.transform, false);
            ((RectTransform)textObject.transform).sizeDelta = new Vector2(3f, 1f);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TutorialArt.Font();
            text.fontSize = 0.3f;
            text.alignment = TextAlignmentOptions.Center;
            text.text = "E to Sell All";

            var face = go.AddComponent<FaceCamera>();
            SetVector(face, "offsetFromParent", new Vector3(0f, 2.9f, 0f));
            return go;
        }

        //Helpers

        private static HudStyle BuildStyle()
        {
            return new HudStyle
            {
                font = TutorialArt.Font(),
                panel = TutorialArt.Bar,
                slot = TutorialArt.Slot
            };
        }

        private static void SetSeedCatalog(TutorialManager manager)
        {
            SeedData[] seeds = AssetDatabase.FindAssets("t:SeedData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<SeedData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(seed => seed != null)
                .ToArray();

            var serialized = new SerializedObject(manager);
            SerializedProperty list = serialized.FindProperty("seedCatalog");
            list.arraySize = seeds.Length;
            for (int i = 0; i < seeds.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = seeds[i];
            serialized.ApplyModifiedProperties();
        }

        private static void Tag(GameObject target, string id, float markerHeight, float reach, string lookedFor, List<string> done, List<string> warnings)
        {
            if (target == null)
            {
                warnings.Add("Couldn't find " + lookedFor + ". Add a TutorialTarget with id '" + id + "' to it by hand.");
                return;
            }

            var tag = target.GetComponent<TutorialTarget>();
            if (tag == null) tag = Undo.AddComponent<TutorialTarget>(target);
            Undo.RecordObject(tag, "Configure target");
            tag.Configure(id, markerHeight, reach);
            EditorUtility.SetDirty(tag);
            done.Add("Target '" + id + "' on " + target.name);
        }

        /// <summary>Marks every plot and growbed as Not Walkable, so Gogo never stands on a bed.</summary>
        private static int MarkBedsNotWalkable(Scene scene)
        {
            int notWalkable = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            if (notWalkable < 0) notWalkable = 1;

            var beds = new HashSet<GameObject>();
            foreach (PlotScript plot in Object.FindObjectsByType<PlotScript>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (plot.gameObject.scene == scene) beds.Add(plot.gameObject);
            }

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (Transform t in rootObject.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("growbed", System.StringComparison.OrdinalIgnoreCase)) beds.Add(t.gameObject);
                }
            }

            foreach (GameObject bed in beds)
            {
                var modifier = Ensure<NavMeshModifier>(bed);
                Undo.RecordObject(modifier, "Mark bed not walkable");
                modifier.overrideArea = true;
                modifier.area = notWalkable;
                EditorUtility.SetDirty(modifier);
            }

            return beds.Count;
        }

        private static void Anchor(Transform parent, string id, Vector3 position, List<string> done)
        {
            Transform existing = parent.Find(id);
            GameObject go = existing != null ? existing.gameObject : null;
            if (go == null)
            {
                go = new GameObject(id);
                Undo.RegisterCreatedObjectUndo(go, "Add anchor");
                go.transform.SetParent(parent, false);
                go.transform.position = position;
                done.Add("Anchor '" + id + "'");
            }

            var tag = go.GetComponent<TutorialTarget>();
            if (tag == null) tag = go.AddComponent<TutorialTarget>();
            tag.Configure(id, 1.2f, 3.5f);
            EditorUtility.SetDirty(tag);
        }

        private static GameObject FindOrCreate(string name, Transform parent)
        {
            Transform found = parent != null ? parent.Find(name) : null;
            if (found == null && parent == null)
            {
                foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (rootObject.name == name) return rootObject;
                }
            }
            if (found != null) return found.gameObject;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static T Ensure<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (Transform t in rootObject.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name) return t.gameObject;
                }
            }
            return null;
        }

        private static Vector3 Ground(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * 30f, Vector3.down, out RaycastHit hit, 80f, ~0, QueryTriggerInteraction.Ignore)) return hit.point;
            return point;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static void SetReference(Object target, string field, Object value)
        {
            if (target == null) return;
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Tutorial Setup] Field '" + field + "' not found on " + target.GetType().Name);
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static void SetVector(Object target, string field, Vector3 value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) return;
            property.vector3Value = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
