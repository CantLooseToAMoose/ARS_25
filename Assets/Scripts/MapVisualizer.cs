using UnityEngine;
using UnityEngine.UI; // Only needed if using RawImage

public class MapVisualizer : MonoBehaviour
{
    public Mapping mapping; // Reference to your Mapping instance
    public MapBehaviour MapBehaviour;
    public RawImage rawImage; // Optional, only for UI visualization
    public int textureScale = 4; // Scale for better visibility

    private Texture2D mapTexture;

    void Start()
    {
        mapping = MapBehaviour.Mapping;
        int width = mapping.width;
        int height = mapping.height;
        // Scale the plane to fit the world
        transform.localScale = new Vector3(mapping.width * mapping.resolution,mapping.height * mapping.resolution ,1);


         mapTexture = new Texture2D(mapping.width + 1, mapping.height + 1, TextureFormat.RGB24, false);

        mapTexture.filterMode = FilterMode.Point; // Make it pixelated
        UpdateTexture();

        if (rawImage != null)
        {
            rawImage.texture = mapTexture;
            rawImage.rectTransform.sizeDelta = new Vector2(width * textureScale, height * textureScale);
        }
        else
        {
            GetComponent<Renderer>().material.mainTexture = mapTexture;
        }
    }

    void Update()
    {
        UpdateTexture(); // Call this only when the map changes, or control it via events
    }

    void UpdateTexture()
    {
        for (int x = 0; x <= mapping.width; x++)
        {
            for (int y = 0; y <= mapping.height; y++)
            {
                float prob = mapping.ObtainProbabilityOccupied(x, y);
                Color color = Color.Lerp(Color.white, Color.black, prob);

                // Flip Y to match Unity's texture coordinate system
                mapTexture.SetPixel(x, mapping.height - y, color);
            }
        }

        mapTexture.Apply();
    }

}