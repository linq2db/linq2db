// Odp.Net Data Provider.
// http://www.oracle.com/technology/tech/windows/odpnet/index.html
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace LinqToDB.DataProvider
{
	using Common;
	using Data;
	using Mapping;
	using Reflection;
	using SqlProvider;

	/// <summary>
	/// Implements access to the Data Provider for Oracle.
	/// </summary>
	/// <remarks>
	/// See the <see cref="DbManager.AddDataProvider(DataProviderBaseOld)"/> method to find an example.
	/// </remarks>
	/// <seealso cref="DbManager.AddDataProvider(DataProviderBaseOld)">AddDataManager Method</seealso>
	public class OracleDataProvider : DataProviderBaseOld
	{
		public OracleDataProvider()
		{
			MappingSchema = new OracleMappingSchema();
		}

		static OracleDataProvider()
		{
			// Fix Oracle.Net bug #1: Array types are not handled.
			//
			var oraDbDbTypeTableType = typeof(OracleParameter).Assembly.GetType("Oracle.DataAccess.Client.OraDb_DbTypeTable");

			if (null != oraDbDbTypeTableType)
			{
				var typeTable = (Hashtable)oraDbDbTypeTableType.InvokeMember(
					"s_table", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField,
					null, null, Type.EmptyTypes);

				if (null != typeTable)
				{
					typeTable[typeof(DateTime[])]          = OracleDbType.TimeStamp;
					typeTable[typeof(Int16[])]             = OracleDbType.Int16;
					typeTable[typeof(Int32[])]             = OracleDbType.Int32;
					typeTable[typeof(Int64[])]             = OracleDbType.Int64;
					typeTable[typeof(Single[])]            = OracleDbType.Single;
					typeTable[typeof(Double[])]            = OracleDbType.Double;
					typeTable[typeof(Decimal[])]           = OracleDbType.Decimal;
					typeTable[typeof(TimeSpan[])]          = OracleDbType.IntervalDS;
					typeTable[typeof(String[])]            = OracleDbType.Varchar2;
					typeTable[typeof(OracleBFile[])]       = OracleDbType.BFile;
					typeTable[typeof(OracleBinary[])]      = OracleDbType.Raw;
					typeTable[typeof(OracleBlob[])]        = OracleDbType.Blob;
					typeTable[typeof(OracleClob[])]        = OracleDbType.Clob;
					typeTable[typeof(OracleDate[])]        = OracleDbType.Date;
					typeTable[typeof(OracleDecimal[])]     = OracleDbType.Decimal;
					typeTable[typeof(OracleIntervalDS[])]  = OracleDbType.IntervalDS;
					typeTable[typeof(OracleIntervalYM[])]  = OracleDbType.IntervalYM;
					typeTable[typeof(OracleRefCursor[])]   = OracleDbType.RefCursor;
					typeTable[typeof(OracleString[])]      = OracleDbType.Varchar2;
					typeTable[typeof(OracleTimeStamp[])]   = OracleDbType.TimeStamp;
					typeTable[typeof(OracleTimeStampLTZ[])]= OracleDbType.TimeStampLTZ;
					typeTable[typeof(OracleTimeStampTZ[])] = OracleDbType.TimeStampTZ;
					typeTable[typeof(OracleXmlType[])]     = OracleDbType.XmlType;

					typeTable[typeof(Boolean)]             = OracleDbType.Byte;
					typeTable[typeof(Guid)]                = OracleDbType.Raw;
					typeTable[typeof(SByte)]               = OracleDbType.Decimal;
					typeTable[typeof(UInt16)]              = OracleDbType.Decimal;
					typeTable[typeof(UInt32)]              = OracleDbType.Decimal;
					typeTable[typeof(UInt64)]              = OracleDbType.Decimal;

					typeTable[typeof(Boolean[])]           = OracleDbType.Byte;
					typeTable[typeof(Guid[])]              = OracleDbType.Raw;
					typeTable[typeof(SByte[])]             = OracleDbType.Decimal;
					typeTable[typeof(UInt16[])]            = OracleDbType.Decimal;
					typeTable[typeof(UInt32[])]            = OracleDbType.Decimal;
					typeTable[typeof(UInt64[])]            = OracleDbType.Decimal;

					typeTable[typeof(Boolean?)]            = OracleDbType.Byte;
					typeTable[typeof(Guid?)]               = OracleDbType.Raw;
					typeTable[typeof(SByte?)]              = OracleDbType.Decimal;
					typeTable[typeof(UInt16?)]             = OracleDbType.Decimal;
					typeTable[typeof(UInt32?)]             = OracleDbType.Decimal;
					typeTable[typeof(UInt64?)]             = OracleDbType.Decimal;
					typeTable[typeof(DateTime?[])]         = OracleDbType.TimeStamp;
					typeTable[typeof(Int16?[])]            = OracleDbType.Int16;
					typeTable[typeof(Int32?[])]            = OracleDbType.Int32;
					typeTable[typeof(Int64?[])]            = OracleDbType.Int64;
					typeTable[typeof(Single?[])]           = OracleDbType.Single;
					typeTable[typeof(Double?[])]           = OracleDbType.Double;
					typeTable[typeof(Decimal?[])]          = OracleDbType.Decimal;
					typeTable[typeof(TimeSpan?[])]         = OracleDbType.IntervalDS;
					typeTable[typeof(Boolean?[])]          = OracleDbType.Byte;
					typeTable[typeof(Guid?[])]             = OracleDbType.Raw;
					typeTable[typeof(SByte?[])]            = OracleDbType.Decimal;
					typeTable[typeof(UInt16?[])]           = OracleDbType.Decimal;
					typeTable[typeof(UInt32?[])]           = OracleDbType.Decimal;
					typeTable[typeof(UInt64?[])]           = OracleDbType.Decimal;

					typeTable[typeof(XmlReader)]           = OracleDbType.XmlType;
					typeTable[typeof(XmlDocument)]         = OracleDbType.XmlType;
					typeTable[typeof(MemoryStream)]        = OracleDbType.Blob;
					typeTable[typeof(XmlReader[])]         = OracleDbType.XmlType;
					typeTable[typeof(XmlDocument[])]       = OracleDbType.XmlType;
					typeTable[typeof(MemoryStream[])]      = OracleDbType.Blob;
				}
			}
		}

		/// <summary>
		/// Creates the database connection object.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBaseOld)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBaseOld)">AddDataManager Method</seealso>
		/// <returns>The database connection object.</returns>
		public override IDbConnection CreateConnectionObject()
		{
			return new OracleConnection();
		}

		public override IDbCommand CreateCommandObject(IDbConnection connection)
		{
			var oraConnection = connection as OracleConnection;

			if (null != oraConnection)
			{
				var oraCommand = oraConnection.CreateCommand();

				// Fix Oracle.Net bug #2: Empty arrays can not be sent to the server.
				//
				oraCommand.BindByName = true;

				return oraCommand;
			}

			return base.CreateCommandObject(connection);
		}

		public override IDbDataParameter CloneParameter(IDbDataParameter parameter)
		{
			var oraParameter = (parameter is OracleParameterWrap)?
				(parameter as OracleParameterWrap).OracleParameter: parameter as OracleParameter;

			if (null != oraParameter)
			{
				var oraParameterClone = (OracleParameter)oraParameter.Clone();

				// Fix Oracle.Net bug #3: CollectionType property is not cloned.
				//
				oraParameterClone.CollectionType = oraParameter.CollectionType;

				// Fix Oracle.Net bug #8423178
				// See http://forums.oracle.com/forums/thread.jspa?threadID=975902&tstart=0
				//
				if (oraParameterClone.OracleDbType == OracleDbType.RefCursor)
				{
					// Set OracleDbType to itself to reset m_bSetDbType and m_bOracleDbTypeExSet
					//
					oraParameterClone.OracleDbType = OracleDbType.RefCursor;
				}

				return OracleParameterWrap.CreateInstance(oraParameterClone);
			}

			return base.CloneParameter(parameter);
		}

		public override void SetUserDefinedType(IDbDataParameter parameter, string typeName)
		{
			var oraParameter = (parameter is OracleParameterWrap) ?
				(parameter as OracleParameterWrap).OracleParameter : parameter as OracleParameter;

			if (oraParameter == null)
				throw new ArgumentException("OracleParameter expected.", "parameter");

			oraParameter.UdtTypeName = typeName;
		}

		/// <summary>
		/// Creates the data adapter object.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBaseOld)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBaseOld)">AddDataManager Method</seealso>
		/// <returns>A data adapter object.</returns>
		public override DbDataAdapter CreateDataAdapterObject()
		{
			return new OracleDataAdapter();
		}

		/// <summary>
		/// Populates the specified IDbCommand object's Parameters collection with 
		/// parameter information for the stored procedure specified in the IDbCommand.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBaseOld)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBaseOld)">AddDataManager Method</seealso>
		/// <param name="command">The IDbCommand referencing the stored procedure for which the parameter
		/// information is to be derived. The derived parameters will be populated into
		/// the Parameters of this command.</param>
		public override bool DeriveParameters(IDbCommand command)
		{
			var oraCommand = command as OracleCommand;

			if (null != oraCommand)
			{
				try
				{
					OracleCommandBuilder.DeriveParameters(oraCommand);
				}
				catch (Exception ex)
				{
					// Make Oracle less laconic.
					//
					throw new DataException(string.Format("{0}\nCommandText: {1}", ex.Message, oraCommand.CommandText), ex);
				}

				return true;
			}

			return false;
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return ParameterPrefix == null? value: ParameterPrefix + value;

				case ConvertType.SprocParameterToName:
					var name = (string)value;

					if (name.Length > 0)
					{
						if (name[0] == ':')
							return name.Substring(1);

						if (ParameterPrefix != null &&
							name.ToUpper(CultureInfo.InvariantCulture).StartsWith(ParameterPrefix))
						{
							return name.Substring(ParameterPrefix.Length);
						}
					}

					break;

				case ConvertType.ExceptionToErrorNumber:
					if (value is OracleException)
						return ((OracleException)value).Number;
					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		public override void PrepareCommand(ref CommandType commandType, ref string commandText, ref IDbDataParameter[] commandParameters)
		{
			base.PrepareCommand(ref commandType, ref commandText, ref commandParameters);

			if (commandType == CommandType.Text)
			{
				// Fix Oracle bug #11 '\r' is not a valid character!
				//
				commandText = commandText.Replace('\r', ' ');
			}
		}

		public override void AttachParameter(IDbCommand command, IDbDataParameter parameter)
		{
			var oraParameter = (parameter is OracleParameterWrap)?
				(parameter as OracleParameterWrap).OracleParameter: parameter as OracleParameter;

			if (null != oraParameter)
			{
				if (oraParameter.CollectionType == OracleCollectionType.PLSQLAssociativeArray)
				{
					if (oraParameter.Direction == ParameterDirection.Input
						|| oraParameter.Direction == ParameterDirection.InputOutput)
					{
						var ar = oraParameter.Value as Array;

						if (null != ar && !(ar is byte[] || ar is char[]))
						{
							oraParameter.Size = ar.Length;

							if (oraParameter.DbType == DbType.String
								&& oraParameter.Direction == ParameterDirection.InputOutput)
							{
								var arrayBindSize = new int[oraParameter.Size];

								for (var i = 0; i < oraParameter.Size; ++i)
								{
									arrayBindSize[i] = 1024;
								}
								
								oraParameter.ArrayBindSize = arrayBindSize;
							}
						}

						if (oraParameter.Size == 0)
						{
							// Skip this parameter.
							// Fix Oracle.Net bug #2: Empty arrays can not be sent to the server.
							//
							return;
						}

						if (oraParameter.Value is Stream[])
						{
							var streams = (Stream[]) oraParameter.Value;

							for (var i = 0; i < oraParameter.Size; ++i)
							{
								if (streams[i] is OracleBFile || streams[i] is OracleBlob ||
									streams[i] is OracleClob || streams[i] is OracleXmlStream)
								{
									// Known Oracle type.
									//
									continue;
								}

								streams[i] = CopyStream(streams[i], (OracleCommand)command);
							}
						}
						else if (oraParameter.Value is XmlDocument[])
						{
							var xmlDocuments = (XmlDocument[]) oraParameter.Value;
							var values       = new object[oraParameter.Size];

							switch (oraParameter.OracleDbType)
							{
								case OracleDbType.XmlType:

									for (var i = 0; i < oraParameter.Size; ++i)
									{
										values[i] = xmlDocuments[i].DocumentElement == null?
											(object) DBNull.Value:
											new OracleXmlType((OracleConnection)command.Connection, xmlDocuments[i]);
									}

									oraParameter.Value = values;

									break;

								// Fix Oracle.Net bug #9: XmlDocument.ToString() returns System.Xml.XmlDocument,
								// so m_value.ToString() is not enought.
								//
								case OracleDbType.Clob:
								case OracleDbType.NClob:
								case OracleDbType.Varchar2:
								case OracleDbType.NVarchar2:
								case OracleDbType.Char:
								case OracleDbType.NChar:
									for (var i = 0; i < oraParameter.Size; ++i)
									{
										values[i] = xmlDocuments[i].DocumentElement == null?
											(object) DBNull.Value:
											xmlDocuments[i].InnerXml;
									}

									oraParameter.Value = values;

									break;

								// Or convert to bytes if need.
								//
								case OracleDbType.Blob:
								case OracleDbType.BFile:
								case OracleDbType.Raw:
								case OracleDbType.Long:
								case OracleDbType.LongRaw:
									for (var i = 0; i < oraParameter.Size; ++i)
									{
										if (xmlDocuments[i].DocumentElement == null)
											values[i] = DBNull.Value;
										else
											using (var s = new MemoryStream())
											{
												xmlDocuments[i].Save(s);
												values[i] = s.GetBuffer();
											}
									}

									oraParameter.Value = values;

									break;
							}
						}
					}
					else if (oraParameter.Direction == ParameterDirection.Output)
					{
						// Fix Oracle.Net bug #4: ArrayBindSize must be explicitly specified.
						//
						if (oraParameter.DbType == DbType.String)
						{
							oraParameter.Size = 1024;
							var arrayBindSize = new int[oraParameter.Size];
							for (var i = 0; i < oraParameter.Size; ++i)
							{
								arrayBindSize[i] = 1024;
							}
							
							oraParameter.ArrayBindSize = arrayBindSize;
						}
						else
						{
							oraParameter.Size = 32767;
						}
					}
				}
				else if (oraParameter.Value is Stream)
				{
					var stream = (Stream) oraParameter.Value;

					if (!(stream is OracleBFile) && !(stream is OracleBlob) &&
						!(stream is OracleClob) && !(stream is OracleXmlStream))
					{
						oraParameter.Value = CopyStream(stream, (OracleCommand)command);
					}
				}
				else if (oraParameter.Value is Byte[])
				{
					var bytes = (Byte[]) oraParameter.Value;

					if (bytes.Length > 32000)
					{
						oraParameter.Value = CopyStream(bytes, (OracleCommand)command);
					}
				}
				else if (oraParameter.Value is XmlDocument)
				{
					var xmlDocument = (XmlDocument)oraParameter.Value;
					if (xmlDocument.DocumentElement == null)
						oraParameter.Value = DBNull.Value;
					else
					{

						switch (oraParameter.OracleDbType)
						{
							case OracleDbType.XmlType:
								oraParameter.Value = new OracleXmlType((OracleConnection)command.Connection, xmlDocument);
								break;

							// Fix Oracle.Net bug #9: XmlDocument.ToString() returns System.Xml.XmlDocument,
							// so m_value.ToString() is not enought.
							//
							case OracleDbType.Clob:
							case OracleDbType.NClob:
							case OracleDbType.Varchar2:
							case OracleDbType.NVarchar2:
							case OracleDbType.Char:
							case OracleDbType.NChar:
								using (TextWriter w = new StringWriter())
								{
									xmlDocument.Save(w);
									oraParameter.Value = w.ToString();
								}
								break;

							// Or convert to bytes if need.
							//
							case OracleDbType.Blob:
							case OracleDbType.BFile:
							case OracleDbType.Raw:
							case OracleDbType.Long:
							case OracleDbType.LongRaw:
								using (var s = new MemoryStream())
								{
									xmlDocument.Save(s);
									oraParameter.Value = s.GetBuffer();
								}
								break;
						}
					}
				}

				parameter = oraParameter;
			}
			
			base.AttachParameter(command, parameter);
		}

		private static Stream CopyStream(Stream stream, OracleCommand cmd)
		{
			return CopyStream(Common.ConvertOld.ToByteArray(stream), cmd);
		}

		private static Stream CopyStream(Byte[] bytes, OracleCommand cmd)
		{
			var ret = new OracleBlob(cmd.Connection);
			ret.Write(bytes, 0, bytes.Length);
			return ret;
		}

		public override bool IsValueParameter(IDbDataParameter parameter)
		{
			var oraParameter = (parameter is OracleParameterWrap)?
				(parameter as OracleParameterWrap).OracleParameter: parameter as OracleParameter;

			if (null != oraParameter)
			{
				if (oraParameter.OracleDbType == OracleDbType.RefCursor
					&& oraParameter.Direction == ParameterDirection.Output)
				{
					// Ignore out ref cursors, while out parameters of other types are o.k.
					return false;
				}
			}

			return base.IsValueParameter(parameter);
		}

		public override IDbDataParameter CreateParameterObject(IDbCommand command)
		{
			var parameter = base.CreateParameterObject(command);

			if (parameter is OracleParameter)
				parameter = OracleParameterWrap.CreateInstance(parameter as OracleParameter);

			return parameter;
		}

		public override IDbDataParameter GetParameter(IDbCommand command, NameOrIndexParameter nameOrIndex)
		{
			var parameter = base.GetParameter(command, nameOrIndex);

			if (parameter is OracleParameter)
				parameter = OracleParameterWrap.CreateInstance(parameter as OracleParameter);

			return parameter;
		}

		/// <summary>
		/// Returns connection type.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBaseOld)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBaseOld)">AddDataManager Method</seealso>
		/// <value>An instance of the <see cref="Type"/> class.</value>
		public override Type ConnectionType
		{
			get { return typeof(OracleConnection); }
		}

		public const string NameString = LinqToDB.ProviderName.Oracle;

		/// <summary>
		/// Returns the data provider name.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBaseOld)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBaseOld)">AddDataProvider Method</seealso>
		/// <value>Data provider name.</value>
		public override string Name
		{
			get { return NameString; }
		}

		public override int MaxBatchSize
		{
			get { return 0; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new OracleSqlProvider();
		}

		public override IDataReader GetDataReader(MappingSchemaOld schema, IDataReader dataReader)
		{
			return dataReader is OracleDataReader ?
				new OracleDataReaderEx((OracleDataReader)dataReader) :
				base.GetDataReader(schema, dataReader);
		}

		class OracleDataReaderEx: DataReaderEx<OracleDataReader>
		{
			public OracleDataReaderEx(OracleDataReader rd)
				: base(rd)
			{
			}

			public override DateTimeOffset GetDateTimeOffset(int i)
			{
				var ts = DataReader.GetOracleTimeStampTZ(i);
				return new DateTimeOffset(ts.Value, ts.GetTimeZoneOffset());
			}
		}

		private string _parameterPrefix = "P";
		public  string  ParameterPrefix
		{
			get { return _parameterPrefix;  }
			set
			{
				_parameterPrefix = string.IsNullOrEmpty(value)? null:
					value.ToUpper(CultureInfo.InvariantCulture);
			}
		}

		/// <summary>
		/// One time initialization from a configuration file.
		/// </summary>
		/// <param name="attributes">Provider specific attributes.</param>
		public override void Configure(System.Collections.Specialized.NameValueCollection attributes)
		{
			var val = attributes["ParameterPrefix"];
			if (val != null)
				ParameterPrefix = val;

			base.Configure(attributes);
		}

		#region Inner types

		public class OracleMappingSchema : MappingSchemaOld
		{
			public override DataReaderMapper CreateDataReaderMapper(IDataReader dataReader)
			{
				return new OracleDataReaderMapper(this, dataReader);
			}

			#region Convert

			#region Primitive Types

			[CLSCompliant(false)]
			public override SByte ConvertToSByte(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultSByteNullValue: (SByte)oraDecimal.Value;
				}

				return base.ConvertToSByte(value);
			}

			public override Int16 ConvertToInt16(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultInt16NullValue: oraDecimal.ToInt16();
				}

				return base.ConvertToInt16(value);
			}

			public override Int32 ConvertToInt32(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultInt32NullValue: oraDecimal.ToInt32();
				}

				return base.ConvertToInt32(value);
			}

			public override Int64 ConvertToInt64(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultInt64NullValue: oraDecimal.ToInt64();
				}

				return base.ConvertToInt64(value);
			}

			public override Byte ConvertToByte(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultByteNullValue: oraDecimal.ToByte();
				}

				return base.ConvertToByte(value);
			}

			[CLSCompliant(false)]
			public override UInt16 ConvertToUInt16(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultUInt16NullValue: (UInt16)oraDecimal.Value;
				}

				return base.ConvertToUInt16(value);
			}

			[CLSCompliant(false)]
			public override UInt32 ConvertToUInt32(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultUInt32NullValue: (UInt32)oraDecimal.Value;
				}

				return base.ConvertToUInt32(value);
			}

			[CLSCompliant(false)]
			public override UInt64 ConvertToUInt64(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultUInt64NullValue: (UInt64)oraDecimal.Value;
				}

				return base.ConvertToUInt64(value);
			}

			public override Single ConvertToSingle(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultSingleNullValue: oraDecimal.ToSingle();
				}

				return base.ConvertToSingle(value);
			}

			public override Double ConvertToDouble(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultDoubleNullValue: oraDecimal.ToDouble();
				}

				return base.ConvertToDouble(value);
			}

			public override Boolean ConvertToBoolean(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultBooleanNullValue: (oraDecimal.Value != 0);
				}

				return base.ConvertToBoolean(value);
			}

			public override DateTime ConvertToDateTime(object value)
			{
				if (value is OracleDate)
				{
					var oraDate = (OracleDate)value;
					return oraDate.IsNull? DefaultDateTimeNullValue: oraDate.Value;
				}

				return base.ConvertToDateTime(value);
			}

			public override Decimal ConvertToDecimal(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? DefaultDecimalNullValue: oraDecimal.Value;
				}

				return base.ConvertToDecimal(value);
			}

			public override Guid ConvertToGuid(object value)
			{
				if (value is OracleString)
				{
					var oraString = (OracleString)value;
					return oraString.IsNull? DefaultGuidNullValue: new Guid(oraString.Value);
				}

				if (value is OracleBlob)
				{
					var oraBlob = (OracleBlob)value;
					return oraBlob.IsNull? DefaultGuidNullValue: new Guid(oraBlob.Value);
				}

				return base.ConvertToGuid(value);
			}

			public override String ConvertToString(object value)
			{
				if (value is OracleString)
				{
					var oraString = (OracleString)value;
					return oraString.IsNull? DefaultStringNullValue: oraString.Value;
				}

				if (value is OracleXmlType)
				{
					var oraXmlType = (OracleXmlType)value;
					return oraXmlType.IsNull ? DefaultStringNullValue : oraXmlType.Value;
				}

				if (value is OracleClob)
				{
					var oraClob = (OracleClob)value;
					return oraClob.IsNull? DefaultStringNullValue: oraClob.Value;
				}

				return base.ConvertToString(value);
			}


			public override Stream ConvertToStream(object value)
			{
				if (value is OracleXmlType)
				{
					var oraXml = (OracleXmlType)value;
					return oraXml.IsNull? DefaultStreamNullValue: oraXml.GetStream();
				}

				return base.ConvertToStream(value);
			}

			public override XmlReader ConvertToXmlReader(object value)
			{
				if (value is OracleXmlType)
				{
					var oraXml = (OracleXmlType)value;
					return oraXml.IsNull? DefaultXmlReaderNullValue: oraXml.GetXmlReader();
				}

				return base.ConvertToXmlReader(value);
			}

			public override XmlDocument ConvertToXmlDocument(object value)
			{
				if (value is OracleXmlType)
				{
					var oraXml = (OracleXmlType)value;
					return oraXml.IsNull? DefaultXmlDocumentNullValue: oraXml.GetXmlDocument();
				}

				return base.ConvertToXmlDocument(value);
			}

			public override Byte[] ConvertToByteArray(object value)
			{
				if (value is OracleBlob)
				{
					var oraBlob = (OracleBlob)value;
					return oraBlob.IsNull? null: oraBlob.Value;
				}

				if (value is OracleBinary)
				{
					var oraBinary = (OracleBinary)value;
					return oraBinary.IsNull? null: oraBinary.Value;
				}
				
				if (value is OracleBFile)
				{
					var oraBFile = (OracleBFile)value;
					return oraBFile.IsNull? null: oraBFile.Value;
				}

				return base.ConvertToByteArray(value);
			}

			public override Char[] ConvertToCharArray(object value)
			{
				if (value is OracleString)
				{
					var oraString = (OracleString)value;
					return oraString.IsNull? null: oraString.Value.ToCharArray();
				}

				if (value is OracleClob)
				{
					var oraClob = (OracleClob)value;
					return oraClob.IsNull? null: oraClob.Value.ToCharArray();
				}

				return base.ConvertToCharArray(value);
			}

			#endregion

			#region Nullable Types

			[CLSCompliant(false)]
			public override SByte? ConvertToNullableSByte(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (SByte?)oraDecimal.Value;
				}

				return base.ConvertToNullableSByte(value);
			}

			public override Int16? ConvertToNullableInt16(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (Int16?)oraDecimal.ToInt16();
				}

				return base.ConvertToNullableInt16(value);
			}

			public override Int32? ConvertToNullableInt32(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (Int32?)oraDecimal.ToInt32();
				}

				return base.ConvertToNullableInt32(value);
			}

			public override Int64? ConvertToNullableInt64(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (Int64?)oraDecimal.ToInt64();
				}

				return base.ConvertToNullableInt64(value);
			}

			public override Byte? ConvertToNullableByte(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (Byte?)oraDecimal.ToByte();
				}

				return base.ConvertToNullableByte(value);
			}

			[CLSCompliant(false)]
			public override UInt16? ConvertToNullableUInt16(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (UInt16?)oraDecimal.Value;
				}

				return base.ConvertToNullableUInt16(value);
			}

			[CLSCompliant(false)]
			public override UInt32? ConvertToNullableUInt32(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (UInt32?)oraDecimal.Value;
				}

				return base.ConvertToNullableUInt32(value);
			}

			[CLSCompliant(false)]
			public override UInt64? ConvertToNullableUInt64(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (UInt64?)oraDecimal.Value;
				}

				return base.ConvertToNullableUInt64(value);
			}

			public override Single? ConvertToNullableSingle(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (Single?)oraDecimal.ToSingle();
				}

				return base.ConvertToNullableSingle(value);
			}

			public override Double? ConvertToNullableDouble(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (Double?)oraDecimal.ToDouble();
				}

				return base.ConvertToNullableDouble(value);
			}

			public override Boolean? ConvertToNullableBoolean(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (Boolean?)(oraDecimal.Value != 0);
				}

				return base.ConvertToNullableBoolean(value);
			}

			public override DateTime? ConvertToNullableDateTime(object value)
			{
				if (value is OracleDate)
				{
					var oraDate = (OracleDate)value;
					return oraDate.IsNull? null: (DateTime?)oraDate.Value;
				}

				return base.ConvertToNullableDateTime(value);
			}

			public override Decimal? ConvertToNullableDecimal(object value)
			{
				if (value is OracleDecimal)
				{
					var oraDecimal = (OracleDecimal)value;
					return oraDecimal.IsNull? null: (Decimal?)oraDecimal.Value;
				}

				return base.ConvertToNullableDecimal(value);
			}

			public override Guid? ConvertToNullableGuid(object value)
			{
				if (value is OracleString)
				{
					var oraString = (OracleString)value;
					return oraString.IsNull? null: (Guid?)new Guid(oraString.Value);
				}

				if (value is OracleBlob)
				{
					var oraBlob = (OracleBlob)value;
					return oraBlob.IsNull? null: (Guid?)new Guid(oraBlob.Value);
				}

				return base.ConvertToNullableGuid(value);
			}

			#endregion

			#endregion

			public override object MapValueToEnum(object value, Type type)
			{
				if (value is OracleString)
				{
					var oracleValue = (OracleString)value;
					value = oracleValue.IsNull? null: oracleValue.Value;
				}
				else if (value is OracleDecimal)
				{
					var oracleValue = (OracleDecimal)value;
					if (oracleValue.IsNull)
						value = null;
					else 
						value = oracleValue.Value;
				}

				return base.MapValueToEnum(value, type);
			}

			public override object ConvertChangeType(object value, Type conversionType)
			{
				// Handle OracleDecimal with IsNull == true case
				//
				return base.ConvertChangeType(IsNull(value)? null: value, conversionType);
			}

			public override bool IsNull(object value)
			{
				// ODP 10 does not expose this interface to public.
				//
				// return value is INullable && ((INullable)value).IsNull;

				return
					value is OracleDecimal?      ((OracleDecimal)     value).IsNull:
					value is OracleString?       ((OracleString)      value).IsNull:
					value is OracleDate?         ((OracleDate)        value).IsNull:
					value is OracleTimeStamp?    ((OracleTimeStamp)   value).IsNull:
					value is OracleTimeStampTZ?  ((OracleTimeStampTZ) value).IsNull:
					value is OracleTimeStampLTZ? ((OracleTimeStampLTZ)value).IsNull:
					value is OracleXmlType?      ((OracleXmlType)     value).IsNull:
					value is OracleBlob?         ((OracleBlob)        value).IsNull:
					value is OracleClob?         ((OracleClob)        value).IsNull:
					value is OracleBFile?        ((OracleBFile)       value).IsNull:
					value is OracleBinary?       ((OracleBinary)      value).IsNull:
					value is OracleIntervalDS?   ((OracleIntervalDS)  value).IsNull:
					value is OracleIntervalYM?   ((OracleIntervalYM)  value).IsNull:
						base.IsNull(value);
			}
		}

		// TODO: implement via IDataReaderEx / DataReaderEx
		//
		public class OracleDataReaderMapper : DataReaderMapper
		{
			public OracleDataReaderMapper(MappingSchemaOld mappingSchema, IDataReader dataReader)
				: base(mappingSchema, dataReader)
			{
				_dataReader = dataReader is OracleDataReaderEx?
					((OracleDataReaderEx)dataReader).DataReader:
					(OracleDataReader)dataReader;
			}

			private readonly OracleDataReader _dataReader;

			public override Type GetFieldType(int index)
			{
				var fieldType = _dataReader.GetProviderSpecificFieldType(index);

				if (fieldType != typeof(OracleXmlType) && fieldType != typeof(OracleBlob))
					fieldType = _dataReader.GetFieldType(index);

				return fieldType;
			}

			public override object GetValue(object o, int index)
			{
				var fieldType = _dataReader.GetProviderSpecificFieldType(index);

				if (fieldType == typeof(OracleXmlType))
				{
					var xml = _dataReader.GetOracleXmlType(index);
					return MappingSchema.ConvertToXmlDocument(xml);
				}

				if (fieldType == typeof(OracleBlob))
				{
					var blob = _dataReader.GetOracleBlob(index);
					return MappingSchema.ConvertToStream(blob);
				}

				return _dataReader.IsDBNull(index)? null:
					_dataReader.GetValue(index);
			}

			public override Boolean  GetBoolean(object o, int index) { return MappingSchema.ConvertToBoolean(GetValue(o, index)); }
			public override Char     GetChar   (object o, int index) { return MappingSchema.ConvertToChar   (GetValue(o, index)); }
			public override Guid     GetGuid   (object o, int index) { return MappingSchema.ConvertToGuid   (GetValue(o, index)); }

			[CLSCompliant(false)]
			public override SByte    GetSByte  (object o, int index) { return  (SByte)_dataReader.GetDecimal(index); }
			[CLSCompliant(false)]
			public override UInt16   GetUInt16 (object o, int index) { return (UInt16)_dataReader.GetDecimal(index); }
			[CLSCompliant(false)]
			public override UInt32   GetUInt32 (object o, int index) { return (UInt32)_dataReader.GetDecimal(index); }
			[CLSCompliant(false)]
			public override UInt64   GetUInt64 (object o, int index) { return (UInt64)_dataReader.GetDecimal(index); }

			public override Decimal  GetDecimal(object o, int index) { return OracleDecimal.SetPrecision(_dataReader.GetOracleDecimal(index), 28).Value; }

			public override Boolean? GetNullableBoolean(object o, int index) { return MappingSchema.ConvertToNullableBoolean(GetValue(o, index)); }
			public override Char?    GetNullableChar   (object o, int index) { return MappingSchema.ConvertToNullableChar   (GetValue(o, index)); }
			public override Guid?    GetNullableGuid   (object o, int index) { return MappingSchema.ConvertToNullableGuid   (GetValue(o, index)); }

			[CLSCompliant(false)]
			public override SByte?   GetNullableSByte  (object o, int index) { return _dataReader.IsDBNull(index)? null:  (SByte?)_dataReader.GetDecimal(index); }
			[CLSCompliant(false)]
			public override UInt16?  GetNullableUInt16 (object o, int index) { return _dataReader.IsDBNull(index)? null: (UInt16?)_dataReader.GetDecimal(index); }
			[CLSCompliant(false)]
			public override UInt32?  GetNullableUInt32 (object o, int index) { return _dataReader.IsDBNull(index)? null: (UInt32?)_dataReader.GetDecimal(index); }
			[CLSCompliant(false)]
			public override UInt64?  GetNullableUInt64 (object o, int index) { return _dataReader.IsDBNull(index)? null: (UInt64?)_dataReader.GetDecimal(index); }

			public override Decimal? GetNullableDecimal(object o, int index) { return _dataReader.IsDBNull(index)? (decimal?)null: OracleDecimal.SetPrecision(_dataReader.GetOracleDecimal(index), 28).Value; }
		}

		[CLSCompliant(false)]
		public class OracleParameterWrap : IDbDataParameter, IDisposable, ICloneable
		{
			protected OracleParameter _oracleParameter;
			public    OracleParameter  OracleParameter
			{
				get { return _oracleParameter; }
			}

			public static IDbDataParameter CreateInstance(OracleParameter oraParameter)
			{
				var wrap = TypeAccessor<OracleParameterWrap>.CreateInstanceEx();

				wrap._oracleParameter = oraParameter;

				return (IDbDataParameter)wrap;
			}

			public override string ToString()
			{
				return _oracleParameter.ToString();
			}

			#region IDbDataParameter Members

			byte IDbDataParameter.Precision
			{
				get { return _oracleParameter.Precision;  }
				set { _oracleParameter.Precision = value; }
			}

			byte IDbDataParameter.Scale
			{
				get { return _oracleParameter.Scale;  }
				set { _oracleParameter.Scale = value; }
			}

			int IDbDataParameter.Size
			{
				get { return _oracleParameter.Size;  }
				set { _oracleParameter.Size = value; }
			}

			#endregion

			#region IDataParameter Members

			DbType IDataParameter.DbType
			{
				get { return _oracleParameter.DbType;  }
				set { _oracleParameter.DbType = value; }
			}

			ParameterDirection IDataParameter.Direction
			{
				get { return _oracleParameter.Direction;  }
				set { _oracleParameter.Direction = value; }
			}

			bool IDataParameter.IsNullable
			{
				get { return _oracleParameter.IsNullable; }
			}

			string IDataParameter.ParameterName
			{
				get { return _oracleParameter.ParameterName;  }
				set { _oracleParameter.ParameterName = value; }
			}

			string IDataParameter.SourceColumn
			{
				get { return _oracleParameter.SourceColumn;  }
				set { _oracleParameter.SourceColumn = value; }
			}

			DataRowVersion IDataParameter.SourceVersion
			{
				get { return _oracleParameter.SourceVersion;  }
				set { _oracleParameter.SourceVersion = value; }
			}

			///<summary>
			///Gets or sets the value of the parameter.
			///</summary>
			///<returns>
			///An <see cref="T:System.Object"/> that is the value of the parameter.
			///The default value is null.
			///</returns>
			object IDataParameter.Value
			{
#if CONVERTORACLETYPES
				[MixinOverride]
				get
				{
					object value = _oracleParameter.Value;
					if (value is OracleBinary)
					{
						OracleBinary oracleValue = (OracleBinary)value;
						return oracleValue.IsNull? null: oracleValue.Value;
					}
					if (value is OracleDate)
					{
						OracleDate oracleValue = (OracleDate)value;
						if (oracleValue.IsNull)
							return null;
						return oracleValue.Value;
					}
					if (value is OracleDecimal)
					{
						OracleDecimal oracleValue = (OracleDecimal)value;
						if (oracleValue.IsNull)
							return null;
						return oracleValue.Value;
					}
					if (value is OracleIntervalDS)
					{
						OracleIntervalDS oracleValue = (OracleIntervalDS)value;
						if (oracleValue.IsNull)
							return null;
						return oracleValue.Value;
					}
					if (value is OracleIntervalYM)
					{
						OracleIntervalYM oracleValue = (OracleIntervalYM)value;
						if (oracleValue.IsNull)
							return null;
						return oracleValue.Value;
					}
					if (value is OracleString)
					{
						OracleString oracleValue = (OracleString)value;
						return oracleValue.IsNull? null: oracleValue.Value;
					}
					if (value is OracleTimeStamp)
					{
						OracleTimeStamp oracleValue = (OracleTimeStamp)value;
						if (oracleValue.IsNull)
							return null;
						return oracleValue.Value;
					}
					if (value is OracleTimeStampLTZ)
					{
						OracleTimeStampLTZ oracleValue = (OracleTimeStampLTZ)value;
						if (oracleValue.IsNull)
							return null;
						return oracleValue.Value;
					}
					if (value is OracleTimeStampTZ)
					{
						OracleTimeStampTZ oracleValue = (OracleTimeStampTZ)value;
						if (oracleValue.IsNull)
							return null;
						return oracleValue.Value;
					}
					if (value is OracleXmlType)
					{
						OracleXmlType oracleValue = (OracleXmlType)value;
						return oracleValue.IsNull? null: oracleValue.Value;
					}

					return value;
				}
#else
				get { return _oracleParameter.Value; }
#endif
				set
				{
					if (null != value)
					{
						if (value is Guid)
						{
							// Fix Oracle.Net bug #6: guid type is not handled
							//
							value = ((Guid)value).ToByteArray();
						}
						else if (value is Array && !(value is byte[] || value is char[]))
						{
							_oracleParameter.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
						}
						else if (value is IConvertible)
						{
							var convertible = (IConvertible)value;
							var typeCode   = convertible.GetTypeCode();

							switch (typeCode)
							{
								case TypeCode.Boolean:
									// Fix Oracle.Net bug #7: bool type is handled wrong
									//
									value = convertible.ToByte(null);
									break;

								case TypeCode.SByte:
								case TypeCode.UInt16:
								case TypeCode.UInt32:
								case TypeCode.UInt64:
									// Fix Oracle.Net bug #8: some integer types are handled wrong
									//
									value = convertible.ToDecimal(null);
									break;

									// Fix Oracle.Net bug #10: zero-length string can not be converted to
									// ORAXML type, but null value can be.
									//
								case TypeCode.String:
									if (((string)value).Length == 0)
										value = null;
									break;

								default:
									// Fix Oracle.Net bug #5: Enum type is not handled
									//
									if (value is Enum)
									{
										// Convert a Enum value to it's underlying type.
										//
										value = System.Convert.ChangeType(value, typeCode);
									}
									break;
							}
						}
					}

					_oracleParameter.Value = value;
				}
			}

			#endregion

			#region IDisposable Members

			void IDisposable.Dispose()
			{
				_oracleParameter.Dispose();
			}

			#endregion

			#region ICloneable Members

			object ICloneable.Clone()
			{
				return _oracleParameter.Clone();
			}

			#endregion
		}

		#endregion

		#region InsertBatch

		public override int InsertBatch<T>(
			DbManager      db,
			string         insertText,
			IEnumerable<T> collection,
			MemberMapper[] members,
			int            maxBatchSize,
			DbManager.ParameterProvider<T> getParameters)
		{
			var sb  = new StringBuilder();
			var sp  = new OracleSqlProvider();
			var n   = 0;
			var cnt = 0;
			var str = "\t" + insertText
				.Substring(0, insertText.IndexOf(") VALUES ("))
				.Substring(7)
				.Replace("\r", "")
				.Replace("\n", "")
				.Replace("\t", " ")
				.Replace("( ", "(")
				//.Replace("  ", " ")
				+ ") VALUES (";

			foreach (var item in collection)
			{
				if (sb.Length == 0)
					sb.AppendLine("INSERT ALL");

				sb.Append(str);

				foreach (var member in members)
				{
					var value = member.GetValue(item);

					if (value is Nullable<DateTime>)
						value = ((DateTime?)value).Value;

					if (value is DateTime)
					{
						var dt = (DateTime)value;
						sb.Append(string.Format("to_timestamp('{0:dd.MM.yyyy HH:mm:ss.ffffff}', 'DD.MM.YYYY HH24:MI:SS.FF6')", dt));
					}
					else
						sp.BuildValue(sb, value);

					sb.Append(", ");
				}

				sb.Length -= 2;
				sb.AppendLine(")");

				n++;

				if (n >= maxBatchSize)
				{
					sb.AppendLine("SELECT * FROM dual");

					var sql = sb.ToString();

					if (DbManager.TraceSwitch.TraceInfo)
						DbManager.WriteTraceLine("\n" + sql.Replace("\r", ""), DbManager.TraceSwitch.DisplayName);

					cnt += db.SetCommand(sql).ExecuteNonQuery();

					n = 0;
					sb.Length = 0;
				}
			}

			if (n > 0)
			{
				sb.AppendLine("SELECT * FROM dual");

				var sql = sb.ToString();

				if (DbManager.TraceSwitch.TraceInfo)
					DbManager.WriteTraceLine("\n" + sql.Replace("\r", ""), DbManager.TraceSwitch.DisplayName);

				cnt += db.SetCommand(sql).ExecuteNonQuery();
			}

			return cnt;
		}

		#endregion
	}
}
