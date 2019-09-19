// script for setting an adaptive frustum to a camera in Unity
// this script needs Osc.cs and UDPPacketIO.cs to work

using UnityEngine;
using System.Collections;

 
public class OSCReceiveCameraAdaptive : MonoBehaviour {
	public string RemoteIP = "127.0.0.1"; //127.0.0.1 signifies a local host (if testing locally
	public int SendToPort = 0; //the port you will be sending from
	public int ListenerPort = 0; //the port you will be listening on
	public GameObject object_to_look; // object that the camera will look at

	private Osc handler;
	//VARIABLES YOU WANT TO BE ANIMATED
	private Vector3 Cyclop_eye_pos;
	private Matrix4x4 my_projection_matrix;
	private float near;
	private float far; 

	Matrix4x4  PerspectiveOffCenter(
		float left, float right,
		float bottom, float top,
		float near, float far)
	{        
		float x =  (2.0f * near) / (right - left);
		float y =  (2.0f * near) / (top - bottom);
		float a =  (right + left) / (right - left);
		float b =  (top + bottom) / (top - bottom);
		float c= -(far + near) / (far - near);
		float d= -(2.0f * far * near) / (far - near);
		float e = -1.0f;
		
		Matrix4x4 m = new Matrix4x4();
		m[0,0] = x;  m[0,1] = 0;  m[0,2] = a;  m[0,3] = 0;
		m[1,0] = 0;  m[1,1] = y;  m[1,2] = b;  m[1,3] = 0;
		m[2,0] = 0;  m[2,1] = 0;  m[2,2] = c;  m[2,3] = d;
		m[3,0] = 0;  m[3,1] = 0;  m[3,2] = e;  m[3,3] = 0;
		return m;
	}

	void SetAssymetricFrustum () {

		// what does this line do ?

		transform.position = Cyclop_eye_pos;
		
		// compute frustum
		// refer to gen-perspective.pdf
		float minx = object_to_look.GetComponent<Renderer>().bounds.min.x;
		float miny = object_to_look.GetComponent<Renderer>().bounds.min.y;
		float maxx = object_to_look.GetComponent<Renderer>().bounds.max.x;
		float maxy = object_to_look.GetComponent<Renderer>().bounds.max.y;
		
		Vector3 screenleftdownpos = new Vector3(minx,miny,0.0f);
		Vector3 screenrightdownpos = new Vector3(maxx,miny, 0.0f);
		Vector3 screenleftuppos = new Vector3 (minx, maxy, 0.0f);
		Vector3 eye  = transform.position;
		
		
		Vector3 pa = screenleftdownpos;
		Vector3 pb = screenrightdownpos;
		Vector3 pc = screenleftuppos;
		Vector3	pe = eye;
		
		float near = GetComponent<Camera>().nearClipPlane;
		float far = GetComponent<Camera>().farClipPlane;
		
		
		Vector3 vr = pb - pa;
		
		Vector3 vu = pc - pa;
		
		vr.Normalize ();
		
		vu.Normalize ();
		
		Vector3 vn = Vector3.Cross(vr, vu);
		
		vn.Normalize();
		
		Vector3 va = pa - pe;
		
		Vector3 vb = pb - pe;
		
		Vector3 vc = pc - pe;
		
		float d = Vector3.Dot (va, vn);
		
		// here the frustum matrix is computed
		float left = Vector3.Dot (vr, va) * near / d;
		float right = Vector3.Dot (vr, vb) * near / d;
		float bottom = Vector3.Dot (vu, va) * near / d;
		float top = Vector3.Dot (vu, vc) * near / d;
		
		
		// equivalent of glFrustum
		Matrix4x4 m = PerspectiveOffCenter(left, right, bottom, top,
		                                   near, far);
		
		Matrix4x4 M = new Matrix4x4 ();
		
		M[0,0] = vr[0];  M[0,1] = vu[0];  M[0,2] = vn[0];  M[0,3] = 0;
		M[1,0] = vr[1];  M[1,1] = vu[1];  M[1,2] = vn[1];  M[1,3] = 0;
		M[2,0] = vr[2];  M[2,1] = vu[2];  M[2,2] = vn[2];  M[2,3] = 0;
		M[3,0] = 0;  M[3,1] = 0;  M[3,2] = 0;  M[3,3] = 1.0f;
		
		GetComponent<Camera>().projectionMatrix = M*m;

	}

	void Set_Position(float posX,  float posY,  float posZ) 
	{
        // why is Z axis reversed ?
		Cyclop_eye_pos.Set(posX, posY, -posZ);
	}

	//These functions are called when messages are received
	//Access values via: oscMessage.Values[0], oscMessage.Values[1], etc
	void AllMessageHandler(OscMessage oscMessage){
		string msgString = Osc.OscMessageToString(oscMessage); //the message and value combined
		string msgAddress = oscMessage.Address; //the message parameters
		
		float posX = (float)oscMessage.Values[0];
		float posY = (float)oscMessage.Values[1]; 
		float posZ = (float)oscMessage.Values[2]; 
		Debug.Log(msgString); //log the message and values coming from OSC
		//FUNCTIONS YOU WANT CALLED WHEN A SPECIFIC MESSAGE IS RECEIVED
		Debug.Log("message tag: "+msgAddress);
		switch (msgAddress){
		case "/tracker/head/pos_xyz/cyclope_eye":
			Debug.Log("eye position received: "+posZ);
			Set_Position(posX, posY, posZ);
			break;
		default:
			break;
		}
	}

	// Use this for initialization
	void Start () {
        // init OSC packet receiver
		UDPPacketIO udp = GetComponent<UDPPacketIO>();
		udp.init(RemoteIP, SendToPort, ListenerPort);
		handler = GetComponent<Osc>();
		handler.init(udp); 
		handler.SetAllMessageHandler(AllMessageHandler);

        // init camera
		near = 0.2f;
		far = 300f;

		GetComponent<Camera>().nearClipPlane = near;
		GetComponent<Camera>().farClipPlane = far;
	}
	
	// Update is called once per frame
	void LateUpdate () {
// something should be done here to perform assymetric frustum rendering
		SetAssymetricFrustum();
	}

}
