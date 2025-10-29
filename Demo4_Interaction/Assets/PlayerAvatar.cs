using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.ARFoundation;
using VelNet;
using Meta.XR.Audio;

public class PlayerAvatar : NetworkComponent,IPackState
{

	public string playerName = "noname";
	public Transform rpmPlayerBody;
	public Transform hmd; //will be null on non-owners
	public Transform leftHand;
	public Transform rightHand;
	public Transform leftHandSkeleton;
	public Transform rightHandSkeleton;

	public Transform myHead;
	public AvatarHand myLeftHand;
	public AvatarHand myRightHand;
	private bool fixAvatar = true;
	public AudioSource audioSource;
	public byte[] PackState()
	{
		var ms = new MemoryStream();
		var bw = new BinaryWriter(ms);
		bw.Write(playerName);
		return ms.ToArray();
	}
	public void UnpackState(byte[] state)
	{
		var ms = new MemoryStream(state);
		var br = new BinaryReader(ms);
		playerName = br.ReadString();
	}

	public override void ReceiveBytes(byte[] message)
	{
		
	}
	

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		if (IsMine)
		{
			myHead.position = hmd.position;
			myHead.rotation = hmd.rotation;
			myLeftHand.transform.position = leftHand.position;
			myLeftHand.transform.rotation  = leftHand.rotation;
			myRightHand.transform.position= rightHand.position;
			myRightHand.transform.rotation= rightHand.rotation;
			myLeftHand.trackedSkeleton = leftHandSkeleton;
			myRightHand.trackedSkeleton = rightHandSkeleton;
		}

		//update the avatar

		var rpmHead = rpmPlayerBody.Find("AvatarRoot/Hips/Spine/Neck/Head");
		if (rpmHead == null) return;

		if (fixAvatar)
		{
			fixAvatar = false;
			Destroy(rpmPlayerBody.GetComponent<Animation>());
			//foreach (MeshRenderer mr in rpmPlayerBody.GetComponentsInChildren<MeshRenderer>())
			//{

			//	Material m = mr.material;
			//	mr.material = occlusionMat;
			//	mr.material.mainTexture = m.mainTexture;
			//	mr.material.color = m.color;

			//}

			//add ovr lip sync on the audio source for the player
			var olsc = audioSource.gameObject.AddComponent<OVRLipSyncContext>();
			olsc.audioSource = audioSource;
			olsc.audioLoopback = true;
			var morpher = audioSource.gameObject.AddComponent<OVRLipSyncContextMorphTarget>();
			morpher.skinnedMeshRenderer = rpmPlayerBody.GetComponentInChildren<SkinnedMeshRenderer>();
			morpher.visemeToBlendTargets = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

		}
		var rpmBody = rpmPlayerBody.Find("AvatarRoot");
		var rpmLH = rpmPlayerBody.Find("AvatarRoot/Hips/Spine/LeftHand");
		var rpmRH = rpmPlayerBody.Find("AvatarRoot/Hips/Spine/RightHand");

		//step 1, move the head and rotate it to align the eyes with the hmd
		rpmHead.transform.rotation = myHead.rotation;
		Vector3 headPosition = myHead.transform.position - myHead.transform.up * 0.08040534f - myHead.transform.forward * 0.05694653f;

		rpmBody.transform.position = headPosition - new Vector3(0, 0.5640153f, 0);
		//rpmNeck.transform.rotation = head.transform.rotation;
		Vector3 headForward = myHead.forward;
		headForward.y = 0;
		headForward.Normalize();
		rpmBody.forward = headForward;
		rpmHead.transform.position = headPosition;
		rpmHead.transform.rotation = myHead.rotation;

		rpmLH.transform.localScale = Vector3.zero; //zero out the hands...we don't need them
		rpmRH.transform.localScale = Vector3.zero;
		//rpmLH.transform.position = myLeftHand.transform.position;
		//rpmLH.transform.rotation  = myLeftHand.transform.rotation;
		//rpmRH.transform.position = myRightHand.transform.position;
		//rpmRH.transform.rotation = myRightHand.transform.rotation;

	}
}
