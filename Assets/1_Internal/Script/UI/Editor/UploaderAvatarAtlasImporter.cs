#if UNITY_EDITOR
using UnityEditor;

internal sealed class UploaderAvatarAtlasImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith("/Resources/UploaderAvatars/uploader-avatar-atlas.png"))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.sRGBTexture = true;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.compressionQuality = 60;
        importer.isReadable = false;
    }

    public override uint GetVersion() => 1;
}
#endif
