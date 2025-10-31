using System.IO;
using UnityEngine;
using UnityEngine.XR.OpenXR.Input;
using VelNet;

public class AvatarHand : NetworkComponent
{
    public Transform trackedSkeleton;
    public Transform avatarSkeleton;
    public Renderer handRenderer;
	public override void ReceiveBytes(byte[] message)
	{
		throw new System.NotImplementedException();
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    [VelNetRPC]
    void RPCHandUpdate(byte[] data)
    {
        //called on other clients
        BinaryReader br = new BinaryReader(new MemoryStream(data));
        DeserializeTransformAndChildren(avatarSkeleton, br);


    }
    void DeserializeTransformAndChildren(Transform t, BinaryReader br)
    {
        
        if (t.childCount == 0) return; //tips don't matter
		//t.transform.localPosition = br.ReadVector3();

		byte px = br.ReadByte();
		byte py = br.ReadByte();
		byte pz = br.ReadByte();

		t.transform.localPosition = new Vector3(
			((px / 127.5f) - 1.0f) * .1f,
			((py / 127.5f) - 1.0f) * .1f,
			((pz / 127.5f) - 1.0f) * .1f
		);

		float x = (br.ReadByte() / 127.5f) - 1.0f;
		float y = (br.ReadByte() / 127.5f) - 1.0f;
		float z = (br.ReadByte() / 127.5f) - 1.0f;
        float w2 = 1- (x * x + y * y + z * z);
        float w = Mathf.Sqrt(Mathf.Clamp(w2,0,1));
        t.transform.localRotation = new Quaternion(x, y, z, w);
        //t.transform.localScale = br.ReadVector3();
		for (int i = 0; i < t.childCount; i++)
		{
			Transform c = t.GetChild(i);
			DeserializeTransformAndChildren(c, br);
        }
    }
    void SerializeTransformAndChildren(Transform t, BinaryWriter bw)
    {

        //apply a simple compression scheme to reduce the amount of information sent.  We are only going to send compressed position a compressed rotation, and just 1 byte per componenent
        //this reduces the data from 28 bytes per joint to 6 bytes per joint.  28 joints
        //not all of the joints matter though (e.g. the tips don't matter at all), so 23 joints.  We could further reduce this with a lot of effort...
        //bw.Write(t.localPosition); //we don't absolutely need position, but meta does change it, so we should probably send it.  Position is a bit hard to compress, as you need to know the range.  From what I can tell, all of these values are between -.1 and .1 (most much less)
        if (t.childCount == 0) return; //tips don't matter
		float posX = Mathf.Clamp(t.localPosition.x, -.1f, .1f);
		float posY = Mathf.Clamp(t.localPosition.y, -.1f, .1f);
		float posZ = Mathf.Clamp(t.localPosition.z, -.1f, .1f);

		byte px = (byte)(((posX / .1f) + 1.0f) * 127.5f);
		byte py = (byte)(((posY / .1f) + 1.0f) * 127.5f);
		byte pz = (byte)(((posZ / .1f) + 1.0f) * 127.5f);

		bw.Write(px);
		bw.Write(py);
		bw.Write(pz);

		//bw.Write(t.localRotation); //instead of sending the whole quaternion, we can send just x y, z, ensuring w is positive (we know that x*x+y*y+z*z +w*w = 1, so we don't need to send w
		float w = t.localRotation.w;
        float x = t.localRotation.x;
        float y = t.localRotation.y;
        float z = t.localRotation.z;
        if(w < 0)
        {
            x = -x;
            y = -y;
            z = -z;
        }
        //now we map x,y, and z to integer space
        byte bx = (byte)((x+1)*127.5f);
		byte by = (byte)((y+1)*127.5f);
		byte bz = (byte)((z+1)*127.5f);
        //
        bw.Write(bx);
        bw.Write(by);
        bw.Write(bz); 
        //bw.Write(t.localScale); //scale is always 1,1,1
        for(int i = 0; i < t.childCount; i++) 
        {
            Transform c = t.GetChild(i);
            SerializeTransformAndChildren(c,bw);
        }

    }
    // Update is called once per frame
    void Update()
    {

        if (IsMine)
        {
            if(trackedSkeleton != null)
            {
                //we have to drive our skeleton, or really, just set the data
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                //start by writing out the hand position and rotation
                SerializeTransformAndChildren(trackedSkeleton.transform, bw);
                SendRPC(nameof(RPCHandUpdate), false, ms.ToArray());
                
                avatarSkeleton.gameObject.SetActive(false); //we already see our hand
                handRenderer.enabled = false;
            }

        }
       


        
    }
}
