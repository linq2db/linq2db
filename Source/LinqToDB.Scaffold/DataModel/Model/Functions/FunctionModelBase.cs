using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Base class for stored procedure or function descriptors.
	/// </summary>
	public abstract class FunctionModelBase
	{
		protected FunctionModelBase(SqlObjectName name, MethodModel method)
		{
			Name   = name;
			Method = method;
		}

		/// <summary>
		/// Gets or sets database name of function/procedure.
		/// </summary>
		public SqlObjectName                Name       { get; set; }
		/// <summary>
		/// Gets or sets method descriptor for function/procedure.
		/// </summary>
		public MethodModel                  Method     { get; set; }
		/// <summary>
		/// Ordered (in call order) collection of function/procedure parameters.
		/// Includes in, out and in/out parameters, but not return parameters.
		/// Return parameter is not a part of call signature and defined by separate property.
		/// </summary>
		public List<FunctionParameterModel> Parameters { get;      } = new();
	}
}
