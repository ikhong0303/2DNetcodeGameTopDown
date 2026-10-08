// CLIENT: verifies local movement does not wait for network RTT and stops immediately.
string resultPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/client-movement-smoke.json");
async void Run()
{
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var player = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<TopDownShooter.Networking.NetworkPlayerController>();
    var input = player.GetComponent<TopDownShooter.Networking.PlayerInputHandler>();
    var inputField = typeof(TopDownShooter.Networking.PlayerInputHandler).GetField("<MoveInput>k__BackingField", flags);
    var fixedUpdate = typeof(TopDownShooter.Networking.NetworkPlayerController).GetMethod("FixedUpdate", flags);
    var body = player.GetComponent<UnityEngine.Rigidbody2D>();
    var originalInput = input.MoveInput;
    try
    {
        var start = body.position;
        float simulationStarted = UnityEngine.Time.fixedTime;
        if(body.interpolation != UnityEngine.RigidbodyInterpolation2D.Interpolate)
            throw new System.Exception("Owner Rigidbody interpolation was overwritten during NGO initialization.");
        inputField.SetValue(input, UnityEngine.Vector2.right);
        fixedUpdate.Invoke(player, null);
        float immediateSpeed = body.linearVelocity.x;
        if (immediateSpeed <= 0) throw new System.Exception("Local movement waited for server.");
        await System.Threading.Tasks.Task.Delay(1000);
        float distance = body.position.x - start.x;
        float simulatedSeconds = UnityEngine.Time.fixedTime - simulationStarted;
        float measuredSpeed = distance / simulatedSeconds;
        inputField.SetValue(input, UnityEngine.Vector2.zero);
        fixedUpdate.Invoke(player, null);
        if (body.linearVelocity != UnityEngine.Vector2.zero) throw new System.Exception("Local stop failed.");
        if (simulatedSeconds <= 0 || measuredSpeed < 4.5f || measuredSpeed > 5.5f)
            throw new System.Exception("Unexpected local movement speed: " + measuredSpeed);
        System.IO.File.WriteAllText(resultPath, Newtonsoft.Json.JsonConvert.SerializeObject(new
            { immediateSpeed, distance, simulatedSeconds, measuredSpeed, stopped = true, interpolation = body.interpolation.ToString() }));
    }
    catch (System.Exception ex) { System.IO.File.WriteAllText(resultPath, Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.ToString() })); }
    finally { inputField.SetValue(input, originalInput); }
}
Run();
return resultPath;
