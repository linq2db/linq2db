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
	using System.Threading.Tasks;

	public class SQLiteDataProvider : DynamicDataProviderBase<SQLiteProviderAdapter>
	{
		/// <summary>
		/// Creates the specified SQLite provider based on the provider name.
		/// </summary>
		/// <param name="name">If ProviderName.SQLite is provided,
		/// the detection mechanism preferring System.Data.SQLite
		/// to Microsoft.Data.Sqlite will be used.</param>
		public SQLiteDataProvider(string name)
			: this(name, MappingSchemaInstance.Get(name))
		{
		}

		protected SQLiteDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema, SQLiteProviderAdapter.GetInstance(name))
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
			SqlProviderFlags.DefaultMultiQueryIsolationLevel   = IsolationLevel.Serializable;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("nchar", (r, i) => DataTools.GetChar(r, i));

			_sqlOptimizer = new SQLiteSqlOptimizer(SqlProviderFlags);
		}

		protected override string? NormalizeTypeName(string? typeName)
		{
			if (typeName == null)
				return null;

			if (typeName.StartsWith("char("))
				return "char";

			if (typeName.StartsWith("nchar("))
				return "nchar";

			return typeName;
		}

		public override IDisposable? ExecuteScope(DataConnection dataConnection)
		{
			if (Adapter.DisposeCommandOnError)
				return new CallOnExceptionRegion(() => dataConnection.DisposeCommand());

			return base.ExecuteScope(dataConnection);
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SQLiteSqlBuilder(mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema ClassicMappingSchema   = new SQLiteMappingSchema.ClassicMappingSchema();
			public static readonly MappingSchema MicrosoftMappingSchema = new SQLiteMappingSchema.MicrosoftMappingSchema();

			public static MappingSchema Get(string name) => name == ProviderName.SQLiteClassic ? ClassicMappingSchema : MicrosoftMappingSchema;
		}

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

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			// handles situation, when char values were serialized as character hex value for some
			// versions of Microsoft.Data.Sqlite
			if (Name == ProviderName.SQLiteMS && value is char)
				value = value.ToString();

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

		public override Expression GetReaderExpression(IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			if (Name != ProviderName.SQLiteMS)
				return base.GetReaderExpression(reader, idx, readerExpression, toType);

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
			var dataReaderType = readerExpression.Type;

			if (FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out var expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType, ProviderFieldType = providerType                                                 }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                  ProviderFieldType = providerType                                                 }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                  ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                  ProviderFieldType = providerType, FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType,                                   FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType,                                   FieldType = fieldType                          }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                                                    FieldType = fieldType, DataTypeName = typeName }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType, ToType = toType                                                                                   }, out expr) ||
			    FindExpression(new ReaderInfo { DataReaderType = dataReaderType,                                                    FieldType = fieldType                          }, out expr))
				return expr;

			if (FindExpression(new ReaderInfo { ToType = toType, ProviderFieldType = providerType, FieldType = fieldType, DataTypeName = typeName }, out expr) ||
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
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SQLiteBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SQLiteTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SQLiteBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SQLiteTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

#if !NET45 && !NET46
		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
		{
			return new SQLiteBulkCopy().BulkCopyAsync(
				options.BulkCopyType == BulkCopyType.Default ? SQLiteTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}
#endif

		#endregion
	}
}
