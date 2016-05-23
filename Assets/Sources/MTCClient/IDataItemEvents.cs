using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MTConnectSharp4Unity3D
{
	[ComVisible(true)]
	[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
	public interface IDataItemEvents
	{
		void SampleAdded(object sender, EventArgs e);
	}
}
