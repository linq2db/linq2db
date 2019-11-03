using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using OleDbType = LinqToDB.DataProvider.Wrappers.Mappers.OleDb.OleDbType;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class AccessDataProvider : DynamicDataProviderBase
	{
		private readonly OleDbType _decimalType = OleDbType.Decimal;

		public AccessDataProvider()
			: this(ProviderName.Access, new AccessMappingSchema())
		{
		}

		protected AccessDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.AcceptsTakeAsParameter           = false;
			SqlProviderFlags.IsSkipSupported                  = false;
			SqlProviderFlags.IsCountSubQuerySupported         = false;
			SqlProviderFlags.IsInsertOrUpdateSupported        = false;
			SqlProviderFlags.TakeHintsSupported               = TakeHints.Percent;
			SqlProviderFlags.IsCrossJoinSupported             = false;
			SqlProviderFlags.IsInnerJoinAsCrossSupported      = false;
			SqlProviderFlags.IsDistinctOrderBySupported       = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported = false;
			SqlProviderFlags.IsParameterOrderDependent        = true;
			SqlProviderFlags.IsUpdateFromSupported            = false;

			SetCharField            ("DBTYPE_WCHAR", (r, i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("DBTYPE_WCHAR", (r, i) => DataTools.GetChar(r, i));

			SetProviderField<IDataReader, TimeSpan, DateTime>((r, i) => r.GetDateTime(i) - new DateTime(1899, 12, 30));
			SetProviderField<IDataReader, DateTime, DateTime>((r, i) => GetDateTime(r, i));

			_sqlOptimizer = new AccessSqlOptimizer(SqlProviderFlags);

			if (System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".")
				_decimalType = OleDbType.VarChar;
		}

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1899 && value.Month == 12 && value.Day == 30)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

#if !NET45 && !NET46
		public string AssemblyName => "System.Data.OleDb";
#else
		public string AssemblyName => "System.Data";
#endif

		public override string ConnectionNamespace   => "System.Data.OleDb";
		protected override string ConnectionTypeName => $"{ConnectionNamespace}.OleDbConnection, {AssemblyName}";
		protected override string DataReaderTypeName => $"{ConnectionNamespace}.OleDbDataReader, {AssemblyName}";

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			if (Wrappers.Mappers.OleDb.ParameterType == null)
			{
				Wrappers.Mappers.OleDb.Initialize(connectionType.Assembly);
			}
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new AccessSqlBuilder(this, GetSqlOptimizer(), SqlProviderFlags, mappingSchema.ValueToSqlConverter);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new AccessSchemaProvider(this);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DbDataType dataType)
		{
			OleDbType? type = null;
			// Do some magic to workaround 'Data type mismatch in criteria expression' error
			// in JET for some european locales.
			//
			// OleDbType.Decimal is locale aware, OleDbType.Currency is locale neutral.
			// OleDbType.DBTimeStamp is locale aware, OleDbType.Date is locale neutral.
			switch (dataType.DataType)
			{
				case DataType.Decimal   :
				case DataType.VarNumeric: type = _decimalType          ; break;
				case DataType.DateTime  :
				case DataType.DateTime2 : type = OleDbType.Date        ; break;
				case DataType.Text      : type = OleDbType.LongVarChar ; break;
				case DataType.NText     : type = OleDbType.LongVarWChar; break;
			}

			if (type != null && Wrappers.Mappers.OleDb.TypeSetter != null)
			{
				var param = TryConvertParameter(Wrappers.Mappers.OleDb.ParameterType, parameter);
				if (param != null)
				{
					Wrappers.Mappers.OleDb.TypeSetter(param, type.Value);
					return;
				}
			}

			base.SetParameterType(parameter, dataType);
		}

		[ComImport, Guid("00000602-0000-0010-8000-00AA006D2EA4")]
		class CatalogClass
		{
		}

		public void CreateDatabase([JetBrains.Annotations.NotNull] string databaseName, bool   deleteIfExists = false)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			databaseName = databaseName.Trim();

			if (!databaseName.ToLower().EndsWith(".mdb"))
				databaseName += ".mdb";

			if (File.Exists(databaseName))
			{
				if (!deleteIfExists)
					return;
				File.Delete(databaseName);
			}

			var connectionString = string.Format(
				@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Locale Identifier=1033;Jet OLEDB:Engine Type=5",
				databaseName);

			CreateFileDatabase(
				databaseName, deleteIfExists, ".mdb",
				dbName =>
				{
					dynamic catalog = new CatalogClass();

					var conn = catalog.Create(connectionString);

					if (conn != null)
						conn.Close();
				});
		}

		public void DropDatabase([JetBrains.Annotations.NotNull] string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DropFileDatabase(databaseName, ".mdb");
		}

#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{

			return new AccessBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? AccessTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
				options,
				source);
		}

#endregion
	}
}
