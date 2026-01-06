using UnityEditor;
using UnityEngine;

public static class VertexColorUnlitDefaultsApplier
{
    private const string ShaderName = "Custom/VertexColorUnlitURP";

    // Keep these in sync with the shader's Properties defaults.
    private const float VertexJitterDefault = 1f;
    private static readonly Vector4 JitterResolutionDefault = new(1920f, 1080f, 0f, 0f);
    private const float JitterPixelScaleDefault = 3f;

    private const float AffineMappingDefault = 1f;
    private const float AffineBlendDefault = 1f;

    [MenuItem("Tools/PS1/Apply VertexColorUnlit Defaults (All Materials)")]
    public static void ApplyDefaultsToAllMaterials()
    {
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"Could not find shader '{ShaderName}'.");
            return;
        }

        var materialGuids = AssetDatabase.FindAssets("t:Material");
        int updatedCount = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader != shader)
                    continue;

                bool changed = false;

                if (material.HasProperty("_VertexJitter"))
                {
                    material.SetFloat("_VertexJitter", VertexJitterDefault);
                    changed = true;
                }

                if (material.HasProperty("_JitterResolution"))
                {
                    material.SetVector("_JitterResolution", JitterResolutionDefault);
                    changed = true;
                }

                if (material.HasProperty("_JitterPixelScale"))
                {
                    material.SetFloat("_JitterPixelScale", JitterPixelScaleDefault);
                    changed = true;
                }

                if (material.HasProperty("_AffineMapping"))
                {
                    material.SetFloat("_AffineMapping", AffineMappingDefault);
                    changed = true;
                }

                if (material.HasProperty("_AffineBlend"))
                {
                    material.SetFloat("_AffineBlend", AffineBlendDefault);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(material);
                    updatedCount++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"Applied VertexColorUnlit defaults to {updatedCount} material(s).");
    }
}
