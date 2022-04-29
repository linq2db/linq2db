﻿using System.Collections.Generic;
using LinqToDB.Schema;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Stored procedure descriptor.
	/// </summary>
	public sealed class StoredProcedureModel : TableFunctionModelBase
	{
		public StoredProcedureModel(ObjectName name, MethodModel method)
			: base(name, method)
		{
		}

		/// <summary>
		/// Gets or sets return parameter descriptor.
		/// </summary>
		public FunctionParameterModel? Return  { get; set; }
		/// <summary>
		/// Gets or sets record types for returned result set(s).
		/// </summary>
		public List<FunctionResult>    Results { get; set; } = new();
	}
}
