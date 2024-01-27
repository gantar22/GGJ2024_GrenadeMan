using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class GameManager : MonoBehaviour
{
    [SerializeField] private MatchManager[] LevelPrefabs;

    [SerializeField] private LobbyManager LobbyManagerPrefab;

    private LobbyManager LobbyManager;
    private MatchManager[] Levels;

    private Random random = new Random();
    private void Start()
    {
        Levels = LevelPrefabs.Select(_ => Instantiate(_)).ToArray();
        foreach (var matchManager in Levels)
        {
            matchManager.gameObject.SetActive(false);
        }
        
        LobbyManager = Instantiate(LobbyManagerPrefab);
        LobbyManager.PerformLobby(output =>
        {
            var nextLevel = LevelPrefabs[random.NextInt(Levels.Length)];
            nextLevel.gameObject.SetActive(true);
            MatchParams matchParams = new MatchParams
            {
                Players = output.PlayerData,
            };
            nextLevel.Init(matchParams);
        });
    }
}
