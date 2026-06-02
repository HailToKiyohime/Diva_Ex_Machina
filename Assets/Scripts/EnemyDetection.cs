using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public EnemyBrain enemyBrain;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (enemyBrain == null && GetComponentInParent<EnemyBrain>() != null)
            enemyBrain = GetComponentInParent<EnemyBrain>();
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (enemyBrain != null)
            {
                if (!enemyBrain.targetList.Exists(t => t.target == other.transform))
                {
                    TargetPriority newTarget = new TargetPriority
                    {
                        target = other.transform,
                        baseAggro = 100,
                        isMainTarget = false
                    };
                    enemyBrain.targetList.Add(newTarget);
                }
            }
        }
    }
}
