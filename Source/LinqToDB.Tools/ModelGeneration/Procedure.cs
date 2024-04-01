using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	public interface IProcedure : IMethod
	{
		string?         Schema              { get; set; }
		string?         ProcedureName       { get; set; }
		string?         PackageName         { get; set; }
		bool            IsFunction          { get; set; }
		bool            IsTableFunction     { get; set; }
		bool            IsAggregateFunction { get; set; }
		bool            IsDefaultSchema     { get; set; }
		bool            IsLoaded            { get; set; }
		string?         Description         { get; set; }

		ITable?         ResultTable         { get; set; }
		Exception?      ResultException     { get; set; }
		List<ITable>    SimilarTables       { get; set; }
		List<Parameter> ProcParameters      { get; set; }
	}

	public class Procedure<T> : Method<T>, IProcedure
		where T : Procedure<T>, new()
	{
		public string?         Schema              { get; set; }
		public string?         ProcedureName       { get; set; }
		public string?         PackageName         { get; set; }
		public bool            IsFunction          { get; set; }
		public bool            IsTableFunction     { get; set; }
		public bool            IsAggregateFunction { get; set; }
		public bool            IsDefaultSchema     { get; set; }
		public bool            IsLoaded            { get; set; }
		public string?         Description         { get; set; }

		public ITable?         ResultTable         { get; set; }
		public Exception?      ResultException     { get; set; }
		public List<ITable>    SimilarTables       { get; set; } = new();
		public List<Parameter> ProcParameters      { get; set; } = new();
	}
}
