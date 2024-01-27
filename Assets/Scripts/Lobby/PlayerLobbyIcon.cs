using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerLobbyIcon : MonoBehaviour
{
    [SerializeField] private UnityEvent joinedEvent;
    [SerializeField] private UnityEvent leaveEvent;
    [SerializeField] private UnityEvent readyEvent;
    [SerializeField] private UnityEvent unreadyEvent;
    
    
    public int ID;
    public bool bActive = false;
    public bool bReady = false;
    
    public void Init(int inID)
    {
        ID = inID;
        unreadyEvent.Invoke();
        leaveEvent.Invoke();
        bActive = false;
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
        if (gamepads[ID].buttonSouth.IsPressed())
        {
            if (bActive)
            {
                if (bReady)
                {
                    unreadyEvent.Invoke();
                    bReady = false;
                }
                else
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

        if (gamepads[ID].buttonEast.IsPressed())
        {
            if (bActive)
            {
                bActive = false;
                leaveEvent.Invoke();
            }
        }
    }

    public void Clear()
    {
        ID = -1;
    }
}
