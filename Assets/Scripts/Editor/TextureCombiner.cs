using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Tuntenfisch.Editor
{
    public class TextureCombiner : AssetPostprocessor
    {
        private const string c_metallic_suffix = "_metallic";
        private const string c_occlusion_suffix = "_occlusion";
        private const string c_height_suffix = "_height";
        private const string c_smoothness_suffix = "_smoothness";
        private const string c_roughness_suffix = "_roughness";

        private readonly string[] suffixes = new string[] { c_metallic_suffix, c_occlusion_suffix, c_height_suffix, c_smoothness_suffix, c_roughness_suffix };

        void OnPreprocessTexture()
        {
            string textureAssetFilename = Path.GetFileNameWithoutExtension(assetPath);

            if (suffixes.All(suffix => !textureAssetFilename.EndsWith(suffix)))
            {
                return;
            }

            // Sets some required import values for reading the texture's pixels for combining.
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            textureImporter.isReadable = true;
        }

        private void OnPostprocessTexture(Texture2D assetTexture)
        {
            string textureAssetFilename = Path.GetFileNameWithoutExtension(assetPath);

            if (suffixes.All(suffix => !textureAssetFilename.EndsWith(suffix)))
            {
                return;
            }

            string textureAssetBaseFilename = textureAssetFilename.Substring(0, textureAssetFilename.LastIndexOf('_'));
            string textureAssetDirectory = Path.GetDirectoryName(assetPath);

            Texture2D[] textures = new Texture2D[suffixes.Length];

            for (int index = 0; index < textures.Length; index++)
            {
                string suffix = suffixes[index];
                string textureFilePath = Path.Combine(textureAssetDirectory, textureAssetBaseFilename + suffix + ".png");

                if (File.Exists(textureFilePath))
                {
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFilePath);
                    // Load asset returns null for the texture that triggered this postprocessor.
                    if (texture == null)
                    {
                        textures[index] = assetTexture;
                    }
                    else if (texture.width != assetTexture.width || texture.height != assetTexture.height)
                    {
                        if (Debug.isDebugBuild)
                        {
                            Debug.LogError($"Texture dimensions for combining aren't all of equal size. Aborting.");
                        }

                        return;
                    }
                    else
                    {
                        textures[index] = texture;
                    }
                }
            }

            if (textures.Count((texture) => texture != null) < 2)
            {
                return;
            }

            // If we found both a smoothness and roughness texture we will use the roughness texture but display a warning that both exist.
            if (textures[3] != null && textures[4] != null)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogWarning($"Found {AssetDatabase.GetAssetPath(textures[3])} and {AssetDatabase.GetAssetPath(textures[4])} texture. Using roughness for combining.");
                }
            }

            Texture2D combinedTexture = new Texture2D(assetTexture.width, assetTexture.height, TextureFormat.ARGB32, true);

            for (int x = 0; x < combinedTexture.width; x++)
            {
                for (int y = 0; y < combinedTexture.height; y++)
                {
                    // default value for each property (metallic, occlusion, height, smoothness) if not present.
                    Color combinedPixel = new Color(0.0f, 1.0f, 0.0f, 0.0f);

                    for (int index = 0; index < textures.Length; index++)
                    {
                        // The last two textures (smoothness and roughness) go into the same color channel.
                        int channel = math.min(index, 3);
                        // If the texture doesn't exist, use the default value of the combined pixel.
                        float value = textures[index] != null ? textures[index].GetPixel(x, y).r : combinedPixel[channel];
                        // We need to invert the roughness texture's value to convert it to a smoothness value.
                        value = textures[index] != null && suffixes[index] == c_roughness_suffix ? 1.0f - value : value;
                        combinedPixel[channel] = value;
                    }
                    combinedTexture.SetPixel(x, y, combinedPixel);
                }
            }
            string combinedTextureAssetFilename = textureAssetBaseFilename + "_MOHS";
            string combinedTextureAssetPath = Path.Combine(textureAssetDirectory, combinedTextureAssetFilename + ".png");
            File.WriteAllBytes(combinedTextureAssetPath, combinedTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(combinedTextureAssetPath);
        }
    }
}