using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public struct PlayerControllerData
{
    public uint ID;
    public PlayerControllerData(uint id)
    {
        ID = id;
    }
}

public enum PlayerState
{
    Normal,
    Stunned,
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerTuning Tuning;
    [SerializeField] private Rigidbody2D rigidbody;

    [Serializable]
    public struct RayCastMarkUp
    {
        public Transform Start;
        public Transform End;
    }
    [SerializeField] private RayCastMarkUp[] GroundRayCastPoints;
    public PlayerControllerData Data;
    public uint ID;
    public PlayerState playerState;
    private float movementAcceleration = 0f;
    private float timeSinceLastGround = Mathf.Infinity;
    private float timeSinceLastJump = Mathf.Infinity;
    private float timeSinceJumpPressed = Mathf.Infinity;
    public void Init(uint inID)
    {
        ID = inID;
    }

    void UpdateTimers(Gamepad gamepad)
    {
        if (CalculateGrounded())
        {
            timeSinceLastGround = 0f;
        }
        else
        {
            timeSinceLastGround += Time.deltaTime;
        }

        if (rigidbody.velocity.y < 0 || !(gamepad != null && gamepad.buttonSouth.IsPressed()))
        {
            rigidbody.gravityScale = Tuning.FallGravityMultiplier;
        }
        else
        {
            rigidbody.gravityScale = 1;
        }

        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
        {
            timeSinceJumpPressed = 0f;
        }
        else
        {
            timeSinceJumpPressed += Time.deltaTime;
        }
    }
    
    public void Tick(Gamepad gamepad)
    {
        UpdateTimers(gamepad);
        switch (playerState)
        {
            case PlayerState.Normal:
                if (gamepad != null)
                {
                    PerformMovement(gamepad);
                }
                break;
            case PlayerState.Stunned:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void PerformMovement(Gamepad gamepad)
    {
        var moveAxis = gamepad != null ? gamepad.leftStick.value.x : 0f;
        var targetXVel = moveAxis * Tuning.WalkSpeed;
        var newXVel = Mathf.SmoothDamp(rigidbody.velocity.x, targetXVel, ref movementAcceleration,Tuning.MovementSmoothTime,Tuning.MaxMovementAcceleration);
        rigidbody.velocity = new Vector2(newXVel,rigidbody.velocity.y);
        if (timeSinceJumpPressed < Tuning.JumpButtonDuration && timeSinceLastGround < Tuning.JumpGraceDelay && timeSinceLastJump > .5f)
        {
            timeSinceLastGround = Mathf.Infinity;
            timeSinceLastJump = 0f;
            timeSinceJumpPressed = Mathf.Infinity;
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, Tuning.JumpForce);
        }
        else
        {
            timeSinceLastJump += Time.deltaTime;
        }
    }

    bool CalculateGrounded()
    {
        foreach (var markup in GroundRayCastPoints)
        {
            var castDir = markup.End.position - markup.Start.position;
            var hits = Physics2D.RaycastAll(markup.Start.position, castDir.normalized,castDir.magnitude);
            //Debug.DrawLine(markup.Start.position,markup.End.position,Color.red);
            if (hits.Any(_ => _.rigidbody != rigidbody))
            {
                return true;
            }
        }

        return false;
    }
}
