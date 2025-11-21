using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 연결된 방들의 집합을 관리하기 위한 Disjoint Set Union (DSU) 헬퍼 클래스
/// </summary>
public class DisjointSet
{
    private int[] parent;
    public DisjointSet(int size)
    {
        parent = new int[size];
        for (int i = 0; i < size; i++) { parent[i] = i; }
    }
    public int Find(int i)
    {
        if (parent[i] == i) return i;
        return parent[i] = Find(parent[i]);
    }
    public void Union(int i, int j)
    {
        int rootI = Find(i);
        int rootJ = Find(j);
        if (rootI != rootJ) { parent[rootI] = rootJ; }
    }
}

/// <summary>
/// BSP 트리의 각 노드를 나타내는 클래스. 분할된 공간 정보를 가짐.
/// </summary>
public class RoomNode
{
    public RectInt rect;
    public RoomNode leftChild;
    public RoomNode rightChild;
    public RoomNode(RectInt rect) { this.rect = rect; }

    public bool Split(int minRoomSize, int maxIterations, int currentIteration, float minSplitRatio)
    {
        if (currentIteration >= maxIterations) return false;
        bool splitHorizontal;
        if (rect.width > rect.height && rect.width / (float)rect.height >= 1.25f) splitHorizontal = false;
        else if (rect.height > rect.width && rect.height / (float)rect.width >= 1.25f) splitHorizontal = true;
        else splitHorizontal = Random.Range(0f, 1f) > 0.5f;
        if ((splitHorizontal && rect.height < minRoomSize * 2) || (!splitHorizontal && rect.width < minRoomSize * 2)) return false;
        if (splitHorizontal)
        {
            int minSplit = Mathf.FloorToInt(rect.height * minSplitRatio);
            int maxSplit = Mathf.CeilToInt(rect.height * (1 - minSplitRatio));
            minSplit = Mathf.Max(minSplit, minRoomSize);
            maxSplit = Mathf.Min(maxSplit, rect.height - minRoomSize);
            if (minSplit >= maxSplit) return false;
            int splitPoint = Random.Range(minSplit, maxSplit);
            leftChild = new RoomNode(new RectInt(rect.x, rect.y, rect.width, splitPoint));
            rightChild = new RoomNode(new RectInt(rect.x, rect.y + splitPoint, rect.width, rect.height - splitPoint));
        }
        else
        {
            int minSplit = Mathf.FloorToInt(rect.width * minSplitRatio);
            int maxSplit = Mathf.CeilToInt(rect.width * (1 - minSplitRatio));
            minSplit = Mathf.Max(minSplit, minRoomSize);
            maxSplit = Mathf.Min(maxSplit, rect.width - minRoomSize);
            if (minSplit >= maxSplit) return false;
            int splitPoint = Random.Range(minSplit, maxSplit);
            leftChild = new RoomNode(new RectInt(rect.x, rect.y, splitPoint, rect.height));
            rightChild = new RoomNode(new RectInt(rect.x + splitPoint, rect.y, rect.width - splitPoint, rect.height));
        }
        leftChild.Split(minRoomSize, maxIterations, currentIteration + 1, minSplitRatio);
        rightChild.Split(minRoomSize, maxIterations, currentIteration + 1, minSplitRatio);
        return true;
    }
}

public class BSP_Generator : MonoBehaviour
{
    private struct Door
    {
        public int roomIndexA;
        public int roomIndexB;
        public RectInt doorRect;
    }

    [Header("Dungeon Generation Settings")]
    public RectInt dungeonArea = new RectInt(0, 0, 100, 60);
    [Range(0, 10)]
    public int maxIterations = 4;
    [Range(5, 20)]
    public int minRoomSize = 10;
    [Range(0.1f, 0.49f)]
    public float minSplitRatio = 0.4f;
    [Tooltip("추가적인 문(경로)이 생성될 확률")]
    [Range(0f, 1f)]
    public float extraDoorChance = 0.2f;

    [Tooltip("생성될 문의 두께 (타일 단위)")]
    [Range(1, 5)]
    public int doorWidth = 1;

