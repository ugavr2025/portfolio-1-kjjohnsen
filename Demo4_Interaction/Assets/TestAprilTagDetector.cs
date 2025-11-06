using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

public class TestAprilTagDetector : MonoBehaviour
{

	[SerializeField] int _decimation = 2;
	[SerializeField] float _tagSize = 0.05f;
	[SerializeField] Material _tagMaterial = null;

	AprilTag.TagDetector _detector;
	[SerializeField]QuestStereoCamera QuestStereoCamera;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		
	}

	void OnDestroy()
	{
		if(_detector != null)
			_detector.Dispose();
	
	}

	// Update is called once per frame
	void LateUpdate()
    {
		if (_detector == null && QuestStereoCamera.texture1 != null)
		{
			_detector = new AprilTag.TagDetector(QuestStereoCamera.texture1.width, QuestStereoCamera.texture2.height, _decimation);
		}
		if (_detector == null) return; //don't process until we have a detector

		
		_detector.ProcessImage(TextureReadback.AsSpan(QuestStereoCamera.texture1), 70, _tagSize);
		// Detected tag visualization
		foreach (var tag in _detector.DetectedTags)
		{
			Debug.Log(tag.ID);
		}
	}
	
}

static class TextureReadback
{
	// Texture readback as Span with AsyncGPUReadback in a synced fashion
	public unsafe static ReadOnlySpan<Color32> AsSpan(this Texture source)
	{
		var req = AsyncGPUReadback.Request(source);

		req.WaitForCompletion();
		if (req.hasError) return ReadOnlySpan<Color32>.Empty;

		var data = req.GetData<Color32>(0);

		var ptr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
		return new Span<Color32>(ptr, data.Length);
	}
}