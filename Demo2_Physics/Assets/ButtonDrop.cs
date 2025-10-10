using System.Collections;
using UnityEngine;

public class ButtonDrop : MonoBehaviour
{
    public bool triggered = false;
    public Transform platform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	private void OnCollisionEnter(Collision collision)
	{
        if (!triggered)
        {
            triggered = true;
            StartCoroutine(rotatePlatformQuickly());
        }
	}

    public IEnumerator rotatePlatformQuickly()
    {
        while (platform.localEulerAngles.z <90)
        {
            platform.Rotate(0, 0, Time.deltaTime * 500);
            yield return null;
        }
    }
}
