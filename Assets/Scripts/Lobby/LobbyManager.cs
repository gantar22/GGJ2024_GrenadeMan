using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct PlayerLobbyData
{
    public int ID;
    public Color Color;

}

public struct LobbyOutput
{
    public PlayerLobbyData[] PlayerData;
}

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private PlayerLobbyIcon[] Icons = null;
    [SerializeField] private TMPro.TMP_Text CountdownText;
    private Action<LobbyOutput> onFinish = null;
    public void PerformLobby(PlayerLobbyData[] inPlayers,Color[] Colors,int? inWinner, Action<LobbyOutput> inOnFinish)
    {
        CountdownText.text = "";
        for (int i = 0; i < Icons.Length; i++)
        {
            Icons[i].Init(i,Colors[i],Colors);
            if (inPlayers.Any(_ => _.ID == i))
            {
                Icons[i].joinedEvent.Invoke();
                Icons[i].bActive = true;
                if (inWinner.HasValue && inWinner.Value == i)
                {
                    Icons[i].crownEvent.Invoke();
                }
            }
        }
        onFinish = inOnFinish;
    }

    public void Clear()
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
        output.PlayerData = Icons.Where(_=>_.bActive).Select(_ => new PlayerLobbyData{ID = _.ID, Color = _.color}).ToArray();
        
        var callback = onFinish;
        callback(output);
    }

    public void Tick()
    {
        foreach (var icon in Icons)
        {
            icon.Tick();
        }

        if (Icons.All(_=>_.bReady || !_.bActive) && Icons.Count(_=>_.bActive) > 1)
        {
            Finish();
        }
    }

    public void CountDown(float TimeLeft)
    {
        CountdownText.text = $"{1 + (int)TimeLeft}!";
    }
}
