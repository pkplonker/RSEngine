using System.Numerics;
using Engine;
using Engine.Logging;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Editor.Controls;

public class TerrainEditor : IPanel
{
    private int width = 64;
    private int height = 64;
    private float scale = 1.0f;
    private float heightScale = 10.0f;

    private bool isPaintMode = false;
    private float brushSize = 3.0f;
    private float brushStrength = 0.5f;
    private int selectedBrushMode = 0; // 0 = Raise, 1 = Lower, 2 = Flatten, 3 = Smooth
    private float targetHeight = 0.0f;

    private GameObject? currentTerrain;
    private float[,]? heightData;
    private Vector3? lastPaintPosition;
    private IMetadata? terrainMaterial;

    private bool showWireframe = false;
    private bool showNormals = false;
    private readonly GL gl;

    public TerrainEditor(GL gl)
    {
        this.gl = gl;
    }

    public string PanelName { get; set; } = "Terrain Editor";

    public void Draw(IRenderer renderer)
    {
        ImGui.Begin(PanelName);

        DrawCreationPanel();
        ImGui.Separator();
        DrawPaintPanel();
        ImGui.Separator();
        DrawVisualizationPanel();
        ImGui.Separator();
        DrawInfoPanel();

        ImGui.End();

        if (isPaintMode && currentTerrain != null && heightData != null)
        {
            HandleTerrainPainting(renderer);
        }
    }

