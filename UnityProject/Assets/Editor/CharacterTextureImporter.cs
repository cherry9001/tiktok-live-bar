#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TikTokLiveGame.Editor
{
    public sealed class CharacterTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            bool isCharacter = assetPath.StartsWith("Assets/Resources/Characters/");
            bool isBanner = assetPath.StartsWith("Assets/Resources/Banners/");
            bool isStage = assetPath.StartsWith("Assets/Resources/Stage/");
            bool isNpcAvatar = assetPath.StartsWith("Assets/Resources/NpcAvatars/");
            if (!isCharacter && !isBanner && !isStage && !isNpcAvatar) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = isBanner ? 180f : isNpcAvatar ? 128f : 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = isCharacter ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = isNpcAvatar ? 256 : isCharacter ? 512 : 2048;
        }
    }
}
#endif
