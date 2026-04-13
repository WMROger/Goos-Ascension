using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; 

public class RuleTileCaveGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap foregroundTilemap; 
    public Tilemap backgroundTilemap; 
    // --- NEW: Decorator Tilemap ---
    public Tilemap decorationTilemap; 

    [Header("Tiles")]
    public RuleTile foregroundRuleTile; 
    public TileBase backgroundTile; 
    
    // --- NEW: Decoration Tiles & Prefabs ---
    [Header("Decorations")]
    public TileBase[] floorDecorations;   // Drag grass/mushroom tiles here
    public TileBase[] ceilingDecorations; // Drag hanging vine/stalactite tiles here
    [Range(0f, 100f)] public float floorDecorChance = 15f;
    [Range(0f, 100f)] public float ceilingDecorChance = 10f;
    
    [Space(10)]
    public TileBase waterfallTile; // Now accepts your Animated Rule Tile!
    [Range(0f, 100f)] public float waterfallChance = 1f;

    [Header("Placement")]
    public Vector2Int startPos; 
    public int width = 100;
    public int height = 80;

    [Header("Spawn Protection")]
    public Transform playerSpawn; 
    public float safeRadius = 6f; 

    [Header("Quest Settings (Secret Room & Lever)")]
    public GameObject leverPrefab; 
    public Vector3 leverOffset = new Vector3(0, 0.5f, 0);
    public Tilemap secretDoorTilemap; 
    public RuleTile secretDoorTile; 

    [Header("Tower Architecture")]
    public int numberOfFloors = 5;
    public int holeWidth = 8; 
    public int doubleJumpHeight = 4; 
    public int stepWidth = 6; 
    [Range(0, 100)] public int obstacleChance = 5; 
    [Range(1, 5)] public int iterations = 3;

    [Header("Water Settings")]
    public GameObject waterPrefab; 
    public int waterLevel = 12; 
    public Vector2 waterScaleAdjuster = new Vector2(0.1f, 1f); 
    public Vector3 waterPositionOffset = new Vector3(0, 0, 0);
    public string waterSortingLayer = "Water";

    [Header("Lighting")]
    public GameObject lanternPrefab; 
    [Range(0f, 10f)] public float lanternChance = 2f; 
    public Vector3 lanternOffset = new Vector3(0, 0f, 0); 
    public float minLanternDistance = 8f; 

    [Header("Mob Settings")]
    public GameObject[] mobPrefabs; 
    public int mobsPerFloor = 3; 
    public Vector3 mobOffset = new Vector3(0, 0, 0);
    public float minMobDistance = 12f; 

    private int[,] map;

    [ContextMenu("Generate Cave")]
    public void Generate() {
        map = new int[width, height];
        ClearOldObjects(); 
        RandomFill();
        BuildStairs();
        for (int i = 0; i < iterations; i++) Smooth();
        BuildFloorPlatforms();
        CarveHolesAndHeadroom();
        CarveQuestLocations();
        
        Draw();
        
        PlaceMobs();
        PlaceLever();
    }

    void ClearOldObjects() {
        while (transform.childCount > 0) {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    bool IsInSafeZone(int x, int y) {
        if (playerSpawn == null) return false;
        Vector3Int spawnGridPos = foregroundTilemap.WorldToCell(playerSpawn.position);
        int localX = spawnGridPos.x - startPos.x;
        int localY = spawnGridPos.y - startPos.y;
        return Vector2.Distance(new Vector2(x, y), new Vector2(localX, localY)) <= safeRadius;
    }

    void RandomFill() {
        int floorSpacing = height / numberOfFloors;
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) {
                    map[x, y] = 1; 
                } 
                else if (IsInSafeZone(x, y)) {
                    map[x, y] = 0;
                }
                else {
                    bool isFloorDivider = false;
                    for (int i = 1; i < numberOfFloors; i++) {
                        if (y == i * floorSpacing || y == (i * floorSpacing) - 1) {
                            isFloorDivider = true;
                            break;
                        }
                    }
                    if (isFloorDivider) map[x, y] = 1; 
                    else map[x, y] = (Random.Range(0, 100) < obstacleChance) ? 1 : 0;
                }
            }
        }
    }

    void BuildFloorPlatforms() {
        int floorSpacing = height / numberOfFloors;
        for (int i = 1; i < numberOfFloors; i++) {
            int floorY = i * floorSpacing;
            bool isLeftEdge = (i % 2 != 0);
            int holeStartX = isLeftEdge ? 2 : width - 2 - holeWidth;
            int holeEndX = holeStartX + holeWidth;

            for (int x = 1; x < width - 1; x++) {
                if (x >= holeStartX && x <= holeEndX) continue;
                for (int t = -1; t <= 1; t++) {
                    int y = floorY + t;
                    if (y > 0 && y < height) map[x, y] = 1;
                }
            }
        }
    }

    void BuildStairs() {
        int floorSpacing = height / numberOfFloors;
        for (int i = 1; i < numberOfFloors; i++) {
            int floorY = i * floorSpacing;
            bool isLeftEdge = (i % 2 != 0);
            int holeStartX = isLeftEdge ? 2 : width - 2 - holeWidth;
            int stepsNeeded = floorSpacing / doubleJumpHeight; 

            for (int s = 1; s <= stepsNeeded; s++) {
                int stepTopY = floorY - (s * doubleJumpHeight);
                int floorBelowY = floorY - floorSpacing + 2; 
                int startX, endX;

                if (isLeftEdge) {
                    startX = 1; 
                    endX = holeStartX + holeWidth + (s * stepWidth);
                } else {
                    endX = width - 2; 
                    startX = holeStartX - (s * stepWidth);
                }

                for (int px = startX; px <= endX; px++) {
                    for (int py = floorBelowY; py <= stepTopY; py++) {
                        if (px > 0 && px < width - 1 && py > 0 && py < height) map[px, py] = 1; 
                    }
                }
            }
        }
    }

    void Smooth() {
        int[,] newMap = new int[width, height];
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int neighbors = GetNeighbors(x, y);
                if (IsInSafeZone(x, y)) newMap[x, y] = 0;
                else if (neighbors > 4) newMap[x, y] = 1;
                else if (neighbors < 4) newMap[x, y] = 0;
                else newMap[x, y] = map[x, y];
            }
        }
        map = newMap;
    }

    void CarveHolesAndHeadroom() {
        int floorSpacing = height / numberOfFloors;
        for (int i = 1; i < numberOfFloors; i++) {
            int floorY = i * floorSpacing;
            bool isLeftEdge = (i % 2 != 0);
            int holeStartX = isLeftEdge ? 2 : width - 2 - holeWidth;

            for (int x = holeStartX; x <= holeStartX + holeWidth; x++) {
                for (int yOffset = -2; yOffset <= 6; yOffset++) { 
                    if (x > 0 && x < width - 1 && floorY + yOffset > 0 && floorY + yOffset < height) map[x, floorY + yOffset] = 0;
                }
            }

            int stepsNeeded = floorSpacing / doubleJumpHeight; 
            for (int s = 1; s <= stepsNeeded; s++) {
                int stepTopY = floorY - (s * doubleJumpHeight);
                int startX, endX;
                if (isLeftEdge) {
                    startX = (s == 1) ? 1 : holeStartX + holeWidth + ((s - 1) * stepWidth) - 1;
                    endX = holeStartX + holeWidth + (s * stepWidth) + 1;
                } else {
                    endX = (s == 1) ? width - 2 : holeStartX - ((s - 1) * stepWidth) + 1;
                    startX = holeStartX - (s * stepWidth) - 1;
                }
                for (int px = startX; px <= endX; px++) {
                    for (int py = stepTopY + 1; py <= stepTopY + 6; py++) { 
                        if (px > 0 && px < width - 1 && py > 0 && py < height) map[px, py] = 0;
                    }
                }
            }
        }
    }

    void CarveQuestLocations() {
        // 1. Top Left Teleporter Room (The "-|" Enclosed Box)
        int topFloorY = (numberOfFloors - 1) * (height / numberOfFloors);
        int roomWidth = 16;
        int roomHeight = 6;

        for (int x = 1; x <= roomWidth; x++) {
            for (int y = topFloorY; y <= topFloorY + roomHeight + 2; y++) {
                
                // A. The Floor
                if (y == topFloorY && x < width && y < height) {
                    map[x, y] = 1; 
                }
                // B. The Ceiling (The "-" on top)
                else if (y >= topFloorY + roomHeight + 1 && x < width && y < height) {
                    map[x, y] = 1; 
                }
                // C. The Right Wall (Set to 0 so we can paint the Secret Door there instead!)
                else if (x == roomWidth && x < width && y < height) {
                    map[x, y] = 0; 
                }
                // D. The Hollow Inside
                else if (x < width && y < height) {
                    map[x, y] = 0; 
                }
            }
        }

        // 2. Bottom Right Slime Tunnel & Lever Room
        int tunnelLength = 35; // Total length of the tunnel section.
        int tunnelStartX = width - tunnelLength; // Calculate where it starts on the X-axis.

        // --- NEW: Entrance Carving ---
        // We need to ensure connectivity by blasting open an entrance at tunnelStartX.
        // We'll carve 4 blocks to the left to force an opening from the cave into the tunnel.
        int entranceLength = 4;
        for (int x = tunnelStartX - entranceLength; x < tunnelStartX; x++) {
            if (x > 0 && x < width - 1) {
                map[x, waterLevel + 1] = 0;
                map[x, waterLevel + 2] = 0;
                map[x, waterLevel + 3] = 0;
            }
        }

        for (int x = tunnelStartX; x < width - 1; x++) {
            map[x, waterLevel] = 1;
            map[x, waterLevel + 1] = 0;
            map[x, waterLevel + 2] = 0;
            map[x, waterLevel + 3] = 0;

            if (x > width - 12) {
                map[x, waterLevel + 4] = 0;
                map[x, waterLevel + 5] = 0;
                if (waterLevel + 6 < height) map[x, waterLevel + 6] = 1;
                if (waterLevel + 7 < height) map[x, waterLevel + 7] = 1;
            } else {
                if (waterLevel + 4 < height) map[x, waterLevel + 4] = 1;
                if (waterLevel + 5 < height) map[x, waterLevel + 5] = 1;
            }
        }
    }


    int GetNeighbors(int gx, int gy) {
        int count = 0;
        for (int x = gx - 1; x <= gx + 1; x++) {
            for (int y = gy - 1; y <= gy + 1; y++) {
                if (x >= 0 && x < width && y >= 0 && y < height) count += map[x, y];
                else count++;
            }
        }
        return count;
    }

    void Draw() {
        foregroundTilemap.ClearAllTiles();
        backgroundTilemap.ClearAllTiles();
        if (secretDoorTilemap != null) secretDoorTilemap.ClearAllTiles();
        if (decorationTilemap != null) decorationTilemap.ClearAllTiles(); 

        int topFloorY = (numberOfFloors - 1) * (height / numberOfFloors);
        int roomWidth = 16;
        int roomHeight = 6;

        List<Vector2> spawnedLanterns = new List<Vector2>();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Vector3Int pos = new Vector3Int(x + startPos.x, y + startPos.y, 0);
                
                if (map[x, y] == 1) {
                    foregroundTilemap.SetTile(pos, foregroundRuleTile);
                } else {
                    backgroundTilemap.SetTile(pos, backgroundTile);

                    // --- NEW: Paint the Secret Door Wall! ---
                    if (x == roomWidth && y > topFloorY && y <= topFloorY + roomHeight) {
                        if (secretDoorTilemap != null && secretDoorTile != null) {
                            secretDoorTilemap.SetTile(pos, secretDoorTile);
                        }
                    }
                    
                    // Lanterns Check
                    if (y >= waterLevel && y > 0 && map[x, y - 1] == 1 && !(x <= roomWidth && y > topFloorY && y <= topFloorY + roomHeight) && !IsInSafeZone(x, y)) {
                        Vector2 currentPos = new Vector2(x, y);
                        if (lanternPrefab != null && Random.Range(0f, 100f) < lanternChance) {
                            bool tooClose = false;
                            foreach (Vector2 p in spawnedLanterns) {
                                if (Vector2.Distance(currentPos, p) < minLanternDistance) {
                                    tooClose = true; break;
                                }
                            }
                            if (!tooClose) {
                                Vector3 worldPos = foregroundTilemap.GetCellCenterWorld(pos) + lanternOffset;
                                GameObject newLantern = Instantiate(lanternPrefab, worldPos, Quaternion.identity);
                                newLantern.transform.parent = this.transform;
                                spawnedLanterns.Add(currentPos);
                            }
                        }
                    }

                    // --- DECORATION LOGIC ---
                    if (decorationTilemap != null && map[x, y] == 0)
                    {
                        // 1. Check Floor (Grass)
                        if (y > 0 && map[x, y - 1] == 1 && floorDecorations.Length > 0) {
                            if (Random.Range(0f, 100f) < floorDecorChance) {
                                TileBase randomDecor = floorDecorations[Random.Range(0, floorDecorations.Length)];
                                decorationTilemap.SetTile(pos, randomDecor);
                            }
                        }
                        
                        // 2. Check Ceiling (Waterfalls OR Vines)
                        if (y < height - 1 && map[x, y + 1] == 1) {
                            
                            bool spawnedWaterfall = false;

                            if (waterfallTile != null && y > waterLevel + 5) {
                                if (Random.Range(0f, 100f) < waterfallChance) {
                                    spawnedWaterfall = true;
                                    for (int flowY = y; flowY >= waterLevel; flowY--) {
                                        Vector3Int flowPos = new Vector3Int(x + startPos.x, flowY + startPos.y, 0);
                                        decorationTilemap.SetTile(flowPos, waterfallTile);
                                    }
                                }
                            }

                            if (!spawnedWaterfall && ceilingDecorations.Length > 0) {
                                if (Random.Range(0f, 100f) < ceilingDecorChance) {
                                    TileBase randomDecor = ceilingDecorations[Random.Range(0, ceilingDecorations.Length)];
                                    int vineLength = Random.Range(2, 7); 
                                    for (int v = 0; v < vineLength; v++) {
                                        if (y - v > 0 && map[x, y - v] == 0) { 
                                            Vector3Int vinePos = new Vector3Int(pos.x, pos.y - v, 0);
                                            decorationTilemap.SetTile(vinePos, randomDecor);
                                        } else {
                                            break; 
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Spawn Water
        if (waterPrefab != null && waterLevel > 0) {
            // (Your water spawning code stays the same)
            Vector3 bottomLeft = foregroundTilemap.GetCellCenterWorld(new Vector3Int(startPos.x, startPos.y, 0));
            Vector3 waterCenter = new Vector3(bottomLeft.x + (width / 2f) - 0.5f, bottomLeft.y + (waterLevel / 2f) - 0.5f, 0);
            Vector3 finalWaterPos = waterCenter + waterPositionOffset;

            GameObject singleWater = Instantiate(waterPrefab, finalWaterPos, Quaternion.identity);
            singleWater.transform.parent = this.transform;
            singleWater.transform.localScale = new Vector3(width * waterScaleAdjuster.x, waterLevel * waterScaleAdjuster.y, 1);

            Renderer waterRenderer = singleWater.GetComponentInChildren<Renderer>();
            if (waterRenderer != null) {
                waterRenderer.sortingLayerName = waterSortingLayer; 
                waterRenderer.sortingOrder = 5; 
            }
        }
    }

    void PlaceMobs() {
        if (mobPrefabs == null || mobPrefabs.Length == 0) return;

        int floorSpacing = height / numberOfFloors;
        int topFloorY = (numberOfFloors - 1) * floorSpacing;
        int roomWidth = 16;
        
        List<Vector2> allSpawnedMobs = new List<Vector2>();

        for (int f = 1; f < numberOfFloors; f++) {
            int floorBottomY = f * floorSpacing;
            int floorTopY = (f + 1) * floorSpacing;
            if (f == numberOfFloors - 1) floorTopY = height;

            List<Vector3Int> validSpots = new List<Vector3Int>();

            for (int x = 1; x < width - 1; x++) {
                for (int y = floorBottomY; y < floorTopY; y++) {
                    if (map[x, y] == 0 && map[x, y - 1] == 1 && y >= waterLevel) {
                        if (IsInSafeZone(x, y)) continue; 
                        if (f == numberOfFloors - 1 && x <= roomWidth && y > topFloorY) continue; 
                        
                        validSpots.Add(new Vector3Int(x, y, 0));
                    }
                }
            }

            int mobsSpawnedOnThisFloor = 0;
            int safetyCounter = 0; 

            while (mobsSpawnedOnThisFloor < mobsPerFloor && validSpots.Count > 0 && safetyCounter < 100) {
                safetyCounter++;
                int randomIndex = Random.Range(0, validSpots.Count);
                Vector3Int spot = validSpots[randomIndex];
                Vector2 currentPos = new Vector2(spot.x, spot.y);

                bool tooClose = false;
                foreach (Vector2 p in allSpawnedMobs) {
                    if (Vector2.Distance(currentPos, p) < minMobDistance) {
                        tooClose = true; break;
                    }
                }

                if (!tooClose) {
                    Vector3Int actualGridPos = new Vector3Int(spot.x + startPos.x, spot.y + startPos.y, 0);
                    Vector3 worldPos = foregroundTilemap.GetCellCenterWorld(actualGridPos) + mobOffset;
                    
                    GameObject mobToSpawn = mobPrefabs[Random.Range(0, mobPrefabs.Length)];
                    GameObject newMob = Instantiate(mobToSpawn, worldPos, Quaternion.identity);
                    newMob.transform.parent = this.transform;

                    allSpawnedMobs.Add(currentPos);
                    mobsSpawnedOnThisFloor++;
                }
                validSpots.RemoveAt(randomIndex);
            }
        }
    }

    void PlaceLever() {
        if (leverPrefab == null) return;
        int leverX = width - 6; 
        
        Vector3Int gridPos = new Vector3Int(leverX + startPos.x, waterLevel + 1 + startPos.y, 0);
        Vector3 worldPos = foregroundTilemap.GetCellCenterWorld(gridPos) + leverOffset;
        
        worldPos.z = leverPrefab.transform.position.z; 
        
        leverPrefab.transform.position = worldPos;
        leverPrefab.SetActive(true);
    }
    
    void Start() {
        Generate();
    }
}
