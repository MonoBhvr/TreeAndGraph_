using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PathFollower : MonoBehaviour
{
    [Header("Navigation Data")]
    public BSP_Generator dungeonData;
    public Vector2 agentSize = new Vector2(1f, 1f);
    [FormerlySerializedAs("pathUpdateInterval")] public float pathUpdateInterval_stop = 2f;
    public float pathUpdateInterval_move = 1f;
    public float waypointProximity = 0.5f;

    private Transform target;
    private List<Vector3> path;
    private int currentWaypointIndex = 0;
    private float timer = 0f;
    private Vector2 lastTargetPosition;

    public void SetTarget(Transform newTarget) => target = newTarget;

    private bool Target_Moved()
    {
        if (target == null) return false;
        if (Vector2.Distance(lastTargetPosition, target.position) > 0.1f)
        {
            lastTargetPosition = target.position;
            return true;
        }
        return false;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (target == null || dungeonData == null) return;

        if (Target_Moved() && timer >= pathUpdateInterval_move)
        {
            UpdatePath();
            timer = 0f;
        }
        else if (timer >= pathUpdateInterval_stop)
        {
            UpdatePath();
            timer = 0f;
        }
    }

    private void UpdatePath()
    {
        var walkableTiles = dungeonData.GetWalkableTiles();
        List<Vector3> newPath = Thetastar_PathFind.FindPath(walkableTiles, agentSize, transform.position, target.position);

        if (newPath != null && newPath.Count > 0)
        {
            path = newPath;
            currentWaypointIndex = 0;
        }
        else path = null;
    }

    public Vector2 GetDesiredVelocity(float currentSpeed)
    {
        if (path == null || path.Count == 0 || currentWaypointIndex >= path.Count)
            return Vector2.zero;

        Vector3 targetPos = path[currentWaypointIndex];
        if (Vector3.Distance(transform.position, targetPos) < waypointProximity)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= path.Count)
                return Vector2.zero;
            targetPos = path[currentWaypointIndex];
        }

        return (targetPos - transform.position).normalized * currentSpeed;
    }

    void OnDrawGizmos()
    {
        if (path == null || path.Count == 0) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, path[0]);
        for (int i = 0; i < path.Count - 1; i++)
            Gizmos.DrawLine(path[i], path[i + 1]);
    }

    public float GetRemainingDistance()
    {
        if (path == null || currentWaypointIndex >= path.Count)
            return 0f;

        float distance = Vector3.Distance(transform.position, path[currentWaypointIndex]);
        for (int i = currentWaypointIndex; i < path.Count - 1; i++)
            distance += Vector3.Distance(path[i], path[i + 1]);
        return distance;
    }
}
