// HOST: checks physical bullet impact, score, and full monster reset on the same instance.
string resultPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/impact-reuse-smoke.json");
async void Run()
{
    var pool = TopDownShooter.Pooling.NetworkObjectPool.Instance;
    var monsters = new System.Collections.Generic.List<Unity.Netcode.NetworkObject>();
    Unity.Netcode.NetworkObject bullet = null;
    Unity.Netcode.NetworkObject respawn = null;
    try
    {
        TopDownShooter.Networking.NetworkGameManager.Instance.StopAllCoroutines();
        TopDownShooter.Networking.EnemySpawner.Instance.DespawnAllEnemies();
        foreach (var e in UnityEngine.Object.FindObjectsByType<TopDownShooter.Networking.NetworkEnemy>(UnityEngine.FindObjectsSortMode.None)) pool.Despawn(e.NetworkObject);
        var enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/EnemyNet.prefab").GetComponent<Unity.Netcode.NetworkObject>();
        var bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/ProjectileNet.prefab").GetComponent<Unity.Netcode.NetworkObject>();
        for(int i=0;i<32;i++) monsters.Add(pool.Spawn(enemyPrefab,new UnityEngine.Vector3(1000+i*3,1000,0),UnityEngine.Quaternion.identity));
        await System.Threading.Tasks.Task.Delay(100);
        foreach(var e in monsters) e.GetComponent<TopDownShooter.Networking.EnemyAI>().Deactivate();
        var target = monsters[0];
        var player = TopDownShooter.Networking.NetworkPlayerController.ActivePlayers[0];
        int scoreBefore = player.Score.Value;
        var nm = Unity.Netcode.NetworkManager.Singleton;
        bullet = pool.Spawn(bulletPrefab,target.transform.position-UnityEngine.Vector3.right,UnityEngine.Quaternion.identity,
            instance => instance.GetComponent<TopDownShooter.Networking.NetworkProjectile>().Initialize(UnityEngine.Vector2.right,10,100,2,player.OwnerClientId,1000,nm.ServerTime.Time));
        await System.Threading.Tasks.Task.Delay(350);
        if(target.IsSpawned || bullet.IsSpawned) throw new System.Exception("Server impact did not despawn the monster/bullet.");
        if(player.Score.Value <= scoreBefore) throw new System.Exception("Server impact did not award score.");
        respawn = pool.Spawn(enemyPrefab,new UnityEngine.Vector3(1000,1000,0),UnityEngine.Quaternion.identity);
        if(respawn != target) throw new System.Exception("Monster was destroyed instead of reused.");
        await System.Threading.Tasks.Task.Delay(50);
        var health = respawn.GetComponent<TopDownShooter.Networking.NetworkHealth>();
        if(health.CurrentHealth.Value <= 0 || !respawn.GetComponent<TopDownShooter.Networking.NetworkEnemy>().IsReady)
            throw new System.Exception("Reused monster retained death state.");
        System.IO.File.WriteAllText(resultPath,Newtonsoft.Json.JsonConvert.SerializeObject(new
            { impact="passed", sameMonsterReused=true, restoredHp=health.CurrentHealth.Value, scoreAwarded=player.Score.Value-scoreBefore }));
    }
    catch(System.Exception ex) { System.IO.File.WriteAllText(resultPath,Newtonsoft.Json.JsonConvert.SerializeObject(new { error=ex.ToString() })); }
    finally
    {
        if(bullet != null && bullet.IsSpawned) pool.Despawn(bullet);
        foreach(var e in monsters) if(e != null && e.IsSpawned) pool.Despawn(e);
        if(respawn != null && respawn.IsSpawned) pool.Despawn(respawn);
    }
}
Run();
return resultPath;
