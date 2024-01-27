using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public enum GameStateFlag
    {
        Uninit,
        Lobby,
        Match,
    }

    public struct GameState
    {
        public GameStateFlag flag;
        public int? LevelIndex;

        public static GameState Lobby()
        {
            return new GameState()
            {
                flag = GameStateFlag.Lobby,
                LevelIndex = null,
            };
        }
    }
    
    [SerializeField] private MatchManager[] LevelPrefabs;

    [SerializeField] private LobbyManager LobbyManager;

    public GameState gameState = GameState.Lobby();

    private MatchManager[] Levels;

    private void Start()
    {
        Levels = LevelPrefabs.Select(_ => Instantiate(_)).ToArray();
        foreach (var matchManager in Levels)
        {
            matchManager.gameObject.SetActive(false);
        }
        GoToLobby();
    }

    private void Update()
    {
        switch (gameState.flag)
        {
            case GameStateFlag.Uninit:
                break;
            case GameStateFlag.Lobby:
                LobbyManager.Tick();
                break;
            case GameStateFlag.Match:
                Levels[gameState.LevelIndex.Value].Tick();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void GoToLobby()
    {
        gameState.flag = GameStateFlag.Lobby;
        gameState.LevelIndex = null;
        //TODO the callback here is probably not the greatest, we could just poll and not have to invert control flow like this
        LobbyManager.PerformLobby(output =>
        {
            GoToLevel(output.PlayerData);
        });
    }

    void GoToLevel(PlayerLobbyData[] inPlayerData, int? inLevelIndex = null)
    {
        int levelIndex = inLevelIndex.HasValue ? inLevelIndex.Value : UnityEngine.Random.Range(0, Levels.Length);
        
        gameState.flag = GameStateFlag.Match;
        gameState.LevelIndex = levelIndex;
        var nextLevel = Levels[gameState.LevelIndex.Value];
        nextLevel.gameObject.SetActive(true);
        MatchParams matchParams = new MatchParams
        {
            Players = inPlayerData,
        };
        nextLevel.Init(matchParams);
    }
}
