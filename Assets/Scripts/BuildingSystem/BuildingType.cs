using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BuildingType", menuName = "RTS/BuildingType", order = 2)]
public class BuildingType : ScriptableObject
{
    public string buildingName;
    public GameObject buildingPrefab;
    public Sprite icon;
    public int[] buildCost = new int[4]; // [dirt, honey, leaf, shell]
    public List<UnitSpawnData> spawnableUnits; // Các loại unit có thể spawn từ công trình này
}
