using System.Collections;
using UnityEngine;

public class KyleController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Animator animator;
	[SerializeField]Transform headBone;
	PlayerController closePlayer;
	IEnumerator Start()
    {
        animator = GetComponent<Animator>();
        yield return new WaitForSeconds(5);
        animator.SetTrigger("triggerCombo");

    }

    // Update is called once per frame
    void Update()
    {
        
    }

	private void LateUpdate()
	{
		Collider[] colliders = Physics.OverlapSphere(this.transform.position, 3, Physics.AllLayers, QueryTriggerInteraction.Collide);
		closePlayer = null;
		foreach (Collider collider in colliders)
		{
			if (collider.attachedRigidbody?.GetComponent<PlayerController>() != null)
			{
				closePlayer = collider.attachedRigidbody?.GetComponent<PlayerController>();
				break;
			}
		}

		if (closePlayer != null)
		{
			Vector3 between = closePlayer.Head.position - headBone.position;
			headBone.forward = between.normalized;

		}
	}
}
