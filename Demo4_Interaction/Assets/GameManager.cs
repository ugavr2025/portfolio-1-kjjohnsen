using UnityEngine;
using VelNet;

public class GameManager : MonoBehaviour
{
    [SerializeField] CubePuzzleGenerator generator;
    public PlayerAvatar myAvatar;
    public Transform hmd;
    public Transform leftHand;
    public Transform rightHand;
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
        };
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void StartGame()
    {
        generator.GeneratePuzzle();
    }
}
