using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<GameObject> enemies; // Use List instead of array

    [Header("Attack Limiter")]
    [Tooltip("Max number of enemies allowed to ATTACK (shoot / deal ranged damage) at the same time")]
    public int maxSimultaneousAttackers = 3;

    // Track which EnemyBrain instances currently hold an "attack slot"
    private readonly HashSet<int> _attackers = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        _attackers.Clear();

        // Initialize the list with enemies found in the scene
        enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
    }

    public void AddEnemy(GameObject enemy)
    {
        enemies.Add(enemy);
    }

    public void RemoveEnemy(GameObject enemy)
    {
        enemies.Remove(enemy);
    }

    public List<GameObject> GetEnemies()
    {
        return enemies;
    }

    // ============================
    // Attack slot API (called by EnemyBrain)
    // ============================
    public bool TryClaimAttackSlot(EnemyBrain brain)
    {
        if (brain == null) return false;

        int id = brain.GetInstanceID();
        if (_attackers.Contains(id)) return true; // already has a slot

        int cap = Mathf.Max(0, maxSimultaneousAttackers);
        if (_attackers.Count >= cap) return false;

        _attackers.Add(id);
        return true;
    }

    public void ReleaseAttackSlot(EnemyBrain brain)
    {
        if (brain == null) return;
        int id = brain.GetInstanceID();
        _attackers.Remove(id);
    }

    public int CurrentAttackersCount => _attackers.Count;
}