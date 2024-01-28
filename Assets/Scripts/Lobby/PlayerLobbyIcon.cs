using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerLobbyIcon : MonoBehaviour
{
    [SerializeField] public UnityEvent joinedEvent;
    [SerializeField] public UnityEvent leaveEvent;
    [SerializeField] public UnityEvent readyEvent;
    [SerializeField] public UnityEvent unreadyEvent;
    [SerializeField] public UnityEvent clearEvent;
    [SerializeField] public UnityEvent crownEvent;
    [SerializeField] public UnityEvent unCrownEvent;

    [SerializeField] private Graphic[] Colorables;
    
    public int ID;
    public bool bActive = false;
    public bool bReady = false;

    public Color color;
    private Color[] colorOptions;
    public void Init(int inID,Color inColor,Color[] inColorOptions)
    {
        ID = inID;
        unreadyEvent.Invoke();
        leaveEvent.Invoke();
        bActive = false;
        bReady = false;
        color = inColor;
        colorOptions = inColorOptions;
        foreach (var g in Colorables)
        {
            g.color = color;
        }
    }
    
    public void Tick()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        var gamepads = Gamepad.all;
        if (gamepads.Count <= ID)
            return;
        if (gamepads[ID].buttonSouth.wasReleasedThisFrame)
        {
            if (bActive)
            {
                if(!bReady)
                {
                    readyEvent.Invoke();
                    bReady = true;
                }
            }
            else
            {
                joinedEvent.Invoke();
                bActive = true;
            }
        }

        if (gamepads[ID].buttonEast.wasReleasedThisFrame)
        {
            if (bActive)
            {
                if (bReady)
                {
                    bReady = false;
                    unreadyEvent.Invoke();
                }
                else
                {
                    bActive = false;
                    leaveEvent.Invoke();
                }
            }
        }
    }

    public void Clear()
    {
        bReady = false;
        bActive = false;
        ID = -1;
        clearEvent.Invoke();
        unCrownEvent.Invoke();
    }
}
