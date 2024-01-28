using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PlayerTuning")]
public class PlayerTuning : ScriptableObject
{
    public float WalkSpeed = 100f;
    public float MovementSmoothTime = .05f;
    public float MaxMovementAcceleration = 500f;
    public float JumpForce = 12f;
    public float JumpGraceDelay = .25f;
    public float JumpButtonDuration = .5f;
    public float FallGravityMultiplier = 1.5f;
    public float GrabDistance = 3f;
    
}
