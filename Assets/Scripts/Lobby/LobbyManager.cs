using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct PlayerLobbyData
{
    public int ID;

    public PlayerLobbyData(int id)
    {
        ID = id;
    }
}

public struct LobbyOutput
{
    public PlayerLobbyData[] PlayerData;
}

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private PlayerLobbyIcon[] Icons = null;
    private Action<LobbyOutput> onFinish = null;
    public void PerformLobby(Action<LobbyOutput> inOnFinish)
    {
        for (int i = 0; i < Icons.Length; i++)
        {
            Icons[i].Init(i);
        }
        onFinish = inOnFinish;
    }

    private void Clear()
    {
        foreach (var icon in Icons)
        {
            icon.Clear();
        }
        onFinish = null;
    }
    
    
    
    private void Finish()
    {
        LobbyOutput output = new LobbyOutput();
        output.PlayerData = Icons.Where(_=>_.bActive).Select(_ => new PlayerLobbyData(_.ID)).ToArray();
        
        var callback = onFinish;
        Clear();
        callback(output);
    }

    public void Tick()
    {
        foreach (var icon in Icons)
        {
            icon.Tick();
        }

        if (Icons.All(_=>_.bReady || !_.bActive) && Icons.Count(_=>_.bActive) > 1)
        {//todo count down
            Finish();
        }
    }
}
