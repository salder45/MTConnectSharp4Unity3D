using System;
namespace MTConnectSharp4Unity3D
{
	public interface IDataItemSample
	{
		DateTime TimeStamp { get; }
		string ToString();
		string Value { get; }
	}
}
