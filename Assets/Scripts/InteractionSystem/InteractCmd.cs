using UnityEngine;

public enum InteractType
{
    None,
    AttackMe,
    CollectHoney,
    CollectLeaf,
    CollectDirt,
    CollectShell,
    ResourceWarehouse,
    FoodWarehouse,
    BuildAntHill
}

// Optimized InteractCmd script
public class InteractCmd : MonoBehaviour
{
    public InteractType interactCommand;
}
