
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class HandController : MonoBehaviour
{
    XRGrabbable grabbedObject;

    List<XRGrabbable> grabbablesInTrigger = new List<XRGrabbable>();
    [SerializeField] AudioClip tapSound;
    [SerializeField] InputAction grabAction;
    [SerializeField] float grabThreshold = .2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grabAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        float grabber = grabAction.ReadValue<float>();
        if (grabber > grabThreshold && grabbedObject == null && grabbablesInTrigger.Count > 0)
        {
            grabbedObject = grabbablesInTrigger[0];
            grabbedObject.Grab(this);
            var renderers = this.GetComponentsInChildren<Renderer>();
            foreach(var r in renderers)
            {
                r.enabled = false;
            }
        }
        if (grabber <= grabThreshold && grabbedObject != null)
        {
            grabbedObject.Release(this);
			var renderers = this.GetComponentsInChildren<Renderer>();
			foreach (var r in renderers)
			{
				r.enabled = true;
			}
			grabbedObject = null;
        }
    }

	private void OnTriggerEnter(Collider other)
	{
        var grabbable = other.attachedRigidbody?.GetComponent<XRGrabbable>();
		if (grabbable != null && !grabbablesInTrigger.Contains(grabbable))
        {
            grabbablesInTrigger.Add(grabbable);
        }
	}

	private void OnTriggerExit(Collider other)
	{
		var grabbable = other.attachedRigidbody?.GetComponent<XRGrabbable>();
		if (grabbable != null && grabbablesInTrigger.Contains(grabbable))
		{
            grabbablesInTrigger.Remove(grabbable);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
        Enemy enemy = collision.collider.attachedRigidbody?.GetComponent<Enemy>();
		if(enemy != null)
		{
           AudioSource.PlayClipAtPoint(tapSound, collision.contacts[0].point);
           StartCoroutine(destroyEnemyOverTime(enemy, 1));
		}
		
		
	}
    IEnumerator destroyEnemyOverTime(Enemy e, float t)
    {
        
        while(t >= 0)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        if (e.gameObject != null)
        {
            Destroy(e.gameObject);
        }

    }
    
}
