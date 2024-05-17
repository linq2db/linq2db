using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IProcedure<TTable> : IMethod
		where TTable : class, ITable, new()
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

		TTable?         ResultTable         { get; set; }
		Exception?      ResultException     { get; set; }
		List<TTable>    SimilarTables       { get; set; }
		List<Parameter> ProcParameters      { get; set; }
	}
}
