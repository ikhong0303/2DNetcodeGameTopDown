// Run in a LAN host Play session with a connected client and no active wave:
// unity command --project-path . eval_file <absolute-path-to-this-file> --result-only
// Uses positions outside the arena; leaves one long-lived test bullet for client inspection.
var manager = Unity.Netcode.NetworkManager.Singleton;
if (!manager.IsHost || manager.ConnectedClientsIds.Count < 2)
    throw new System.InvalidOperationException("Connect a host and client before running this smoke test.");
TopDownShooter.Networking.NetworkGameManager.Instance.StopAllCoroutines();
TopDownShooter.Networking.EnemySpawner.Instance.DespawnAllEnemies();
var pool = TopDownShooter.Pooling.NetworkObjectPool.Instance;
var projectile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/ProjectileNet.prefab").GetComponent<Unity.Netcode.NetworkObject>();
var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/EnemyNet.prefab").GetComponent<Unity.Netcode.NetworkObject>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var originals = (System.Collections.IDictionary)typeof(TopDownShooter.Pooling.NetworkObjectPool).GetField("originals", flags).GetValue(pool);
int before = originals.Count;
var bulletIds = new System.Collections.Generic.HashSet<int>();
var enemyIds = new System.Collections.Generic.HashSet<int>();
for (int i = 0; i < 128; i++)
{
    var bullet = pool.Spawn(projectile, new UnityEngine.Vector3(1000, 1000, 0), UnityEngine.Quaternion.identity,
        instance => instance.GetComponent<TopDownShooter.Networking.NetworkProjectile>().Initialize(UnityEngine.Vector2.right, 10, 1, 20, 0, (uint)i, manager.ServerTime.Time));
    bulletIds.Add(bullet.GetInstanceID());
    pool.Despawn(bullet);
    pool.Despawn(bullet); // Must not return twice or duplicate the queue entry.
    var monster = pool.Spawn(enemy, new UnityEngine.Vector3(2000, 2000, 0), UnityEngine.Quaternion.identity);
    enemyIds.Add(monster.GetInstanceID());
    pool.Despawn(monster); // Also cancels its pending ready coroutine.
}
if (originals.Count != before) throw new System.Exception("Pool grew during sequential spawn/despawn cycles.");
var flight = new TopDownShooter.Networking.NetworkProjectile.FlightState
{ Origin = new UnityEngine.Vector3(1, 2, 0), Velocity = UnityEngine.Vector2.right * 10, Started = 5, Lifetime = 2 };
if (TopDownShooter.Networking.NetworkProjectile.PositionAt(flight, 4) != flight.Origin ||
    TopDownShooter.Networking.NetworkProjectile.PositionAt(flight, 6) != new UnityEngine.Vector3(11, 2, 0) ||
    TopDownShooter.Networking.NetworkProjectile.PositionAt(flight, 9) != new UnityEngine.Vector3(21, 2, 0))
    throw new System.Exception("Trajectory time clamping failed.");
var inspectionBullet = pool.Spawn(projectile, new UnityEngine.Vector3(1000, 1000, 0), UnityEngine.Quaternion.identity,
    instance => instance.GetComponent<TopDownShooter.Networking.NetworkProjectile>().Initialize(UnityEngine.Vector2.right, 10, 1, 60, 0, 999, manager.ServerTime.Time));
return new { cyclesPerPrefab = 128, instancesBefore = before, instancesAfter = originals.Count,
    reusedBulletInstances = bulletIds.Count, reusedEnemyInstances = enemyIds.Count,
    inspectionNetworkId = inspectionBullet.NetworkObjectId, trajectoryAssertions = "passed" };
