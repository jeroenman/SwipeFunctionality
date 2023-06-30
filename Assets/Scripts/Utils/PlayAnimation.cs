using UnityEngine;

public class PlayAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip clip;

    void Start()
    {
        animator.Play(clip.name);
    }
}
