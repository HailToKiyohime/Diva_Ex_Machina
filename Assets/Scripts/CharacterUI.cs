using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Cinemachine;

public class CharacterUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public Transform target;
    public float horizontalRotationSpeed = 5f;

    [Header("Vertical Move")]
    public float minY = 0.5f;
    public float maxY = 2.0f;
    public float verticalRotationSpeed = 3f; 
    public float verticalDeadZone = 3f;   // 小於這個就當沒拖上下

    [Header("Zoom")]
    public float zoomSpeed = 0.2f;
    public float maxZoom = 1.5f;
    public float minZoom = 0.25f;
    public CinemachineCamera cinemachineCamera;

    bool dragging;

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || target == null) return;

        float deltaX = eventData.delta.x;
        float deltaY = eventData.delta.y;

        if (Mathf.Abs(deltaX )> Mathf.Abs(deltaY)) {
            // 先做旋轉（水平一定要順）
            if (Mathf.Abs(deltaX) > 0.01f)
            {
                target.Rotate(0f, deltaX * horizontalRotationSpeed * Time.deltaTime, 0f, Space.World);
            }
        }
        else {
            // 只有真的上下拖到一定距離才移動
            if (Mathf.Abs(deltaY) > verticalDeadZone)
            {
                float worldDeltaY = deltaY * Time.deltaTime * 0.1f;
                var pos = target.localPosition;
                pos.y -= worldDeltaY;
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
                target.localPosition = pos;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (cinemachineCamera == null) return;

        float scroll = eventData.scrollDelta.y * -zoomSpeed* Time.deltaTime;
        float size = cinemachineCamera.Lens.OrthographicSize + scroll;
        size = Mathf.Clamp(size, minZoom, maxZoom);
        cinemachineCamera.Lens.OrthographicSize = size;
    }
}
