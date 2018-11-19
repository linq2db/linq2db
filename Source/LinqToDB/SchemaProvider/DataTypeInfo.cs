using System;
using System.Diagnostics;

namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Database data type descriptor.
	/// Implements subset of DataTypes schema collection:
	/// https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/common-schema-collections.
	/// </summary>
	[DebuggerDisplay("TypeName = {TypeName}, DataType = {DataType}, CreateFormat = {CreateFormat}, CreateParameters = {CreateParameters}")]
	public class DataTypeInfo
	{
		/// <summary>
		/// Gets or sets SQL name of data type.
		/// </summary>
		public string TypeName;
		/// <summary>
		/// Gets or sets .NET type name, used by provider for current type.
		/// </summary>
		public string DataType;
		/// <summary>
		/// Gets or sets SQL type name template - type name and, optionally, parameters. This template could be used
		/// to define column or variable of specific type.
		/// E.g. DECIMAL({0}, {1}).
		/// </summary>
		public string CreateFormat;
		/// <summary>
		/// Gets or sets comma-separated positional list of <see cref="CreateFormat"/> parameters.
		/// E.g. "precision,scale".
		/// Order of parameters must match order in <see cref="CreateFormat"/>.
		/// </summary>
		public string CreateParameters;
		/// <summary>
		/// Gets or sets provider-specific type identifier to use for query parameters of this type.
		/// Corresponds to some provider's enumeration, e.g. SqlDbType, OracleType, etc.
		/// </summary>
		public int    ProviderDbType;
	}
}
