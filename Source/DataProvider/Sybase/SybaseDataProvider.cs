using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SybaseDataProvider : DynamicDataProviderBase
	{
		#region Init

		public SybaseDataProvider()
			: this(ProviderName.Sybase, new SybaseMappingSchema())
		{
		}

		protected SybaseDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.AcceptsTakeAsParameter    = false;
			SqlProviderFlags.IsSkipSupported           = false;
			SqlProviderFlags.IsSubQueryTakeSupported   = false;
			//SqlProviderFlags.IsCountSubQuerySupported  = false;
			SqlProviderFlags.CanCombineParameters      = false;
			SqlProviderFlags.IsSybaseBuggyGroupBy      = true;
			SqlProviderFlags.IsCrossJoinSupported      = false;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1900, 1, 1));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r, i));

			_sqlOptimizer = new SybaseSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace { get { return SybaseTools.AssemblyName; } }
		protected override string ConnectionTypeName  { get { return "{1}, {0}".Args(ConnectionNamespace, "Sybase.Data.AseClient.AseConnection"); } }
		protected override string DataReaderTypeName  { get { return "{1}, {0}".Args(ConnectionNamespace, "Sybase.Data.AseClient.AseDataReader"); } }

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1900 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			_setUInt16        = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "UnsignedSmallInt");
			_setUInt32        = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "UnsignedInt");
			_setUInt64        = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "UnsignedBigInt");
			_setText          = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Text");
			_setNText         = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Unitext");
			_setBinary        = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Binary");
			_setVarBinary     = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "VarBinary");
			_setImage         = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Image");
			_setMoney         = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Money");
			_setSmallMoney    = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "SmallMoney");
			_setDate          = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Date");
			_setTime          = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "Time");
			_setSmallDateTime = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "SmallDateTime");
			_setTimestamp     = GetSetParameter(connectionType, "AseParameter", "AseDbType", "AseDbType", "TimeStamp");
			_isUnsupported    = IsGetParameter (connectionType, "AseParameter", "AseDbType", "AseDbType", "Unsupported");
		}

		static Action<IDbDataParameter> _setUInt16;
		static Action<IDbDataParameter> _setUInt32;
		static Action<IDbDataParameter> _setUInt64;
		static Action<IDbDataParameter> _setText;
		static Action<IDbDataParameter> _setNText;
		static Action<IDbDataParameter> _setBinary;
		static Action<IDbDataParameter> _setVarBinary;
		static Action<IDbDataParameter> _setImage;
		static Action<IDbDataParameter> _setMoney;
		static Action<IDbDataParameter> _setSmallMoney;
		static Action<IDbDataParameter> _setDate;
		static Action<IDbDataParameter> _setTime;
		static Action<IDbDataParameter> _setSmallDateTime;
		static Action<IDbDataParameter> _setTimestamp;

		static Func<IDbDataParameter,bool> _isUnsupported;

		#endregion

		#region Overrides

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new SybaseSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETSTANDARD
		public override ISchemaProvider GetSchemaProvider()
		{
			return new SybaseSchemaProvider();
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
				case DataType.UInt16        : _setUInt16       (parameter);               break;
				case DataType.UInt32        : _setUInt32       (parameter);               break;
				case DataType.UInt64        : _setUInt64       (parameter);               break;
				case DataType.Text          : _setText         (parameter);               break;
				case DataType.NText         : _setNText        (parameter);               break;
				case DataType.Binary        : _setBinary       (parameter);               break;
				case DataType.Blob          :
				case DataType.VarBinary     : _setVarBinary    (parameter);               break;
				case DataType.Image         : _setImage        (parameter);               break;
				case DataType.Money         : _setMoney        (parameter);               break;
				case DataType.SmallMoney    : _setSmallMoney   (parameter);               break;
				case DataType.Date          : _setDate         (parameter);               break;
				case DataType.Time          : _setTime         (parameter);               break;
				case DataType.SmallDateTime : _setSmallDateTime(parameter);               break;
				case DataType.Timestamp     : _setTimestamp    (parameter);               break;
				case DataType.DateTime2     : 
					base.SetParameterType(parameter, dataType);

					if (_isUnsupported(parameter))
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
	}
}
