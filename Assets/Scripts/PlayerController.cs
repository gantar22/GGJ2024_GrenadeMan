using System;
using System.Collections;
using System.Collections.Generic;
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
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float WalkSpeed = 100f;
    public PlayerControllerData Data;
    public uint ID;
    public void Init(uint inID)
    {
        ID = inID;
    }

    public void Tick(Gamepad gamepad)
    {
        transform.position += Vector3.right * (gamepad.leftStick.value.x * WalkSpeed * Time.deltaTime);
    }
}
