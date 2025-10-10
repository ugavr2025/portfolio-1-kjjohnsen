using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class XRGrabbable : MonoBehaviour
{

    [SerializeField] bool useGoGo = true;
    [SerializeField] float goGoThreshold = 0.7f; 
    GameObject grabbedOffsetGO; // this is only used by method 1 (unity way)
	Matrix4x4 grabbedOffset; //this is only used by method 2 (math way)
	public HandController grabbedBy;
	public Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = Mathf.Infinity;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	private void FixedUpdate()
	{
		if(grabbedBy != null)
        {


            //Matrix4x4 target = grabbedBy.transform.localToWorldMatrix * grabbedOffset;
            //Vector3 targetPos = new Vector3(target.m03,target.m13, target.m23);
            //Vector3 targetForward = new Vector3(target.m02, target.m12, target.m22).normalized;
            //Vector3 targetUp = new Vector3(target.m01, target.m11, target.m21).normalized;
            //Quaternion targetRot = Quaternion.LookRotation(targetForward, targetUp);

            Vector3 targetPos = grabbedOffsetGO.transform.position;
            Quaternion targetRot = grabbedOffsetGO.transform.rotation; 

            Vector3 toHand = targetPos - this.transform.position;
            rb.linearVelocity = toHand / Time.fixedDeltaTime;

            Quaternion toHandRot = targetRot * Quaternion.Inverse(this.transform.rotation);
            Vector3 axis;
            float angle;
            toHandRot.ToAngleAxis(out angle, out axis);

            rb.angularVelocity = angle * Mathf.Deg2Rad* axis / Time.fixedDeltaTime;
            
        }
	}

	public void Grab(HandController hand)
    {
        grabbedBy = hand;
        
        grabbedOffsetGO = new GameObject(this.name + "_grabbed"); //method 1 way
        grabbedOffsetGO.transform.position = this.transform.position;
        grabbedOffsetGO.transform.rotation = this.transform.rotation;
        grabbedOffsetGO.transform.SetParent(hand.transform, true);

		grabbedOffset = hand.transform.worldToLocalMatrix * this.transform.localToWorldMatrix; //method 2 way
        //T_h,o = T_h,w*T_w,o
	}
	public void Release(HandController hand)
    {
        if(grabbedBy != hand)
        {
            return;
        }
        grabbedBy = null;
        if(grabbedOffsetGO != null)
        {
            Destroy(grabbedOffsetGO);
        }
    }
}
