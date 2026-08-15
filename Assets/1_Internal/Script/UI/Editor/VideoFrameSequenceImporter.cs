using UnityEditor;
using UnityEngine;

internal sealed class VideoFrameSequenceImporter : AssetPostprocessor
{
    private const string FrameSequencePath = "/Resources/VideoFrames/";
    private const string ProcessedVideoPath = "/Data/Video_Processed/";

    public override uint GetVersion()
    {
        return 2;
    }

    private void OnPreprocessTexture()
    {
        string normalizedPath = assetPath.Replace('\\', '/');
        bool isFrameSheet = normalizedPath.Contains(FrameSequencePath);
        bool isProcessedThumbnail = normalizedPath.Contains(ProcessedVideoPath) &&
                                    normalizedPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase);
        if (!isFrameSheet && !isProcessedThumbnail)
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = isProcessedThumbnail ? TextureImporterType.Sprite : TextureImporterType.Default;
        if (isProcessedThumbnail)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
        }
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = isProcessedThumbnail ? 512 : 2048;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.compressionQuality = 50;
        importer.isReadable = false;
    }
}
