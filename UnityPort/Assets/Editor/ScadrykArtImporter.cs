#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Prepares Scadryk source art so runtime can slice its sprite sheets safely.</summary>
public sealed class ScadrykArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/ScadrykArt/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.maxTextureSize = 2048;
    }
}
#endif
