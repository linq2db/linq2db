using System;
using System.IO;
using Android.App;
using Android.Content.PM;
using NUnit.Runner.Services;

namespace Tests.Android
{
	[Activity(Label = "NUnit", Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo.Light", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		private const string SQLITE_DB = "TestData.sqlite";

		public static string SQLiteDbPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), SQLITE_DB);

		protected override void OnCreate(global::Android.OS.Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			using (var binaryReader = new BinaryReader(Assets.Open(SQLITE_DB)))
			{
				using (var binaryWriter = new BinaryWriter(new FileStream(SQLiteDbPath, FileMode.Create)))
				{
					byte[] buffer = new byte[2048];
					int length = 0;
					while ((length = binaryReader.Read(buffer, 0, buffer.Length)) > 0)
					{
						binaryWriter.Write(buffer, 0, length);
					}
				}

			}

			global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

			// This will load all tests within the current project
			var nunit = new NUnit.Runner.App();

			// If you want to add tests in another assembly
			//nunit.AddTestAssembly(typeof(MyTests).Assembly);

			// Available options for testing
			nunit.Options = new TestOptions
			{
				// If True, the tests will run automatically when the app starts
				// otherwise you must run them manually.
				AutoRun = true,

				// If True, the application will terminate automatically after running the tests.
				//TerminateAfterExecution = true,

				// Information about the tcp listener host and port.
				// For now, send result as XML to the listening server.
				//TcpWriterParameters = new TcpWriterInfo("192.168.0.108", 13000),

				// Creates a NUnit Xml result file on the host file system using PCLStorage library.
				// CreateXmlResultFile = true,

				// Choose a different path for the xml result file
				// ResultFilePath = Path.Combine(Environment.ExternalStorageDirectory.Path, Environment.DirectoryDownloads, "Nunit", "Results.xml")
			};

			LoadApplication(nunit);
		}
	}
}

