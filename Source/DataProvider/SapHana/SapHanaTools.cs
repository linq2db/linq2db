using System;

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
            catch
            {
            }
            DataConnection.AddDataProvider(_hanaDataProvider);			
		}

		public static IDataProvider GetDataProvider()
		{
			return _hanaDataProvider;
		}

	}
}
