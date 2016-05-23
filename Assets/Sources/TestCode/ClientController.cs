using UnityEngine;
using System;
using System.Collections;
using MTConnectSharp4Unity3D;

public class ClientController : MonoBehaviour {
	public string url=Constants.DEFAULT_URL;
	public Int32 interval = Constants.DEFAULT_INTERVAL;

	private MTConnectClient client;

	void Reset(){
		url = Constants.DEFAULT_URL;
		interval = Constants.DEFAULT_INTERVAL;
	}

	// Use this for initialization
	void Start () {
		InitClient ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	void InitClient(){
		client = new MTConnectClient (url,this);
		Debug.Log ("URL: "+client.AgentUri);
		Debug.Log ("Interval : "+client.UpdateInterval);
		client.ProbeCompleted += client_ProbeCompleted;
		client.DataItemChanged += client_DataItemChanged;
		client.DataItemsChanged += client_DataItemsChanged;
	}

	void client_DataItemsChanged(object sender, EventArgs e)
	{
		Debug.Log ("client_DataItemsChanged");
	}

	void client_DataItemChanged(object sender, DataItemChangedEventArgs e)
	{
		Debug.Log ("client_DataItemChanged");
		Debug.Log ("-> "+e.DataItem+" -> "+e.DataItem.CurrentSample);
	}

	void client_ProbeCompleted(object sender, EventArgs e)
	{
		Debug.Log ("client_ProbeCompleted");
		client.StartStreaming ();
	}


	void OnApplicationQuit(){
		if(client!=null){
			client.StopStreaming ();
		}
	}
}
