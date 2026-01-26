using System;
using UnityEngine;

namespace TopDownShooter.Core
{
    [CreateAssetMenu(menuName = "TopDownShooter/Events/Int Event Channel")]
    public class IntEventChannelSO : ScriptableObject
    {
        public event Action<int> EventRaised;

        public void Raise(int value)
        {
            DevLogger.Log(nameof(IntEventChannelSO), $"Raised event on {name} with value {value}.", this);
            EventRaised?.Invoke(value);
        }
    }
}
