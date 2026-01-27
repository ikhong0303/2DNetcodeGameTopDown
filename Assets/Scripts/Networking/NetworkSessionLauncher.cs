using Unity.Netcode;
using UnityEngine;

namespace TopDownShooter.Networking
{
    public class NetworkSessionLauncher : MonoBehaviour
    {
        public void StartHost()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }
        }

        public void StartClient()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartClient();
            }
        }

        public void StartServer()
        {
            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartServer();
            }
        }

        public void LeaveSession()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
    }
}
