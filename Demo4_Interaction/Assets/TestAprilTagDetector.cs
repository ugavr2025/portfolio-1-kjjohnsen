using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

// Using the AprilTag namespace to make the type names cleaner
using AprilTag;

public class TestAprilTagDetector : MonoBehaviour
{
	[SerializeField] int _decimation = 4;
	[SerializeField] float _tagSize = 0.05f;
	[SerializeField] Material _tagMaterial = null;
	[SerializeField] QuestStereoCamera questStereoCamera;
	[SerializeField] Transform tag0;
	[SerializeField] Transform tag1;
	[SerializeField] Transform rig;
	private TagDetector _detector;

	// --- Threading & Buffering ---
	private BlockingCollection<Color32[]> _frameQueue = new BlockingCollection<Color32[]>(new ConcurrentQueue<Color32[]>(), 1);
	private ConcurrentBag<Color32[]> _bufferPool = new ConcurrentBag<Color32[]>();
	private int _bufferSize;

	private int _pipelineState = 0; // 0 = Idle, 1 = Busy

	private Task _processingTask;
	private CancellationTokenSource _cts;

	// --- Results (Corrected Type) ---
	private readonly List<TagPose> _detectedTags = new List<TagPose>();
	private readonly List<TagPose> _tagsForVisualization = new List<TagPose>();
	private readonly object _tagLock = new object();
	float fov_y=70;
	
	void Start()
	{
		
		_cts = new CancellationTokenSource();
		_processingTask = Task.Run(() => ProcessingLoop(_cts.Token), _cts.Token);
	}

	void OnDestroy()
	{
		_cts?.Cancel();
		_frameQueue?.CompleteAdding();
		_processingTask?.Wait(2000);
		_cts?.Dispose();
		_frameQueue?.Dispose();
		_detector?.Dispose();
		_bufferPool.Clear();
	}

	void LateUpdate()
	{
		// 1. Initialize detector
		if (_detector == null && questStereoCamera.texture1 != null)
		{
			fov_y = 2 * Mathf.Atan(questStereoCamera.leftIntrinsics.SensorResolution.y / (2 * questStereoCamera.leftIntrinsics.FocalLength.y)); 
			Debug.Log("Initializing AprilTag Detector");
			int width = questStereoCamera.texture1.width;
			int height = questStereoCamera.texture1.height;
			_detector = new TagDetector(width, height, _decimation);
			_bufferSize = width * height;
		}

		if (_detector == null) return;

		// 2. Check pipeline state and request frame
		if (Interlocked.CompareExchange(ref _pipelineState, 1, 0) == 0)
		{
			AsyncGPUReadback.Request(questStereoCamera.texture1, 0, OnReadbackComplete);
		}

		// 3. Visualize results
		VisualizeTags();
	}

	/// <summary>
	/// This callback runs on the Main Thread when the GPU readback is complete.
	/// </summary>
	void OnReadbackComplete(AsyncGPUReadbackRequest req)
	{
		if (req.hasError || _detector == null || _cts.Token.IsCancellationRequested)
		{
			Interlocked.Exchange(ref _pipelineState, 0);
			return;
		}

		var data = req.GetData<Color32>(0);

		if (data.Length != _bufferSize)
		{
			Debug.LogWarning("AprilTag buffer size mismatch. Dropping frame.");
			Interlocked.Exchange(ref _pipelineState, 0);
			return;
		}

		if (!_bufferPool.TryTake(out var buffer))
		{
			buffer = new Color32[_bufferSize];
		}

		data.CopyTo(buffer);

		if (!_frameQueue.TryAdd(buffer))
		{
			_bufferPool.Add(buffer);
			Interlocked.Exchange(ref _pipelineState, 0);
		}
	}

	/// <summary>
	/// This runs entirely on a background thread.
	/// </summary>
	void ProcessingLoop(CancellationToken token)
	{
		// --- Corrected Type ---
		var threadLocalTagList = new List<TagPose>();

		try
		{
			while (!token.IsCancellationRequested)
			{
				Color32[] buffer = _frameQueue.Take(token);

				try
				{
					var span = new ReadOnlySpan<Color32>(buffer);
					_detector.ProcessImage(span, fov_y, _tagSize);

					threadLocalTagList.Clear();
					threadLocalTagList.AddRange(_detector.DetectedTags); // This is the list of TagPose

					lock (_tagLock)
					{
						_detectedTags.Clear();
						_detectedTags.AddRange(threadLocalTagList);
					}
				}
				finally
				{
					_bufferPool.Add(buffer);
					Interlocked.Exchange(ref _pipelineState, 0);
				}
			}
		}
		catch (OperationCanceledException)
		{
			Debug.Log("Processing thread shutting down.");
		}
		catch (InvalidOperationException)
		{
			Debug.Log("Processing queue has been completed.");
		}
	}

	/// <summary>
	/// This runs on the Main Thread every frame.
	/// </summary>
	void VisualizeTags()
	{
		lock (_tagLock)
		{
			_tagsForVisualization.Clear();
			_tagsForVisualization.AddRange(_detectedTags);
		}

		// Now _tagsForVisualization contains TagPose objects.
		foreach (var tagPose in _tagsForVisualization)
		{
			// Assuming TagPose has an 'ID' property, as 'Tag' does.
			Debug.Log($"[Main Thread] Detected Tag ID: {tagPose.ID} at Pos: {tagPose.Position} and Rot: {tagPose.Rotation}");
			
			Matrix4x4 leftCameraTransform = rig.localToWorldMatrix*Matrix4x4.TRS(questStereoCamera.leftPassthrough.GetCameraPose().position, questStereoCamera.leftPassthrough.GetCameraPose().rotation, Vector3.one);
			if (tagPose.ID == 1)
			{
				tag1.position = leftCameraTransform.MultiplyPoint(tagPose.Position);
				tag1.rotation = rig.rotation*questStereoCamera.leftPassthrough.GetCameraPose().rotation * tagPose.Rotation;
			}
			if (tagPose.ID == 0)
			{
				tag0.position = leftCameraTransform.MultiplyPoint(tagPose.Position);
				tag0.rotation = rig.rotation*questStereoCamera.leftPassthrough.GetCameraPose().rotation * tagPose.Rotation;
			}

		}
	}
}