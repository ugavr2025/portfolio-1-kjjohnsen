using SocketIOClient.Newtonsoft.Json;
using SocketIOClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VelNet.Voice;
using static SocketIOUnity;
using TMPro;
using UnityEngine.UI;
using Concentus.Structs;
using UnityEngine.InputSystem;

public class VelAIManager : MonoBehaviour
{
	public class ChatJSON
	{
		public string prompt = ""; //set this if you don't want to use audio
		public List<string> opusAudioPackets = new List<string>();
		public List<string> images = new List<string>();
		public bool transcribe_only = false; //if true, you will only get a trascription.  Use the transcribe id to tag it so you know which one it's for
		public string transcribe_id = "";
		public string voice_mode = "af_heart"; //for kokoro, male option that's decent is am_fenrir
		public int generation_mode = 1; //1 would be context based, 0 is text only.  If 1, you will get either text, an image, or a model response. 
		public ChatJSON(string prompt) { this.prompt = prompt; }
		public ChatJSON(string prompt, string b64image) { this.prompt = prompt; images[0] = b64image; }
		public ChatJSON(List<string> audioPackets) { this.opusAudioPackets = audioPackets; }
		public ChatJSON(List<string> audioPackets, string b64image) { images.Add(b64image); opusAudioPackets = audioPackets; }

	}

	public class AudioFrameJSON
	{
		public byte[] frame;
		public int frame_number;
		public int total_frames;
	}
	public class ResetContextJSON
	{
		public string system_prompt = "";
		public ResetContextJSON(string prompt)
		{
			system_prompt = prompt;
		}
	}
	public class AIThinkingJSON
	{
		public string prompt;
	}
	public class PartialResponseJSON
	{
		public string text;
		public int chunk_index;
	}

	public class ImageResponseJSON
	{
		public string image_url;
	}
	public class ModelResponseJSON
	{
		public string model_url;
	}

	public class FullResponseJSON
	{
		public string prompt;
		public string final_text;
	}
	public class TranscriptionResponseJSON
	{
		public string transcribe_id;
		public string text;
	}

    VelVoice voice;
    public bool capturing = false;
    List<string> vorbisFrames = new List<string>();

	private SocketIOUnity socketIOClient;
	public AudioSource outputSource;
	public OpusDecoder decoder;
	float[] decoded_data = new float[480];
	List<AudioClip> audioClipsToPlay = new List<AudioClip>();
	AudioClip currentClip;

	//public string AIServer = "https://chat.vn.ugavel.com";
	public string AIServer = "http://localhost:8085";
	public string defaultPrompt = "You are a helpful AI agent";
	public Action<FullResponseJSON> ResponseReceived;
	public Action<PartialResponseJSON> PartialResponseReceived;
	public Action<AIThinkingJSON> ThinkingResponseReceived;
	public Action<ImageResponseJSON> ImageReceived;
	public Action<ModelResponseJSON> ModelReceived;
	public Action<TranscriptionResponseJSON> TranscriptionReceived;
	async Task CleanupSocketIO()
	{
		if (socketIOClient != null && socketIOClient.Connected)
		{
			await socketIOClient.DisconnectAsync();
			socketIOClient = null;
			Debug.Log("[SocketIO] Client cleaned up.");
		}
	}

