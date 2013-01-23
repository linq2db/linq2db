using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public class ColumnAttribute : Attribute
	{
		public ColumnAttribute()
		{
			IsColumn = true;
		}

		public  string   Configuration { get; set; }
		public  string   Name          { get; set; }
		public  DataType DataType      { get; set; }
		public  string   DbType        { get; set; }
		public  bool     IsColumn      { get; set; }
		public  string   Storage       { get; set; }

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
