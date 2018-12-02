using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;
	using System.Collections.Concurrent;

	public class SybaseDataProvider : DynamicDataProviderBase
	{
		#region Init

		public SybaseDataProvider()
			: this(SybaseTools.DetectedProviderName)
		{
		}

		public SybaseDataProvider(string name)
			: base(name, null)
		{
			SqlProviderFlags.AcceptsTakeAsParameter     = false;
			SqlProviderFlags.IsSkipSupported            = false;
			SqlProviderFlags.IsSubQueryTakeSupported    = false;
			//SqlProviderFlags.IsCountSubQuerySupported  = false;
			SqlProviderFlags.CanCombineParameters       = false;
			SqlProviderFlags.IsSybaseBuggyGroupBy       = true;
			SqlProviderFlags.IsCrossJoinSupported       = false;
			SqlProviderFlags.IsSubQueryOrderBySupported = false;
			SqlProviderFlags.IsDistinctOrderBySupported = false;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharFieldToType<char>("char",  (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("nchar", (r, i) => DataTools.GetChar(r, i));

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1900, 1, 1));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r, i));

			_sqlOptimizer = new SybaseSqlOptimizer(SqlProviderFlags);
		}

		public             string AssemblyName        => Name == ProviderName.Sybase ? SybaseTools.NativeAssemblyName : "AdoNetCore.AseClient";
		public    override string ConnectionNamespace => Name == ProviderName.Sybase ? "Sybase.Data.AseClient" : "AdoNetCore.AseClient";
		protected override string ConnectionTypeName  => $"{ConnectionNamespace}.AseConnection, {AssemblyName}";
		protected override string DataReaderTypeName  => $"{ConnectionNamespace}.AseDataReader, {AssemblyName}";

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		public override string DbFactoryProviderName => "Sybase.Data.AseClient";
#endif

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1900 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		private Action<IDbDataParameter> _setUInt16;
		private Action<IDbDataParameter> _setUInt32;
		private Action<IDbDataParameter> _setUInt64;
		private Action<IDbDataParameter> _setText;
		private Action<IDbDataParameter> _setNText;
		private Action<IDbDataParameter> _setBinary;
		private Action<IDbDataParameter> _setVarBinary;
		private Action<IDbDataParameter> _setImage;
		private Action<IDbDataParameter> _setMoney;
		private Action<IDbDataParameter> _setSmallMoney;
		private Action<IDbDataParameter> _setDate;
		private Action<IDbDataParameter> _setTime;
		private Action<IDbDataParameter> _setSmallDateTime;
		private Action<IDbDataParameter> _setTimestamp;

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_setUInt16        = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "UnsignedSmallInt");
			_setUInt32        = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "UnsignedInt"     );
			_setUInt64        = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "UnsignedBigInt"  );
			_setText          = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Text"            );
			_setNText         = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Unitext"         );
			_setBinary        = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Binary"          );
			_setVarBinary     = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "VarBinary"       );
			_setImage         = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Image"           );
			_setMoney         = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Money"           );
			_setSmallMoney    = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "SmallMoney"      );
			_setDate          = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Date"            );
			_setTime          = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Time"            );
			_setSmallDateTime = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "SmallDateTime"   );
			_setTimestamp     = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "TimeStamp"       );
		}

		#endregion

		#region Overrides

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new SybaseSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		static class MappingSchemaInstance
		{
			public static readonly SybaseMappingSchema.NativeMappingSchema  NativeMappingSchema  = new SybaseMappingSchema.NativeMappingSchema();
			public static readonly SybaseMappingSchema.ManagedMappingSchema ManagedMappingSchema = new SybaseMappingSchema.ManagedMappingSchema();
		}

		public override MappingSchema MappingSchema => Name == ProviderName.Sybase
			? MappingSchemaInstance.NativeMappingSchema as MappingSchema
			: MappingSchemaInstance.ManagedMappingSchema;

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETSTANDARD1_6
		public override ISchemaProvider GetSchemaProvider()
		{
			return new SybaseSchemaProvider(Name);
		}
#endif

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.SByte      :
					dataType = DataType.Int16;
					if (value is sbyte)
						value = (short)(sbyte)value;
					break;

				case DataType.Time       :
					if (value is TimeSpan) value = new DateTime(1900, 1, 1) + (TimeSpan)value;
					break;

				case DataType.Xml        :
					dataType = DataType.NVarChar;
						 if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;

				case DataType.Guid       :
					if (value != null)
						value = value.ToString();
					dataType = DataType.Char;
					parameter.Size = 36;
					break;

				case DataType.Undefined  :
					if (value == null)
						dataType = DataType.Char;
					break;
			}

			base.SetParameter(parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.VarNumeric    : parameter.DbType = DbType.Decimal;          break;
				case DataType.UInt16        : _setUInt16(parameter);                      break;
				case DataType.UInt32        : _setUInt32(parameter);                      break;
				case DataType.UInt64        : _setUInt64(parameter);                      break;
				case DataType.Text          : _setText(parameter);                        break;
				case DataType.NText         : _setNText(parameter);                       break;
				case DataType.Binary        : _setBinary(parameter);                      break;
				case DataType.Blob          :
				case DataType.VarBinary     : _setVarBinary(parameter);                   break;
				case DataType.Image         : _setImage(parameter);                       break;
				case DataType.Money         : _setMoney(parameter);                       break;
				case DataType.SmallMoney    : _setSmallMoney(parameter);                  break;
				case DataType.Date          : _setDate(parameter);                        break;
				case DataType.Time          : _setTime(parameter);                        break;
				case DataType.SmallDateTime : _setSmallDateTime(parameter);               break;
				case DataType.Timestamp     : _setTimestamp(parameter);                   break;
				case DataType.DateTime2     :
					base.SetParameterType(parameter, DataType.DateTime);
					                                                                      break;

				default                     : base.SetParameterType(parameter, dataType); break;
			}
		}

#endregion

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new SybaseBulkCopy().BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? SybaseTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

		#endregion

		#region Merge
		protected override BasicMergeBuilder<TTarget, TSource> GetMergeBuilder<TTarget, TSource>(
			DataConnection connection,
			IMergeable<TTarget,TSource> merge)
		{
			return new SybaseMergeBuilder<TTarget, TSource>(connection, merge);
		}
		#endregion
	}
}
