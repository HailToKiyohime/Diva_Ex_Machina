using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public ModularEntityBrain enemyBrain;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (enemyBrain == null && GetComponentInParent<ModularEntityBrain>() != null)
            enemyBrain = GetComponentInParent<ModularEntityBrain>();
    }
    public void OnTriggerEnter(Collider other)
    {
        for (int x = enemyBrain.targets.Count - 1; x >= 0; x--)
        {
            if (enemyBrain.targets[x].targetTransform == other.transform)
            {
                return;
            }
        }
        if (other.tag == "Player")
        {
            enemyBrain.AddTarget(other.transform, TargetType.Player, 25,1);
        }
        else if (other.tag == "Defence Fortifications")
        {
            enemyBrain.AddTarget(other.transform, TargetType.Building, 50, 1);

        }else if (other.tag == "Obstacle")  
        {
            enemyBrain.AddTarget(other.transform, TargetType.Obstacle, 10, 1);
        }
    }

}
