using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    
    public bool bActive = false;
    public bool bReady = false;

    public Color color;
    public int colorId;
    public int SlotIndex;
    public PlayerInput InputDevice = null;
    public void Init(int inSlotIndex)
    {
        unreadyEvent.Invoke();
        leaveEvent.Invoke();
        bActive = false;
        bReady = false;
        SlotIndex = inSlotIndex;
    }

    public void SetInputDevice(PlayerInput inInputDevice)
    {
        InputDevice = inInputDevice;
    }
    
    public void SetColor(Color inColor, int inColorId)
    {
        color = inColor;
        colorId = inColorId;
        foreach (var g in Colorables)
        {
            g.color = color;
        }
    }
    
    public void Tick(IEnumerable<(int,Color)> availableColors)// todo add color changing here
    {
        if (InputDevice)
        {
            if (InputDevice.actions["join"].WasPressedThisFrame()) //todo look up action names
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

            if (InputDevice.actions["leave"].WasPressedThisFrame())
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

            if (bActive)
            {
                if (InputDevice.actions["right"].WasPressedThisFrame())
                {
                    MoveColor(availableColors,1);
                }

                if (InputDevice.actions["left"].WasPressedThisFrame())
                {
                    MoveColor(availableColors, -1);
                }
            }
        }
    }

    static int Mod(int a, int b)
    {
        return ((a % b) + b) % b;
    }

    void MoveColor(IEnumerable<(int,Color)> inColors, int diff)
    {
        var colors = inColors as (int, Color)[] ?? inColors.ToArray();
        var nextColors = 
            colors
            .Where(_ => _.Item1 > colorId)
            .OrderBy(_ => _.Item1)
            .Concat
            (
                colors
                .Where(_ => _.Item1 < colorId)
                .OrderBy(_ => _.Item1)
            )
            .Prepend((colorId,color))
            .ToArray();
        if (nextColors.Any())
        {
            var nextColor = nextColors[Mod(diff,nextColors.Length)];
            SetColor(nextColor.Item2,nextColor.Item1);
        }
    }

    public void Clear()
    {
        bReady = false;
        bActive = false;
        clearEvent.Invoke();
        unCrownEvent.Invoke();
    }
}
