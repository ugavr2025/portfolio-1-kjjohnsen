
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class HandController : MonoBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] Transform controller; 
	[SerializeField] OVRHand hand;
	[SerializeField] Transform rig;



	[SerializeField] bool useGoGo = true;
	[SerializeField] float goGoThreshold;
    [SerializeField] float goGoScale;

	XRGrabbable grabbedObject;
    Dictionary<XRGrabbable,List<Collider>> grabbablesInTrigger = new(); //this let's me have a list of XR grabbables that I can find based on a collider quickly
   
    [SerializeField] InputAction grabAction;
    [SerializeField] InputAction handVelocity;
    [SerializeField] InputAction handAngularVelocity;
    [SerializeField] InputAction vibration;
    [SerializeField] InputAction thumbstick;
	[SerializeField] float grabThreshold = .2f;

    [SerializeField] bool useTeleport = true;
    [SerializeField] float stickDeadZone = .5f;
    [SerializeField] float snapDegrees = 15;
    private bool teleportingActive = false;
    private bool snapActive = false;
    
    private Vector3 teleportingTarget;
    private bool teleportingValid;

    [SerializeField] bool useAirGrab = true;
    private bool isAirGrabbing = false;
    private Vector3 airGrabStartPositionWorld; //this is the basis for moving

    [SerializeField] GameObject teleporterArcPrefab;
	[SerializeField] float teleporterDt = .1f; //time between ray casts, effectively teleporter length
    GameObject[] arcPieces = new GameObject[50]; //how many ray casts, also teleporter length
    [SerializeField] float teleporterGravity = -1f; //lower is more curved
	void Start()
    {
        grabAction.Enable();
        handVelocity.Enable();
        handAngularVelocity.Enable();
        vibration.Enable();
        thumbstick.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        bool handTracked = hand.IsTracked && hand.IsPointerPoseValid;
        hand.GetComponentInChildren<SkinnedMeshRenderer>().enabled = handTracked;


		if (useTeleport)
        {
            //handle snap rotation & teleportation
            var stick = thumbstick.ReadValue<Vector2>();

            if (Mathf.Abs(stick.x) < stickDeadZone*.9f && snapActive)
            {
                snapActive = false;
            }

			if (Mathf.Abs(stick.x) > stickDeadZone && !snapActive)
            {
                snapActive = true;
                //save our foot world position
                var footWorld = projectToRigFloor(head.position); //see helper function below
                rig.Rotate(0, Mathf.Sign(stick.x) * snapDegrees, 0, Space.World); //head word position will move
                var footWorldNew = projectToRigFloor(head.position);
                rig.Translate(footWorld - footWorldNew, Space.World); //move it back so that the head didn't move!

			}
            if (stick.y < stickDeadZone*.9f && teleportingActive)
            {
                teleportingActive = false;
				foreach (var part in arcPieces)
				{
					if (part != null) { part.SetActive(false); } //start all as invisible
				}
				//we want to do the teleport now, if the location is valid
				if (teleportingValid)
                {
                    rig.Translate(teleportingTarget - projectToRigFloor(head.transform.position), Space.World);
                }
            }
            if (stick.y > stickDeadZone && !teleportingActive)
            {
                teleportingActive = true;
                //we want to start the teleporting                
            }
			
			if (teleportingActive)
            {
                
                //we simulate projectile motion shooting out of the controller, so we need position, velocity, and acceleration
                var p = transform.position;
                var v = transform.forward; 
                var a = new Vector3(0, teleporterGravity, 0); 
                
				teleportingValid = false; //assume it's not a valid position
                
                for (var i = 0; i < arcPieces.Length; i++) 
                {
                    if(arcPieces[i] == null) //create them if they do not exist
                    {
                        arcPieces[i] = Instantiate<GameObject>(teleporterArcPrefab);

                    }
                    else //activate them, because they may be inactive
                    {
                        arcPieces[i].SetActive(true);
                    }
                    arcPieces[i].transform.position = p; //move this piece into position
                    var p_next = p + v * teleporterDt; //compute the next projectile position
                    var r = p_next - p; //this is to vector to check
                    arcPieces[i].transform.forward = r.normalized; //set the piece to face it (make the arc look curved)
					arcPieces[i].transform.localScale = new Vector3(1, 1, r.magnitude); //and scale it so it doesn't go past the point
					var hits = Physics.RaycastAll(p, r.normalized, r.magnitude); //actually do the raycast from the last position
                    
					if (hits.Length > 0) //we got a hit!
                    {
                        if (hits[0].normal.y > .7f) //roughly vertical
                        {
                            teleportingValid = true; //we should be able to teleport now
                            teleportingTarget = hits[0].point; //this is where we will teleport
                            arcPieces[i].transform.localScale = new Vector3(1, 1, hits[0].distance); //we probably overshot the visualization a bit, so move it back

                            //set the rest of the teleporter arc inactive
                            for (var j = i + 1; j < arcPieces.Length; j++)
                            {
                                if (arcPieces[j] != null)
                                {
                                    arcPieces[j].SetActive(false);
                                }
                            }
                            break;
                        }
                    }
                    
                    v += a * teleporterDt; //compute the new velocity
                    p = p_next; //and update the position
                }

            }
        }

        if (useGoGo)
        {
            Vector3 headToController = controller.position - head.position;
            float d = headToController.magnitude;
            if (d >= goGoThreshold)
            {
               
                float e = (d - goGoThreshold); //the extra amount to move
                transform.position = head.position + headToController.normalized * (d + goGoScale*e*e); //move it 
            }
            else
            {
                transform.localPosition = Vector3.zero; //no offset from the controller
            }
        }

		float grabber = grabAction.ReadValue<float>();

        if (handTracked)
        {
            grabber = hand.GetFingerIsPinching(OVRHand.HandFinger.Index) ? 1 : 0;
        }

        
		if (!isAirGrabbing && grabber > grabThreshold && grabbedObject == null && grabbablesInTrigger.Count > 0)
		{
            grabbedObject = grabbablesInTrigger.FirstOrDefault().Key;
            //grabbedObject.GetComponent<NetworkObject>()?.TakeOwnership();
			grabbedObject.Grab(this);
			
			var renderers = this.GetComponentsInChildren<Renderer>();
			foreach (var r in renderers)
			{
				r.enabled = false;
			}
		}
		if (grabber <= grabThreshold*.9f && grabbedObject != null)
		{
			grabbedObject.Release(this, rig.TransformDirection(handVelocity.ReadValue<Vector3>()), rig.TransformDirection(handAngularVelocity.ReadValue<Vector3>()));
			var renderers = this.GetComponentsInChildren<Renderer>();
			foreach (var r in renderers)
			{
				r.enabled = true;
			}
			grabbedObject = null;
		}
        if (useAirGrab)
        {
            if (grabber > grabThreshold && grabbedObject == null && !isAirGrabbing) //conditions to grip.  Only grip if we have no grabbed object
            {
                isAirGrabbing = true;
                airGrabStartPositionWorld = transform.position;
            }
            if (grabber < grabThreshold*.9f && isAirGrabbing)
            {
                isAirGrabbing = false; //stop, maybe fling yourself
            }
            if (isAirGrabbing)
            {
                Vector3 handMovement = transform.position - airGrabStartPositionWorld;
                rig.transform.position = rig.transform.position - handMovement; //move the rig to bring the hand back into position
            }
        }

	}

	public void rumble(float magnitude, float duration)
	{
		if (vibration.controls.Count > 0)
		{
			//this may be the most generic way to rumble
			(vibration.controls[0].device as XRControllerWithRumble)?.SendImpulse(magnitude, duration);

			//this does not work (vibrates both for some reason)
			//OpenXRInput.SendHapticImpulse(vibration, 1, .01f); 
		}
	}

	Vector3 projectToRigFloor(Vector3 worldPos)
	{
		var rigLocal = rig.InverseTransformPoint(worldPos);
		rigLocal.y = 0;
		return rig.TransformPoint(rigLocal);

	}

	private void OnTriggerEnter(Collider other)
	{
        var g = other.attachedRigidbody?.GetComponent<XRGrabbable>();
		if (g!= null)
		{
            if (!grabbablesInTrigger.ContainsKey(g))
            {
                grabbablesInTrigger[g] = new List<Collider> {};
				g.OnHoverEnter(this);
			}
            if (!grabbablesInTrigger[g].Contains(other))
            {
                grabbablesInTrigger [g].Add(other);
            }
		}
		
		
        
	}

	private void OnTriggerExit(Collider other)
	{
		var g = other.attachedRigidbody?.GetComponent<XRGrabbable>();
		if (g != null)
		{
			if (grabbablesInTrigger.ContainsKey(g))
			{
                grabbablesInTrigger[g].Remove(other);
			}
			if (grabbablesInTrigger[g].Count == 0)
			{
                g.OnHoverExit(this);
                grabbablesInTrigger.Remove(g); 
			}
		}
		
	}


	

}
