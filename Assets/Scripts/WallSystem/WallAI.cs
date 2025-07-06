using Unity.VisualScripting;
using Pathfinding;
using UnityEngine;

public class WallAI : MonoBehaviour
{
    void Awake()
    {
        UpdatePath();
    }

    void OnDestroy()
    {
        UpdatePath();
    }

    public void UpdatePath()
    {
        if (AstarPath.active == null) return;

        Bounds bounds = new Bounds(transform.position, transform.localScale);
        AstarPath.active.UpdateGraphs(bounds);
    }
}
