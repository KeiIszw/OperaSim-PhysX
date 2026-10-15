#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class Mid360VisualInstaller
{
    const string Folder = "Assets/Models/LivoxMid360";
    const string VisualName = "Livox MID-360 Visual";
    [Serializable] class Data { public float[] vertices; public int[] glass; public int[] housing; public int[] connector; }
    // CAD revision 2 includes surface shells omitted by solid-only import.
    static Mid360VisualInstaller() { EditorApplication.delayCall += AutoInstall; }
    public static void InstallBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SimpleScene.unity");
        Install();
        if (!File.Exists("Logs/Mid360VisualValidation.txt")) throw new Exception("MID-360 installation did not complete");
    }
    static void AutoInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name == "SimpleScene" && (!scene.isDirty || UnityEngine.Object.FindObjectsOfType<Transform>().Any(t => t.name == "Mid360Visual" || t.name == VisualName))) Install();
    }
    static Material Material(string name, Color color, float metallic, float smoothness)
    {
        string path = Folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color; mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(mat); return mat;
    }
    [MenuItem("Tools/OperaSim/Install MID-360 Visual")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var candidates = UnityEngine.Object.FindObjectsOfType<Transform>().Where(t => t.name == "lidar1" &&
            t.GetComponentsInChildren<Transform>(true).Any(c => c.name == "Mid-360")).ToArray();
        if (candidates.Length != 1) { Debug.LogWarning("MID-360 visual: expected one lidar1, found " + candidates.Length); return; }
        Transform lidar = candidates[0];
        bool alreadyInstalled = lidar.Find(VisualName) != null || lidar.Find("Mid360Visual") != null;
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(Folder + "/mid360-mesh.json"));
        var mesh = new Mesh { name = "Livox MID-360 official CAD", indexFormat = IndexFormat.UInt32 };
        mesh.vertices = Enumerable.Range(0,data.vertices.Length/3).Select(i => new Vector3(data.vertices[i*3],data.vertices[i*3+1],data.vertices[i*3+2])).ToArray();
        mesh.subMeshCount = 3; mesh.SetTriangles(data.glass,0); mesh.SetTriangles(data.housing,1); mesh.SetTriangles(data.connector,2);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string meshPath = Folder + "/Mid360.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing == null) AssetDatabase.CreateAsset(mesh,meshPath);
        else { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
        if (alreadyInstalled)
        {
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/Mid360VisualValidation.txt", "Updated CAD surfaces; vertices: " + mesh.vertexCount + "; triangles: " + (mesh.triangles.Length / 3));
            Debug.Log("MID-360 visual mesh updated including CAD surface shells");
            return;
        }
        var materials = new[] {
            Material("OpticalWindow",new Color(.035f,.025f,.032f),.15f,.92f),
            Material("AluminiumHousing",new Color(.38f,.40f,.43f),.8f,.45f),
            Material("Connector",new Color(.10f,.11f,.12f),.65f,.55f) };
        var model = new GameObject(VisualName);
        model.AddComponent<MeshFilter>().sharedMesh = mesh;
        model.AddComponent<MeshRenderer>().sharedMaterials = materials;
        string prefabPath = Folder + "/Mid360Visual.prefab";
        PrefabUtility.SaveAsPrefabAsset(model,prefabPath);
        UnityEngine.Object.DestroyImmediate(model);
        model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),lidar);
        model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.one;
        // Disable only the original visual renderer: sensor, publisher and transforms stay intact.
        var oldModel = lidar.Find("Mid-360/Model");
        if (oldModel != null)
            foreach (var renderer in oldModel.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = false;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(lidar.gameObject.scene);
        EditorSceneManager.SaveScene(lidar.gameObject.scene);
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Mid360VisualValidation.txt", "Installed official CAD visual at " + lidar.name + "\nVertices: " + mesh.vertexCount + "\nBounds (m): " + mesh.bounds + "\nNo colliders added; sensor transforms unchanged.\n");
        Debug.Log("MID-360 official CAD visual installed on lidar1");
    }
}
#endif
