using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SmartMeshTool : EditorWindow
{
    private GameObject target;

    [MenuItem("Tools/Smart Mesh Tool")]
    public static void ShowWindow()
    {
        GetWindow<SmartMeshTool>("Smart Mesh Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("⚙️ Инструменты для мешей", EditorStyles.boldLabel);
        target = (GameObject)EditorGUILayout.ObjectField("Объект", target, typeof(GameObject), true);

        GUILayout.Space(10);
        if (GUILayout.Button("🧩 Разделить меш на части (и сохранить)", GUILayout.Height(30)))
        {
            if (target == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Выберите объект со MeshFilter!", "OK");
                return;
            }
            SplitAndSave(target);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("🎨 Дублировать материалы у выделенных объектов", GUILayout.Height(30)))
        {
            DuplicateMaterialsForSelected();
        }
    }

    // =====================================================
    // 🧩 Разделение меша
    // =====================================================
    private void SplitAndSave(GameObject obj)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();

        if (mf == null || mf.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "У объекта нет MeshFilter или меша!", "OK");
            return;
        }

        Mesh mesh = mf.sharedMesh;
        Material material = mr ? mr.sharedMaterial : null;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // --- создаём группы треугольников ---
        List<List<int>> groups = new List<List<int>>();
        bool[] visited = new bool[triangles.Length / 3];
        Dictionary<int, List<int>> vertexToTris = new Dictionary<int, List<int>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            for (int j = 0; j < 3; j++)
            {
                int v = triangles[i + j];
                if (!vertexToTris.ContainsKey(v))
                    vertexToTris[v] = new List<int>();
                vertexToTris[v].Add(i / 3);
            }
        }

        for (int i = 0; i < triangles.Length / 3; i++)
        {
            if (visited[i]) continue;

            Queue<int> queue = new Queue<int>();
            List<int> currentGroup = new List<int>();

            queue.Enqueue(i);
            visited[i] = true;

            while (queue.Count > 0)
            {
                int triIndex = queue.Dequeue();
                currentGroup.Add(triIndex);

                for (int j = 0; j < 3; j++)
                {
                    int vertex = triangles[triIndex * 3 + j];
                    foreach (int neighbor in vertexToTris[vertex])
                    {
                        if (!visited[neighbor])
                        {
                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            groups.Add(currentGroup);
        }

        if (groups.Count <= 1)
        {
            EditorUtility.DisplayDialog("Результат", "Меш состоит из одной связной части — разделение не требуется.", "OK");
            return;
        }

        string meshFolder = "Assets/SavedMeshes";
        string materialFolder = "Assets/SavedMaterials";
        if (!Directory.Exists(meshFolder)) Directory.CreateDirectory(meshFolder);
        if (!Directory.Exists(materialFolder)) Directory.CreateDirectory(materialFolder);

        Undo.IncrementCurrentGroup();
        var parent = obj.transform.parent;

        for (int g = 0; g < groups.Count; g++)
        {
            Mesh newMesh = new Mesh();
            List<Vector3> newVerts = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUV = new List<Vector2>();
            List<int> newTris = new List<int>();
            Dictionary<int, int> map = new Dictionary<int, int>();

            foreach (int triIndex in groups[g])
            {
                for (int j = 0; j < 3; j++)
                {
                    int oldIndex = triangles[triIndex * 3 + j];
                    if (!map.ContainsKey(oldIndex))
                    {
                        map[oldIndex] = newVerts.Count;
                        newVerts.Add(vertices[oldIndex]);
                        if (mesh.normals.Length > 0) newNormals.Add(mesh.normals[oldIndex]);
                        if (mesh.uv.Length > 0) newUV.Add(mesh.uv[oldIndex]);
                    }
                    newTris.Add(map[oldIndex]);
                }
            }

            newMesh.SetVertices(newVerts);
            if (newNormals.Count > 0) newMesh.SetNormals(newNormals);
            if (newUV.Count > 0) newMesh.SetUVs(0, newUV);
            newMesh.SetTriangles(newTris, 0);
            newMesh.RecalculateBounds();
            newMesh.name = mesh.name + "_part" + g;

            string meshPath = $"{meshFolder}/{obj.name}_Part{g}_Mesh.asset";
            AssetDatabase.CreateAsset(Object.Instantiate(newMesh), meshPath);

            // создаем дубликат материала
            Material newMat = null;
            if (material != null)
            {
                newMat = new Material(material);
                newMat.name = material.name + "_Copy" + g;

                string matPath = $"{materialFolder}/{newMat.name}.mat";
                AssetDatabase.CreateAsset(newMat, matPath);
            }

            GameObject newObj = new GameObject(obj.name + "_Part" + g);
            Undo.RegisterCreatedObjectUndo(newObj, "Split Mesh");
            newObj.transform.SetParent(parent);
            newObj.transform.position = obj.transform.position;
            newObj.transform.rotation = obj.transform.rotation;
            newObj.transform.localScale = obj.transform.localScale;

            var newMF = newObj.AddComponent<MeshFilter>();
            var newMR = newObj.AddComponent<MeshRenderer>();
            newMF.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            newMR.sharedMaterial = newMat;
        }

        Undo.DestroyObjectImmediate(obj);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Готово", $"Разделено на {groups.Count} частей!\nМеши и материалы сохранены в Assets/SavedMeshes и Assets/SavedMaterials.", "OK");
    }

    // =====================================================
    // 🎨 Дублирование материалов у выделенных объектов
    // =====================================================
    private void DuplicateMaterialsForSelected()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите хотя бы один объект!", "OK");
            return;
        }

        string materialFolder = "Assets/SavedMaterials";
        if (!Directory.Exists(materialFolder)) Directory.CreateDirectory(materialFolder);

        foreach (var obj in Selection.gameObjects)
        {
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            Material original = renderer.sharedMaterial;
            Material copy = new Material(original);
            copy.name = original.name + "_Dup_" + obj.name;

            string path = $"{materialFolder}/{copy.name}.mat";
            AssetDatabase.CreateAsset(copy, path);

            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            Debug.Log($"🎨 Материал {original.name} дублирован для {obj.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Готово", "Материалы успешно продублированы!", "OK");
    }
}
