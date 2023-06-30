using UnityEngine;
using System;

// PROBABLY WANNA USE UNIRX HERE, BUT A BIT OVERKILL ATM

public class Events : MonoBehaviour
{
    static public event Action<int, int> scrollFeedStart;
    static public event Action<int, int> scrollFeedEnd;
    static public event Action<int, float> draggingFeed;

    static public Action<int, int> InvokeScrollFeedStart => ((feedNum, scrollDirection) => scrollFeedStart?.Invoke(feedNum, scrollDirection));
    static public Action<int, int> InvokeScrollFeedEnd => ((feedNum, scrollDirection) => scrollFeedEnd?.Invoke(feedNum, scrollDirection));
    static public Action<int, float> InvokeDraggingFeed => ((feedNum, feedY) => draggingFeed?.Invoke(feedNum, feedY));

    static public void Clear()
    {
        if (scrollFeedStart != null)
        {
            foreach (Delegate handler in scrollFeedStart.GetInvocationList())
            {
                scrollFeedStart -= (Action<int, int>)handler;
            }
        }

        if (scrollFeedEnd != null)
        {
            foreach (Delegate handler in scrollFeedEnd.GetInvocationList())
            {
                scrollFeedEnd -= (Action<int, int>)handler;
            }
        }

        if (draggingFeed != null)
        {
            foreach (Delegate handler in draggingFeed.GetInvocationList())
            {
                draggingFeed -= (Action<int, float>)handler;
            }
        }
    }
}
