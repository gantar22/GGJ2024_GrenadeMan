using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
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

        [HideInInspector] public float? TimeLeftTillTransistion;
        [HideInInspector] public MatchResults? PreviousResults;

        public Dictionary<int,ConnectedPlayer> playerConnections;
        public static GameState Lobby()
        {
            return new GameState()
            {
                flag = GameStateFlag.Lobby,
                LevelIndex = -1,
                playerConnections = new Dictionary<int, ConnectedPlayer>(),
            };
        }
    }

    [SerializeField] private PlayerController PlayerControllerPrefab;
    [SerializeField] private Grenade GrenadePrefab;
    [SerializeField] private PlayerTuning Tuning;
    
    [SerializeField] private MatchManager[] LevelPrefabs;

    [SerializeField] private LobbyManager LobbyManager;
    [SerializeField] private Colorset Colors;
    [SerializeField] private TMPro.TMP_Text WinnerText;

    public GameState gameState = GameState.Lobby();

    private MatchManager[] Levels;

    public struct ConnectedPlayer
    {
        public PlayerInput Input;
        public int ColorId;
        public int SlotIndex;
    }

    public void AddPlayer(PlayerInput newPlayer)
    {
        switch (gameState.flag)
        {
            case GameStateFlag.Uninit:
                gameState.playerConnections.Add(newPlayer.playerIndex,new ConnectedPlayer()
                {
                    Input = newPlayer,
                    ColorId = Enumerable.Range(0,Int32.MaxValue).First(i => !gameState.playerConnections.Values.Any(_=>_.ColorId == i)),
                    SlotIndex = Enumerable.Range(0,Int32.MaxValue).First(i => !gameState.playerConnections.Values.Any(_=>_.SlotIndex == i)),
                });
                break;
            case GameStateFlag.Lobby:
            case GameStateFlag.EndLobby:
                LobbyManager.AddPlayer(newPlayer);
                break;
            case GameStateFlag.Match:
            case GameStateFlag.EndMatch:
                Levels[gameState.LevelIndex].AddPlayer(newPlayer);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void RemovePlayer(PlayerInput playerToRemove)
    {
        switch (gameState.flag)
        {
            case GameStateFlag.Uninit:
                gameState.playerConnections.Remove(playerToRemove.playerIndex); 
                break;
            case GameStateFlag.Lobby:
            case GameStateFlag.EndLobby:
                LobbyManager.RemovePlayer(playerToRemove);
                break;
            case GameStateFlag.Match:
            case GameStateFlag.EndMatch:
                Levels[gameState.LevelIndex].RemovePlayer(playerToRemove);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void Start()
    {
        Cursor.visible = false;
        SpawnLevels();
        LobbyManager.gameObject.SetActive(false);
        switch (gameState.flag)
        {
            case GameStateFlag.Uninit:
                break;
            case GameStateFlag.Lobby:
                Jukebox.Instance.ToggleMuffle(true);
                GoToLobby(gameState.playerConnections.Values.ToArray());
                break;
            case GameStateFlag.Match:
                Jukebox.Instance.ToggleMuffle(false);
                GoToLevel(gameState.playerConnections.Values.ToArray(),gameState.LevelIndex != -1 ? gameState.LevelIndex : null);
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
                var lobbyOutput = LobbyManager.Tick();
                if (lobbyOutput.HasValue)
                {
                    gameState.flag = GameStateFlag.EndLobby;
                    gameState.TimeLeftTillTransistion = 3;
                    gameState.playerConnections = lobbyOutput.Value.PlayerData.ToDictionary(_ => _.Input.playerIndex);
                }
                break;
            case GameStateFlag.EndLobby:
                lobbyOutput = LobbyManager.Tick();
                if (lobbyOutput.HasValue)
                {
                    gameState.playerConnections = lobbyOutput.Value.PlayerData.ToDictionary(_ => _.Input.playerIndex);
                }
                else
                {
                    gameState.TimeLeftTillTransistion = null;
                    gameState.flag = GameStateFlag.Lobby;
                    LobbyManager.ResetCountDown();
                    break;
                }
                gameState.TimeLeftTillTransistion -= Time.deltaTime * .85f;
                LobbyManager.CountDown(gameState.TimeLeftTillTransistion.Value);
                if (gameState.TimeLeftTillTransistion < 0)
                {
                    LobbyManager.Clear();
                    LobbyManager.gameObject.SetActive(false);
                    Jukebox.Instance.ToggleMuffle(false);
                    GoToLevel(gameState.playerConnections.Values.ToArray());
                }
                break;
            case GameStateFlag.Match:
                var matchResults = Levels[gameState.LevelIndex].Tick();
                if (matchResults.HasValue)
                {
                    gameState.TimeLeftTillTransistion = 4f;
                    gameState.flag = GameStateFlag.EndMatch;
                    WinnerText.gameObject.SetActive(true);
                    if (matchResults.Value.Winner.HasValue)
                    {
                        gameState.playerConnections = matchResults.Value.players.ToDictionary(_ => _.Input.playerIndex);
                        var winnerData = gameState.playerConnections.Values.First(_ => _.SlotIndex == matchResults.Value.Winner);
                        WinnerText.text = $"Player <color=#{Colors.colors[winnerData.ColorId].ToHexString()}>{matchResults.Value.Winner + 1}</color> Won!";
                    }
                    else
                    {
                        WinnerText.text = $"Everyone died!";
                    }
                    gameState.PreviousResults = matchResults;
                }
                break;
            case GameStateFlag.EndMatch:
                Levels[gameState.LevelIndex].Tick();
                gameState.playerConnections =
                    Levels[gameState.LevelIndex].players.Values.Select(_ => _.LobbyData).ToDictionary(_=>_.Input.playerIndex);
                gameState.TimeLeftTillTransistion -= Time.deltaTime;
                if (gameState.TimeLeftTillTransistion < 0)
                {
                    gameState.TimeLeftTillTransistion = null;
                    Levels[gameState.LevelIndex].CleanUp();
                    Levels[gameState.LevelIndex].gameObject.SetActive(false);
                    WinnerText.text = "";
                    WinnerText.gameObject.SetActive(false);
                    Jukebox.Instance.ToggleMuffle(true);
                    GoToLobby(gameState.playerConnections.Values.ToArray(),gameState.PreviousResults.Value.Winner);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void GoToLobby(ConnectedPlayer[] inPlayers, int? inWinner = null)
    {
        gameState.flag = GameStateFlag.Lobby;
        gameState.LevelIndex = -1;
        LobbyManager.gameObject.SetActive(true);
        //TODO the callback here is probably not the greatest, we could just poll and not have to invert control flow like this
        LobbyManager.PerformLobby(inPlayers,Colors.colors,inWinner);
    }

    void GoToLevel(ConnectedPlayer[] inPlayerData, int? inLevelIndex = null)
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
        nextLevel.Init(matchParams,PlayerControllerPrefab,GrenadePrefab,Tuning,Colors.colors);
    }
}
