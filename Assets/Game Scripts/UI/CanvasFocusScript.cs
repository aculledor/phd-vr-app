using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasFocusScript : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvas;
    public Transform mainCamera;
    // Update is called once per frame
    void Update()
    {
        Vector3 dir = mainCamera.position - this.gameObject.transform.position;
        canvas.alpha = Mathf.Clamp01(Vector3.Dot(dir.normalized, -this.gameObject.transform.forward));
        
    }
}
