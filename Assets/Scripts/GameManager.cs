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
        EndLobby,
        Match,
        EndMatch,
    }

    [Serializable]
    public struct GameState
    {
        [SerializeField]
        public GameStateFlag flag;
        [SerializeField]
        public int LevelIndex;//-1 for random/uninit
        [SerializeField]
        public PlayerLobbyData[] PlayerInfo;

        [HideInInspector] public float? TimeLeftTillTransistion;
        [HideInInspector] public MatchResults? PreviousResults;

        public static GameState Lobby()
        {
            return new GameState()
            {
                flag = GameStateFlag.Lobby,
                LevelIndex = -1,
            };
        }
    }

    [SerializeField] private PlayerController PlayerControllerPrefab;
    [SerializeField] private Grenade GrenadePrefab;
    [SerializeField] private PlayerTuning Tuning;
    
    [SerializeField] private MatchManager[] LevelPrefabs;

    [SerializeField] private LobbyManager LobbyManager;
    [SerializeField] private Colorset Colors;

    public GameState gameState = GameState.Lobby();

    private MatchManager[] Levels;

    private void Start()
    {
        SpawnLevels();
        LobbyManager.gameObject.SetActive(false);
        switch (gameState.flag)
        {
            case GameStateFlag.Uninit:
                break;
            case GameStateFlag.Lobby:
                GoToLobby(gameState.PlayerInfo);
                break;
            case GameStateFlag.Match:
                GoToLevel(gameState.PlayerInfo,gameState.LevelIndex != -1 ? gameState.LevelIndex : null);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SpawnLevels()
    {
        Levels = LevelPrefabs.Select(_ => Instantiate(_)).ToArray();
        foreach (var matchManager in Levels)
        {
            matchManager.gameObject.SetActive(false);
        }
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
            case GameStateFlag.EndLobby:
                gameState.TimeLeftTillTransistion -= Time.deltaTime;
                LobbyManager.CountDown(gameState.TimeLeftTillTransistion.Value);
                if (gameState.TimeLeftTillTransistion < 0)
                {
                    LobbyManager.gameObject.SetActive(false);
                    GoToLevel(gameState.PlayerInfo);
                }
                break;
            case GameStateFlag.Match:
                var results = Levels[gameState.LevelIndex].Tick();
                if (results.HasValue)
                {
                    gameState.TimeLeftTillTransistion = 4f;
                    gameState.flag = GameStateFlag.EndMatch;
                    gameState.PreviousResults = results;
                }
                break;
            case GameStateFlag.EndMatch:
                Levels[gameState.LevelIndex].Tick();
                gameState.TimeLeftTillTransistion -= Time.deltaTime;
                if (gameState.TimeLeftTillTransistion < 0)
                {
                    gameState.TimeLeftTillTransistion = null;
                    Levels[gameState.LevelIndex].CleanUp();
                    Levels[gameState.LevelIndex].gameObject.SetActive(false);
                    GoToLobby(gameState.PlayerInfo,gameState.PreviousResults.Value.Winner);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void GoToLobby(PlayerLobbyData[] inPlayers, int? inWinner = null)
    {
        gameState.flag = GameStateFlag.Lobby;
        gameState.LevelIndex = -1;
        LobbyManager.gameObject.SetActive(true);
        //TODO the callback here is probably not the greatest, we could just poll and not have to invert control flow like this
        LobbyManager.PerformLobby(inPlayers,Colors.colors,output =>
        {
            gameState.flag = GameStateFlag.EndLobby;
            gameState.TimeLeftTillTransistion = 3;
            gameState.PlayerInfo = output.PlayerData;
        });
    }

    void GoToLevel(PlayerLobbyData[] inPlayerData, int? inLevelIndex = null)
    {
        int levelIndex = inLevelIndex.HasValue ? inLevelIndex.Value : UnityEngine.Random.Range(0, Levels.Length);
        
        gameState.flag = GameStateFlag.Match;
        gameState.LevelIndex = levelIndex;
        var nextLevel = Levels[gameState.LevelIndex];
        nextLevel.gameObject.SetActive(true);
        MatchParams matchParams = new MatchParams
        {
            Players = inPlayerData,
        };
        nextLevel.Init(matchParams,PlayerControllerPrefab,GrenadePrefab,Tuning);
    }
}
