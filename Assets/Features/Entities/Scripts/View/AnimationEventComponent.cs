using System;
using UnityEngine;

public class AnimationEventComponent : MonoBehaviour
{
    public event Action OnAnimationEvent;

    public void TriggerAnimationEvent()
    {
        OnAnimationEvent?.Invoke();
    }
}
