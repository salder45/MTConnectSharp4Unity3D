using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Timers;

using MTConnectSharp4Unity3D;

public class ClientControllerv2 : MonoBehaviour {
	/// <summary>
	/// The base uri of the agent
	/// </summary>
	public string AgentUri=Constants.DEFAULT_URL;

	/// <summary>
	/// Time in milliseconds between sample queries when simulating a streaming connection
	/// </summary>
	public Int32 UpdateInterval = Constants.DEFAULT_INTERVAL;


	void Reset(){
		AgentUri = Constants.DEFAULT_URL;
		UpdateInterval = Constants.DEFAULT_INTERVAL;
	}

	// Use this for initialization
	void Start () {
		ProbeCompleted += client_ProbeCompleted;
		DataItemChanged += client_DataItemChanged;
		DataItemsChanged += client_DataItemsChanged;
	}

	void Awake(){
		Probe ();
	}

	// Update is called once per frame
	void Update () {
	
	}

	//Class Stuff
	/// <summary>
	/// The probe response has been recieved and parsed
	/// </summary>
	public event EventHandler ProbeCompleted;

	/// <summary>
	/// All data items in a current or sample response have been parsed
	/// </summary>
	public event EventHandler DataItemsChanged;

	/// <summary>
	/// The value of a data item changed
	/// </summary>
	public event EventHandler<DataItemChangedEventArgs> DataItemChanged;

	/// <summary>
	/// Devices on the connected agent
	/// </summary>
	public Device[] Devices
	{
		get
		{
			return devices.ToArray<Device>();
		}
	}
	private List<Device> devices;

	/// <summary>
	/// Dictionary Reference to all data items by id for better performance when streaming
	/// </summary>
	private Dictionary<String, DataItem> dataItemsRef = new Dictionary<string,DataItem>(); 

	/// <summary>
	/// Last sequence number read from current or sample
	/// </summary>
	private Int64 lastSequence;

	private Boolean probeCompleted = false;
	private Boolean streamingIsRunning=false;

	/// <summary>
	/// Gets probe response from the agent and populates the devices collection
	/// </summary>
	public void Probe()
	{
		Debug.Log ("Probe");
		StartCoroutine (InternalProbe());
	}

	public void StartStreaming()
	{
		UnityEngine.Debug.Log ("StartStreaming");
		if (streamingIsRunning)
		{
			return;
		}

		GetCurrentState();

		streamingIsRunning = true;
		StartCoroutine (wait(UpdateInterval));
	}

	/// <summary>
	/// Stops sample polling
	/// </summary>
	public void StopStreaming()
	{
		UnityEngine.Debug.Log ("StopStreaming");
		streamingIsRunning = false;
	}

	private IEnumerator InternalProbe (){
		UnityEngine.WWW www = new UnityEngine.WWW (getURL(Constants.PROBE_REQUEST));
		yield return www;
		parseProbeResponse (www);
	}

	/// <summary>
	/// Parses IRestResponse from a probe command into a Device collection
	/// </summary>
	/// <param name="response">An IRestResponse from a probe command</param>
	private void parseProbeResponse(UnityEngine.WWW response)
	{
		devices = new List<Device>();			
		XDocument xDoc = XDocument.Load(new StringReader(response.text));
		foreach (var d in xDoc.Descendants().First(d => d.Name.LocalName == "Devices").Elements())
		{
			devices.Add(new Device(d));
		}
		FillDataItemRefList();

		probeCompleted = true;
		ProbeCompletedHandler();			
	}

	/// <summary>
	/// Loads DataItemRefList with all data items from all devices
	/// </summary>
	private void FillDataItemRefList()
	{
		foreach (Device device in devices)
		{
			List<DataItem> dataItems = new List<DataItem>();
			dataItems.AddRange(device.DataItems);
			dataItems.AddRange(GetDataItems(device.Components));
			foreach (var dataItem in dataItems)
			{
				dataItemsRef.Add(dataItem.id, dataItem);
			}
		}
	}

	/// <summary>
	/// Recursive function to get DataItems list from a Component collection
	/// </summary>
	/// <param name="Components">Collection of Components</param>
	/// <returns>Collection of DataItems from passed Component collection</returns>
	private List<DataItem> GetDataItems(MTConnectSharp4Unity3D.Component[] Components)
	{
		var dataItems = new List<DataItem>();

		foreach (var component in Components)
		{
			dataItems.AddRange(component.DataItems);
			if (component.Components.Length > 0)
			{
				dataItems.AddRange(GetDataItems(component.Components));
			}
		}
		return dataItems;
	}

