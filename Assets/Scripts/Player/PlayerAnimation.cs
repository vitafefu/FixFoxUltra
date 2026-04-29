using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    private PlayerController controller;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        controller = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (controller == null)
            return;

        animator.SetFloat("Speed", controller.AnimationSpeed);
        animator.SetBool("IsGround", controller.IsGrounded);
        animator.SetFloat("YVelocity", controller.VerticalSpeed);
        animator.SetBool("IsInWater", controller.IsInWater);
        animator.SetBool("WillLandInWater", controller.WillLandInWater);

        spriteRenderer.flipX = !controller.FacingRight;
    }
}