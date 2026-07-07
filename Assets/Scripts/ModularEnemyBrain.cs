using UnityEngine;
public enum EnemyState
{
    Idle,
    Patrolling,
    Chasing,
    Combat,
}
public class ModularEnemyBrain : MonoBehaviour
{
    public EnemyState currentState;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentState = EnemyState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
