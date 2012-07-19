using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public class ColumnAttribute : Attribute
	{
		public  string Config    { get; set; }
		public  string Name      { get; set; }
		public  string DbType    { get; set; }

		private bool? _canBeNull;
		public  bool   CanBeNull
		{
			get { return _canBeNull ?? true; }
			set { _canBeNull = value;        }
		}

		public bool? GetCanBeNull()
		{
			return _canBeNull;
		}
	}
}
