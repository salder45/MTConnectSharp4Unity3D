﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MTConnectSharp4Unity3D
{
	[ComVisible(true)]
	[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
	public interface IClientEvents
	{
		void ProbeCompleted(object sender, EventArgs e);
		void DataItemChanged(object sender, DataItemChangedEventArgs e);
		void DataItemsChanged(object sender, EventArgs e);
	}
}
