using System;
using System.Collections.Generic;
namespace MTConnectSharp4Unity3D
{
	public interface IComponent
	{
		Component[] Components { get; }
		DataItem[] DataItems { get; }
		String Type { get; }
		String id { get; }
		String Name { get; }
		String LongName { get; }
	}
}