    private void DrawCreationPanel()
    {
        if (ImGui.CollapsingHeader("Terrain Creation", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.InputInt("Width", ref width);
            width = Math.Clamp(width, 2, 512);

            ImGui.InputInt("Height", ref height);
            height = Math.Clamp(height, 2, 512);

            ImGui.SliderFloat("Scale", ref scale, 0.1f, 10.0f);
            ImGui.SliderFloat("Height Scale", ref heightScale, 0.1f, 50.0f);

            if (ImGui.Button("Create Heightfield", new Vector2(-1, 0)))
            {
                CreateHeightfield();
            }

            if (currentTerrain != null)
            {
                if (ImGui.Button("Regenerate Mesh", new Vector2(-1, 0)))
                {
                    RegenerateMesh();
                }

                if (ImGui.Button("Clear Terrain", new Vector2(-1, 0)))
                {
                    ClearTerrain();
                }
            }
        }
    }

    private void DrawPaintPanel()
    {
        if (ImGui.CollapsingHeader("Terrain Painting"))
        {
            ImGui.Checkbox("Paint Mode (P)", ref isPaintMode);

            if (currentTerrain == null)
            {
                ImGui.TextDisabled("Create a terrain first to enable painting");
                return;
            }

            ImGui.Spacing();
            ImGui.Text("Brush Settings:");
            ImGui.SliderFloat("Brush Size", ref brushSize, 0.5f, 20.0f);
            ImGui.SliderFloat("Brush Strength", ref brushStrength, 0.01f, 5.0f);

            ImGui.Spacing();
            ImGui.Text("Brush Mode:");
            ImGui.RadioButton("Raise", ref selectedBrushMode, 0);
            ImGui.SameLine();
            ImGui.RadioButton("Lower", ref selectedBrushMode, 1);
            ImGui.RadioButton("Flatten", ref selectedBrushMode, 2);
            ImGui.SameLine();
            ImGui.RadioButton("Smooth", ref selectedBrushMode, 3);

            if (selectedBrushMode == 2)
            {
                ImGui.SliderFloat("Target Height", ref targetHeight, 0.0f, heightScale);
            }

            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f),
                "Left Click: Paint | Right Click: Invert | Ctrl: Sample Height");
        }
    }

    private void DrawVisualizationPanel()
    {
        if (ImGui.CollapsingHeader("Visualization"))
        {
            ImGui.Checkbox("Show Wireframe", ref showWireframe);
            ImGui.Checkbox("Show Normals", ref showNormals);

            if (ImGui.Button("Export Heightmap"))
            {
                ExportHeightmap();
            }

            ImGui.SameLine();

            if (ImGui.Button("Import Heightmap"))
            {
                ImportHeightmap();
            }
        }
    }

    private void DrawInfoPanel()
    {
        if (ImGui.CollapsingHeader("Info"))
        {
            if (currentTerrain != null && heightData != null)
            {
                ImGui.Text($"Terrain Size: {heightData.GetLength(0)} x {heightData.GetLength(1)}");
                ImGui.Text($"Vertex Count: {heightData.GetLength(0) * heightData.GetLength(1)}");
                ImGui.Text($"Triangle Count: {(heightData.GetLength(0) - 1) * (heightData.GetLength(1) - 1) * 2}");

                if (lastPaintPosition.HasValue)
                {
                    ImGui.Text(
                        $"Last Paint: ({lastPaintPosition.Value.X:F2}, {lastPaintPosition.Value.Y:F2}, {lastPaintPosition.Value.Z:F2})");
                }
            }
            else
            {
                ImGui.TextDisabled("No terrain created");
            }
        }
    }

    private void CreateHeightfield()
    {
        heightData = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                heightData[x, z] = 0.0f;
            }
        }

        currentTerrain = GameObjectFactory.CreateMesh(SceneController.ActiveScene);
        currentTerrain.Name = "Heightfield Terrain";
        
        // Reset the material for the new terrain
        terrainMaterial = null;

        RegenerateMesh();
    }

    private void RegenerateMesh()
    {
        if (currentTerrain == null || heightData == null) return;

        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new List<uint>();

        int width = heightData.GetLength(0);
        int height = heightData.GetLength(1);

        float offsetX = (width - 1) * scale * 0.5f;
        float offsetZ = (height - 1) * scale * 0.5f;

        // Generate vertices
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float heightValue = heightData[x, z];
                positions.Add(new Vector3(x * scale - offsetX, heightValue, z * scale - offsetZ));
                uvs.Add(new Vector2((float)x / (width - 1), (float)z / (height - 1)));
            }
        }

        // Generate indices
        for (int z = 0; z < height - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                uint topLeft = (uint)(z * width + x);
                uint topRight = (uint)(z * width + x + 1);
                uint bottomLeft = (uint)((z + 1) * width + x);
                uint bottomRight = (uint)((z + 1) * width + x + 1);

                indices.Add(topLeft);
                indices.Add(bottomLeft);
                indices.Add(topRight);

                indices.Add(topRight);
                indices.Add(bottomLeft);
                indices.Add(bottomRight);
            }
        }

        // Calculate normals
        normals = CalculateNormals(positions, indices, width, height);

        // Update mesh
        currentTerrain.GetOrAddComponent<MeshFilter>().UpdateMesh(gl, positions, normals, uvs, indices);
        
        // Only create material once per terrain
        if (terrainMaterial == null)
        {
            terrainMaterial = GetDefaultMaterial();
        }
        
        if (terrainMaterial != null)
        {
            currentTerrain.GetOrAddComponent<MeshRenderer>().MaterialGuid = terrainMaterial.GUID;
        }
        else
        {
            Logger.Error("Failed to get Terrain Default Material");
        }
    }

    private IMetadata GetDefaultMaterial()
    {
        var material = new Material();
        var metaData = new MaterialMetadata();
        ResourceManager.Instance.RegisterRuntimeResource(metaData, material);
        material.Color = new Vector4(25/255.0f, 102/255.0f, 25/255.0f, 1);
        material.ShaderGUID = ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_SHADER).GUID;
        return metaData;
    }

    private List<Vector3> CalculateNormals(List<Vector3> vertices, List<uint> indices, int width, int height)
    {
        var normals = new List<Vector3>(new Vector3[vertices.Count]);

        // Calculate face normals and accumulate
        for (int i = 0; i < indices.Count; i += 3)
        {
            uint i0 = indices[i];
            uint i1 = indices[i + 1];
            uint i2 = indices[i + 2];

            Vector3 v0 = vertices[(int)i0];
            Vector3 v1 = vertices[(int)i1];
            Vector3 v2 = vertices[(int)i2];

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

            normals[(int)i0] += normal;
            normals[(int)i1] += normal;
            normals[(int)i2] += normal;
        }

        // Normalize all normals
        for (int i = 0; i < normals.Count; i++)
        {
            normals[i] = Vector3.Normalize(normals[i]);
        }

        return normals;
    }

    private void HandleTerrainPainting(IRenderer renderer)
    {
        // This is where you'd handle mouse input and raycasting
        // Pseudocode for the painting logic:
        /*
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Ray ray = Camera.ScreenPointToRay(Input.MousePosition);
            if (RaycastTerrain(ray, out Vector3 hitPoint))
            {
                bool invert = Input.GetMouseButton(1);
                PaintAtPosition(hitPoint, invert);
                RegenerateMesh();
            }
        }

        if (Input.GetKey(KeyCode.P))
        {
            _isPaintMode = !_isPaintMode;
        }
        */
    }

    private void PaintAtPosition(Vector3 worldPosition, bool invert = false)
    {
        if (heightData == null) return;

        int width = heightData.GetLength(0);
        int height = heightData.GetLength(1);

        float offsetX = (width - 1) * scale * 0.5f;
        float offsetZ = (height - 1) * scale * 0.5f;

        // Convert world position to heightfield coordinates
        int centerX = (int)Math.Round((worldPosition.X + offsetX) / scale);
        int centerZ = (int)Math.Round((worldPosition.Z + offsetZ) / scale);

        int brushRadius = (int)Math.Ceiling(brushSize);

        for (int z = -brushRadius; z <= brushRadius; z++)
        {
            for (int x = -brushRadius; x <= brushRadius; x++)
            {
                int px = centerX + x;
                int pz = centerZ + z;

                if (px < 0 || px >= width || pz < 0 || pz >= height) continue;

                float distance = MathF.Sqrt(x * x + z * z);
                if (distance > brushSize) continue;

                // Falloff calculation
                float falloff = 1.0f - (distance / brushSize);
                falloff = falloff * falloff; // Smooth falloff

                float strength = brushStrength * falloff;
                if (invert) strength = -strength;

                switch (selectedBrushMode)
                {
                    case 0: // Raise
                        heightData[px, pz] += strength;
                        break;
                    case 1: // Lower
                        heightData[px, pz] -= strength;
                        break;
                    case 2: // Flatten
                        heightData[px, pz] = MathHelper.Lerp(heightData[px, pz], targetHeight, falloff * 0.1f);
                        break;
                    case 3: // Smooth
                        heightData[px, pz] = SmoothHeight(px, pz, falloff * 0.1f);
                        break;
                }

                heightData[px, pz] = Math.Clamp(heightData[px, pz], 0, heightScale);
            }
        }

        lastPaintPosition = worldPosition;
    }

    private float SmoothHeight(int x, int z, float strength)
    {
        if (heightData == null) return 0;

        int width = heightData.GetLength(0);
        int height = heightData.GetLength(1);

        float sum = 0;
        int count = 0;

        for (int oz = -1; oz <= 1; oz++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                int nx = x + ox;
                int nz = z + oz;

                if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                {
                    sum += heightData[nx, nz];
                    count++;
                }
            }
        }

        float average = sum / count;
        return MathHelper.Lerp(heightData[x, z], average, strength);
    }

    private void ClearTerrain()
    {
        if (heightData != null)
        {
            for (int x = 0; x < heightData.GetLength(0); x++)
            {
                for (int z = 0; z < heightData.GetLength(1); z++)
                {
                    heightData[x, z] = 0;
                }
            }

            RegenerateMesh();
        }
    }

    private void ExportHeightmap()
    {
        //todo
    }

    private void ImportHeightmap()
    {
        //todo
    }
}

public static class MathHelper
{
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0, 1);
    }
}