	/// <summary>
	/// Gets current response and updates DataItems
	/// </summary>
	public void GetCurrentState()
	{
		UnityEngine.Debug.Log ("GetCurrentState");
		if (!probeCompleted)
		{
			throw new InvalidOperationException("Cannot get DataItem values. Agent has not been probed yet.");
		}
		StartCoroutine (GetCurrentStateInternal());

	}

	private IEnumerator GetCurrentStateInternal(){
		UnityEngine.WWW www = new UnityEngine.WWW (getURL(Constants.CURRENT_REQUEST));
		yield return www;
		parseStream (www);
	}

	private void streamingTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		StartCoroutine (streamingTimer_ElapsedInternal());

	}

	private IEnumerator streamingTimer_ElapsedInternal(){
		string t = getURL (getURL(Constants.SAMPLE_REQUEST))+""+(lastSequence+1);
		UnityEngine.WWW www = new UnityEngine.WWW (t);
		yield return www;
		parseStream (www);
	}

	/// <summary>
	/// Parses response from a current or sample request, updates changed data items and fires events
	/// </summary>
	/// <param name="response">IRestResponse from the MTConnect request</param>
	private void parseStream(UnityEngine.WWW response)
	{
		String xmlContent = response.text;
		UnityEngine.Debug.Log (response.text);
		using (StringReader sr = new StringReader(xmlContent))
		{
			XDocument xDoc = XDocument.Load(sr);
			lastSequence = Convert.ToInt64(xDoc.Descendants().First(e => e.Name.LocalName == "Header").Attribute("lastSequence").Value);
			if (xDoc.Descendants().Any(e => e.Attributes().Any(a => a.Name.LocalName == "dataItemId")))
			{
				IEnumerable<XElement> xmlDataItems = xDoc.Descendants()
					.Where(e => e.Attributes().Any(a => a.Name.LocalName == "dataItemId"));

				var dataItems = (from e in xmlDataItems
					select new
					{
						id = e.Attribute("dataItemId").Value,
						//timestamp = DateTime.Parse(e.Attribute("timestamp").Value, null, System.Globalization.DateTimeStyles.RoundtripKind),
						timestamp = Convert.ToDateTime(e.Attribute("timestamp").Value), 
						value = e.Value
					}).ToList();
				foreach (var item in dataItems.OrderBy(i => i.timestamp))
				{
					var dataItem = dataItemsRef[item.id];
					dataItem.AddSample(new DataItemSample(item.value.ToString(), item.timestamp));
					DataItemChangedHandler(dataItemsRef[item.id]);
				}
				DataItemsChangedHandler();
			}
		}
	}


	private void ProbeCompletedHandler()
	{
		var args = new EventArgs();
		if (ProbeCompleted != null)
		{
			ProbeCompleted(this, args);
		}
	}

	private void DataItemChangedHandler(DataItem dataItem)
	{
		var args = new DataItemChangedEventArgs(dataItem);
		if (DataItemChanged != null)
		{
			DataItemChanged(this, args);
		}
	}

	private void DataItemsChangedHandler()
	{
		var args = new EventArgs();
		if (DataItemsChanged != null)
		{
			DataItemsChanged(this, args);
		}
	}

	private string getURL(string request){
		string url = AgentUri;

		if(request.Equals(Constants.PROBE_REQUEST)){
			url = url + Constants.SLASH + Constants.PROBE_REQUEST;
		}else if(request.Equals(Constants.CURRENT_REQUEST)){
			url = url + Constants.SLASH + Constants.CURRENT_REQUEST;
		}else if(request.Equals(Constants.SAMPLE_REQUEST)){
			url = url + Constants.SAMPLE_REQUEST + Constants.AT_PART;
		}

		return url;
	}



	//TEST SECTION

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
		StartStreaming ();
	}

	void OnApplicationQuit(){
		if(streamingIsRunning){
			StopStreaming ();
		}
	}

	IEnumerator wait(float miliseconds){
		while(streamingIsRunning){
			StartCoroutine (streamingTimer_ElapsedInternal());
			yield return new WaitForSeconds (miliseconds/1000);
		}
	}
}
