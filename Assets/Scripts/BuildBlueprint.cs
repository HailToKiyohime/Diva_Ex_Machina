using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "BuildBlock/BludeBlueprint")]
public class BuildBlueprint : ScriptableObject
{
    public BoolMatrix footprint;
    public GameObject buildingPrefab;
    public GameObject pendingPrefab;
    public Material pendingMaterial;
    public Material unavailableMaterial;
    public Material buildingMaterial;
    public ItemInstance[] costs;
}
