using UnityEngine;
using UnityEngine.Android;
using Uralstech.UXR.QuestCamera;
using VELShareUnity;


public class QuestStereoCamera : MonoBehaviour
{
	public WebRTCReceiver sender;
	CameraDevice deviceLeft;
	CaptureSessionObject<ContinuousCaptureSession> captureSessionLeft;
	CameraDevice deviceRight;
	CaptureSessionObject<ContinuousCaptureSession> captureSessionRight;

	public Material combineMaterial;
	public Texture texture1;
	public Texture texture2;
	public RenderTexture combinedTexture;
	public Shader combineShader;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	async void Start()
    {

		if (!Permission.HasUserAuthorizedPermission(UCameraManager.HeadsetCameraPermission))
			Permission.RequestUserPermission(UCameraManager.HeadsetCameraPermission);
		deviceLeft = UCameraManager.Instance.OpenCamera(UCameraManager.Instance.GetCamera(CameraInfo.CameraEye.Left));
		await deviceLeft.WaitForInitializationAsync();

		deviceRight = UCameraManager.Instance.OpenCamera(UCameraManager.Instance.GetCamera(CameraInfo.CameraEye.Right));
		await deviceRight.WaitForInitializationAsync();

		Resolution r = new Resolution();
		r.width = 640;
		r.height = 480;

		captureSessionLeft = deviceLeft.CreateContinuousCaptureSession(r);
		await captureSessionLeft.CaptureSession.WaitForInitializationAsync();
		texture1 = captureSessionLeft.TextureConverter.FrameRenderTexture;

		captureSessionRight = deviceRight.CreateContinuousCaptureSession(r);
		await captureSessionRight.CaptureSession.WaitForInitializationAsync();
		texture2 = captureSessionRight.TextureConverter.FrameRenderTexture;

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

		// Ensure textures are assigned before proceeding
		if (texture1 == null || texture2 == null)
		{
			Debug.LogError("Please assign both source textures in the Inspector.");
			enabled = false;
			return;
		}

		// Create the destination RenderTexture
		int width = texture1.width;
		int height = texture1.height;
		combinedTexture = new RenderTexture(width * 2, height, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SRGB); // No depth buffer needed
		sender.renderTexture = combinedTexture;
		sender.Startup(sender.streamRoom);
	}

    // Update is called once per frame
    void Update()
    {
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
}
