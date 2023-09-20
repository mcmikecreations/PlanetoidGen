using Assimp;
using Assimp.Configs;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ModelLoadingDebugger : MonoBehaviour
{
    void Start()
    {
        LoadModel();
    }

    private void LoadModel()
    {
        var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "racket.fbx");
        Debug.Log($"Loading model from {filePath}.");
        if (!File.Exists(filePath))
        {
            Debug.Log("File not found.");
            return;
        }

        //AssimpUnity.ManuallyInitialize();
        //Debug.Log($"Is Assimp initialized: {AssimpUnity.IsAssimpAvailable}.");

        var ppSteps = PostProcessPreset.TargetRealTimeQuality | PostProcessSteps.Triangulate;// | PostProcessSteps.FlipWindingOrder;
        var importer = new AssimpContext();
        importer.SetConfig(new NormalSmoothingAngleConfig(66f));
        var scene = importer.ImportFile(filePath, ppSteps);

        var mesh = new UnityEngine.Mesh();

        var inputMesh = scene.Meshes[0];
        mesh.vertices = inputMesh.Vertices.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
        mesh.triangles = inputMesh.Faces.SelectMany(x => x.Indices).ToArray();
        mesh.normals = inputMesh.Normals.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
        mesh.tangents = inputMesh.Tangents.Select(x => new Vector4(x.X, x.Y, x.Z, 1f)).ToArray();
        mesh.uv = inputMesh.TextureCoordinateChannels[0].Select(x => new Vector2(x.X, x.Y)).ToArray();

        var material = new UnityEngine.Material(Shader.Find("Standard"));

        var inputMaterial = scene.Materials[inputMesh.MaterialIndex];

        var texture = new Texture2D(2, 2);
        var inputTexture = inputMaterial.TextureDiffuse;
        var imageData = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", inputTexture.FilePath));
        texture.LoadImage(imageData);

        material.mainTexture = texture;
        var filter = GetComponent<MeshFilter>();
        filter.mesh = mesh;
        var renderer = GetComponent<MeshRenderer>();
        renderer.material = material;
    }
}
