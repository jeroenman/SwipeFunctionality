using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Content : MonoBehaviour
{
    [SerializeField] public Camera renderCamera;
    [SerializeField] public GameObject avatar;

    private Animator animator;

    void Start()
    {
        animator = avatar.GetComponent<Animator>();
    }

    public void Play()
    {
        SetSpeed(1);
    }

    public void Stop()
    {
        SetSpeed(0);
    }

    private void SetSpeed(int speed)
    {
        animator.speed = speed;
    }
}
