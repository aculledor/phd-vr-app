using UnityEngine;

public class CanvasFocusScript : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvas;
    [SerializeField]
    private PlayerTracker playerTracker;

    private void Start()
    {
        ResolvePlayerTracker();
    }

    // Update is called once per frame
    void Update()
    {
        if (canvas == null)
        {
            Debug.LogError("CanvasFocusScript no tiene CanvasGroup asignado.", this);
            return;
        }

        ResolvePlayerTracker();

        if (playerTracker == null)
        {
            Debug.LogError("CanvasFocusScript necesita PlayerTracker para seguir la cabeza del rig Auto Hand/OpenXR.", this);
            return;
        }

        Vector3 dir = playerTracker.PlayerPosition - this.gameObject.transform.position;
        canvas.alpha = Mathf.Clamp01(Vector3.Dot(dir.normalized, -this.gameObject.transform.forward));
    }

    private PlayerTracker ResolvePlayerTracker()
    {
        if (playerTracker != null)
        {
            return playerTracker;
        }

        if (ServiceLocator.Instance != null && ServiceLocator.Instance.PlayerTracker != null)
        {
            playerTracker = ServiceLocator.Instance.PlayerTracker;
        }

        return playerTracker;
    }
}
