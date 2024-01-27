using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct PlayerControllerData
{
    public Gamepad gamepad;

    public PlayerControllerData(Gamepad gamepad)
    {
        this.gamepad = gamepad;
    }
}
public class PlayerController : MonoBehaviour
{
    public PlayerControllerData data;
    public void Init(PlayerControllerData inData)
    {
        this.data = inData;
    }

    public void Update()
    {
        transform.position += Vector3.right * data.gamepad.leftStick.value.x;
    }
}
