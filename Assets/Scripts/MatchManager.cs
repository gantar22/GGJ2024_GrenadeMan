using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

public struct MatchParams
{
    public GameManager.ConnectedPlayer[] Players;
}

public struct PlayerMatchData
{
    public PlayerController Controller;
    public bool bConnected;
    public GameManager.ConnectedPlayer LobbyData;
    public bool bAlive;
}

[Serializable]
public struct GrenadeSpawnPoint
{
    public float MinSpawnDelay;
    public float MaxSpawnDelay;
    public GameObject Location;
}

[Serializable]
public struct GrenadeSpawner
{
    public float TimeLeft;
}

public struct MatchResults
{
    public GameManager.ConnectedPlayer[] players;
    public int? Winner;//null for everyone died
}

public class MatchManager : MonoBehaviour
{
    [SerializeField] public GameObject[] spawnPoints;
    [SerializeField] public GrenadeSpawnPoint[] grenadePoints;
    [SerializeField] public Transform DeathLevel;
    private PlayerTuning Tuning;

    private GrenadeSpawner[] grenadeSpawners;
    private Grenade grenadePrefab;
    private PlayerController playerControllerPrefab;
    private Color[] Colors;


    public Dictionary<int, PlayerMatchData> players = new Dictionary<int, PlayerMatchData>();
    private List<Grenade> ActiveGrenades = new List<Grenade>();
    private float TimeSinceStarted;
    public void Init(MatchParams inParams, PlayerController inPlayerControllerPrefab, Grenade inGrenadePrefab, PlayerTuning inTuning, Color[] inColors)
    {
        if (inParams.Players.Length > spawnPoints.Length)
        {
            Debug.LogError($"Match started with {inParams.Players.Length} players but level only has {spawnPoints.Length} spawn locations");
            return;
        }

        TimeSinceStarted = 0;
        grenadePrefab = inGrenadePrefab;
        playerControllerPrefab = inPlayerControllerPrefab;
        Colors = inColors;
        uint[] PlayerSpawnOrder = PermuteNumbers(inParams.Players.Length);
        Tuning = inTuning;

        players = new Dictionary<int, PlayerMatchData>();
        for (uint i = 0; i < inParams.Players.Length; i++)
        {
            var player = Instantiate(inPlayerControllerPrefab, spawnPoints[PlayerSpawnOrder[i]].transform.position, Quaternion.identity);
            player.Init(inColors[inParams.Players[i].ColorId]);
            PlayerMatchData data = new PlayerMatchData
            {
                LobbyData = inParams.Players[i],
                bConnected = true,
                Controller = player,
                bAlive = true
            };
            players.Add(inParams.Players[i].SlotIndex,data);
        }

        grenadeSpawners = grenadePoints
            .Select(_ => new GrenadeSpawner() { TimeLeft = UnityEngine.Random.value * 2f + 1f }).ToArray();
    }


