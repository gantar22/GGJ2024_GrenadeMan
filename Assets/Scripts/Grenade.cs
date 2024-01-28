using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] public float FuseTime = 3.5f;
    [SerializeField] public float KillRadius = 3f;
    [SerializeField] public Rigidbody2D MainRB;
    [SerializeField] public Rigidbody2D PinRB;
    [SerializeField] public SpriteRenderer Highlight;
    [SerializeField] public Joint2D PinJoint;
    [SerializeField] public ParticleSystem ExplosionParticles;

    public float? FuseTimeLeft = null;

    public void Prime()
    {
        FuseTimeLeft = FuseTime;
        PinRB.transform.SetParent(null,true);
        PinJoint.enabled = false;
        PinRB.simulated = true;
        PinRB.AddForce(new Vector2(.8f,UnityEngine.Random.value * .5f - .25f),ForceMode2D.Impulse);
    }

    public void Explode()
    {
        Instantiate(ExplosionParticles, transform.position, quaternion.Euler(-90,0,0));
    }
}
