using Meta.XR;
using System;
using UnityEngine;
using UnityEngine.Android;
using Uralstech.UXR.QuestCamera;
using VELShareUnity;

public class QuestStereoCamera : MonoBehaviour
{
	public WebRTCReceiver sender;
	public PassthroughCameraAccess leftPassthrough;
	public PassthroughCameraAccess rightPassthrough;
	public PassthroughCameraAccess.CameraIntrinsics leftIntrinsics;
	public PassthroughCameraAccess.CameraIntrinsics rightIntrinsics;

	public Material combineMaterial;
	public Texture texture1;
	public Texture texture2;
	public RenderTexture combinedTexture;
	public Shader combineShader;


	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {

		//leftPassthrough = gameObject.AddComponent<PassthroughCameraAccess>();
		//leftPassthrough.CameraPosition = PassthroughCameraAccess.CameraPositionType.Left;
		//leftPassthrough.RequestedResolution = new Vector2Int(1280, 960);

		//rightPassthrough = gameObject.AddComponent<PassthroughCameraAccess>();
		//rightPassthrough.CameraPosition = PassthroughCameraAccess.CameraPositionType.Right;
		//rightPassthrough.RequestedResolution = new Vector2Int(1280, 960);


		if (combineShader == null)
		{
			Debug.LogError("Could not find the 'Unlit/CombineSideBySide' shader. Make sure it's in your project.");
			enabled = false;
			return;
		}
		if (combineMaterial == null)
		{
			combineMaterial = new Material(combineShader);
		}
		int width = leftPassthrough.RequestedResolution.x;
		int height = leftPassthrough.RequestedResolution.y;
		combinedTexture = new RenderTexture(width * 2/4, height/4, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SRGB); // No depth buffer needed
		//sender.renderTexture = combinedTexture;
		//sender.Startup(sender.streamRoom);
	}

    // Update is called once per frame
    void Update()
    {

		if (leftPassthrough.enabled)
		{
			texture1 = leftPassthrough.GetTexture();
		}
		if(rightPassthrough.enabled)
		{
			texture2 = rightPassthrough.GetTexture();
		}

		// Wait until PassthroughCameraAccess.IsPlaying is true
		if (leftPassthrough.IsPlaying && rightPassthrough.IsPlaying)
		{
			
			// Camera data is available only when IsPlaying is true
			leftIntrinsics = leftPassthrough.Intrinsics;
			rightIntrinsics = rightPassthrough.Intrinsics;
			Pose poseLeft = leftPassthrough.GetCameraPose();
			Pose poseRight = rightPassthrough.GetCameraPose();
			//Debug.Log("position left ="+poseLeft.position);
			//Debug.Log("rotation left ="+poseLeft.rotation);
			//Debug.Log("position right =" + poseRight.position);
			//Debug.Log("rotation right =" + poseRight.rotation);
			
			DateTime timestamp = leftPassthrough.Timestamp;
			//Debug.Log(timestamp);
		}

		// Ensure everything is still valid before blitting
		if (combineMaterial == null || texture1 == null || texture2 == null)
		{
			return;
		}


		// Set the two source textures on our material
		combineMaterial.SetTexture("_MainTex", texture1);
		combineMaterial.SetTexture("_Tex2", texture2);

		// Perform the blit operation
		// The 'null' source means we are just running a fullscreen shader pass
		Graphics.Blit(null, combinedTexture, combineMaterial);
	}

	public Vector3 getWorldPosition(Vector3 cameraPosition, bool left)
	{
		return Vector3.zero;
	}
}
