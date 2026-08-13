using UnityEditor;
using UnityEngine;

internal sealed class VideoFrameSequenceImporter : AssetPostprocessor
{
    private const string FrameSequencePath = "/Resources/VideoFrames/";

    public override uint GetVersion()
    {
        return 1;
    }

    private void OnPreprocessTexture()
    {
        string normalizedPath = assetPath.Replace('\\', '/');
        if (!normalizedPath.Contains(FrameSequencePath))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.compressionQuality = 50;
        importer.isReadable = false;
    }
}
