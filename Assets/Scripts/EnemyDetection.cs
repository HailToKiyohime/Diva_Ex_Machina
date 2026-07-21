using UnityEngine;
using static UnityEngine.UI.Image;

public class EnemyDetection : MonoBehaviour
{
    private ModularEntityBrain enemyBrain;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (enemyBrain == null && GetComponentInParent<ModularEntityBrain>() != null)
            enemyBrain = GetComponentInParent<ModularEntityBrain>();
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && other.tag == "Defence Fortifications")
        {
            Vector3 dir = (other.transform.position - transform.position).normalized;
            int aimRayMask = LayerMask.GetMask("Player", "Defence Fortifications");
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, Mathf.Infinity, aimRayMask, QueryTriggerInteraction.Ignore))
            {
                enemyBrain.
            }
        }
    }

}
