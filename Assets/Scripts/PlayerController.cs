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
    [SerializeField] public SpriteRenderer[] Colorables;
    [SerializeField] private PlayerTuning Tuning;
    [SerializeField] public Rigidbody2D rigidbody;
    [SerializeField] private Animator Animator;
    [SerializeField] private Transform GrenadeHolder;
    [SerializeField] public Transform PickupPoint;
    [SerializeField] public ThrowArrow ThrowArrow;
    [SerializeField] public Rigidbody2D[] Gibblets;
    [Serializable]
    public struct RayCastMarkUp
    {
        public Transform Start;
        public Transform End;
    }
    [SerializeField] private RayCastMarkUp[] GroundRayCastPoints;
    [SerializeField] private RayCastMarkUp[] WallLeftRayCastPoints;
    [SerializeField] private RayCastMarkUp[] WallRightRayCastPoints;
    [SerializeField] public GameObject Crown;
    public PlayerState playerState;
    private Color color;
    private float movementAcceleration = 0f;
    private float timeSinceLastGround = Mathf.Infinity;
    private float timeSinceLastJump = Mathf.Infinity;
    private float timeSinceJumpPressed = Mathf.Infinity;
    private float throwAlpha = 0f;
    private Vector2 throwAngle = Vector2.right;
    private List<Rigidbody2D> ActiveGibs = new List<Rigidbody2D>();

    public Grenade PossibleHeldGrenade = null;
    private Grenade PossiblePreviouslyColoredGrenade;
    [SerializeField] private float ThrowChargeSpeed;
    [SerializeField] private float ThrowStrength = 10f;
    [SerializeField] private float ThrowTorque = 1f;

    public void Init(Color inColor)
    {
        color = inColor;
        foreach (var g in Colorables)
        {
            g.color = inColor;
        }
        Crown.SetActive(false);
    }

    void UpdateTimers(PlayerInput gamepad)
    {
        if (CalculateGrounded())
        {
            timeSinceLastGround = 0f;
        }
        else
        {
            timeSinceLastGround += Time.deltaTime;
        }

        if (rigidbody.velocity.y < 0 || !(gamepad != null && gamepad.actions["jump"].IsPressed()))
        {
            rigidbody.gravityScale = Tuning.FallGravityMultiplier;
        }
        else
        {
            rigidbody.gravityScale = 1;
        }

        if (gamepad != null && gamepad.actions["jump"].WasPressedThisFrame())
        {
            timeSinceJumpPressed = 0f;
        }
        else
        {
            timeSinceJumpPressed += Time.deltaTime;
        }
    }
    
    public void Tick(PlayerInput gamepad, Grenade PossibleNearestGrenade)
    {
        UpdateTimers(gamepad);
        Animator.SetBool("Grounded",timeSinceLastGround <= .0f);
        if (playerState != PlayerState.Stunned)
        {
            if (gamepad != null && gamepad.actions["prime"].WasPressedThisFrame())
            {
                if (PossibleHeldGrenade)
                {
                    Animator.SetTrigger("Pull");
                    PossibleHeldGrenade.Prime(new Vector2(.8f,UnityEngine.Random.value * .5f * VisualParts.transform.right.x),ForceMode2D.Impulse);
                }
            }
        }
        switch (playerState)
        {
            case PlayerState.Normal:
                Animator.SetBool("Throwing",false);
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
                    if (gamepad.actions["throw"].WasPressedThisFrame())
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
                Animator.SetBool("Throwing",true);
                if (gamepad != null)
                {
                    ThrowArrow.SetActive(true);
                    ThrowArrow.SetColor(color);
                    Vector2 moveStick = gamepad.actions["move"].ReadValue<Vector2>();
                    if (moveStick.magnitude > .5f)
                    {
                        throwAngle = moveStick;
                    }
                    ThrowArrow.SetAngle(throwAngle);
                    throwAlpha += Time.deltaTime * ThrowChargeSpeed;
                    ThrowArrow.SetAlpha(Mathf.PingPong(throwAlpha, 1f));

                    if (gamepad.actions["throw"].WasReleasedThisFrame())
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

    void PerformMovement(PlayerInput gamepad)
    {
        //x movement
        var moveAxis = gamepad != null ? gamepad.actions["move"].ReadValue<Vector2>().x : 0f;
        var targetXVel = moveAxis * Tuning.WalkSpeed;
        var newXVel = Mathf.SmoothDamp(rigidbody.velocity.x, targetXVel, ref movementAcceleration,Tuning.MovementSmoothTime,Tuning.MaxMovementAcceleration);
        bool blocked = false;
        {
            if (moveAxis > .1f)
            {
                foreach (var markup in WallRightRayCastPoints)
                {
                    var castDir = markup.End.position - markup.Start.position;
                    var hits = Physics2D.RaycastAll(markup.Start.position, castDir.normalized,castDir.magnitude);
                    //Debug.DrawLine(markup.Start.position,markup.End.position,Color.red);
                    if (hits.Any(_ => _.rigidbody != rigidbody && Mathf.Abs(_.normal.y) < .2f))
                    {
                        blocked = true;
                    }
                }
            }

            if (moveAxis < -.1f)
            {
                foreach (var markup in WallLeftRayCastPoints)
                {
                    var castDir = markup.End.position - markup.Start.position;
                    var hits = Physics2D.RaycastAll(markup.Start.position, castDir.normalized,castDir.magnitude);
                    //Debug.DrawLine(markup.Start.position,markup.End.position,Color.red);
                    if (hits.Any(_ => _.rigidbody != rigidbody && Mathf.Abs(_.normal.y) < .2f))
                    {
                        blocked = true;
                    }
                }
            }
        }
        if (!blocked)
        {
            rigidbody.velocity = new Vector2(newXVel,rigidbody.velocity.y);
        }
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

    void ThrowGrenade(PlayerInput gamepad)
    {
        var grenade = PossibleHeldGrenade;
        if (grenade == null || gamepad == null)
        {
            return;
        }

        Animator.SetTrigger("Throw");
        SoundMachine.Instance.PlaySound("Throw");
        PossibleHeldGrenade = null;
        
        //move grenade to outside of you then launch with alpha power
        var alpha = Mathf.PingPong(throwAlpha, 1f);
        throwAlpha = 0f;
        grenade.transform.SetParent(null,true);

        Vector3 newLocation = transform.position + (Vector3)throwAngle.normalized * (1f * alpha);
        {
            var hits = Physics2D.RaycastAll(transform.position, throwAngle.normalized,alpha);
            var blocks = hits.Where(_ => _.rigidbody != rigidbody).OrderBy(_ => Vector2.Distance(_.point, transform.position));
            if (blocks.Any())
            {
                newLocation = transform.position;
            }
        }
        grenade.transform.position = newLocation; // TODO deal with popping
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

    public void Kill(Grenade killingGrenade)
    {
        int DeathSoundId = UnityEngine.Random.Range(1, 4);
        SoundMachine.Instance.PlaySound($"Death_{DeathSoundId}");
        var randomDirs = new List<Vector2>();
        for (int i = 0; i < Gibblets.Length; i++)
        {
            float theta = ((float)i / Gibblets.Length) * Mathf.PI;
            randomDirs.Add(new Vector2(Mathf.Sin(theta),Mathf.Cos(theta)));
        }

        for (var i = 0; i < Gibblets.Length; i++)
        {
            var gibblet = Gibblets[i];
            var gib = Instantiate(gibblet, transform.position, Quaternion.identity);
            if (killingGrenade)
            {
                Vector2 genadeDir = (transform.position - killingGrenade.transform.position);
                gib.velocity = Vector2.up * Tuning.JumpForce * .5f +
                               genadeDir.normalized * (1 - genadeDir.magnitude / killingGrenade.ExplosionRadius) * killingGrenade.ExplosionForcePlayers +
                               randomDirs[i];
            }
            else
            {
                gib.velocity = randomDirs[i] + Vector2.up * 2.5f * Tuning.JumpForce;
            }
            gib.GetComponent<SpriteRenderer>().color = color; //BAD GET COMPONENT CALL
            ActiveGibs.Add(gib);
            // TODO add force to gibs
        }

        gameObject.SetActive(false);
    }

    public void CleanUp()
    {
        foreach (var gib in ActiveGibs)
        {
            Destroy(gib.gameObject);
        }
    }
}
