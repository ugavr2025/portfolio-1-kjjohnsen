using UnityEngine;

public class YBotIKController : MonoBehaviour
{
    Animator animator;
    public Transform rightHandFollow;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	private void OnAnimatorIK(int layerIndex)
	{
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandFollow.position);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
        animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandFollow.rotation);
	}
}