    public MatchResults? Tick()
    {
        TimeSinceStarted += Time.deltaTime;
        var gamepads = Gamepad.all;
        foreach(var player in players.Values.Where(_=>_.bAlive))
        {
            Grenade NearestGrenade = ActiveGrenades
                .Where(g=>!players.Any(_=>_.Value.Controller.PossibleHeldGrenade == g))
                .OrderBy(_ => Vector2.Distance(player.Controller.PickupPoint.position, _.transform.position))
                .FirstOrDefault(_ =>
                {
                    var VectorToGrenade = _.transform.position - player.Controller.PickupPoint.position;
                    
                    return Vector2.Dot(VectorToGrenade,-player.Controller.VisualParts.transform.right) > -.1f &&  VectorToGrenade.magnitude < Tuning.GrabDistance;
                });
            player.Controller.Tick(player.LobbyData.Input,NearestGrenade);
        }

        for(int i = 0; i < grenadeSpawners.Length;i++)
        {
            grenadeSpawners[i].TimeLeft -= Time.deltaTime;
            if (grenadeSpawners[i].TimeLeft < 0f)
            {
                var newGrenade = Instantiate(grenadePrefab, grenadePoints[i].Location.transform.position,Quaternion.Euler(0,0,UnityEngine.Random.value * 360));
                ActiveGrenades.Add(newGrenade);
                grenadeSpawners[i].TimeLeft =
                    UnityEngine.Random.Range(grenadePoints[i].MinSpawnDelay, grenadePoints[i].MaxSpawnDelay)
                    * Mathf.Min(.5f,60/(60+TimeSinceStarted * players.Values.Count(_=>!_.bAlive)));
            }
        }

        for (var i = ActiveGrenades.Count - 1; i >= 0; --i)
        {
            var grenade = ActiveGrenades[i];
            if (grenade.FuseTimeLeft.HasValue)
            {
                grenade.FuseTimeLeft -= Time.deltaTime;
                if (grenade.FuseTimeLeft < 0f)
                {//Explode!
                    grenade.Explode();
                    var deadPlayers = new List<int>();
                    foreach (var player in players.Where(_=>_.Value.bAlive))
                    {
                        var dist = Vector3.Distance(grenade.transform.position, player.Value.Controller.transform.position);
                        var diff = player.Value.Controller.transform.position - grenade.transform.position;
                        if (dist < grenade.ExplosionRadius)
                        {
                            player.Value.Controller.rigidbody.AddForce(
                                diff.normalized * (grenade.ExplosionForcePlayers * (1 - (dist/grenade.ExplosionRadius))) 
                                    + Vector3.up * (grenade.ExplosionForcePlayersUpwardPart * (1 - dist/grenade.ExplosionRadius))
                                ,ForceMode2D.Impulse);
                        }
                        if (dist < grenade.KillRadius)
                        {
                            if(!BlockedByEnvironment(grenade.transform.position, player.Value.Controller.transform.position))
                            {
                                Debug.DrawLine(grenade.transform.position, player.Value.Controller.transform.position, Color.red, 10f);
                                player.Value.Controller.Kill(grenade);

                                deadPlayers.Add(player.Key);
                            }
                        }
                    }

                    foreach (var id in deadPlayers)
                    {
                        var data = players[id];
                        data.bAlive = false;
                        players[id] = data;
                    }

                    foreach (var otherGrenade in ActiveGrenades)
                    {
                        var diff = otherGrenade.transform.position - grenade.transform.position;
                        if (diff.magnitude < grenade.ExplosionRadius)
                        {
                            if (diff.magnitude < grenade.KillRadius * .66f)
                            {
                                if(!BlockedByEnvironment(grenade.transform.position, otherGrenade.transform.position))
                                {
                                    otherGrenade.Prime(new Vector2(.8f, UnityEngine.Random.value - .5f), ForceMode2D.Impulse);
                                    otherGrenade.FuseTimeLeft = Mathf.Min(otherGrenade.FuseTimeLeft.Value,
                                        otherGrenade.ChainReactionDelay);
                                }
                            }

                            var sqrDist = Mathf.Max(diff.sqrMagnitude,.2f);
                            otherGrenade.MainRB.AddForce(
                                diff.normalized * (otherGrenade.ExplosionForceGrenades * (1f/sqrDist)) 
                                    + Vector3.up * (otherGrenade.ExplosionForceGrenadesUpwardPart * (1f/sqrDist)),
                                ForceMode2D.Impulse);
                        }
                    }
                    Destroy(grenade.PinRB.gameObject);
                    Destroy(grenade.gameObject);
                    //TODO do explosion logic
                    ActiveGrenades.RemoveAt(i);
                }
            }
        }


        // Kill players who fell off
        {
            var DeadPlayers = players.Where(_ => _.Value.bAlive).Where(_ =>
                    _.Value.Controller.transform.position.y < DeathLevel.transform.position.y)
                .Select(_ => _.Key).ToArray();
            foreach (var id in DeadPlayers)
            {
                players[id].Controller.Kill(null);
                var data = players[id];
                data.bAlive = false;
                players[id] = data;
            }
        }

        // Check for winner
        if (players.Values.Count(_ => _.bAlive) < 2 && players.Values.Count(_=>!_.bAlive) > 0)
        {
            var Winner = players.Where(_ => _.Value.bAlive);
            foreach (var winner in Winner)
            {
                winner.Value.Controller.Crown.SetActive(true);
            }
            return new MatchResults()
            {
                players = players.Values.Select(_=>_.LobbyData).ToArray(),
                Winner = Winner.Select(_ => (int?)_.Key).FirstOrDefault(),
            };
        }
        else
        {
            return null;
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

            UsedInts.Add(v);

            output[i] = (uint)v;
        }

        return output;
    }

    public void CleanUp()
    {
        foreach (var player in players)
        {
            player.Value.Controller.CleanUp();
            Destroy(player.Value.Controller.gameObject);
        }
        players.Clear();
        foreach (var grenade in ActiveGrenades)
        {
            Destroy(grenade.PinJoint.gameObject);
            Destroy(grenade.gameObject);
        }
        ActiveGrenades.Clear();
        grenadeSpawners = null;
    }

    public void AddPlayer(PlayerInput newPlayer)
    {
        var matchingPlayer = players.Values.Where(_ => !_.bConnected).Where(_ =>
            _.LobbyData.Input != null && _.LobbyData.Input.playerIndex == newPlayer.playerIndex).ToArray();
        
        if (matchingPlayer.Length == 1)
        {
            var playerData = matchingPlayer[0];
            playerData.bConnected = true;
            playerData.LobbyData.Input = newPlayer; // don't think this is necessary, but we technically didn't check for it 
            players[playerData.LobbyData.SlotIndex] = playerData;
        }
        else
        {
            var slotIndex = Enumerable.Range(0, Int32.MaxValue)
                .First(i => !players.Values.Any(_ => _.LobbyData.SlotIndex == i));
            var colorId = Enumerable.Range(0, Int32.MaxValue)
                .First(i => !players.Values.Any(_ => _.LobbyData.ColorId == i));
            var player = Instantiate(playerControllerPrefab, spawnPoints[UnityEngine.Random.Range(0,spawnPoints.Length)].transform.position, Quaternion.identity);
            player.Init(Colors[colorId]);
            PlayerMatchData data = new PlayerMatchData
            {
                LobbyData = new GameManager.ConnectedPlayer
                {
                    Input = newPlayer,
                    ColorId = colorId,
                    SlotIndex = slotIndex
                },
                bConnected = true,
                Controller = player,
                bAlive = true
            };
            players.Add(slotIndex,data);
        }
    }

    public void RemovePlayer(PlayerInput playerToRemove)
    {
        var matchingPlayer = players.Values.Where(_ => _.bConnected).Where(_ =>
            _.LobbyData.Input != null && _.LobbyData.Input.playerIndex == playerToRemove.playerIndex).ToArray();
        if (matchingPlayer.Length == 1)
        {
            var data = matchingPlayer[0];
            data.bConnected = false;
            players[data.LobbyData.SlotIndex] = data;
        }
    }

    bool BlockedByEnvironment(Vector3 start, Vector3 end)
    {
        return Physics2D.Raycast(start, end - start, Vector3.Distance(start, end), LayerMask.GetMask("Environment"));
    }
}
