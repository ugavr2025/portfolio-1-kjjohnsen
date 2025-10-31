using UnityEngine;
using VelNet;

[RequireComponent(typeof(XRGrabbable))]
public class NetworkGrabbable : MonoBehaviour
{
    XRGrabbable grabbable;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        grabbable = GetComponent<XRGrabbable>();
        grabbable.grabbed += () => { grabbable.GetComponent<NetworkObject>().TakeOwnership(); };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
