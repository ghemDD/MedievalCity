using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class WFCTexture : MonoBehaviour {
    // output texture 
    public Texture2D outputTexture;
    public Texture2D normalOutput;

    // input texture
    public Texture2D inputTexture;
    public Texture2D normalTexture;

    // set in Editor
    public Vector2 tiling = new Vector2(100f, 100f);
    public int outputSize = 512; 
    public int pattern_size = 32; 
    public string saveFolderPath = "Assets/Environment/Pavements/WFC_Textures"; 

    void Start() {
        ImportTexture(inputTexture);
        ImportTexture(normalTexture);

        outputTexture = new Texture2D(outputSize, outputSize);
        normalOutput = new Texture2D(outputSize, outputSize);
        
        GenerateTexture();

        // material creation
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetTexture("_BaseMap", outputTexture);
        material.SetTexture("_NormalMap", normalOutput);
        material.SetTextureScale("_BaseMap", tiling);
        material.SetTextureScale("_NormalMap", tiling);

        Renderer renderer = this.GetComponent<Renderer>();
        renderer.material = material;

        SaveTexture();
    }

    private void ImportTexture(Texture2D texture) {
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture);

        UnityEditor.TextureImporter textureImporter = (UnityEditor.TextureImporter) UnityEditor.AssetImporter.GetAtPath(assetPath);
        textureImporter.isReadable = true;

        UnityEditor.AssetDatabase.ImportAsset(assetPath);
    }

    private void GenerateTexture() {
        Color[] pattern = inputTexture.GetPixels();
        Color[] normalPattern = normalTexture.GetPixels();

        int numTiles = outputSize / pattern_size;

        int[,] indices = new int[numTiles, numTiles];

        for (int y = 0; y < numTiles; y++) {
            for (int x = 0; x < numTiles; x++) {
                int[] availablePatterns = GetAvailablePatterns(indices, x, y, pattern);

                int patternIndex = Random.Range(0, availablePatterns.Length);
                int selectedPattern = availablePatterns[patternIndex];

                indices[x, y] = selectedPattern;

                Color[] tilePattern = GetTilePattern(pattern, selectedPattern);
                Color[] tileNormal = GetTilePattern(normalPattern, selectedPattern);

                int startX = x * pattern_size;
                int startY = y * pattern_size;
                outputTexture.SetPixels(startX, startY, pattern_size, pattern_size, tilePattern);
                normalOutput.SetPixels(startX, startY, pattern_size, pattern_size, tileNormal);
            }
        }

        outputTexture.Apply();

        GetComponent<Renderer>().material.mainTexture = outputTexture;
    }

    private Color[] GetTilePattern(Color[] pattern, int patternIndex) {
        // input texture size
        int inputSize = (int) Mathf.Sqrt(pattern.Length);

        // index in input texture
        int patternStartX = (patternIndex % inputSize) * pattern_size;
        int patternStartY = (patternIndex / inputSize) * pattern_size;

        Color[] tilePattern = new Color[pattern_size * pattern_size];

        for (int y = 0; y < pattern_size; y++) {
            for (int x = 0; x < pattern_size; x++) {
                int inputX = patternStartX + x;
                int inputY = patternStartY + y;

                inputX = (inputX + inputSize) % inputSize;
                inputY = (inputY + inputSize) % inputSize;

                Color inputColor = pattern[inputY * inputSize + inputX];
                tilePattern[y * pattern_size + x] = inputColor;
            }
        }

        return tilePattern;
    }

    private int[] GetAvailablePatterns(int[,] indices, int x, int y, Color[] pattern) {
        List<int> availablePatterns = new List<int>();

        for (int patternIndex = 0; patternIndex < pattern.Length; patternIndex++) {
            bool valid = true;

            for (int offset_x = -1; offset_x <= 1; offset_x++) {
                for (int offset_y = -1; offset_y <= 1; offset_y++) {
                    // skip current tile
                    if (offset_x == 0 && offset_y == 0)
                        continue;

                    int neighbor_x = x + offset_x;
                    int neighbor_y = y + offset_y;

                    if (neighbor_x >= 0 && neighbor_x < indices.GetLength(0) && neighbor_y >= 0 && neighbor_y < indices.GetLength(1)) {
                        int neighborPatternIndex = indices[neighbor_x, neighbor_y];

                        if (!CheckCompatibility(patternIndex, neighborPatternIndex, pattern)) {
                            valid = false;
                            break;
                        }
                    }
                }

                if (!valid)
                    break;
            }

            if (valid) {
                availablePatterns.Add(patternIndex);
            }
                
        }

        return availablePatterns.ToArray();
    }

    private bool CheckCompatibility(int patternIndex1, int patternIndex2, Color[] pattern) {
        int inputSize = (int) Mathf.Sqrt(pattern.Length);
        int pattern_size = (int) Mathf.Sqrt(patternIndex1);

        // pattern coordinates
        int pattern1_x_start = (patternIndex1 % inputSize) * pattern_size;
        int pattern1_y_start = (patternIndex1 / inputSize) * pattern_size;
        int pattern2_x_start = (patternIndex2 % inputSize) * pattern_size;
        int pattern2_y_start = (patternIndex2 / inputSize) * pattern_size;

        // neighbouring pixels condition
        for (int y = 0; y < pattern_size; y++) {
            for (int x = 0; x < pattern_size; x++) {
                int pattern1_x = pattern1_x_start + x;
                int pattern1_y = pattern1_y_start + y;

                int pattern2_x = pattern2_x_start + x;
                int pattern2_y = pattern2_y_start + y;

                // wrapping coordinates
                pattern1_x = (pattern1_x + inputSize) % inputSize;
                pattern1_y = (pattern1_y + inputSize) % inputSize;
                pattern2_x = (pattern2_x + inputSize) % inputSize;
                pattern2_y = (pattern2_y + inputSize) % inputSize;

                Color color1 = pattern[pattern1_y * inputSize + pattern1_x];
                Color color2 = pattern[pattern2_y * inputSize + pattern2_x];

                // matching colors condition
                if (color1 != color2)
                    return false;
            }
        }

        return true;
    }

    private void SaveTexture() {
        byte[] textureBytes = outputTexture.EncodeToPNG();

        string fileName = "pavement_" + (int) UnityEngine.Random.Range(0, 10000) + ".png";
        string filePath = Path.Combine(saveFolderPath, fileName);

        File.WriteAllBytes(filePath, textureBytes);
        Debug.Log("Texture saved at: " + filePath);
    }
}
