using System;
using System.Data;
using System.Data.Common;

namespace LinqToDB.DataProvider
{
	/// <summary>
	/// Provides metadata required by a data provider to create and configure a <see cref="DbParameter"/>.
	/// </summary>
	public readonly struct DataProviderParameterContext
	{
		public DataProviderParameterContext(
			string              name,
			DbDataType          dbDataType,
			object?             value,
			ParameterDirection? direction = null,
			bool                isDbDataTypeExplicit = false)
		{
			Name                 = name;
			DbDataType           = dbDataType;
			Value                = value;
			Direction            = direction;
			IsDbDataTypeExplicit = isDbDataTypeExplicit;
		}

		/// <summary>
		/// Gets the name of the parameter.
		/// </summary>
		public string              Name       { get; }
		/// <summary>
		/// Gets the database type of the parameter.
		/// </summary>
		public DbDataType          DbDataType { get; }
		/// <summary>
		/// Gets the value of the parameter.
		/// </summary>
		public object?             Value      { get; }
		/// <summary>
		/// Gets the direction of the parameter.
		/// </summary>
		public ParameterDirection? Direction  { get; }
		/// <summary>
		/// Gets a value indicating whether the database type is explicitly specified.
		/// </summary>
		public bool                IsDbDataTypeExplicit { get; }
	}
}
