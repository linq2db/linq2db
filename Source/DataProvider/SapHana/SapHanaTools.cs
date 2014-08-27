using System;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SapHana
{
    using System.IO;

    using Data;

	public static class SapHanaTools
	{
        public static string AssemblyName = "Sap.Data.Hana";

        static readonly SapHanaDataProvider _hanaDataProvider = new SapHanaDataProvider();
		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

        static SapHanaTools()
		{
            try
            {
                var path = typeof(SapHanaTools).Assembly.CodeBase.Replace("file:///", "");
                path = Path.GetDirectoryName(path);
                if (!String.IsNullOrEmpty(path))
                {
                    if (!File.Exists(Path.Combine(path, AssemblyName + ".dll")))
                    {
                        if (File.Exists(Path.Combine(path, AssemblyName +".v4.5.dll")))
                            AssemblyName += ".v4.5";
                    }
                }
            }
            catch (Exception)
            {
            }
			DataConnection.AddDataProvider(_hanaDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _hanaDataProvider;
		}

		public static void ResolveSapHana([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException("path");
			new AssemblyResolver(path, AssemblyName);
		}

		public static void ResolveSapHana([NotNull] Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");
            new AssemblyResolver(assembly, AssemblyName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_hanaDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_hanaDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_hanaDataProvider, transaction);
		}

		#endregion
	}
}
