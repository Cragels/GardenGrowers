using System.Collections.Generic;
using System.Linq;
using GardenGrowers.Dialogue;
using GardenGrowers.NPC;
using UnityEditor;
using UnityEngine;

namespace GardenGrowers.EditorTools
{
    /// <summary>
    /// Tool 1: splits the Granny2 model into parts (body, head, hands, stick) and builds Gogo.prefab.
    /// Granny2.fbx itself is never changed.
    /// </summary>
    public static class GogoBaker
    {
        public const string SourcePath = "Assets/FBXs/Granny2.fbx";
        public const string OutputFolder = "Assets/Tutorial/Art/Gogo";
        public const string MeshAssetPath = OutputFolder + "/GogoParts.asset";
        public const string PrefabPath = OutputFolder + "/Gogo.prefab";
        public const string SpeakerPath = "Assets/Tutorial/Data/Gogo Thandi.asset";

        // Same size as the stall granny in Level.unity.
        private const float TargetHeight = 2.6f;

        private enum Part { Body, Head, FreeHand, StickHand, Stick }

        private sealed class PartData
        {
            public readonly List<(int submesh, List<int> triangles)> pieces = new List<(int, List<int>)>();
            public Vector3 pivot;

            public List<int> PieceFor(int submesh)
            {
                foreach ((int s, List<int> tris) in pieces)
                {
                    if (s == submesh) return tris;
                }
                var list = new List<int>();
                pieces.Add((submesh, list));
                return list;
            }
        }

        [MenuItem("Tools/Garden Growers/Tutorial/1. Build Gogo From Granny2", priority = 1)]
        public static void BuildFromMenu()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Gogo Baker", "Please leave Play mode first.", "OK");
                return;
            }

            if (!TryLoad(out GameObject model, out MeshFilter filter, out Material[] materials)) return;

            ModelImporter importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter;
            bool switchedReadable = false;

            if (!CanRead(filter.sharedMesh))
            {
                bool ok = EditorUtility.DisplayDialog("Gogo Baker",
                    "Unity needs to read Granny2's mesh for a moment.\n\n" +
                    "I'll switch on Read/Write for Granny2.fbx, bake the parts, then switch it straight back off. " +
                    "If GitHub Desktop still shows Granny2.fbx.meta as changed afterwards, just discard that change.",
                    "OK, go", "Cancel");
                if (!ok || importer == null) return;

                importer.isReadable = true;
                importer.SaveAndReimport();
                switchedReadable = true;
                if (!TryLoad(out model, out filter, out materials)) return;
            }

