using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;
using System.Xml;

using LinqToDB.Data.Sql.SqlProvider;
using LinqToDB.Mapping;
// System.Data.SQLite.dll must be referenced.
// http://sqlite.phxsoftware.com/
//

namespace LinqToDB.Data.DataProvider
{
	/// <summary>
	/// Implements access to the Data Provider for SQLite.
	/// </summary>
	/// <remarks>
	/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
	/// </remarks>
	/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
	public sealed class SQLiteDataProvider : DataProviderBase
	{
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
			get { return typeof (SQLiteConnection); }
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
			get { return DataProvider.ProviderName.SQLite; }
		}

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
			return new SQLiteConnection();
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
			return new SQLiteDataAdapter();
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
				case ConvertType.ExceptionToErrorNumber:
					{
						if (value is SQLiteException)
							return ((SQLiteException) value).ErrorCode;
						break;
					}
			}

			return SqlProvider.Convert(value, convertType);
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

		public override ISqlProvider CreateSqlProvider()
		{
			return new SQLiteSqlProvider();
		}

		#region Nested type: LoverFunction
		/// <summary>
		/// SQLite built-in text processor is ANSI-only  Just override it.
		/// </summary>
		[SQLiteFunction(Name = "lower", Arguments = 1, FuncType = FunctionType.Scalar)]
		internal class LoverFunction : SQLiteFunction
		{
			public override object Invoke(object[] args)
			{
				Debug.Assert(args != null && args.Length == 1);
				var arg = args[0];

				Debug.Assert(arg is string || arg is DBNull || arg is byte[]);
				return
					arg is string
						? ((string) arg).ToLower()
						: arg is byte[]
						  	? Encoding.UTF8.GetString((byte[]) arg).ToLower()
						  	: arg;
			}
		}
		#endregion

		#region Nested type: SQLiteMappingSchema
		public class SQLiteMappingSchema : MappingSchema
		{
			#region Convert
			public override XmlReader ConvertToXmlReader(object value)
			{
				if (value is byte[])
					value = Encoding.UTF8.GetString((byte[]) value);

				return base.ConvertToXmlReader(value);
			}

			public override XmlDocument ConvertToXmlDocument(object value)
			{
				if (value is byte[])
					value = Encoding.UTF8.GetString((byte[]) value);

				return base.ConvertToXmlDocument(value);
			}
			#endregion
		}
		#endregion

		#region Nested type: UpperFunction
		/// <summary>
		/// SQLite built-in text processor is ANSI-only  Just override it.
		/// </summary>
		[SQLiteFunction(Name = "upper", Arguments = 1, FuncType = FunctionType.Scalar)]
		internal class UpperFunction : SQLiteFunction
		{
			public override object Invoke(object[] args)
			{
				Debug.Assert(args != null && args.Length == 1);
				var arg = args[0];

				Debug.Assert(arg is string || arg is DBNull || arg is byte[]);
				return
					arg is string
						? ((string) arg).ToUpper()
						: arg is byte[]
						  	? Encoding.UTF8.GetString((byte[]) arg).ToUpper()
						  	: arg;
			}
		}
		#endregion
	}
}