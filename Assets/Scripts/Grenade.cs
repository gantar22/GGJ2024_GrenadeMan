using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] public float FuseTime = 3.5f;
    [SerializeField] public float ChainReactionDelay = .75f;
    [SerializeField] public float KillRadius = 3f;
    [SerializeField] public float ExplosionRadius = 3.25f;
    [SerializeField] public float ExplosionForceGrenades = 10;
    [SerializeField] public float ExplosionForceGrenadesUpwardPart = 1;
    [SerializeField] public float ExplosionForcePlayers = 40;
    [SerializeField] public float ExplosionForcePlayersUpwardPart = 40;
    [SerializeField] public Rigidbody2D MainRB;
    [SerializeField] public Rigidbody2D PinRB;
    [SerializeField] public SpriteRenderer Highlight;
    [SerializeField] public Joint2D PinJoint;
    [SerializeField] public ParticleSystem ExplosionParticles;

    public float? FuseTimeLeft = null;

    public void Prime(Vector2 Force, ForceMode2D impulse)
    {
        if (FuseTimeLeft.HasValue)
        {
            return;
        }
        FuseTimeLeft = FuseTime;
        PinRB.transform.SetParent(null,true);
        PinJoint.enabled = false;
        PinRB.simulated = true;
        PinRB.AddForce(Force);
    }

    public void Explode()
    {
        Instantiate(ExplosionParticles, transform.position, quaternion.Euler(-90,0,0));
    }
}
