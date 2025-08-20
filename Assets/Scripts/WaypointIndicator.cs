using UnityEngine;
using UnityEngine.UI;

public class WaypointIndicator : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] RectTransform uiIcon; 
    [SerializeField] Camera cam;
    [SerializeField] float edgePadding = 30f;

    public void SetTarget(Transform t) { target = t; }
    public void SetCamera(Camera a) { cam = a; }

    void LateUpdate()
    {
        if (!target || !uiIcon) return;
        if (!cam) cam = Camera.main;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);
        bool behind = screenPos.z < 0;

        float x = Mathf.Clamp(screenPos.x, edgePadding, Screen.width - edgePadding);
        float y = Mathf.Clamp(screenPos.y, edgePadding, Screen.height - edgePadding);

        if (behind) { x = Screen.width - x; y = Screen.height - y; }

        uiIcon.position = new Vector3(x, y, 0);
        uiIcon.gameObject.SetActive(true);
    }
}
