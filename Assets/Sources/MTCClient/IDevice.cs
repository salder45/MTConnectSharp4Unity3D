﻿using System;
using System.Collections.Generic;
namespace MTConnectSharp4Unity3D
{
	public interface IDevice
	{
		Component[] Components { get; }
		DataItem[] DataItems { get; }
		String Description { get; }
		String Manufacturer { get; }
		String SerialNumber { get; }
		String id { get; }
		String Name { get; }
		String LongName { get; }
	}
}
