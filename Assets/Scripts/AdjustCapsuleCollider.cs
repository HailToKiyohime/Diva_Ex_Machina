using UnityEngine;

public class AdjustCapsuleCollider : MonoBehaviour
{
    private CapsuleCollider capsuleCollider;
    public float newHeight = 5.0f; // The new desired height
    public float heightIncrease = 2.0f; // How much longer you want to make it

    void Start()
    {
        // Get the CapsuleCollider component attached to the GameObject
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            MakeBottomLonger(heightIncrease);
        }
        else
        {
            Debug.LogError("No CapsuleCollider found on this GameObject.");
        }
    }

    private void Update()
    {
    }
    /// <summary>
    /// Increases the height of the capsule collider and repositions its center to extend the bottom.
    /// </summary>
    /// <param name="increaseAmount">The amount to increase the height by.</param>
    void MakeBottomLonger(float increaseAmount)
    {
        // Store the original height and center
        float originalHeight = capsuleCollider.height;
        Vector3 originalCenter = capsuleCollider.center;

        // Calculate the new height
        float newHeight = originalHeight + increaseAmount;

        // Calculate the change in Y position needed to keep the top fixed
        // The capsule center moves down by half of the height increase
        float centerOffsetY = increaseAmount / 2.0f;

        // Set the new height
        capsuleCollider.height = newHeight;

        // Set the new center position
        // Make a copy of the center vector, modify the y-axis, and reassign it
        Vector3 newCenter = originalCenter;
        // This assumes the capsule's direction is the Y-axis (default: 1)
        newCenter.y -= centerOffsetY;
        capsuleCollider.center = newCenter;

        Debug.Log($"Capsule height changed from {originalHeight} to {newHeight}. Center moved from {originalCenter} to {newCenter}.");
    }
}
