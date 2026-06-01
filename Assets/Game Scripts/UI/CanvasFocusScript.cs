using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasFocusScript : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvas;
    [SerializeField]
    private PlayerTracker playerTracker;
    public Transform mainCamera;

    private void Start()
    {
        if (playerTracker == null && ServiceLocator.Instance != null)
        {
            playerTracker = ServiceLocator.Instance.PlayerTracker;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (canvas == null)
        {
            Debug.LogError("CanvasFocusScript no tiene CanvasGroup asignado.", this);
            return;
        }

        Vector3 cameraPosition;

        if (playerTracker != null)
        {
            cameraPosition = playerTracker.PlayerPosition;
        }
        else if (mainCamera != null)
        {
            cameraPosition = mainCamera.position;
        }
        else
        {
            Debug.LogError("CanvasFocusScript necesita PlayerTracker o mainCamera para seguir la cabeza del rig Auto Hand/OpenXR.", this);
            return;
        }

        Vector3 dir = cameraPosition - this.gameObject.transform.position;
        canvas.alpha = Mathf.Clamp01(Vector3.Dot(dir.normalized, -this.gameObject.transform.forward));
    }
}
