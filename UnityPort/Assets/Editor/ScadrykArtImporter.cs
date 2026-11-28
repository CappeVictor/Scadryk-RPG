#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Prepares Scadryk source art for GPU-only runtime sprite slicing.</summary>
public sealed class ScadrykArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/ScadrykArt/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        // Runtime uses Sprite.Create/GUI.DrawTexture only; no code reads texture pixels on the CPU.
        // Discard the CPU-side copy after import to lower player memory use.
        importer.isReadable = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.maxTextureSize = 2048;
    }
}
#endif
