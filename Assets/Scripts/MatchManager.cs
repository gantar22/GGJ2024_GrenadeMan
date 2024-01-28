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
    public PlayerLobbyData[] Players;
}

public struct PlayerMatchData
{
    public PlayerController Controller;
    public PlayerLobbyData LobbyData;
    public bool Alive;
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
    
    private Dictionary<uint, PlayerMatchData> players = new Dictionary<uint, PlayerMatchData>();
    private List<Grenade> ActiveGrenades = new List<Grenade>();
    public void Init(MatchParams inParams, PlayerController inPlayerControllerPrefab, Grenade inGrenadePrefab, PlayerTuning inTuning)
    {
        if (inParams.Players.Length > spawnPoints.Length)
        {
            Debug.LogError($"Match started with {inParams.Players.Length} players but level only has {spawnPoints.Length} spawn locations");
            return;
        }

        grenadePrefab = inGrenadePrefab;
        uint[] PlayerSpawnOrder = PermuteNumbers(inParams.Players.Length);
        Tuning = inTuning;

        players = new Dictionary<uint, PlayerMatchData>();
        for (uint i = 0; i < inParams.Players.Length; i++)
        {
            var player = Instantiate(inPlayerControllerPrefab, spawnPoints[PlayerSpawnOrder[i]].transform.position, Quaternion.identity);
            player.Init(inParams.Players[i].Color);
            PlayerMatchData data = new PlayerMatchData
            {
                LobbyData = inParams.Players[i],
                Controller = player,
                Alive = true
            };
            players.Add((uint)inParams.Players[i].ID,data);
        }

        grenadeSpawners = grenadePoints
            .Select(_ => new GrenadeSpawner() { TimeLeft = UnityEngine.Random.value * 2f + 1f }).ToArray();
    }


    public MatchResults? Tick()
    {
        var gamepads = Gamepad.all;
        foreach(var player in players.Where(_=>_.Value.Alive))
        {
            Gamepad gamepad = gamepads.Count > player.Key ? gamepads[(int)player.Key] : null;
            Grenade NearestGrenade = ActiveGrenades
                .OrderBy(_ => Vector2.Distance(player.Value.Controller.PickupPoint.position, _.transform.position))
                .FirstOrDefault(_ =>
                {
                    var VectorToGrenade = _.transform.position - player.Value.Controller.PickupPoint.position;
                    
                    return Vector2.Dot(VectorToGrenade,-player.Value.Controller.VisualParts.transform.right) > -.1f &&  VectorToGrenade.magnitude < Tuning.GrabDistance;
                });
            player.Value.Controller.Tick(gamepad,NearestGrenade);
        }

        for(int i = 0; i < grenadeSpawners.Length;i++)
        {
            grenadeSpawners[i].TimeLeft -= Time.deltaTime;
            if (grenadeSpawners[i].TimeLeft < 0f)
            {
                var newGrenade = Instantiate(grenadePrefab, grenadePoints[i].Location.transform.position,Quaternion.Euler(0,0,UnityEngine.Random.value * 360));
                ActiveGrenades.Add(newGrenade);
                grenadeSpawners[i].TimeLeft =
                    UnityEngine.Random.Range(grenadePoints[i].MinSpawnDelay, grenadePoints[i].MaxSpawnDelay);
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
                    var deadPlayers = new List<uint>();
                    foreach (var player in players.Where(_=>_.Value.Alive))
                    {
                        var dist = Vector3.Distance(grenade.transform.position, player.Value.Controller.transform.position);
                        var diff = grenade.transform.position - player.Value.Controller.transform.position;
                        if (dist < grenade.ExplosionRadius)
                        {
                            player.Value.Controller.rigidbody.AddForce(
                                diff.normalized * (grenade.ExplosionForcePlayers * (1f/diff.sqrMagnitude)) 
                                    + Vector3.up * (grenade.ExplosionForcePlayersUpwardPart * (1f/diff.sqrMagnitude))
                                ,ForceMode2D.Impulse);
                        }
                        if (dist < grenade.KillRadius)
                        {
                            player.Value.Controller.Kill(grenade);
                            
                            deadPlayers.Add(player.Key);
                        }
                    }

                    foreach (var id in deadPlayers)
                    {
                        var data = players[id];
                        data.Alive = false;
                        players[id] = data;
                    }

                    foreach (var otherGrenade in ActiveGrenades)
                    {
                        var diff = otherGrenade.transform.position - grenade.transform.position;
                        if (diff.magnitude < grenade.ExplosionRadius)
                        {
                            if (diff.magnitude < grenade.KillRadius)
                            {
                                otherGrenade.Prime(new Vector2(.8f,UnityEngine.Random.value - .5f),ForceMode2D.Impulse);
                                otherGrenade.FuseTimeLeft = Mathf.Min(otherGrenade.FuseTimeLeft.Value,
                                    otherGrenade.ChainReactionDelay);
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

        
        var DeadPlayers = players.Where(_ => _.Value.Controller.transform.position.y < DeathLevel.transform.position.y)
            .Select(_ => _.Key).ToArray();
        foreach (var id in DeadPlayers)
        {
            players.Remove(id);
        }

        if (players.Values.Count(_ => _.Alive) < 2)
        {
            return new MatchResults()
            {
                Winner = players.Values.Where(_ => _.Alive).Select((value, key) => (int?)key).FirstOrDefault(),
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
}
