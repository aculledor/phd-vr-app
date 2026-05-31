using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum HitType{
    RIGHT_HIT,
    LEFT_HIT
}
public class PlayerHitting : MonoBehaviour
{

    public HitType type;


    //Invokes the TargetScript hit callback on collision with the object.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hit"))
        {
            TargetScript hit = other.GetComponentInParent<TargetScript>();
            hit.TakeHit(type);
        }
    }


}
