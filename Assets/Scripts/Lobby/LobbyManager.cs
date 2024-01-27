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
        onFinish = inOnFinish;
    }

    public void Update()
    {
        foreach (var icon in Icons)
        {
            icon.Tick();
        }

        bool bAllReady = true;
        foreach (var icon in Icons)
        {
            bAllReady &= icon.bReady;
        }

        if (bAllReady)
        {//todo count down
            Finish();
        }
    }

    private void Reset()
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
        Reset();
        callback(output);
    }
}
