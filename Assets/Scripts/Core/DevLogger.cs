using UnityEngine;

namespace TopDownShooter.Core
{
    public static class DevLogger
    {
        private const string LogPrefix = "[DevLog]";

        public static void Log(string context, string message, Object target = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (target != null)
            {
                Debug.Log($"{LogPrefix} {context}: {message}", target);
            }
            else
            {
                Debug.Log($"{LogPrefix} {context}: {message}");
            }
#endif
        }
    }
}
