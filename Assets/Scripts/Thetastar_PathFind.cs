using UnityEngine;
using System.Collections.Generic;

public class PathNode
{
    public Vector2Int idx;
    public int gCost, hCost;
    public int fCost => gCost + hCost;
    public PathNode parent;
    public Vector3 WorldPos => new Vector3(idx.x / (float)Thetastar_PathFind.SUBDIVISIONS, idx.y / (float)Thetastar_PathFind.SUBDIVISIONS, 0f);
    public PathNode(Vector2Int i) { idx = i; }
}

public static class Thetastar_PathFind
{
    public const int MOVE_STRAIGHT_COST = 10;
    public const int MOVE_DIAGONAL_COST = 14;
    public const int SUBDIVISIONS = 4;
    public const float SUB_CELL_SIZE = 1f / SUBDIVISIONS;
    private static readonly LayerMask obstacleMask = LayerMask.GetMask("Obstacle");

    public static List<Vector3> FindPath(HashSet<Vector3Int> walkableTiles, Vector2 agentSize, Vector3 startWorldPos, Vector3 targetWorldPos)
    {
        // 안전 SubCell 찾기 (벽 근접 시에도 가능)
        Vector3 safeTarget = FindClosestSafeSubCell(walkableTiles, agentSize, targetWorldPos);
        Debug.Log($"Safe target candidate: {safeTarget}");

        Vector2Int startIdx = WorldToSubIndex(startWorldPos);
        Vector2Int endIdx = WorldToSubIndex(safeTarget);

        var startNode = new PathNode(startIdx);
        var endNode = new PathNode(endIdx);

        var openSet = new SimplePriorityQueue<PathNode>();
        var allNodes = new Dictionary<Vector2Int, PathNode> { { startIdx, startNode } };
        var closedSet = new HashSet<Vector2Int>();

        startNode.gCost = 0;
        startNode.hCost = GetGridDistance(startIdx, endIdx);
        openSet.Enqueue(startNode, startNode.fCost);

        while (openSet.Count > 0)
        {
            PathNode current = openSet.Dequeue();
            closedSet.Add(current.idx);

            if (current.idx == endNode.idx)
                return RetracePath(startNode, current);

            foreach (var neighborIdx in GetNeighborIndices(current.idx))
            {
                if (closedSet.Contains(neighborIdx)) continue;

                Vector3 neighborWorld = SubIndexToWorld(neighborIdx);

                // Wall proximity 예외 처리: walkableTiles 기반만 우선 체크
                if (!IsAreaLogicallyWalkable(neighborWorld, agentSize, walkableTiles))
                    continue;

                if (!allNodes.TryGetValue(neighborIdx, out PathNode neighborNode))
                {
                    neighborNode = new PathNode(neighborIdx);
                    allNodes.Add(neighborIdx, neighborNode);
                }

                int tentativeG = current.gCost + GetGridDistance(current.idx, neighborIdx);

                PathNode parentNode = current.parent;
                if (parentNode != null && LineOfSight(parentNode.WorldPos, neighborWorld, agentSize))
                {
                    int parentG = parentNode.gCost + GetGridDistance(parentNode.idx, neighborIdx);
                    if (neighborNode.gCost == 0 || parentG < neighborNode.gCost)
                    {
                        neighborNode.gCost = parentG;
                        neighborNode.hCost = GetGridDistance(neighborIdx, endIdx);
                        neighborNode.parent = parentNode;
                        openSet.Enqueue(neighborNode, neighborNode.fCost);
                    }
                }
                else
                {
                    if (neighborNode.gCost == 0 || tentativeG < neighborNode.gCost)
                    {
                        neighborNode.gCost = tentativeG;
                        neighborNode.hCost = GetGridDistance(neighborIdx, endIdx);
                        neighborNode.parent = current;
                        openSet.Enqueue(neighborNode, neighborNode.fCost);
                    }
                }
            }
        }

        Debug.LogWarning("Pathfinding completely failed.");
        return new List<Vector3>();
    }