	async Task<bool> initSocketIOAsync()
	{
		Debug.Log("intializing socket");
		// If a client instance exists and is connected or trying to connect, clean it up first.
		if (socketIOClient != null && (socketIOClient.Connected))
		{
			Debug.LogWarning("[SocketIO] initSocketIO: Client exists and is connected/connecting. Performing cleanup first.");
			await CleanupSocketIO();
		}
		if (socketIOClient == null)
		{
			socketIOClient = new SocketIOUnity(AIServer, new SocketIOOptions
			{
				Query = new Dictionary<string, string>
				{
					{"token", "QUEST" }
				}
				,
				EIO = EngineIO.V4
				,
				Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
			});
			socketIOClient.JsonSerializer = new NewtonsoftJsonSerializer();
			socketIOClient.unityThreadScope = UnityThreadScope.Update;
		}


		// Set (unityThreadScope) the thread scope function where the code should run.
		// Options are: .Update, .LateUpdate or .FixedUpdate, default: UnityThreadScope.Update


		///// reserved socketio events
		socketIOClient.OnConnected += (sender, e) =>
		{
			UnityThread.executeInUpdate(() =>
			{
				Debug.Log("socket.OnConnected");
				clearContext(); //immediately clear the context
			});
			
		};
		socketIOClient.OnDisconnected += (sender, e) =>
		{
			UnityThread.executeInUpdate(() =>
			{
				Debug.Log("socket OnDisconnected: " + e);
			});

		};
		socketIOClient.OnReconnectAttempt += (sender, e) =>
		{
			UnityThread.executeInUpdate(() =>
			{
				Debug.Log($"{DateTime.Now} Reconnecting: attempt = {e}");
			});
		};

		socketIOClient.OnError += (sender, e) =>
		{
			UnityThread.executeInUpdate(() =>
			{
				Debug.LogError(e);
			});
		};
		socketIOClient.On("status", (SocketIOResponse e) => {
			UnityThread.executeInUpdate(() => {

				Debug.Log("Server Status: " + e.ToString());
				// You could display this in a UI element if desired.
			});
		});

		socketIOClient.On("ai_thinking", (SocketIOResponse e) => {
			UnityThread.executeInUpdate(() => {
				var res = e.GetValue<AIThinkingJSON>();
				ThinkingResponseReceived?.Invoke(res);
			});
		});

		socketIOClient.On("ai_partial_response", (SocketIOResponse e) => {
			UnityThread.executeInUpdate(() => {
				var res = e.GetValue<PartialResponseJSON>();
				PartialResponseReceived?.Invoke(res);
			});
		});

		socketIOClient.On("audio_frame", (SocketIOResponse e) =>
		{
			UnityThread.executeInUpdate(() => {
				var res = e.GetValue<AudioFrameJSON>();
				if(res.frame_number == 0)
				{
					
					currentClip = AudioClip.Create("test"+Time.time, res.total_frames*480, 1, 24000,false);
					audioClipsToPlay.Add(currentClip);
				}
				
				var decoder_res = decoder.Decode(res.frame, 0, res.frame.Length, decoded_data, 0, 480);
				Debug.Log(decoder_res);
				if(!currentClip.SetData(decoded_data, res.frame_number * 480))
				{
					Debug.LogError("Bad set data");
				}
				if(res.frame_number == (res.total_frames - 1))
				{
					currentClip.LoadAudioData();
				}

			});
		});

		socketIOClient.On("transcription_response", (SocketIOResponse e) =>
		{
			UnityThread.executeInUpdate(() =>
			{
				var res = e.GetValue<TranscriptionResponseJSON>();
				TranscriptionReceived?.Invoke(res);
			});
		});

		socketIOClient.On("image_response", (SocketIOResponse e) =>
		{
			UnityThread.executeInUpdate(() => {

				var res = e.GetValue<ImageResponseJSON>();

				ImageReceived?.Invoke(res);
			});

		});

		socketIOClient.On("model_response", (SocketIOResponse e) =>
		{
			UnityThread.executeInUpdate(() => {

				var res = e.GetValue<ModelResponseJSON>();

				ModelReceived?.Invoke(res);
			});

		});

		socketIOClient.On("ai_full_response_done", (SocketIOResponse e) => {
			UnityThread.executeInUpdate(() => {
				
				var res = e.GetValue<FullResponseJSON>();
				ResponseReceived?.Invoke(res);
			});
		});

		socketIOClient.On("error_response", (SocketIOResponse e) => {
			UnityThread.executeInUpdate(() => {
				Debug.Log("error response " + e.ToString());
			});
		});
		await socketIOClient.ConnectAsync();
		if (socketIOClient.Connected)
		{
			Debug.Log("socket created and connected");
			return true;
		}
		else
		{
			return false;
		}

	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	IEnumerator Start()
    {

		
		decoder = new OpusDecoder(24000, 1);
        voice = FindAnyObjectByType<VelVoice>();
		if (voice != null)
        {
			Debug.Log("Enabling voice capture");
            voice.encodedFrameAvailable += (frame) =>
            {
				if (!capturing)
                {
                    return;
                }
				byte[] frameCopy = new byte[frame.count];
				Array.Copy(frame.array, frameCopy, frame.count);
                vorbisFrames.Add(Convert.ToBase64String(frameCopy));
            };
        }
		var t = initSocketIOAsync();
		yield return new WaitUntil(() => t.IsCompleted);
        
    }

    // Update is called once per frame
    void Update()
    {

		if (Keyboard.current.sKey.wasPressedThisFrame)
		{
			beginCapture();
		}
		if (Keyboard.current.sKey.wasReleasedThisFrame)
		{
			
			endCapture(false);
			getAIResponse("", "af_heart", 0, false);
		}

		if(!outputSource.isPlaying) 
		{
			if (audioClipsToPlay.Count > 0)
			{
				outputSource.clip = audioClipsToPlay[0];
				audioClipsToPlay.RemoveAt(0);
				outputSource.Play();
			}
		}
    }



	public void clearContext()
	{
		var resetContextMessage = new ResetContextJSON(defaultPrompt);
		socketIOClient.Emit("reset_context", resetContextMessage);
	}

	public void beginCapture()
    {
        if (capturing)
        {
			return;
        }
        vorbisFrames.Clear();
		capturing = true;
	}

	public void endCapture(bool getResponse=true)
	{
		capturing = false;
		Debug.Log("stopping capture");
		if(getResponse && vorbisFrames.Count > 20)
		{
			getAIResponse();
		}
	}

    public void getAIResponse(string prompt="",string voice_mode="",int generation_mode=0,bool transcribe_only=false)
    {
		Debug.Log("Getting response");
		if (!socketIOClient.Connected)
		{
			Debug.LogError("Socket IO client not connected");
			return;
		}

		ChatJSON sendMessage;

		if (prompt == "")
		{
			if (vorbisFrames.Count > 0)
			{
				sendMessage = new ChatJSON(vorbisFrames);
			}
			else
			{
				Debug.LogError("Attempted to send an empty prompt");
				return;
			}
		}
		else
		{
			sendMessage = new ChatJSON(prompt);
		}
		sendMessage.voice_mode = voice_mode;
		sendMessage.generation_mode = generation_mode;
		sendMessage.transcribe_only = transcribe_only; 
		socketIOClient.Emit("chat_message", sendMessage);

	}

	private void OnDestroy()
	{
		socketIOClient.Disconnect();
		socketIOClient = null;
	}

	private void OnApplicationQuit()
	{
		if (socketIOClient != null)
		{
			socketIOClient.Disconnect();
		}
	}


}
