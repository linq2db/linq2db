using System;

namespace LinqToDB.SchemaProvider
{
	public class ProcedureParameterInfo
	{
		public string ProcedureID;
		public int    Ordinal;
		public string ParameterName;
		public string DataType;
		public int?   Length;
		public int    Precision;
		public int    Scale;
		public bool   IsIn;
		public bool   IsOut;
		public bool   IsResult;
	}
}
