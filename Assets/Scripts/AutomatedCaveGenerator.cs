using UnityEngine;
using UnityEngine.Tilemaps;

public class AutomatedCaveGenerator : MonoBehaviour
{
    [Header("Tilemap Setup")]
    public Tilemap caveTilemap;    // Drag your "Background" or "Wall" Tilemap here
    public Tilemap groundTilemap;  // Drag your "Platforms" or "Edges" Tilemap here
    public TileBase wallTile;      // The Dark Cave tile asset
    public TileBase groundTile;    // The Cyan Glowing Edge tile asset

    [Header("Cave Settings")]
    public int width = 100;        // Width of the generation area
    public int height = 80;       // Height of the generation area (good for verticality)
    [Range(0, 100)] public int fillChance = 45; // Initial % of wall tiles (45-50% is a good start)
    [Range(1, 10)] public int smoothIterations = 5; // How many times to smooth the map (3-5 is typical)

    private int[,] caveMap; // 1 = Wall, 0 = Empty

    private void Start()
    {
        GenerateCave();
    }

    // Call this function (e.g., from a button in the UI or an inspector button) to remake the cave
    [ContextMenu("Generate New Cave")]
    public void GenerateCave()
    {
        caveMap = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < smoothIterations; i++)
        {
            SmoothMap();
        }

        DrawMap();
    }

    void RandomFillMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Create random noise. Higher fillChance = more initial walls.
                caveMap[x, y] = (Random.Range(0, 100) < fillChance) ? 1 : 0;
            }
        }
    }

    void SmoothMap()
    {
        int[,] newMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallCount = GetSurroundingWallCount(x, y);

                // Cellular Automata Rule:
                // If a cell has too many wall neighbors (>4), it becomes a wall (smoothed).
                // If it has too few (<4), it becomes empty (clears out isolated walls).
                if (neighborWallCount > 4)
                    newMap[x, y] = 1;
                else if (neighborWallCount < 4)
                    newMap[x, y] = 0;
                else
                    newMap[x, y] = caveMap[x, y]; // Stay the same if exactly 4 neighbors.
            }
        }
        caveMap = newMap;
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        // Check all 8 surrounding cells (the 3x3 grid around the point)
        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                // Ensure we are inside the map boundaries
                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    // Don't count the cell itself
                    if (neighborX != gridX || neighborY != gridY)
                    {
                        wallCount += caveMap[neighborX, neighborY];
                    }
                }
                else
                {
                    // Count boundary cells as walls to force a closed perimeter
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    void DrawMap()
    {
        // Clear both tilemaps before drawing the new map
        caveTilemap.ClearAllTiles();
        groundTilemap.ClearAllTiles();

        if (caveMap != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // The generation loop handles the overall 'hollow' or 'filled' areas.
                    // We draw the basic wall and then 'paint' the ground edges separately
                    // based on context.
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    // If it's defined as a wall in the array, draw the dark cave background
                    if (caveMap[x, y] == 1)
                    {
                        caveTilemap.SetTile(pos, wallTile);
                    }
                    // We only want 'ground' or 'platform' tiles on top of empty spaces where
                    // there is a wall above them (creating platforms/floors).
                    else if (y > 0 && caveMap[x, y - 1] == 1) // Check cell below for wall
                    {
                        // Check cells left, right, up to ensure it's not a tiny island
                        int neighbors = GetSurroundingWallCount(x, y);
                        // Using a condition on context to draw platforms
                        if (neighbors < 5) // Draws more 'clean' edges
                        {
                            groundTilemap.SetTile(pos, groundTile);
                        }
                    }
                }
            }
        }
    }
}