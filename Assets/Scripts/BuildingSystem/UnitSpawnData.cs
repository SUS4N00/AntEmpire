using UnityEngine;

[CreateAssetMenu(fileName = "UnitSpawnData", menuName = "RTS/UnitSpawnData", order = 1)]
public class UnitSpawnData : ScriptableObject
{
    public string unitName;
    public GameObject unitPrefab;
    public Sprite icon;
    public int[] resourceCost = new int[4]; // [dirt, honey, leaf, shell]
    public float spawnTime = 2f; // Thời gian cần để spawn unit này (giây)
}
