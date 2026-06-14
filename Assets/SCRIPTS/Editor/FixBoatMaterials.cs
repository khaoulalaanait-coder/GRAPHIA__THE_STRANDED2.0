using UnityEngine;
using UnityEditor;
using System.IO;

// Run via: Tools > Fix Boat Materials
public static class FixBoatMaterials
{
    const string BASE = "Assets/Island_V2";

    [MenuItem("Tools/Fix Boat Materials")]
    static void Fix()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            FixTextures();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        // Materials must be created after textures are re-imported
        CreateAndAssignMaterials();

        AssetDatabase.Refresh();
        Debug.Log("[FixBoatMaterials] Done — all URP Lit materials created and assigned.");
    }

    // -------------------------------------------------------------------------
    // Step 1: fix texture import settings before creating materials
    // -------------------------------------------------------------------------
    static void FixTextures()
    {
        // SteeringWheel
        SetNormal  ($"{BASE}/SteeringWheel/textures/Material_Normal.png");
        SetLinear  ($"{BASE}/SteeringWheel/textures/Material_Metallic.png");
        SetLinear  ($"{BASE}/SteeringWheel/textures/Material_Roughness.png");

        // Barrel
        SetNormal  ($"{BASE}/barrel/textures/barrel_Normal.png");
        SetLinear  ($"{BASE}/barrel/textures/barrel_Metallic.png");
        SetLinear  ($"{BASE}/barrel/textures/barrel_Roughness.png");
        SetLinear  ($"{BASE}/barrel/textures/barrel_Mixed_AO.png");

        // Hall Anchor
        SetNormal  ($"{BASE}/hall-anchor/textures/HallAnchor_new_normal.png");
        SetLinear  ($"{BASE}/hall-anchor/textures/HallAnchor_new_rougness.png");
        SetLinear  ($"{BASE}/hall-anchor/textures/HallAnchor_AO.png");

        // Rope
        SetNormal  ($"{BASE}/19th-century-explorers-rope/textures/texture_pbr_v128_normal_0.png");
        SetLinear  ($"{BASE}/19th-century-explorers-rope/textures/texture_pbr_v128_metallic-texture_pbr_v128_roughness_2@chann.png");
    }

    static void SetNormal(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogWarning($"[FixBoatMaterials] Not found: {path}"); return; }
        if (imp.textureType == TextureImporterType.NormalMap) return;
        imp.textureType = TextureImporterType.NormalMap;
        imp.SaveAndReimport();
    }

    static void SetLinear(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogWarning($"[FixBoatMaterials] Not found: {path}"); return; }
        if (!imp.sRGBTexture) return;
        imp.sRGBTexture = false;
        imp.SaveAndReimport();
    }

    // -------------------------------------------------------------------------
    // Step 2: create materials and remap model importers
    // -------------------------------------------------------------------------
    static void CreateAndAssignMaterials()
    {
        // SteeringWheel — wood/metal wheel
        var matWheel = MakeMaterial(
            $"{BASE}/SteeringWheel/Mat_SteeringWheel.mat",
            baseColor:  $"{BASE}/SteeringWheel/textures/Material_BaseColor.png",
            normal:     $"{BASE}/SteeringWheel/textures/Material_Normal.png",
            metallic:   $"{BASE}/SteeringWheel/textures/Material_Metallic.png",
            roughness:  $"{BASE}/SteeringWheel/textures/Material_Roughness.png",
            ao:         null,
            smoothness: 0.35f,
            metallicStr: 1f
        );
        AssignToModel($"{BASE}/SteeringWheel/SteeringWheel.fbx", matWheel);

        // Barrel — wooden barrel with iron bands
        var matBarrel = MakeMaterial(
            $"{BASE}/barrel/Mat_Barrel.mat",
            baseColor:  $"{BASE}/barrel/textures/barrel_Base_Color.png",
            normal:     $"{BASE}/barrel/textures/barrel_Normal.png",
            metallic:   $"{BASE}/barrel/textures/barrel_Metallic.png",
            roughness:  $"{BASE}/barrel/textures/barrel_Roughness.png",
            ao:         $"{BASE}/barrel/textures/barrel_Mixed_AO.png",
            smoothness: 0.25f,
            metallicStr: 1f
        );
        AssignToModel($"{BASE}/barrel/source/model/barrel_f.FBX", matBarrel);

        // Hall Anchor — rusted iron anchor
        var matAnchor = MakeMaterial(
            $"{BASE}/hall-anchor/Mat_HallAnchor.mat",
            baseColor:  $"{BASE}/hall-anchor/textures/HallAnchor_new_color.png",
            normal:     $"{BASE}/hall-anchor/textures/HallAnchor_new_normal.png",
            metallic:   null,  // no metallic map; set metallic value directly
            roughness:  $"{BASE}/hall-anchor/textures/HallAnchor_new_rougness.png",
            ao:         $"{BASE}/hall-anchor/textures/HallAnchor_AO.png",
            smoothness: 0.15f,
            metallicStr: 0.7f  // iron anchor — fairly metallic
        );
        AssignToModel($"{BASE}/hall-anchor/source/HallAnchor_sketchfab.fbx", matAnchor);

        // Rope — 19th century explorer rope
        var matRope = MakeMaterial(
            $"{BASE}/19th-century-explorers-rope/Mat_Rope.mat",
            baseColor:  $"{BASE}/19th-century-explorers-rope/textures/texture_pbr_v128_1.png",
            normal:     $"{BASE}/19th-century-explorers-rope/textures/texture_pbr_v128_normal_0.png",
            metallic:   $"{BASE}/19th-century-explorers-rope/textures/texture_pbr_v128_metallic-texture_pbr_v128_roughness_2@chann.png",
            roughness:  null,
            ao:         null,
            smoothness: 0.1f,  // rope is rough
            metallicStr: 1f
        );
        AssignToModel($"{BASE}/19th-century-explorers-rope/source/exporer rope .glb", matRope);
    }

    // -------------------------------------------------------------------------
    // Creates (or overwrites) one URP Lit material at matPath
    // URP Lit: _BaseMap=albedo, _BumpMap=normal, _MetallicGlossMap=metallic,
    //          _OcclusionMap=AO, _Smoothness=smoothness slider, _Metallic=metallic slider
    // Roughness textures don't map 1:1 to URP (URP uses Smoothness = 1-Roughness).
    // We set a low _Smoothness value when only a roughness map is provided.
    // -------------------------------------------------------------------------
    static Material MakeMaterial(
        string matPath,
        string baseColor,
        string normal,
        string metallic,
        string roughness,     // informational only in URP — drives _Smoothness default
        string ao,
        float smoothness,
        float metallicStr)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[FixBoatMaterials] URP Lit shader not found. Is URP installed?");
            return null;
        }

        // Delete stale asset so CreateAsset doesn't throw
        if (File.Exists(Path.GetFullPath(matPath)))
            AssetDatabase.DeleteAsset(matPath);

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", Color.white);

        Texture2D texBC = Load<Texture2D>(baseColor);
        if (texBC) mat.SetTexture("_BaseMap", texBC);

        Texture2D texN = Load<Texture2D>(normal);
        if (texN)
        {
            mat.SetTexture("_BumpMap", texN);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        Texture2D texM = Load<Texture2D>(metallic);
        if (texM)
        {
            mat.SetTexture("_MetallicGlossMap", texM);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Metallic", metallicStr);
        }
        else
        {
            mat.SetFloat("_Metallic", metallicStr);
        }

        // URP uses Smoothness, not Roughness. A roughness texture would need
        // inversion to work correctly; we approximate with a flat slider value.
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_SmoothnessTextureChannel", 0f); // from metallic map alpha

        Texture2D texAO = Load<Texture2D>(ao);
        if (texAO)
        {
            mat.SetTexture("_OcclusionMap", texAO);
            mat.SetFloat("_OcclusionStrength", 1f);
            mat.EnableKeyword("_OCCLUSIONMAP");
        }

        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    // -------------------------------------------------------------------------
    // Remaps every embedded material in the model to our new material
    // -------------------------------------------------------------------------
    static void AssignToModel(string modelPath, Material mat)
    {
        if (mat == null) return;

        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[FixBoatMaterials] No ModelImporter at: {modelPath}");
            return;
        }

        // Load all sub-assets — embedded Material objects carry the source name
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        bool remapped = false;
        foreach (var sub in subAssets)
        {
            if (sub is Material embeddedMat)
            {
                var id = new AssetImporter.SourceAssetIdentifier(embeddedMat);
                importer.AddRemap(id, mat);
                Debug.Log($"[FixBoatMaterials] Remapped '{embeddedMat.name}' → {mat.name}");
                remapped = true;
            }
        }

        if (!remapped)
            Debug.LogWarning($"[FixBoatMaterials] No embedded materials found in {modelPath}. " +
                             "Drag the created .mat file onto the renderer manually.");

        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
    }

    static T Load<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path)) return null;
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogWarning($"[FixBoatMaterials] Could not load: {path}");
        return asset;
    }
}
