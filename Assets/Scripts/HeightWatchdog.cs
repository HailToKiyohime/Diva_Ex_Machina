using UnityEngine;

/// <summary>
/// 高度漂移診斷。掛到船上、Play、看 Console。查完就可以移除。
///
/// 判讀方式：
///   vel.y ≈ 0 但 rb.y 一直變  → 有程式碼在直接寫入位置（transform.position /
///                                rb.position / MovePosition）。Constraint 擋不住這些。
///   vel.y ≠ 0                 → Freeze Position Y 沒有生效在這個 Rigidbody 上。
///                                檢查是不是場景裡還有第二艘船 / 凍錯物件。
///   rb.y 穩定但 transform.y 在抖 → 只是 Interpolate 的視覺插值，不是真的在掉。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HeightWatchdog : MonoBehaviour
{
    [Tooltip("每隔幾秒印一次")]
    public float logInterval = 1f;

    [Tooltip("單步位移超過這個值就立刻警告（抓一次性的大跳躍）")]
    public float jumpThreshold = 0.05f;

    private Rigidbody rb;
    private float startY;
    private float lastLogY;
    private float lastStepY;
    private float nextLogTime;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        startY = rb.position.y;
        lastLogY = startY;
        lastStepY = startY;
        nextLogTime = Time.time + logInterval;

        Debug.Log($"[HeightWatchdog] 起始 rb.y = {startY:F4}   " +
                  $"constraints = {rb.constraints}   isKinematic = {rb.isKinematic}", this);
    }

    private void FixedUpdate()
    {
        float y = rb.position.y;

        // 單步跳躍：constraint 生效時物理不可能造成這種位移，一定是直接寫入
        float stepDelta = y - lastStepY;
        if (Mathf.Abs(stepDelta) > jumpThreshold)
        {
            Debug.LogWarning(
                $"[HeightWatchdog] 單步位移 {stepDelta:F4}（一步 {Time.fixedDeltaTime}s）\n" +
                $"  rb.y {lastStepY:F4} → {y:F4}   vel.y = {rb.linearVelocity.y:F4}\n" +
                $"  速度接近 0 卻位移這麼多 = 有東西在傳送它，不是物理造成的。", this);
        }
        lastStepY = y;

        if (Time.time < nextLogTime) return;
        nextLogTime = Time.time + logInterval;

        float drift = y - lastLogY;
        lastLogY = y;

        Debug.Log(
            $"[HeightWatchdog] rb.y = {y:F4}   transform.y = {transform.position.y:F4}\n" +
            $"  本區間漂移 = {drift:F4} / {logInterval}s      累計 = {(y - startY):F4}\n" +
            $"  vel.y = {rb.linearVelocity.y:F4}   （≈0 卻在漂 = 被傳送；≠0 = 凍結沒生效）", this);
    }
}
