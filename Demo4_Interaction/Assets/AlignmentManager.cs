using AprilTag;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

//this object should 
public class AlignmentManager : MonoBehaviour
{
    public string uuid_origin; //these are  the meta objects that are saved/loaded
    public string uuid_target; 

    public Transform origin; //these are the things that are moving
    public Transform target;
    public Transform rig;

    GameObject anchor_origin;
	GameObject anchor_target;

    public bool locked = false; //when locked, the alignment will happen

    public InputAction triggerFind;
    public InputAction triggerLock;

    public QuestStereoCamera questCamera;
	private TagDetector _detector;

	List<OVRSpatialAnchor.UnboundAnchor> unboundAnchors = new();


	// Start is called once before the first execution of Update after the MonoBehaviour is created
	async void Start()
    {
        triggerFind.Enable();
		triggerLock.Enable();
        _detector = new TagDetector(questCamera.leftPassthrough.RequestedResolution.x, questCamera.leftPassthrough.RequestedResolution.y,decimation:1);

        uuid_origin = PlayerPrefs.GetString("origin", "");
        uuid_target = PlayerPrefs.GetString("target", "");

        //try to localize them
        if(uuid_origin != "" && uuid_target != "")
        {
			var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new List<Guid>() { new Guid(uuid_origin), new Guid(uuid_target) }, unboundAnchors);
            if (result.Success)
            {
				anchor_origin = new GameObject("anchorOrigin");
                anchor_origin.transform.parent = rig;
				anchor_target = new GameObject("anchorTarget");
				anchor_target.transform.parent = rig;
				if (unboundAnchors[0].Uuid.ToString() == uuid_origin) //order is not guaranteed
                {
					unboundAnchors[0].BindTo(anchor_origin.AddComponent<OVRSpatialAnchor>());
					unboundAnchors[1].BindTo(anchor_target.AddComponent<OVRSpatialAnchor>());
                }
                else
                {
					unboundAnchors[1].BindTo(anchor_origin.AddComponent<OVRSpatialAnchor>());
					unboundAnchors[0].BindTo(anchor_target.AddComponent<OVRSpatialAnchor>());
				}


                    locked = true; //we are locked in now.  Must unlock to relocalize
			}
        }
        
    }

    // Update is called once per frame
    void Update()
    {

        if (triggerFind.WasPerformedThisFrame())
        {
            StartCoroutine(FindTagsAndMoveAnchors());
        }
        if (triggerLock.WasPerformedThisFrame())
        {
            if (locked)
            {
                UnlockAnchors();
            }
            else
            {
				StartCoroutine(LockAnchors());
			}
        }

        if (locked && anchor_origin.GetComponent<OVRSpatialAnchor>().Localized && anchor_target.GetComponent<OVRSpatialAnchor>().Localized)
        {
            //the anchors represent points in the real world, despite being in tracked space, hence they can move around (in tracking system space)
            Vector3 o = anchor_origin.transform.localPosition;
            Vector3 t = anchor_target.transform.localPosition;
            t.y = o.y; //zero out the y component (only flat allowed)
            Vector3 anchorDirection = (t - o).normalized; //tracking space anchor rotation
            Vector3 worldSpaceAnchorDirection = rig.TransformDirection(anchorDirection);  //this should be 0,0,1, but it probably won't be
            float angle = Vector3.SignedAngle(worldSpaceAnchorDirection,Vector3.forward,Vector3.up); //how is it offset?
			rig.transform.Rotate(0, angle, 0, Space.World); //counter rotate the rig in world space so it will be
			Vector3 ow = rig.TransformPoint(o); //now we need to do the same for position, but it's easier.  This should be 0,0,0 in world space
			rig.Translate(-ow,Space.World);  //but it won't be, so we counter move the rig so it will be
            origin.position = anchor_origin.transform.position; //this is unecessary, but nice to visualize when locked
            origin.rotation = anchor_origin.transform.rotation;
            target.position = anchor_target.transform.position;
            target.rotation = anchor_target.transform.rotation;
        }

       
    }

    IEnumerator FindTagsAndMoveAnchors()
    {

        //actually grab the texture
        if(questCamera.texture1 == null) { Debug.LogError("No Camera Data"); yield break; }

		var readback = AsyncGPUReadback.Request(questCamera.texture1, 0); //just doing the left camera, but could do both left and right and average for a better or more reliable result
        while (!readback.done)
        {
            yield return null;
        }

		var data = readback.GetData<Color32>(0);
		var fov_y = 2 * Mathf.Atan(questCamera.leftIntrinsics.SensorResolution.y / (2 * questCamera.leftIntrinsics.FocalLength.y));
		_detector.ProcessImage(data, fov_y, .1f);

        var tags = _detector.DetectedTags;
        bool found_origin = false;
        bool found_target = false;
		foreach (var tagPose in tags)
		{
			
			Matrix4x4 leftCameraTransform = rig.localToWorldMatrix * Matrix4x4.TRS(questCamera.leftPassthrough.GetCameraPose().position, questCamera.leftPassthrough.GetCameraPose().rotation, Vector3.one);
            Quaternion leftCameraRotation = questCamera.leftPassthrough.GetCameraPose().rotation;
			if (tagPose.ID == 0)
			{
				origin.position = leftCameraTransform.MultiplyPoint(tagPose.Position);
				origin.rotation = rig.rotation * leftCameraRotation * tagPose.Rotation;
                found_origin = true;
			}

			if (tagPose.ID == 1)
			{
				target.position = leftCameraTransform.MultiplyPoint(tagPose.Position);
				target.rotation = rig.rotation * leftCameraRotation * tagPose.Rotation;
                found_target = true;
			}

		}
        
        if(!found_origin || !found_target) { yield break; }
        StartCoroutine(LockAnchors());
	}

    IEnumerator LockAnchors()
    {
		anchor_origin = new GameObject("anchorOrigin");
		anchor_origin.transform.parent = rig;
		anchor_origin.transform.position = origin.transform.position;
		anchor_origin.transform.rotation = origin.transform.rotation;

		anchor_target = new GameObject("anchorTarget");
		anchor_target.transform.parent = rig;
		anchor_target.transform.position = target.transform.position;
		anchor_target.transform.rotation = target.transform.rotation;

		OVRSpatialAnchor o = anchor_origin.AddComponent<OVRSpatialAnchor>();
		OVRSpatialAnchor t = anchor_target.AddComponent<OVRSpatialAnchor>();
		yield return new WaitUntil(() => o.Created && t.Created);

		uuid_origin = o.Uuid.ToString();
		uuid_target = t.Uuid.ToString();



		var saveTask = OVRSpatialAnchor.SaveAnchorsAsync(new List<OVRSpatialAnchor>() { o, t });
		yield return new WaitUntil(() => saveTask.IsCompleted);

		//save them
		PlayerPrefs.SetString("origin", uuid_origin);
		PlayerPrefs.SetString("target", uuid_target);

		locked = true;
	}

    public void UnlockAnchors()
    {

       
        anchor_origin.GetComponent<OVRSpatialAnchor>()?.EraseAnchorAsync();
		anchor_target.GetComponent<OVRSpatialAnchor>()?.EraseAnchorAsync();
		GameObject.Destroy(anchor_origin);
		GameObject.Destroy(anchor_target);

		locked = false;


    }
}
