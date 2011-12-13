using System;
using System.Collections;

namespace LinqToDB.Mapping
{
	public class ScalarListMapper : MapDataSourceDestinationBase
	{
		public ScalarListMapper(IList list, Type type)
		{
			_list = list;
			_type = type;
		}

		private readonly IList _list;
		private readonly Type  _type;
		private          int   _index;

		#region Destination

		public override Type GetFieldType(int index)
		{
			return _type;
		}

		public override int GetOrdinal(string name)
		{
			return 0;
		}

		public override void SetValue(object o, int index, object value)
		{
			_list.Add(value);
		}

		public override void SetValue(object o, string name, object value)
		{
			_list.Add(value);
		}

		#endregion

		#region Source

		public override int Count
		{
			get { return _index < _list.Count? 1: 0; }
		}

		public override string GetName(int index)
		{
			return string.Empty;
		}

		public override object GetValue(object o, int index)
		{
			return _list[_index++];
		}

		public override object GetValue(object o, string name)
		{
			return _list[_index++];
		}

		#endregion
	}
}
