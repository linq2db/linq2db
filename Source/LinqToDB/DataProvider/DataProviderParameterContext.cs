using System.Data;

namespace LinqToDB.DataProvider
{
	public readonly struct DataProviderParameterContext
	{
		public DataProviderParameterContext(
			string              name,
			DbDataType          dbDataType,
			object?             value,
			ParameterDirection? direction = null)
		{
			Name       = name;
			DbDataType = dbDataType;
			Value      = value;
			Direction  = direction;
		}

		public string              Name       { get; }
		public DbDataType          DbDataType { get; }
		public object?             Value      { get; }
		public ParameterDirection? Direction  { get; }
	}
}
