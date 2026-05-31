using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WallScript : MonoBehaviour, IObstacleHandling
{

    protected ObstacleSpawner obstacleSpawner;

    private MeshRenderer objectRenderer;
    private float dissolveMesh;
    public Material[] destructionMaterials;
    public bool triggerFailAnimation { get; protected set; }
    public bool failFlag { get; protected set; }

    protected bool is_destroyed;

    public GameEvent successEvent;
    public GameEvent failureEvent;
    protected virtual void Start()
    {
        this.obstacleSpawner = ServiceLocator.Instance.ObstacleSpawner;
        this.triggerFailAnimation = false;
        this.is_destroyed = false;
        this.dissolveMesh = 0.0f;
        this.objectRenderer = this.gameObject.GetComponentInChildren<MeshRenderer>();
    }

    private void Update()
    {
        if (this.triggerFailAnimation)
        {
            this.dissolveMesh = Mathf.Clamp01(this.dissolveMesh + Time.deltaTime *1.25f);
            destructionMaterials[0].SetFloat("_Cutoff", dissolveMesh);
            destructionMaterials[1].SetFloat("_Cutoff", dissolveMesh);
            this.objectRenderer.materials = destructionMaterials;
        }
    }

    public abstract void WallFailed();
    /*{
        this.failed = true;
        this.HandleFailure();
    }*/

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera") && !triggerFailAnimation)
        {
            this.WallFailed();
        }
    }
    /*
        
    */
    public abstract void DestroyObstacle();

    /*{

   if (!is_destroyed) {
       is_destroyed = true;
       if (!this.failed)
       {
           EventBus.Bus.PublishEvent(correctEvent);
       }

       this.obstacleSpawner.RemoveObstacle(this.gameObject);
   }
}*/
}
