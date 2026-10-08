string resultPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Temp/client-fire-smoke.json");
async void Run()
{
try
{
// Run with eval_file in the connected CLIENT during Play mode.
// Optional Network Simulator settings must already be applied on each peer.
var manager = Unity.Netcode.NetworkManager.Singleton;
if (!manager.IsConnectedClient || manager.IsServer) throw new System.Exception("Run this on the remote client.");
var player = manager.LocalClient.PlayerObject.GetComponent<TopDownShooter.Networking.NetworkPlayerController>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var attack = typeof(TopDownShooter.Networking.NetworkPlayerController).GetMethod("HandleAttack", flags);
var predictions = typeof(TopDownShooter.Networking.NetworkPlayerController).GetField("predictedShots", flags).GetValue(player);
var active = (System.Collections.IDictionary)typeof(TopDownShooter.Networking.ProjectilePredictionPool).GetField("active", flags).GetValue(predictions);
var rtts = new System.Collections.Generic.List<ulong>();
var previousPositions = new System.Collections.Generic.Dictionary<uint, UnityEngine.Vector3>();
var flightField = typeof(TopDownShooter.Networking.NetworkProjectile).GetField("flight", flags);
int forwardSamples = 0;
int immediate = 0;
for (int i = 0; i < 16; i++)
{
    int before = active.Count;
    attack.Invoke(player, null);
    if (active.Count != before + 1) throw new System.Exception("Input did not immediately create a visual bullet.");
    immediate++;
    rtts.Add(manager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().GetCurrentRtt(0));
    float until = UnityEngine.Time.realtimeSinceStartup + 0.25f;
    while (UnityEngine.Time.realtimeSinceStartup < until)
    {
        foreach (var bullet in UnityEngine.Object.FindObjectsByType<TopDownShooter.Networking.NetworkProjectile>(UnityEngine.FindObjectsSortMode.None))
        {
            var state = ((Unity.Netcode.NetworkVariable<TopDownShooter.Networking.NetworkProjectile.FlightState>)flightField.GetValue(bullet)).Value;
            if (state.Shooter != manager.LocalClientId) continue;
            var position = bullet.transform.position;
            if (previousPositions.TryGetValue(state.Sequence, out var previous))
            {
                if (UnityEngine.Vector3.Dot(position - previous, state.Velocity) < -0.01f)
                    throw new System.Exception("Owner bullet moved backwards during server confirmation.");
                forwardSamples++;
            }
            previousPositions[state.Sequence] = position;
        }
        await System.Threading.Tasks.Task.Delay(20);
    }
}
await System.Threading.Tasks.Task.Delay(1000);
if (!manager.IsConnectedClient) throw new System.Exception("Client disconnected under network simulation.");
if (active.Count != 0) throw new System.Exception("Predicted bullets were not reconciled/rejected.");
if (forwardSamples == 0) throw new System.Exception("No authoritative bullets were sampled.");
rtts.Sort();
var result = new { shots = immediate, instantVisuals = immediate, predictionsAfterSettling = active.Count,
    rttMedianMs = rtts[rtts.Count / 2], rttMaxMs = rtts[rtts.Count - 1], forwardSamples, connected = manager.IsConnectedClient };

System.IO.File.WriteAllText(resultPath, Newtonsoft.Json.JsonConvert.SerializeObject(result));

}
catch (System.Exception ex) { System.IO.File.WriteAllText(resultPath, Newtonsoft.Json.JsonConvert.SerializeObject(new { error = ex.ToString() })); }
}
Run();
return resultPath;
