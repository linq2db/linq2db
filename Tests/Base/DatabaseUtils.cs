using System;
using System.IO;

using NUnit.Framework;

namespace Tests
{
	public static class DatabaseUtils
	{
		public static void CopyDatabases()
		{
			var databasePath = Path.GetFullPath("Database");
			var dataPath     = Path.Combine(databasePath, "Data");

			TestExternals.Log($"Copy databases {databasePath} => {dataPath}");

			if (TestExternals.IsParallelRun && TestExternals.Configuration != null)
			{
				try
				{
					foreach (var file in Directory.GetFiles(databasePath, "*.*"))
					{
						var fileName = Path.GetFileName(file);

						switch (TestExternals.Configuration, fileName)
						{
							case ("Access.Data", "TestData.mdb"):
							case ("SqlCe.Data", "TestData.sdf"):
							case ("SQLite.Classic.Data", "TestData.sqlite"):
							case ("SQLite.MS.Data", "TestData.MS.sqlite"):
							{
								var destination = Path.Combine(dataPath, Path.GetFileName(file));

								TestExternals.Log($"{file} => {destination}");
								File.Copy(file, destination, true);

								break;
							}
						}
					}
				}
				catch (Exception e)
				{
					TestExternals.Log(e.ToString());
					TestContext.WriteLine(e);
					throw;
				}
			}
			else
			{
				if (Directory.Exists(dataPath))
					Directory.Delete(dataPath, true);

				Directory.CreateDirectory(dataPath);

				foreach (var file in Directory.GetFiles(databasePath, "*.*"))
				{
					var destination = Path.Combine(dataPath, Path.GetFileName(file));
					TestContext.WriteLine("{0} => {1}", file, destination);
					File.Copy(file, destination, true);
				}
			}
		}

	}
}
