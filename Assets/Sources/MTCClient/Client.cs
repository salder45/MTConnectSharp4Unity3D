using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Timers;
using System.Runtime.InteropServices;

namespace MTConnectSharp4Unity3D
{
	/// <summary>
	/// Connects to a single agent and streams data from it.
	/// </summary>
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	[ComSourceInterfaces(typeof(IClientEvents))]
    public class MTConnectClient : IMTConnectClient, IDisposable
    {
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
		/// The base uri of the agent
		/// </summary>
		public string AgentUri { get; set; }

		/// <summary>
		/// Time in milliseconds between sample queries when simulating a streaming connection
		/// </summary>
		public Int32 UpdateInterval { get; set; }

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
		/// Not actually parsing multipart stream - this timer fires sample queries to simulate streaming
		/// </summary>
		private Timer streamingTimer;

		/// <summary>
		/// Last sequence number read from current or sample
		/// </summary>
		private Int64 lastSequence;

		private Boolean probeCompleted = false;

		private UnityEngine.MonoBehaviour monoBehaviour;

		/// <summary>
		/// Initializes a new Client 
		/// </summary>
		public MTConnectClient()
		{
			UpdateInterval = 2000;
		}

		/// <summary>
		/// Initializes a new Client and connects to the agent
		/// </summary>
		/// <param name="agentUri">The base uri of the agent</param>
		public MTConnectClient(String agentUri) : this()
		{
			AgentUri = agentUri;
			//Probe();
		}

		public MTConnectClient(String agentUri,UnityEngine.MonoBehaviour mono):this(agentUri){
			monoBehaviour = mono;

			Probe ();
		}

		/// <summary>
		/// Starts sample polling and updating DataItem values as they change
		/// </summary>
		public void StartStreaming()
		{
			UnityEngine.Debug.Log ("StartStreaming");
			if (streamingTimer != null && streamingTimer.Enabled)
			{
				return;
			}

			GetCurrentState();

			streamingTimer = new Timer(UpdateInterval);
			streamingTimer.Elapsed += streamingTimer_Elapsed;
			streamingTimer.Start();
		}

		/// <summary>
		/// Stops sample polling
		/// </summary>
		public void StopStreaming()
		{
			UnityEngine.Debug.Log ("StopStreaming");
			streamingTimer.Stop();
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
			monoBehaviour.StartCoroutine (GetCurrentStateInternal());
			/*
			var request = new RestRequest();
			request.Resource = "current";
			restClient.ExecuteAsync(request, (a) => parseStream(a));
			*/
		}

		private IEnumerator GetCurrentStateInternal(){
			UnityEngine.WWW www = new UnityEngine.WWW (getURL(Constants.CURRENT_REQUEST));
			yield return www;
			parseStream (www);
		}

		/// <summary>
		/// Gets probe response from the agent and populates the devices collection
		/// </summary>
		public void Probe()
		{
			monoBehaviour.StartCoroutine (InternalProbe());
			/*
			restClient = new RestClient();
			restClient.BaseUrl = new Uri(AgentUri);

			var request = new RestRequest();
			request.Resource = "probe";

			try
			{
				restClient.ExecuteAsync(request, (r) => parseProbeResponse(r));
			}
			catch (Exception ex)
			{
				throw new Exception("Probe request failed.\nAgent Uri: " + AgentUri, ex);
			}
			*/
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
		private List<DataItem> GetDataItems(Component[] Components)
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

		private void streamingTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			monoBehaviour.StartCoroutine (streamingTimer_ElapsedInternal());
			/*
			var request = new RestRequest();
			request.Resource = "sample";
			request.AddParameter("at", lastSequence + 1);
			restClient.ExecuteAsync(request, (r) => parseStream(r));
			*/
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

		/// <summary>
		/// Disposes unmanaged resources
		/// </summary>
		public void Dispose()
		{
			if (streamingTimer != null)
			{
				streamingTimer.Dispose();
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
	}
}
