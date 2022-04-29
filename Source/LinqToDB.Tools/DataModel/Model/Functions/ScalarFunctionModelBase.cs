﻿using LinqToDB.Metadata;
using LinqToDB.Schema;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Base class for scalar or aggregate function descriptors (methods with scalar return value).
	/// </summary>
	public abstract class ScalarFunctionModelBase : FunctionModelBase
	{
		protected ScalarFunctionModelBase(ObjectName name, MethodModel method, FunctionMetadata metadata)
			: base(name, method)
		{
			Metadata = metadata;
		}

		/// <summary>
		/// Gets or sets function metadata.
		/// </summary>
		public FunctionMetadata Metadata { get; set; }
	}
}