    [Header("Tilemap Settings")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    public TileBase floorTile;
    public TileBase wallTile;

    private List<RectInt> doorRects = new List<RectInt>();
    private RoomNode root;
    private List<RoomNode> leafNodes = new List<RoomNode>();

    public void Awake() { Generate(); }
    
    [ContextMenu("Generate Dungeon")]
    public void GenDungeon() { Generate(); }

    public void Generate()
    {
        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();
        root = new RoomNode(dungeonArea);
        root.Split(minRoomSize, maxIterations, 0, minSplitRatio);
        leafNodes.Clear();
        FindAllLeaves(root);
        doorRects.Clear();
        CreateMinimumDoors();
        
        DrawDungeonToTilemap();
    }

    void FindAllLeaves(RoomNode node)
    {
        if (node.leftChild == null && node.rightChild == null) { leafNodes.Add(node); return; }
        if (node.leftChild != null) FindAllLeaves(node.leftChild);
        if (node.rightChild != null) FindAllLeaves(node.rightChild);
    }

    void CreateMinimumDoors()
    {
        if (leafNodes.Count < 2) return;

        int n = leafNodes.Count;
        var allEdges = new List<(int a, int b, int weight, RectInt doorRect)>();

        // 1. 방 쌍마다 문 후보 찾기
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                RectInt roomA = leafNodes[i].rect;
                RectInt roomB = leafNodes[j].rect;
                RectInt? door = null;

                if (roomA.xMax == roomB.xMin || roomA.xMin == roomB.xMax)
                {
                    int overlapStart = Mathf.Max(roomA.yMin, roomB.yMin);
                    int overlapEnd = Mathf.Min(roomA.yMax, roomB.yMax);
                    int overlapLength = overlapEnd - overlapStart;
                    if (overlapLength >= doorWidth + 2)
                    {
                        int doorY = Random.Range(overlapStart + 1, overlapEnd - doorWidth);
                        door = new RectInt(Mathf.Min(roomA.xMax, roomB.xMax) - 1, doorY, 2, doorWidth);
                    }
                }
                else if (roomA.yMax == roomB.yMin || roomA.yMin == roomB.yMax)
                {
                    int overlapStart = Mathf.Max(roomA.xMin, roomB.xMin);
                    int overlapEnd = Mathf.Min(roomA.xMax, roomB.xMax);
                    int overlapLength = overlapEnd - overlapStart;
                    if (overlapLength >= doorWidth + 2)
                    {
                        int doorX = Random.Range(overlapStart + 1, overlapEnd - doorWidth);
                        door = new RectInt(doorX, Mathf.Min(roomA.yMax, roomB.yMax) - 1, doorWidth, 2);
                    }
                }

                if (door.HasValue)
                    allEdges.Add((i, j, 0, door.Value)); // weight은 나중에 지정
            }
        }

        // 2. 가중치 배열 생성 및 셔플
        int[] weights = Enumerable.Range(1, allEdges.Count).OrderBy(_ => Random.value).ToArray();
        for (int k = 0; k < allEdges.Count; k++)
            allEdges[k] = (allEdges[k].a, allEdges[k].b, weights[k], allEdges[k].doorRect);

        // 3. Kruskal 알고리즘으로 MST 구성
        var dsu = new DisjointSet(n);
        var sortedEdges = allEdges.OrderBy(e => e.weight).ToList();
        doorRects.Clear();

        foreach (var edge in sortedEdges)
        {
            if (dsu.Find(edge.a) != dsu.Find(edge.b))
            {
                dsu.Union(edge.a, edge.b);
                doorRects.Add(edge.doorRect);
            }
        }

        // 4. 여분의 문 랜덤 추가 (옵션)
        foreach (var edge in allEdges)
        {
            if (!doorRects.Contains(edge.doorRect) && Random.value < extraDoorChance)
                doorRects.Add(edge.doorRect);
        }
    }

    
    void DrawDungeonToTilemap()
    {
        if (floorTilemap == null || wallTilemap == null || floorTile == null || wallTile == null)
        {
            Debug.LogError("Tilemap settings are not assigned in the inspector!");
            return;
        }

        foreach (var room in leafNodes)
        {
            for (int x = room.rect.xMin + 1; x < room.rect.xMax - 1; x++)
            {
                for (int y = room.rect.yMin + 1; y < room.rect.yMax - 1; y++)
                {
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                }
            }
            for (int x = room.rect.xMin - 1; x <= room.rect.xMax; x++)
            {
                for (int y = room.rect.yMin - 1; y <= room.rect.yMax; y++)
                {
                    if (floorTilemap.GetTile(new Vector3Int(x, y, 0)) == null)
                    {
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                    }
                }
            }
        }
        
        foreach (var door in doorRects)
        {
            for (int x = door.xMin; x < door.xMax; x++)
            {
                for (int y = door.yMin; y < door.yMax; y++)
                {
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), null);
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                }
            }
        }
        
        print("Dungeon generated with " + leafNodes.Count + " rooms and " + doorRects.Count + " doors.");
    }

    private void OnDrawGizmos()
    {
        if (leafNodes == null || leafNodes.Count == 0) return;
        
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f);
        foreach (var leaf in leafNodes) DrawRect(leaf.rect);
        
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.8f);
        foreach (var door in doorRects) DrawRect(door);
    }

    void DrawRect(RectInt rect)
    {
        Vector3 center = new Vector3(rect.center.x, rect.center.y, 0);
        Vector3 size = new Vector3(rect.size.x, rect.size.y, 0);
        Gizmos.DrawWireCube(center, size);
    }
    
    public HashSet<Vector3Int> GetWalkableTiles()
    {
        HashSet<Vector3Int> walkableTiles = new HashSet<Vector3Int>();
        if (floorTilemap == null)
        {
            Debug.LogError("Floor Tilemap is not assigned!");
            return walkableTiles;
        }

        BoundsInt bounds = floorTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (floorTilemap.HasTile(pos))
                {
                    walkableTiles.Add(pos);
                }
            }
        }
        return walkableTiles;
    }
}