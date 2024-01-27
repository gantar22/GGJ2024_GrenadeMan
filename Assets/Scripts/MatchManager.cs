using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

public struct MatchParams
{
    public PlayerLobbyData[] Players;
}

public struct PlayerMatchData
{
    public PlayerController Controller;
    public Gamepad Gamepad;
    public PlayerLobbyData LobbyData;
}

public class MatchManager : MonoBehaviour
{
    [SerializeField] public GameObject[] spawnPoints;
    [SerializeField] public PlayerController playerControllerPrefab;

    private Dictionary<uint, PlayerMatchData> players;
    public void Init(MatchParams inParams)
    {
        if (inParams.Players.Length > spawnPoints.Length)
        {
            Debug.LogError($"Match started with {inParams.Players.Length} players but level only has {spawnPoints.Length} spawn locations");
            return;
        }

        uint[] PlayerSpawnOrder = PermuteNumbers(inParams.Players.Length);

        players = new Dictionary<uint, PlayerMatchData>();
        for (uint i = 0; i < inParams.Players.Length; i++)
        {
            var player = Instantiate(playerControllerPrefab, spawnPoints[PlayerSpawnOrder[i]].transform.position, Quaternion.identity);
            player.Init(i);
            PlayerMatchData data = new PlayerMatchData();
            data.LobbyData = inParams.Players[i];
            data.Gamepad = Gamepad.all[(int)i];
            data.Controller = player;
            players.Add(i,data);
        }
    }


    public void Tick()
    {
        foreach(var player in players)
        {
            player.Value.Controller.Tick(player.Value.Gamepad);
        }
    }

    private static uint[] PermuteNumbers(int count)
    {
        uint[] output = new uint[count];
        HashSet<int> UsedInts = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            int v = UnityEngine.Random.Range(0,count);
            while (UsedInts.Contains(v))
            {
                v = UnityEngine.Random.Range(0,count);
            }

            output[i] = (uint)v;
        }

        return output;
    }
}
