using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


public struct LobbyOutput
{
    public GameManager.ConnectedPlayer[] PlayerData;
}

public class LobbyManager : MonoBehaviour
{
    [SerializeField] public PlayerLobbyIcon[] Icons = null;
    [SerializeField] private TMPro.TMP_Text CountdownText;

    private Color[] Colors;
    
    public void PerformLobby(GameManager.ConnectedPlayer[] inPlayers,Color[] inColors,int? inWinner)
    {
        Colors = inColors;
        CountdownText.text = "";
        for (int i = 0; i < Icons.Length; i++) // todo allow dynamically sized parties
        {
            Icons[i].Init(i);
            if (inPlayers.Any(_ => _.SlotIndex == i))
            {
                var player = inPlayers.First(_ => _.SlotIndex == i);
                Icons[i].SetInputDevice(player.Input);
                Icons[i].SetColor(Colors[player.ColorId],player.ColorId);// todo don't search twice
                Icons[i].joinedEvent.Invoke();
                Icons[i].bActive = true;
                if (inWinner.HasValue && inWinner.Value == i)
                {
                    Icons[i].crownEvent.Invoke();
                }
            }
        }
    }

    public void AddPlayer(PlayerInput InputDevice)
    {
        var colorId = Enumerable.Range(0, Int32.MaxValue).First(i => !Icons.Where(_=>_.InputDevice != null).Any(_ => _.colorId == i));
        for (int i = 0; i < Icons.Length; i++)
        {
            if (Icons[i].InputDevice == null)
            {
                Icons[i].SetColor(Colors[colorId % Colors.Length],colorId);
                Icons[i].SetInputDevice(InputDevice);
                return;
            }
        }
    }

    public void RemovePlayer(PlayerInput InputDevice)
    {
        for (int i = 0; i < Icons.Length; i++)
        {
            if (Icons[i].InputDevice == InputDevice)
            {
                Icons[i].bActive = false;
                Icons[i].leaveEvent.Invoke();
            }
        }
    }

    public void Clear()
    {
        foreach (var icon in Icons)
        {
            icon.Clear();
        }
    }
    
    
    

    public LobbyOutput? Tick()
    {
        foreach (var icon in Icons)
        {
            icon.Tick(Colors.Select((c,i) => (i,c)).Where((color)=>!Icons.Any(_=>_.colorId == color.i)));
        }

        if (Icons.All(_=>_.bReady || !_.bActive) && Icons.Count(_=>_.bActive) > 1)
        {
            return new LobbyOutput
            {
                PlayerData = Icons.Where(_=>_.bActive).Select(_ => new GameManager.ConnectedPlayer()
                {
                    SlotIndex = _.SlotIndex,
                    ColorId = _.colorId,
                    Input = _.InputDevice,
                }).ToArray()
            };
        }
        else
        {
            return null;
        }
    }

    public void CountDown(float TimeLeft)
    {
        CountdownText.text = $"{1 + (int)TimeLeft}!";
    }

    public void ResetCountDown()
    {
        CountdownText.text = "";
    }
}
