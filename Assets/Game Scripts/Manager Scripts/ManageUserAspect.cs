using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Updates the controllers materials and removes the laser pointer
//when the exercise session begins.
public class ManageUserAspect : MonoBehaviour
{

    public LineRenderer lineRenderer;
    public Material defaultMaterial;
    public Material rightMaterial;
    public Material leftMaterial;
    public SkinnedMeshRenderer rightRenderer;
    public SkinnedMeshRenderer leftRenderer;


    // Start is called before the first frame update
    private void Start()
    {
        EventBus.Subscribe(CustomizeStart, GameEvent.START_REHAB);
        EventBus.Subscribe(RestoreEnd, GameEvent.END_REHAB);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe(CustomizeStart, GameEvent.START_REHAB);
        EventBus.Unsubscribe(RestoreEnd, GameEvent.END_REHAB);
    }

    private void CustomizeStart()
    {
        lineRenderer.enabled = false;
        rightRenderer.material = rightMaterial;
        leftRenderer.material = leftMaterial;
    }

    private void RestoreEnd()
    {
        lineRenderer.enabled = true;
        rightRenderer.material = defaultMaterial;
        leftRenderer.material = defaultMaterial;
    }

}
