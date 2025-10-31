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
                InventoryManager.Instance.AddItemToInventory(dropItem.item);
                Destroy(other.gameObject);
            }

        }   
    }
}
