using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public ModularEnemyBrain enemyBrain;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (enemyBrain == null && GetComponentInParent<ModularEnemyBrain>() != null)
            enemyBrain = GetComponentInParent<ModularEnemyBrain>();
    }
    public void OnTriggerEnter(Collider other)
    {

    }
}
