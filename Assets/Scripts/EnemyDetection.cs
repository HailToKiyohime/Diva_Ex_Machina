using UnityEngine;

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
            RaycastHit hit;
            if (Physics.Linecast(transform.position, other.transform.position, out hit))
            {

            }
        }
    }

}