            try
            {
                Bake(filter, materials);
            }
            finally
            {
                if (switchedReadable && importer != null)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }
        }

        private static bool TryLoad(out GameObject model, out MeshFilter filter, out Material[] materials)
        {
            model = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            filter = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            materials = renderer != null ? renderer.sharedMaterials : new Material[0];

            if (model == null || filter == null || filter.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("Gogo Baker", "Couldn't find the Granny2 mesh at " + SourcePath, "OK");
                return false;
            }
            return true;
        }

        private static bool CanRead(Mesh mesh)
        {
            if (mesh.isReadable) return true;
            var probe = new List<Vector3>();
            mesh.GetVertices(probe);
            return probe.Count == mesh.vertexCount && probe.Count > 0;
        }

        //The bake

        private static void Bake(MeshFilter filter, Material[] materials)
        {
            Mesh source = filter.sharedMesh;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uvs = new List<Vector2>();
            var colours = new List<Color>();
            source.GetVertices(vertices);
            source.GetNormals(normals);
            source.GetTangents(tangents);
            source.GetUVs(0, uvs);
            source.GetColors(colours);

            // 1. Into the model's own space, so Y is up.
            Matrix4x4 toModel = filter.transform.localToWorldMatrix;
            for (int i = 0; i < vertices.Count; i++) vertices[i] = toModel.MultiplyPoint3x4(vertices[i]);
            for (int i = 0; i < normals.Count; i++) normals[i] = toModel.MultiplyVector(normals[i]).normalized;
            for (int i = 0; i < tangents.Count; i++)
            {
                Vector3 t = toModel.MultiplyVector(tangents[i]).normalized;
                tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
            }

            // 2. Sort every triangle into a part.
            var parts = new Dictionary<Part, PartData>();
            foreach (Part part in System.Enum.GetValues(typeof(Part))) parts[part] = new PartData();

            int skinSubmesh = -1;
            int glassesSubmesh = -1;
            for (int s = 0; s < source.subMeshCount; s++)
            {
                // Skip the extra sub-mesh with no material (Unity never draws it).
                if (materials.Length > 0 && (s >= materials.Length || materials[s] == null)) continue;

                string name = MaterialName(materials, s);
                int[] triangles = source.GetTriangles(s);

                if (name.Contains("skin")) { skinSubmesh = s; continue; } // split below
                if (name.Contains("glass")) glassesSubmesh = s;

                Part part = name.Contains("stick") || name.Contains("cane") ? Part.Stick
                    : name.Contains("glass") || name.Contains("hair") ? Part.Head
                    : Part.Body;
                parts[part].PieceFor(s).AddRange(triangles);
            }

            if (skinSubmesh >= 0) SplitSkin(source.GetTriangles(skinSubmesh), skinSubmesh, vertices, parts);

            // 3. Size, ground and face her forward.
            Bounds all = BoundsOf(vertices, parts.Values.SelectMany(p => p.pieces.SelectMany(x => x.triangles)));
            float scale = TargetHeight / Mathf.Max(0.0001f, all.size.y);
            Bounds bodyBounds = BoundsOf(vertices, parts[Part.Body].pieces.SelectMany(x => x.triangles));
            Vector3 feet = new Vector3(bodyBounds.center.x, all.min.y, bodyBounds.center.z);

            Vector3 headCentre = BoundsOf(vertices, parts[Part.Head].pieces.SelectMany(x => x.triangles)).center;
            Vector3 glassesCentre = glassesSubmesh >= 0
                ? BoundsOf(vertices, parts[Part.Head].PieceFor(glassesSubmesh)).center
                : headCentre + Vector3.forward;
            Vector3 forward = glassesCentre - headCentre;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.000001f) forward = Vector3.forward;
            Quaternion align = Quaternion.FromToRotation(forward.normalized, Vector3.forward);

            for (int i = 0; i < vertices.Count; i++) vertices[i] = align * ((vertices[i] - feet) * scale);
            for (int i = 0; i < normals.Count; i++) normals[i] = align * normals[i];
            for (int i = 0; i < tangents.Count; i++)
            {
                Vector3 t = align * (Vector3)tangents[i];
                tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
            }

            // 4. Pivots: feet, neck, palms, stick handle.
            Bounds head = BoundsOf(vertices, parts[Part.Head].pieces.SelectMany(x => x.triangles));
            Bounds freeHand = BoundsOf(vertices, parts[Part.FreeHand].pieces.SelectMany(x => x.triangles));
            Bounds stickHand = BoundsOf(vertices, parts[Part.StickHand].pieces.SelectMany(x => x.triangles));
            Bounds stick = BoundsOf(vertices, parts[Part.Stick].pieces.SelectMany(x => x.triangles));
            Bounds glasses = glassesSubmesh >= 0 ? BoundsOf(vertices, parts[Part.Head].PieceFor(glassesSubmesh)) : head;

            parts[Part.Body].pivot = Vector3.zero;
            // Neck pivot just under the collar, so nods look natural.
            Bounds bodyPart = BoundsOf(vertices, parts[Part.Body].pieces.SelectMany(x => x.triangles));
            float neckY = Mathf.Clamp(bodyPart.max.y - 0.03f * TargetHeight, head.min.y, head.max.y);
            parts[Part.Head].pivot = new Vector3(head.center.x, neckY, head.center.z);
            parts[Part.FreeHand].pivot = freeHand.center;
            parts[Part.StickHand].pivot = stickHand.center;
            parts[Part.Stick].pivot = new Vector3(stick.center.x, stick.max.y, stick.center.z);
            Vector3 stickTip = LowestPoint(vertices, parts[Part.Stick]);

            // 5. Save the part meshes as one asset (main mesh + sub-assets).
            EnsureFolder(OutputFolder);
            AssetDatabase.DeleteAsset(MeshAssetPath);

            var meshes = new Dictionary<Part, Mesh>();
            foreach (Part part in parts.Keys)
            {
                if (parts[part].pieces.All(p => p.triangles.Count == 0)) continue;
                meshes[part] = BuildMesh("Gogo_" + part, parts[part], vertices, normals, tangents, uvs, colours);
            }

            bool first = true;
            foreach (Mesh mesh in meshes.Values)
            {
                if (first) AssetDatabase.CreateAsset(mesh, MeshAssetPath);
                else AssetDatabase.AddObjectToAsset(mesh, MeshAssetPath);
                first = false;
            }
            AssetDatabase.SaveAssets();

            // 6. Build the prefab.
            GameObject prefab = BuildPrefab(parts, meshes, materials, glasses.center, head.max.y, stickTip);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            string summary = string.Join("\n", meshes.Select(m => "  " + m.Key + ": " + m.Value.vertexCount + " verts"));
            Debug.Log("[Gogo Baker] Built " + PrefabPath + " (" + TargetHeight + "m tall)\n" + summary);
            EditorUtility.DisplayDialog("Gogo Baker", "Gogo is ready at\n" + PrefabPath + "\n\n" + summary, "Nice");
        }

        /// <summary>Skin is one material for her face and both hands, so split it into pieces.</summary>
        private static void SplitSkin(int[] triangles, int submesh, List<Vector3> vertices, Dictionary<Part, PartData> parts)
        {
            List<List<int>> islands = Islands(triangles, vertices);
            if (islands.Count == 0) return;

            // Highest piece is the face. The hand nearest the stick holds it.
            islands = islands.OrderByDescending(island => BoundsOf(vertices, island).center.y).ToList();
            parts[Part.Head].PieceFor(submesh).AddRange(islands[0]);

            var hands = islands.Skip(1).OrderByDescending(island => island.Count).Take(2).ToList();
            var leftovers = islands.Skip(1).Except(hands).ToList();
            foreach (List<int> extra in leftovers) parts[Part.Body].PieceFor(submesh).AddRange(extra);
            if (hands.Count == 0) return;

            Bounds stick = BoundsOf(vertices, parts[Part.Stick].pieces.SelectMany(p => p.triangles));
            Vector3 grip = new Vector3(stick.center.x, stick.max.y, stick.center.z);
            hands = hands.OrderBy(island => (BoundsOf(vertices, island).center - grip).sqrMagnitude).ToList();

            parts[Part.StickHand].PieceFor(submesh).AddRange(hands[0]);
            if (hands.Count > 1) parts[Part.FreeHand].PieceFor(submesh).AddRange(hands[1]);
        }

        /// <summary>Groups triangles into connected pieces.</summary>
        private static List<List<int>> Islands(int[] triangles, List<Vector3> vertices)
        {
            var parent = new Dictionary<int, int>();
            var byPosition = new Dictionary<Vector3Int, int>();

            int Find(int x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]];
                    x = parent[x];
                }
                return x;
            }

            void Union(int a, int b)
            {
                int ra = Find(a), rb = Find(b);
                if (ra != rb) parent[ra] = rb;
            }

            foreach (int index in triangles)
            {
                if (parent.ContainsKey(index)) continue;
                parent[index] = index;
                Vector3 p = vertices[index] * 10000f;
                var key = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
                if (byPosition.TryGetValue(key, out int twin)) Union(index, twin);
                else byPosition[key] = index;
            }

            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                Union(triangles[t], triangles[t + 1]);
                Union(triangles[t], triangles[t + 2]);
            }

            var groups = new Dictionary<int, List<int>>();
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                int root = Find(triangles[t]);
                if (!groups.TryGetValue(root, out List<int> list)) groups[root] = list = new List<int>();
                list.Add(triangles[t]);
                list.Add(triangles[t + 1]);
                list.Add(triangles[t + 2]);
            }
            return groups.Values.ToList();
        }

        private static Mesh BuildMesh(string name, PartData part, List<Vector3> vertices, List<Vector3> normals,
            List<Vector4> tangents, List<Vector2> uvs, List<Color> colours)
        {
            var remap = new Dictionary<int, int>();
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<Vector4>();
            var uv = new List<Vector2>();
            var c = new List<Color>();
            var submeshes = new List<List<int>>();

            foreach ((int _, List<int> triangles) in part.pieces)
            {
                var list = new List<int>(triangles.Count);
                foreach (int index in triangles)
                {
                    if (!remap.TryGetValue(index, out int newIndex))
                    {
                        newIndex = v.Count;
                        remap[index] = newIndex;
                        v.Add(vertices[index] - part.pivot);
                        if (normals.Count == vertices.Count) n.Add(normals[index]);
                        if (tangents.Count == vertices.Count) t.Add(tangents[index]);
                        if (uvs.Count == vertices.Count) uv.Add(uvs[index]);
                        if (colours.Count == vertices.Count) c.Add(colours[index]);
                    }
                    list.Add(newIndex);
                }
                submeshes.Add(list);
            }

            var mesh = new Mesh { name = name };
            if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);
            if (n.Count == v.Count) mesh.SetNormals(n);
            if (t.Count == v.Count) mesh.SetTangents(t);
            if (uv.Count == v.Count) mesh.SetUVs(0, uv);
            if (c.Count == v.Count) mesh.SetColors(c);
            mesh.subMeshCount = submeshes.Count;
            for (int s = 0; s < submeshes.Count; s++) mesh.SetTriangles(submeshes[s], s, true);
            mesh.RecalculateBounds();
            if (n.Count != v.Count) mesh.RecalculateNormals();
            return mesh;
        }

        //Prefab

        private static GameObject BuildPrefab(Dictionary<Part, PartData> parts, Dictionary<Part, Mesh> meshes, Material[] materials,
            Vector3 glassesCentre, float headTop, Vector3 stickTip)
        {
            var root = new GameObject("Gogo");
            try
            {
                var mover = root.AddComponent<GrandmaMover>();
                var voice = root.AddComponent<BabbleVoice>();
                var speaker = root.AddComponent<DialogueSpeaker>();
                var controller = root.AddComponent<GrandmaController>();

                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                var rig = visual.AddComponent<GrandmaRig>();
                var animator = visual.AddComponent<GrandmaAnimator>();

                Transform body = Node("Body", visual.transform, Vector3.zero);
                Transform squash = Node("Squash", body, Vector3.zero);
                Transform head = Node("Head", body, parts[Part.Head].pivot);
                Transform freeHand = Node("FreeHand", body, parts[Part.FreeHand].pivot);
                Transform stickHand = Node("StickHand", body, parts[Part.StickHand].pivot);
                Transform stick = Node("Stick", stickHand, parts[Part.Stick].pivot - parts[Part.StickHand].pivot);

                AddRenderer(Part.Body, squash, parts, meshes, materials);
                AddRenderer(Part.Head, head, parts, meshes, materials);
                AddRenderer(Part.FreeHand, freeHand, parts, meshes, materials);
                AddRenderer(Part.StickHand, stickHand, parts, meshes, materials);
                AddRenderer(Part.Stick, stick, parts, meshes, materials);

                Transform lookPoint = Node("LookPoint", head, glassesCentre - parts[Part.Head].pivot);
                Transform bubbleAnchor = Node("BubbleAnchor", root.transform, new Vector3(0f, headTop + 0.25f, 0f));

                var bubbleObject = new GameObject("SpeechBubble", typeof(RectTransform));
                bubbleObject.layer = 5;
                bubbleObject.transform.SetParent(bubbleAnchor, false);
                bubbleObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var bubble = bubbleObject.AddComponent<SpeechBubble>();
                bubble.Configure(TutorialArt.Font(), TutorialArt.Bar);

                rig.body = body;
                rig.squash = squash;
                rig.head = head;
                rig.freeHand = freeHand;
                rig.stickHand = stickHand;
                rig.stick = stick;
                rig.lookPoint = lookPoint;
                rig.bubbleAnchor = bubbleAnchor;
                rig.height = TargetHeight;
                Vector3 tipLocal = stickTip - parts[Part.Stick].pivot;
                rig.stickTipLocal = tipLocal.sqrMagnitude > 0.0001f ? tipLocal.normalized : Vector3.down;

                SetReference(animator, "mover", mover);
                SetReference(speaker, "bubble", bubble);
                SetReference(speaker, "voice", voice);
                SetReference(speaker, "lookPoint", lookPoint);
                var profile = AssetDatabase.LoadAssetAtPath<SpeakerProfile>(SpeakerPath);
                if (profile != null) SetReference(speaker, "profile", profile);
                SetReference(controller, "mover", mover);
                SetReference(controller, "animator", animator);
                SetReference(controller, "speaker", speaker);

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static void AddRenderer(Part part, Transform holder, Dictionary<Part, PartData> parts, Dictionary<Part, Mesh> meshes, Material[] materials)
        {
            if (!meshes.TryGetValue(part, out Mesh mesh)) return;

            var meshObject = new GameObject(part + "Mesh");
            meshObject.transform.SetParent(holder, false);
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = parts[part].pieces
                .Select(p => p.submesh < materials.Length ? materials[p.submesh] : null)
                .ToArray();
        }

        //Helpers

        private static string MaterialName(Material[] materials, int submesh)
        {
            if (submesh < materials.Length && materials[submesh] != null) return materials[submesh].name.ToLowerInvariant();
            // Fallback: the material order in the FBX.
            string[] fallback = { "skin", "glasses", "hair", "shirt", "dress", "stick" };
            return submesh < fallback.Length ? fallback[submesh] : "body";
        }

        private static Bounds BoundsOf(List<Vector3> vertices, IEnumerable<int> indices)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (int index in indices)
            {
                if (!any)
                {
                    bounds = new Bounds(vertices[index], Vector3.zero);
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(vertices[index]);
                }
            }
            return bounds;
        }

        private static Vector3 LowestPoint(List<Vector3> vertices, PartData part)
        {
            Vector3 lowest = part.pivot;
            float min = float.MaxValue;
            foreach ((int _, List<int> triangles) in part.pieces)
            {
                foreach (int index in triangles)
                {
                    if (vertices[index].y >= min) continue;
                    min = vertices[index].y;
                    lowest = vertices[index];
                }
            }
            return lowest;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Gogo Baker] Field '" + field + "' not found on " + target.GetType().Name);
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
