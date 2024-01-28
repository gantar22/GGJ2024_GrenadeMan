using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Normal,
    Stunned,
    Throwing,
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] public GameObject VisualParts;
    [SerializeField] private PlayerTuning Tuning;
    [SerializeField] private Rigidbody2D rigidbody;
    [SerializeField] private Animator Animator;
    [SerializeField] private Transform GrenadeHolder;
    [SerializeField] public Transform PickupPoint;
    [SerializeField] public ThrowArrow ThrowArrow;

    [Serializable]
    public struct RayCastMarkUp
    {
        public Transform Start;
        public Transform End;
    }
    [SerializeField] private RayCastMarkUp[] GroundRayCastPoints;
    public PlayerState playerState;
    private Color color;
    private float movementAcceleration = 0f;
    private float timeSinceLastGround = Mathf.Infinity;
    private float timeSinceLastJump = Mathf.Infinity;
    private float timeSinceJumpPressed = Mathf.Infinity;
    private float throwAlpha = 0f;
    private Vector2 throwAngle = Vector2.right;

    private Grenade PossibleHeldGrenade = null;
    private Grenade PossiblePreviouslyColoredGrenade;
    [SerializeField] private float ThrowChargeSpeed;
    [SerializeField] private float ThrowStrength = 10f;
    [SerializeField] private float ThrowTorque = 1f;

    public void Init(Color inColor)
    {
        color = inColor;
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
    
    public void Tick(Gamepad gamepad, Grenade PossibleNearestGrenade)
    {
        UpdateTimers(gamepad);
        Animator.SetBool("Grounded",timeSinceLastGround <= .0f);
        if (playerState != PlayerState.Stunned)
        {
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
            {
                if (PossibleHeldGrenade)
                {
                    PossibleHeldGrenade.Prime();
                }
            }
        }
        switch (playerState)
        {
            case PlayerState.Normal:
                if (gamepad != null)
                {
                    PerformMovement(gamepad);
                    if (PossiblePreviouslyColoredGrenade)
                    {
                        PossiblePreviouslyColoredGrenade.Highlight.color = Color.white;
                        PossiblePreviouslyColoredGrenade.Highlight.gameObject.SetActive(false);
                        PossiblePreviouslyColoredGrenade = null;
                    }
                    if (PossibleNearestGrenade && PossibleHeldGrenade == null)
                    {
                        PossibleNearestGrenade.Highlight.gameObject.SetActive(true);
                        PossibleNearestGrenade.Highlight.color = color;
                        PossiblePreviouslyColoredGrenade = PossibleNearestGrenade;
                    }
                    if (gamepad.buttonWest.wasPressedThisFrame)
                    {
                        if (PossibleHeldGrenade)
                        {
                            playerState = PlayerState.Throwing;
                        }
                        else
                        {
                            if (PossibleNearestGrenade)
                            {
                                PickupGrenade(PossibleNearestGrenade);
                            }
                            else
                            {
                                //feedback
                            }
                        }
                    }
                    ThrowArrow.SetActive(false);
                }
                break;
            case PlayerState.Stunned:
                break;
            case PlayerState.Throwing:
                Animator.SetBool("Running",false);
                if (gamepad != null)
                {
                    ThrowArrow.SetActive(true);
                    ThrowArrow.SetColor(color);
                    if (gamepad.leftStick.value.magnitude > .5f)
                    {
                        throwAngle = gamepad.leftStick.value;
                    }
                    ThrowArrow.SetAngle(throwAngle);
                    throwAlpha += Time.deltaTime * ThrowChargeSpeed;
                    ThrowArrow.SetAlpha(Mathf.PingPong(throwAlpha, 1f));

                    if (gamepad.buttonWest.wasReleasedThisFrame)
                    {
                       ThrowGrenade(gamepad);
                    }
                }
                else
                {
                    ThrowArrow.SetActive(false);
                }
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void PerformMovement(Gamepad gamepad)
    {
        //x movement
        var moveAxis = gamepad != null ? gamepad.leftStick.value.x : 0f;
        var targetXVel = moveAxis * Tuning.WalkSpeed;
        var newXVel = Mathf.SmoothDamp(rigidbody.velocity.x, targetXVel, ref movementAcceleration,Tuning.MovementSmoothTime,Tuning.MaxMovementAcceleration);
        rigidbody.velocity = new Vector2(newXVel,rigidbody.velocity.y);
        Animator.SetBool("Running",Mathf.Abs(moveAxis) > .05f);
        if (Mathf.Abs(moveAxis) > .05f)
        {
            SetOrientation(moveAxis);
        }
        
        //jumping
        if (timeSinceJumpPressed < Tuning.JumpButtonDuration && timeSinceLastGround < Tuning.JumpGraceDelay && timeSinceLastJump > .5f)
        {
            timeSinceLastGround = Mathf.Infinity;
            timeSinceLastJump = 0f;
            timeSinceJumpPressed = Mathf.Infinity;
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, Tuning.JumpForce);
            Animator.SetBool("Jumped",true);
        }
        else
        {
            timeSinceLastJump += Time.deltaTime;
        }

        Animator.SetBool("Jumped",timeSinceLastJump > 1.0f);
    }

    void SetOrientation(float Sign)
    {
        VisualParts.transform.rotation = Quaternion.Euler(0f, Sign > 0 ? 180 : 0, 0f);
    }

    void PickupGrenade(Grenade grenade)
    {
        PossibleHeldGrenade = grenade;
        foreach (var otherCol in grenade.GetComponentsInChildren<Collider2D>())
        {
            foreach (var col in GetComponentsInChildren<Collider2D>())
            {
                Physics2D.IgnoreCollision(col,otherCol,true);
            }
        }

        grenade.MainRB.simulated = false;
        grenade.PinRB.simulated = false;
        grenade.transform.position = GrenadeHolder.transform.position;
        grenade.transform.rotation = Quaternion.identity;
        grenade.transform.SetParent(GrenadeHolder.transform,true);
    }

    void ThrowGrenade(Gamepad gamepad)
    {
        var grenade = PossibleHeldGrenade;
        if (grenade == null || gamepad == null)
        {
            return;
        }

        PossibleHeldGrenade = null;
        
        //move grenade to outside of you then launch with alpha power
        var alpha = Mathf.PingPong(throwAlpha, 1f);
        throwAlpha = 0f;
        grenade.transform.SetParent(null,true);
        grenade.transform.position = transform.position + (Vector3)throwAngle.normalized * (1f * alpha);
        grenade.MainRB.simulated = true;
        grenade.PinRB.simulated = true;
        grenade.MainRB.AddForce(throwAngle.normalized * (alpha * ThrowStrength),ForceMode2D.Impulse);
        grenade.MainRB.AddTorque(UnityEngine.Random.value * 2f * ThrowTorque - .5f * ThrowTorque,ForceMode2D.Impulse);
        
        foreach (var otherCol in grenade.GetComponentsInChildren<Collider2D>())
        {
            foreach (var col in GetComponentsInChildren<Collider2D>())
            {
                Physics2D.IgnoreCollision(col,otherCol,false);
            }
        }

        playerState = PlayerState.Normal;
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

    public void Kill()
    {
    }

    public void CleanUp()
    {
    }
}
