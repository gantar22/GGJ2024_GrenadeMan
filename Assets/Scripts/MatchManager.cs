using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

public struct MatchParams
{
    public int playerCount;
}

public class MatchManager : MonoBehaviour
{
    [SerializeField] public GameObject[] SpawnPoints;
    [SerializeField] public PlayerController PlayerControllerPrefab;

    private List<PlayerController> Players = new List<PlayerController>();
    public void Init(MatchParams Params)
    {
        if (Params.playerCount > SpawnPoints.Length)
        {
            Debug.LogError($"Match started with {Params.playerCount} players but level only has {SpawnPoints.Length} spawn locations");
            return;
        }

        var Gamepads = Gamepad.all;
        if (Params.playerCount > Gamepads.Count)
        {
            Debug.LogError($"Match started with {Params.playerCount} players but only {Gamepads.Count} gamepad(s) are connected");
            return;
        }

        int[] PlayerSpawnOrder = PermuteNumbers(Params.playerCount);
        
        for (int i = 0; i < Params.playerCount; i++)
        {
            var Player = Instantiate(PlayerControllerPrefab, SpawnPoints[i].transform.position, Quaternion.identity);
            Player.Init(new PlayerControllerData(Gamepads[PlayerSpawnOrder[i]]));
            Players.Add(Player);
        }
    }

    public static int[] PermuteNumbers(int count)
    {
        int[] output = new int[count];
        HashSet<int> UsedInts = new HashSet<int>();
        Random random = new Random();
        for (int i = 0; i < count; i++)
        {
            int v = random.NextInt(count);
            while (UsedInts.Contains(v))
            {
                v = random.NextInt(count);
            }

            output[i] = v;
        }

        return output;
    }
}
