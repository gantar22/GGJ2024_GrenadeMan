using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] public Rigidbody2D MainRB;

    [SerializeField] public Rigidbody2D PinRB;
    [SerializeField] public SpriteRenderer Highlight;

    public float? FuseTimeLeft = null;
}
