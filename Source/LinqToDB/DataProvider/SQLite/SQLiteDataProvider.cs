#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SQLite
{
	using Data;
	using Common;
	using Extensions;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;
	using System.IO;

	public class SQLiteDataProvider : DynamicDataProviderBase
	{
		public SQLiteDataProvider()
			: this(ProviderName.SQLite)
		{
		}

		public SQLiteDataProvider(string name)
			: this(name, null)
		{
		}

		protected SQLiteDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsSkipSupported                   = false;
			SqlProviderFlags.IsSkipSupportedIfTake             = true;
			SqlProviderFlags.IsInsertOrUpdateSupported         = false;
			SqlProviderFlags.IsUpdateSetTableAliasSupported    = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = true;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsDistinctSetOperationsSupported  = true;
			SqlProviderFlags.IsUpdateFromSupported             = false;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("nchar", (r, i) => DataTools.GetChar(r, i));

			_sqlOptimizer = new SQLiteSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace => Name == ProviderName.SQLiteClassic
			? "System.Data.SQLite"
			: "Microsoft.Data.Sqlite";
		protected override string ConnectionTypeName  => Name == ProviderName.SQLiteClassic
			? "System.Data.SQLite.SQLiteConnection, System.Data.SQLite"
			: "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite";
		protected override string DataReaderTypeName   =>Name == ProviderName.SQLiteClassic
			? "System.Data.SQLite.SQLiteDataReader, System.Data.SQLite"
			: "Microsoft.Data.Sqlite.SqliteDataReader, Microsoft.Data.Sqlite";

		protected override string NormalizeTypeName(string typeName)
		{
			if (typeName == null)
				return null;

			if (typeName.StartsWith("char("))
				return "char";

			if (typeName.StartsWith("nchar("))
				return "nchar";

			return typeName;
		}

		// workaround for https://github.com/aspnet/EntityFrameworkCore/issues/17521
		// needed only for Microsoft.Data.Sqlite 3.0.0
		private bool _needsCommandDisposeOnError;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_needsCommandDisposeOnError = connectionType.AssemblyQualifiedName == "Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60";
		}

		public override IDisposable ExecuteScope(DataConnection dataConnection)
		{
			if (_needsCommandDisposeOnError)
				return new CallOnExceptionRegion(() => dataConnection.DisposeCommand());

			return base.ExecuteScope(dataConnection);
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SQLiteSqlBuilder(mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			public static readonly SQLiteMappingSchema.ClassicMappingSchema   ClassicMappingSchema   = new SQLiteMappingSchema.ClassicMappingSchema();
			public static readonly SQLiteMappingSchema.MicrosoftMappingSchema MicrosoftMappingSchema = new SQLiteMappingSchema.MicrosoftMappingSchema();
		}

		public override MappingSchema MappingSchema => Name == ProviderName.SQLiteClassic
			? MappingSchemaInstance.ClassicMappingSchema as MappingSchema
			: MappingSchemaInstance.MicrosoftMappingSchema;

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SQLiteSchemaProvider();
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			if (SQLiteTools.AlwaysCheckDbNull)
				return true;

			return base.IsDBNullAllowed(reader, idx);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object value)
		{
			// handles situation, when char values were serialized as character hex value for some
			// versions of Microsoft.Data.Sqlite
			if (Name == ProviderName.SQLiteMS && value is char)
			{
				value = value.ToString();
			}

			// reverting compatibility breaking change in Microsoft.Data.Sqlite 3.0.0
			// https://github.com/aspnet/EntityFrameworkCore/issues/15078
			// pre-3.0 and System.Data.Sqlite uses binary type for Guid values, there is no reason to replace it with string value
			// we can allow strings later if there will be request for it
			if (Name == ProviderName.SQLiteMS && value is Guid guid)
			{
				value = guid.ToByteArray();
			}

			base.SetParameter(dataConnection, parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				case DataType.UInt32    : dataType = dataType.WithDataType(DataType.Int64);    break;
				case DataType.UInt64    : dataType = dataType.WithDataType(DataType.Decimal);  break;
				case DataType.DateTime2 : dataType = dataType.WithDataType(DataType.DateTime); break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			if (Name == ProviderName.SQLiteClassic)
				return base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);

			var fieldType    = ((DbDataReader)reader).GetFieldType(idx);
			var providerType = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);
			var typeName     = ((DbDataReader)reader).GetDataTypeName(idx);

			if (toType.IsFloatType() && fieldType.IsIntegerType())
			{
				providerType = fieldType = toType;
			}

			if (reader.IsDBNull(idx))
				goto DEFAULT;

			if (fieldType == null)
			{
				var name = ((DbDataReader)reader).GetName(idx);
				throw new LinqToDBException($"Can't create '{typeName}' type or '{providerType}' specific type for {name}.");
			}

#if DEBUG1
			Debug.WriteLine("ToType                ProviderFieldType     FieldType             DataTypeName          Expression");
			Debug.WriteLine("--------------------- --------------------- --------------------- --------------------- ---------------------");
			Debug.WriteLine("{0,-21} {1,-21} {2,-21} {3,-21}".Args(
				toType       == null ? "(null)" : toType.Name,
				providerType == null ? "(null)" : providerType.Name,
				fieldType.Name,
				typeName ?? "(null)"));
			Debug.WriteLine("--------------------- --------------------- --------------------- --------------------- ---------------------");

			foreach (var ex in ReaderExpressions)
			{
				Debug.WriteLine("{0,-21} {1,-21} {2,-21} {3,-21} {4}"
					.Args(
						ex.Key.ToType            == null ? null : ex.Key.ToType.Name,
						ex.Key.ProviderFieldType == null ? null : ex.Key.ProviderFieldType.Name,
						ex.Key.FieldType         == null ? null : ex.Key.FieldType.Name,
						ex.Key.DataTypeName,
						ex.Value));
			}
#endif

			if (FindExpression(new ReaderInfo { ToType = toType, ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out var expr) ||
			    FindExpression(new ReaderInfo { ToType = toType, ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType, ProviderFieldType = providerType                                                 }, out expr) ||
			    FindExpression(new ReaderInfo {                  ProviderFieldType = providerType                                                 }, out expr) ||
			    FindExpression(new ReaderInfo {                  ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo {                  ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType,                                   FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType,                                   FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo {                                                    FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { ToType = toType                                                                                   }, out expr) ||
			    FindExpression(new ReaderInfo {                                                    FieldType = fieldType                          }, out expr))
				return expr;

		DEFAULT:

			var getValueMethodInfo = Expressions.MemberHelper.MethodOf<IDataReader>(r => r.GetValue(0));
			return Expression.Convert(
				Expression.Call(readerExpression, getValueMethodInfo, Expression.Constant(idx)),
				fieldType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SQLiteBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SQLiteTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		#endregion
	}
}
