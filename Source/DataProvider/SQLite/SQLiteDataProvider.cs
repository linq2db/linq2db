using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SQLiteDataProvider : DynamicDataProviderBase
	{
		public SQLiteDataProvider()
			: this(ProviderName.SQLite, new SQLiteMappingSchema())
		{
		}

		protected SQLiteDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsSkipSupported                = false;
			SqlProviderFlags.IsSkipSupportedIfTake          = true;
			SqlProviderFlags.IsInsertOrUpdateSupported      = false;
			SqlProviderFlags.IsUpdateSetTableAliasSupported = false;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new SQLiteSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace { get { return SQLiteTools.AssemblyName; } }
		protected override string ConnectionTypeName  { get { return "{0}.{1}, {0}".Args(SQLiteTools.AssemblyName, SQLiteTools.ConnectionName); } }
		protected override string DataReaderTypeName  { get { return "{0}.{1}, {0}".Args(SQLiteTools.AssemblyName, SQLiteTools.DataReaderName); } }

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new SQLiteSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETSTANDARD
		public override ISchemaProvider GetSchemaProvider()
		{
			return new SQLiteSchemaProvider();
		}
#endif

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			if (SQLiteTools.AlwaysCheckDbNull)
				return true;

			return base.IsDBNullAllowed(reader, idx);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			base.SetParameter(parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.UInt32    : dataType = DataType.Int64;    break;
				case DataType.UInt64    : dataType = DataType.Decimal;  break;
				case DataType.DateTime2 : dataType = DataType.DateTime; break;
			}

			base.SetParameterType(parameter, dataType);
		}

		static Action<string> _createDatabase;

		public void CreateDatabase([JetBrains.Annotations.NotNull] string databaseName, bool deleteIfExists = false)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			CreateFileDatabase(
				databaseName, deleteIfExists, ".sqlite",
				dbName =>
				{
					if (_createDatabase == null)
					{
						var p = Expression.Parameter(typeof(string));
						var l = Expression.Lambda<Action<string>>(
							Expression.Call(GetConnectionType(), "CreateFile", null, p),
							p);
						_createDatabase = l.Compile();
					}

					_createDatabase(dbName);
				});
		}

		public void DropDatabase([JetBrains.Annotations.NotNull] string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			DropFileDatabase(databaseName, ".sqlite");
		}

#if NETSTANDARD

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
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
				throw new LinqToDBException("Can't create '{0}' type or '{1}' specific type for {2}.".Args(
					typeName,
					providerType,
					((DbDataReader)reader).GetName(idx)));
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

			Expression expr;

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
			var getValueMethodInfo = LinqToDB.Expressions.MemberHelper.MethodOf<IDataReader>(r => r.GetValue(0));
			return Expression.Convert(
				Expression.Call(readerExpression, getValueMethodInfo, Expression.Constant(idx)),
				fieldType);
		}

#endif

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SQLiteBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SQLiteTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

#endregion
	}
}
