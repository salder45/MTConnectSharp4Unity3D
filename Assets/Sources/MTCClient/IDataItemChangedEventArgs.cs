using System;
using System.Runtime.InteropServices;
namespace MTConnectSharp4Unity3D
{
	public interface IDataItemChangedEventArgs
	{
		DataItem DataItem { get; set; }
	}
}
