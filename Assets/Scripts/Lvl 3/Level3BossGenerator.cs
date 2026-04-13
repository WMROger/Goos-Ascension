using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; 

public class Level3BossGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap foregroundTilemap; 
    public Tilemap backgroundTilemap; 
    public Tilemap decorationTilemap; 

    [Header("Tiles")]
    public RuleTile foregroundRuleTile; 
    public TileBase backgroundTile; 
    
    [Header("Decorations")]
    public TileBase[] floorDecorations;   
    public TileBase[] ceilingDecorations; 
    [Range(0f, 100f)] public float floorDecorChance = 15f;
    [Range(0f, 100f)] public float ceilingDecorChance = 10f;
    public TileBase waterfallTile; 
    [Range(0f, 100f)] public float waterfallChance = 1f;

    [Header("Placement & Size")]
    public Vector2Int startPos; 
    public int width = 100;
    public int height = 50; 

    [Header("Player & Boss")]
    public Transform playerSpawn; 
    public GameObject bossPrefab; 
    public Vector3 bossOffset = new Vector3(0, 1f, 0);

    [Header("Arena Architecture")]
    public int arenaWidth = 40; 
    public int arenaCeilingHeight = 15; 
    [Range(1, 5)] public int iterations = 3;

    [Header("Arena Platforms")]
    [Tooltip("Drag a platform prefab here!")]
    public GameObject floatingPlatformPrefab; 
    public int numberOfPlatforms = 3;

    [Header("Water Settings")]
    public GameObject waterPrefab; 
    public int waterLevel = 5; 
    public Vector2 waterScaleAdjuster = new Vector2(0.1f, 1f); 
    public Vector3 waterPositionOffset = new Vector3(0, 0, 0);
    public string waterSortingLayer = "Water";

    [Header("Lighting")]
    public GameObject lanternPrefab; 
    [Range(0f, 10f)] public float lanternChance = 2f; 
    public Vector3 lanternOffset = new Vector3(0, 0f, 0); 
    public float minLanternDistance = 8f; 

    private int[,] map;

    [ContextMenu("Generate Boss Cave")]
    public void Generate() {
        map = new int[width, height];
        ClearOldObjects(); 
        RandomFill();
        for (int i = 0; i < iterations; i++) Smooth();
        
        CarveEntranceTunnel();
        CarveBossArena(); 
        
        Draw();
        PlacePlatforms();
        PlaceBoss();
    }

    void ClearOldObjects() {
        while (transform.childCount > 0) {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    void RandomFill() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) map[x, y] = 1; 
                else map[x, y] = (Random.Range(0, 100) < 45) ? 1 : 0;
            }
        }
    }

    void Smooth() {
        int[,] newMap = new int[width, height];
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int neighbors = GetNeighbors(x, y);
                if (neighbors > 4) newMap[x, y] = 1;
                else if (neighbors < 4) newMap[x, y] = 0;
                else newMap[x, y] = map[x, y];
            }
        }
        map = newMap;
    }

    void CarveEntranceTunnel() {
        for (int x = 1; x < width - arenaWidth; x++) {
            map[x, waterLevel] = 1; 
            for (int y = waterLevel + 1; y < waterLevel + 6; y++) map[x, y] = 0; 
        }
    }

    void CarveBossArena() {
        int arenaStartX = width - arenaWidth - 1;
        for (int x = arenaStartX; x < width - 1; x++) {
            map[x, waterLevel] = 1; 
            for (int y = waterLevel + 1; y <= waterLevel + arenaCeilingHeight; y++) {
                if (y < height) map[x, y] = 0; 
            }
            if (waterLevel + arenaCeilingHeight + 1 < height) {
                map[x, waterLevel + arenaCeilingHeight + 1] = 1;
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
        if (decorationTilemap != null) decorationTilemap.ClearAllTiles(); 

        List<Vector2> spawnedLanterns = new List<Vector2>();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Vector3Int pos = new Vector3Int(x + startPos.x, y + startPos.y, 0);
                
                if (map[x, y] == 1) {
                    foregroundTilemap.SetTile(pos, foregroundRuleTile);
                } else {
                    backgroundTilemap.SetTile(pos, backgroundTile);

                    if (y >= waterLevel && y > 0 && map[x, y - 1] == 1) {
                        Vector2 currentPos = new Vector2(x, y);
                        if (lanternPrefab != null && Random.Range(0f, 100f) < lanternChance) {
                            bool tooClose = false;
                            foreach (Vector2 p in spawnedLanterns) {
                                if (Vector2.Distance(currentPos, p) < minLanternDistance) { tooClose = true; break; }
                            }
                            if (!tooClose) {
                                Vector3 worldPos = foregroundTilemap.GetCellCenterWorld(pos) + lanternOffset;
                                GameObject newLantern = Instantiate(lanternPrefab, worldPos, Quaternion.identity);
                                newLantern.transform.parent = this.transform;
                                spawnedLanterns.Add(currentPos);
                            }
                        }
                    }

                    if (decorationTilemap != null && map[x, y] == 0) {
                        if (y > 0 && map[x, y - 1] == 1 && floorDecorations.Length > 0 && Random.Range(0f, 100f) < floorDecorChance) {
                            decorationTilemap.SetTile(pos, floorDecorations[Random.Range(0, floorDecorations.Length)]);
                        }
                        
                        if (y < height - 1 && map[x, y + 1] == 1) {
                            bool spawnedWaterfall = false;
                            if (waterfallTile != null && y > waterLevel + 5 && Random.Range(0f, 100f) < waterfallChance) {
                                spawnedWaterfall = true;
                                for (int flowY = y; flowY >= waterLevel; flowY--) decorationTilemap.SetTile(new Vector3Int(x + startPos.x, flowY + startPos.y, 0), waterfallTile);
                            }
                            if (!spawnedWaterfall && ceilingDecorations.Length > 0 && Random.Range(0f, 100f) < ceilingDecorChance) {
                                TileBase randomDecor = ceilingDecorations[Random.Range(0, ceilingDecorations.Length)];
                                for (int v = 0; v < Random.Range(2, 7); v++) {
                                    if (y - v > 0 && map[x, y - v] == 0) decorationTilemap.SetTile(new Vector3Int(pos.x, pos.y - v, 0), randomDecor);
                                    else break; 
                                }
                            }
                        }
                    }
                }
            }
        }

        if (waterPrefab != null && waterLevel > 0) {
            Vector3 bottomLeft = foregroundTilemap.GetCellCenterWorld(new Vector3Int(startPos.x, startPos.y, 0));
            Vector3 waterCenter = new Vector3(bottomLeft.x + (width / 2f) - 0.5f, bottomLeft.y + (waterLevel / 2f) - 0.5f, 0);
            GameObject singleWater = Instantiate(waterPrefab, waterCenter + waterPositionOffset, Quaternion.identity);
            singleWater.transform.parent = this.transform;
            singleWater.transform.localScale = new Vector3(width * waterScaleAdjuster.x, waterLevel * waterScaleAdjuster.y, 1);
            if (singleWater.GetComponentInChildren<Renderer>() != null) {
                singleWater.GetComponentInChildren<Renderer>().sortingLayerName = waterSortingLayer; 
                singleWater.GetComponentInChildren<Renderer>().sortingOrder = 5; 
            }
        }
    }

    void PlacePlatforms() {
        if (floatingPlatformPrefab == null) return;
        
        int arenaStartX = width - arenaWidth + 5; 
        int spacing = (arenaWidth - 10) / numberOfPlatforms;

        for (int i = 0; i < numberOfPlatforms; i++) {
            int px = arenaStartX + (i * spacing);
            int py = waterLevel + 4 + (i % 2 == 0 ? 0 : 3); // Staggers them up and down
            
            Vector3Int gridPos = new Vector3Int(px + startPos.x, py + startPos.y, 0);
            Vector3 worldPos = foregroundTilemap.GetCellCenterWorld(gridPos);
            
            GameObject plat = Instantiate(floatingPlatformPrefab, worldPos, Quaternion.identity);
            plat.transform.parent = this.transform;
        }
    }

    void PlaceBoss() {
        if (bossPrefab == null) return;
        int bossX = width - 8; 
        Vector3Int gridPos = new Vector3Int(bossX + startPos.x, waterLevel + 1 + startPos.y, 0);
        GameObject spawnedBoss = Instantiate(bossPrefab, foregroundTilemap.GetCellCenterWorld(gridPos) + bossOffset, Quaternion.identity);
        spawnedBoss.transform.parent = this.transform;
    }
    
    void Start() { Generate(); }
}