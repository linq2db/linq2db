using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Xml;

// System.Data.SQLite.dll must be referenced.
// http://sqlite.phxsoftware.com/
//
using Mono.Data.Sqlite;

namespace BLToolkit.Data.DataProvider
{
	/// <summary>
	/// Implements access to the Data Provider for SQLite.
	/// </summary>
	/// <remarks>
	/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
	/// </remarks>
	/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
	public sealed class SQLiteDataProvider: DataProviderBase
	{
		/// <summary>
		/// Creates the database connection object.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <returns>The database connection object.</returns>
		public override IDbConnection CreateConnectionObject()
		{
			return new SqliteConnection();
		}

		/// <summary>
		/// Creates the data adapter object.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <returns>A data adapter object.</returns>
		public override DbDataAdapter CreateDataAdapterObject()
		{
			return new SqliteDataAdapter();
		}

		/// <summary>
		/// Populates the specified IDbCommand object's Parameters collection with 
		/// parameter information for the stored procedure specified in the IDbCommand.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <param name="command">The IDbCommand referencing the stored procedure for which the parameter information is to be derived. The derived parameters will be populated into the Parameters of this command.</param>
		public override bool DeriveParameters(IDbCommand command)
		{
			// SQLiteCommandBuilder does not implement DeriveParameters.
			// This is not surprising, since SQLite has no support for stored procs.
			//
			return false;
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToParameter:
					return "@" + value;

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryTable:
				{
					string name = (string)value;

					if (name.Length > 0 && name[0] == '[')
						return value;

					if (name.IndexOf('.') > 0)
						value = string.Join("].[", name.Split('.'));

					return "[" + value + "]";
				}

				case ConvertType.ParameterToName:
				{
					string name = (string)value;
					return name.Length > 0 && name[0] == '@'? name.Substring(1): name;
				}

				case ConvertType.ExceptionToErrorNumber:
				{
					if (value is SqliteException)
						return ((SqliteException)value).ErrorCode;
					break;
				}
			}

			return value;
		}

		public override void AttachParameter(IDbCommand command, IDbDataParameter parameter)
		{
			if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput)
			{
				if (parameter.Value is XmlDocument)
				{
					parameter.Value = Encoding.UTF8.GetBytes(((XmlDocument) parameter.Value).InnerXml);
					parameter.DbType = DbType.Binary;
				}
			}

			base.AttachParameter(command, parameter);
		}

		/// <summary>
		/// Returns connection type.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <value>An instance of the <see cref="Type"/> class.</value>
		public override Type ConnectionType
		{
			get { return typeof(SqliteConnection); }
		}

		/// <summary>
		/// Returns the data provider name.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataProvider Method</seealso>
		/// <value>Data provider name.</value>
		public override string Name
		{
			get { return "SQLite"; }
		}

		public class SQLiteMappingSchema : Mapping.MappingSchema
		{
			#region Convert

			public override XmlReader ConvertToXmlReader(object value)
			{
				if (value is byte[])
					value = Encoding.UTF8.GetString((byte[])value);

				return base.ConvertToXmlReader(value);
			}

			public override XmlDocument ConvertToXmlDocument(object value)
			{
				if (value is byte[])
					value = Encoding.UTF8.GetString((byte[])value);

				return base.ConvertToXmlDocument(value);
			}

			#endregion
		}
	}
}
