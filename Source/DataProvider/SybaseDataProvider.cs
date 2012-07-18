using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

using Sybase.Data.AseClient;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	public class SybaseDataProvider : DataProviderBaseOld
	{
		public override IDbConnection CreateConnectionObject()
		{
			return new AseConnection();
		}

		public override DbDataAdapter CreateDataAdapterObject()
		{
			return new AseDataAdapter();
		}

		public override bool DeriveParameters(IDbCommand command)
		{
			AseCommandBuilder.DeriveParameters((AseCommand)command);
			return true;
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.ExceptionToErrorNumber:
					if (value is AseException)
					{
						var ex = (AseException)value;

						foreach (AseError error in ex.Errors)
							if (error.IsError)
								return error.MessageNumber;

						foreach (AseError error in ex.Errors)
							if (error.MessageNumber != 0)
								return error.MessageNumber;

						return 0;
					}

					break;

				case ConvertType.ExceptionToErrorMessage:
					if (value is AseException)
					{
						try
						{
							var ex = (AseException)value;
							var sb = new StringBuilder();

							foreach (AseError error in ex.Errors)
								if (error.IsError)
									sb.AppendFormat("{0} Ln: {1}{2}",
										error.Message.TrimEnd('\n', '\r'), error.LineNum, Environment.NewLine);

							foreach (AseError error in ex.Errors)
								if (!error.IsError)
									sb.AppendFormat("* {0}{1}", error.Message, Environment.NewLine);

							return sb.Length == 0 ? ex.Message : sb.ToString();
						}
						catch
						{
						}
					}

					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		public override void AttachParameter(IDbCommand command, IDbDataParameter parameter)
		{
			if (parameter.Value is string && parameter.DbType == DbType.Guid)
				parameter.DbType = DbType.AnsiString;

			base.AttachParameter(command, parameter);
			
			var p = (AseParameter)parameter;

			if (p.AseDbType == AseDbType.Unsupported && p.Value is DBNull)
				parameter.DbType = DbType.AnsiString;
		}

		public override Type ConnectionType
		{
			get { return typeof(AseConnection); }
		}

		public override string Name
		{
			get { return LinqToDB.ProviderName.Sybase; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new SybaseSqlProvider();
		}

		public override bool InitParameter(IDbDataParameter parameter)
		{
			if (parameter.Value is Guid)
			{
				parameter.Value  = parameter.Value.ToString();
				parameter.DbType = DbType.StringFixedLength;
				parameter.Size   = 36;

				return true;
			}

			return false;
		}

		public override void PrepareCommand(ref CommandType commandType, ref string commandText, ref IDbDataParameter[] commandParameters)
		{
			base.PrepareCommand(ref commandType, ref commandText, ref commandParameters);

			List<IDbDataParameter> list = null;

			if (commandParameters != null) for (var i = 0; i < commandParameters.Length; i++)
			{
				var p = commandParameters[i];

				if (p.Value is Guid)
				{
					p.Value  = p.Value.ToString();
					p.DbType = DbType.StringFixedLength;
					p.Size   = 36;
				}

				if (commandType == CommandType.Text)
				{
					if (commandText.IndexOf(p.ParameterName) < 0)
					{
						if (list == null)
						{
							list = new List<IDbDataParameter>(commandParameters.Length);

							for (var j = 0; j < i; j++)
								list.Add(commandParameters[j]);
						}
					}
					else
					{
						if (list != null)
							list.Add(p);
					}
				}
			}

			if (list != null)
				commandParameters = list.ToArray();
		}

		public override DbType GetDbType(Type systemType)
		{
			if (systemType == typeof(byte[]))
				return DbType.Object;

			return base.GetDbType(systemType);
		}

		#region DataReaderEx

		public override IDataReader GetDataReader(MappingSchemaOld schema, IDataReader dataReader)
		{
			return dataReader is AseDataReader?
				new DataReaderEx((AseDataReader)dataReader):
				base.GetDataReader(schema, dataReader);
		}

		class DataReaderEx : DataReaderBase<AseDataReader>, IDataReader
		{
			public DataReaderEx(AseDataReader rd): base(rd)
			{
			}

			public new object GetValue(int i)
			{
				var value = DataReader.GetValue(i);

				if (value is DateTime)
				{
					var dt = (DateTime)value;

					if (dt.Year == 1900 && dt.Month == 1 && dt.Day == 1)
						return new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
				}

				return value;
			}

			public new DateTime GetDateTime(int i)
			{
				var dt = DataReader.GetDateTime(i);

				if (dt.Year == 1900 && dt.Month == 1 && dt.Day == 1)
					return new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);

				return dt;
			}
		}

		#endregion
	}
}
