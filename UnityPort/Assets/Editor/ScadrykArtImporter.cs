#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Automatically imports the 4x4 character sheets as individually named sprites.</summary>
public sealed class ScadrykArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/ScadrykArt/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.spritePixelsPerUnit = 220f;

        if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;
        string prefix = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool isFourByFour = prefix == "Listener" || prefix == "PlayerAttack" || prefix == "StoneGuardian" || prefix == "StoneGuardianAttack" ||
            prefix == "NameWeaver" || prefix == "NameWeaverAttack" || prefix == "Enemies" || prefix == "EnemyAttacks" ||
            prefix == "Bosses" || prefix == "BossesMidgame" || prefix == "BossesLate" || prefix == "BossesFinalWave" || prefix == "BossesEndgame";
        if (!isFourByFour) return;

        const int columns = 4, rows = 4;
        const float width = 1254f, height = 1254f;
        float cellWidth = width / columns;
        float cellHeight = height / rows;
        var slices = new SpriteMetaData[columns * rows];
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            int index = row * columns + column;
            slices[index] = new SpriteMetaData
            {
                name = prefix + "_" + index.ToString("00"),
                rect = new Rect(column * cellWidth, height - (row + 1) * cellHeight, cellWidth, cellHeight),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0.06f)
            };
        }
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritesheet = slices;
    }
}
#endif
