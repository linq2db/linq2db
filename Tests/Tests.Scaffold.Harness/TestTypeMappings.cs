using System;
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.CodeModel;
using LinqToDB.Schema;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Database type name to CLR type mappings for <see cref="TestSchema"/>. Types are resolved by the target language
	/// type parser, so provider-specific types are referenced by name and need no assembly reference here.
	/// </summary>
	public sealed class TestTypeMappings : ITypeMappingProvider
	{
		readonly Dictionary<string, (Func<ITypeParser, IType> Type, DataType DataType)> _mappings = new(StringComparer.OrdinalIgnoreCase);

		ITypeParser? _parser;

		public TestTypeMappings()
		{
			Map<int>     ("int"             , DataType.Int32);
			Map<long>    ("integer"         , DataType.Int64);
			Map<long>    ("bigint"          , DataType.Int64);
			Map<short>   ("smallint"        , DataType.Int16);
			Map<string>  ("text"            , DataType.NText);
			Map<string>  ("nvarchar"        , DataType.NVarChar);
			Map<string>  ("varchar"         , DataType.VarChar);
			Map<double>  ("real"            , DataType.Double);
			Map<double>  ("float"           , DataType.Double);
			Map<decimal> ("decimal"         , DataType.Decimal);
			Map<decimal> ("numeric"         , DataType.Decimal);
			Map<bool>    ("bit"             , DataType.Boolean);
			Map<bool>    ("boolean"         , DataType.Boolean);
			Map<DateTime>("datetime"        , DataType.DateTime);
			Map<DateTime>("date"            , DataType.Date);
			Map<Guid>    ("uniqueidentifier", DataType.Guid);
			Map<Guid>    ("uuid"            , DataType.Guid);
			Map<byte[]>  ("blob"            , DataType.Blob);
			Map<byte[]>  ("varbinary"       , DataType.VarBinary);
			Map<byte[]>  ("bytea"           , DataType.Binary);
		}

		public TestTypeMappings Map<T>(string databaseType, DataType dataType)
		{
			_mappings[databaseType] = (static p => p.Parse<T>(), dataType);
			return this;
		}

		/// <summary>
		/// Maps database type to CLR type, referenced by full name.
		/// </summary>
		public TestTypeMappings Map(string databaseType, string clrTypeName, bool valueType, DataType dataType)
		{
			_mappings[databaseType] = (p => p.Parse(clrTypeName, valueType), dataType);
			return this;
		}

		internal TestTypeMappings WithParser(ITypeParser parser)
		{
			_parser = parser;
			return this;
		}

		TypeMapping? ITypeMappingProvider.GetTypeMapping(DatabaseType databaseType)
		{
			if (_parser == null)
				throw new InvalidOperationException("Type parser not set");

			return databaseType.Name != null && _mappings.TryGetValue(databaseType.Name, out var mapping)
				? new TypeMapping(mapping.Type(_parser), mapping.DataType)
				: null;
		}
	}
}
