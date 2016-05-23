using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace MTConnectSharp4Unity3D
{
	public interface IMTConnectClient
	{
		string AgentUri { get; set; }
		void Probe();
		void StartStreaming();
		void StopStreaming();
		void GetCurrentState();
		Device[] Devices { get; }
		int UpdateInterval { get; set; }
	}
}
