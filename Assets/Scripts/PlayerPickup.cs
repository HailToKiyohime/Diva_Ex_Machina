using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "DropItem")
        {
            if(other.TryGetComponent(out DropItem dropItem))
            {
                Debug.Log("Destroy(other);");
                InventoryManager.Instance.Add(dropItem.item);
                Destroy(other.gameObject);
            }

        }   
    }
}