    // 안전 SubCell 탐색: wall proximity에도 통과 가능
    private static Vector3 FindClosestSafeSubCell(HashSet<Vector3Int> walkableTiles, Vector2 agentSize, Vector3 target)
    {
        int baseX = Mathf.FloorToInt(target.x * SUBDIVISIONS);
        int baseY = Mathf.FloorToInt(target.y * SUBDIVISIONS);
        int ceilX = Mathf.CeilToInt(target.x * SUBDIVISIONS);
        int ceilY = Mathf.CeilToInt(target.y * SUBDIVISIONS);

        Vector2Int[] candidates = {
            new Vector2Int(baseX, baseY),
            new Vector2Int(baseX, ceilY),
            new Vector2Int(ceilX, baseY),
            new Vector2Int(ceilX, ceilY)
        };

        foreach (var c in candidates)
        {
            Vector3 world = SubIndexToWorld(c);
            if (IsAreaLogicallyWalkable(world, agentSize, walkableTiles))
                return world;
        }

        // BFS fallback
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        foreach (var c in candidates)
        {
            queue.Enqueue(c);
            visited.Add(c);
        }

        int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            Vector3 worldPos = SubIndexToWorld(cur);
            if (IsAreaLogicallyWalkable(worldPos, agentSize, walkableTiles))
                return worldPos;

            for (int i = 0; i < 8; i++)
            {
                Vector2Int next = new Vector2Int(cur.x + dx[i], cur.y + dy[i]);
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return SubIndexToWorld(candidates[0]);
    }

    private static List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode cur = endNode;
        while (cur != null && cur != startNode)
        {
            path.Add(cur.WorldPos);
            cur = cur.parent;
        }
        path.Add(startNode.WorldPos);
        path.Reverse();
        return path;
    }

    private static IEnumerable<Vector2Int> GetNeighborIndices(Vector2Int idx)
    {
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (x != 0 || y != 0)
                    yield return new Vector2Int(idx.x + x, idx.y + y);
    }

    private static bool LineOfSight(Vector3 start, Vector3 end, Vector2 agentSize)
    {
        Vector2 dir = end - start;
        float dist = dir.magnitude;
        if (dist < SUB_CELL_SIZE * 0.25f) return true;

        Vector2 norm = dir / dist;
        Vector2 checkSize = agentSize * 0.9f; // 벽 근접 시 예외 처리
        RaycastHit2D hit = Physics2D.BoxCast((Vector2)start + norm * 0.01f, checkSize, 0f, norm, dist - 0.02f, obstacleMask);
        return hit.collider == null;
    }

    private static bool IsAreaLogicallyWalkable(Vector3 pos, Vector2 checkSize, HashSet<Vector3Int> walkableTiles)
    {
        Vector2 half = checkSize / 2f;
        int minX = Mathf.FloorToInt(pos.x - half.x);
        int maxX = Mathf.FloorToInt(pos.x + half.x);
        int minY = Mathf.FloorToInt(pos.y - half.y);
        int maxY = Mathf.FloorToInt(pos.y + half.y);

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                if (!walkableTiles.Contains(new Vector3Int(x, y, 0))) return false;

        return true;
    }

    private static int GetGridDistance(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        int diag = Mathf.Min(dx, dy);
        int straight = Mathf.Abs(dx - dy);
        return diag * MOVE_DIAGONAL_COST + straight * MOVE_STRAIGHT_COST;
    }

    private static Vector2Int WorldToSubIndex(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x * SUBDIVISIONS), Mathf.RoundToInt(worldPos.y * SUBDIVISIONS));
    }

    private static Vector3 SubIndexToWorld(Vector2Int idx)
    {
        return new Vector3(idx.x / (float)SUBDIVISIONS, idx.y / (float)SUBDIVISIONS, 0f);
    }
}
