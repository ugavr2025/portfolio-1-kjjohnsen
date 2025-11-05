using GLTFast;
using UnityEngine;
using UnityEngine.InputSystem;
using VelNet;
using static VelAIManager;

public class GameManager : MonoBehaviour
{
    [SerializeField] CubePuzzleGenerator generator;
    public PlayerAvatar myAvatar;
    public Transform hmd;
    public Transform leftHand;
    public Transform rightHand;
    public Transform leftHandSkeleton;
    public Transform rightHandSkeleton;
    public VelAIManager aiManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        VelNetManager.OnLoggedIn += () => {
            VelNetManager.JoinRoom("default");
        };

        VelNetManager.OnJoinedRoom += (r) => {

            myAvatar = VelNetManager.NetworkInstantiate("PlayerAvatar", (no) => {
                no.GetComponent<PlayerAvatar>().playerName = VelNetManager.LocalPlayer.userid + "";
            }).GetComponent<PlayerAvatar>();
            myAvatar.hmd = hmd;
            myAvatar.leftHand = leftHand;
            myAvatar.rightHand = rightHand;
            myAvatar.leftHandSkeleton = leftHandSkeleton;
            myAvatar.rightHandSkeleton = rightHandSkeleton;
        };

        aiManager.ModelReceived += (ModelResponseJSON json) => {
            Debug.Log(json.model_url);
            string full_url = aiManager.AIServer + json.model_url;
            GameObject model = new GameObject();
            GltfAsset asset = model.AddComponent<GltfAsset>();
            asset.Url = full_url;
        };
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            aiManager.beginCapture();

        }
        if (Keyboard.current.wKey.wasReleasedThisFrame)
        {
            aiManager.endCapture(false);
            aiManager.getAIResponse("", "", 1, false);
        }
        
    }
    public void StartGame()
    {
        generator.GeneratePuzzle();
    }
}
