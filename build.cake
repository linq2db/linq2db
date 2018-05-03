#addin "Cake.DocFx"
#addin "Cake.Git"
#tool "docfx.console"

string GetSolutionName()
{
	return "./linq2db.sln";
}

string GetNugetProject()
{
	return "./Source/LinqToDB/LinqToDB.csproj";
}

ConvertableDirectoryPath GetPackPath()
{
	return Directory("./Source/LinqToDB/LinqToDB.csproj");
}

ConvertableDirectoryPath GetBuildArtifacts()
{
	return Directory("./artifacts/packages");
}


bool IsAppVeyorBuild()
{
	return EnvironmentVariable("APPVEYOR") != null;
}

bool IsRelease()
{
	if(Argument<string>("release", null) != null)
		return true;

	if (IsAppVeyorBuild())
		return AppVeyor.Environment.Repository.Branch.ToLower().StartsWith("release");

	return false;
}

string GetTarget()
{
	var e = EnvironmentVariable("target")
		?? Argument<string>("t", null)
		?? "Default";

	Console.WriteLine("Target: {0}", e);

	return e;
}

string GetConfiguration()
{
	var e = EnvironmentVariable("configuration")
		??  Argument<string>("c", "Release");

	Console.WriteLine("Configuration: {0}", e);

	return e;
}

string GetPackageVersion()
{
	var e = EnvironmentVariable("packageVersion")
		?? Argument<string>("pv", null)
		?? "2.0.0";
	return e;
}

string GetAssemblyVersion()
{
	var e = EnvironmentVariable("assemblyVersion")
		?? Argument<string>("av", null)
		?? GetPackageVersion();

	return e;
}

string GetTestLogger()
{
	var e = EnvironmentVariable("testLogger")
		?? Argument<string>("tl", null);

	return e;
}

string GetTestFilter()
{
	var arg = Argument<string>("tfl", null);
	if (arg != null)
		return arg;

	return EnvironmentVariable("testFilter");
}

string GetTestTargetFramework()
{
	var e = EnvironmentVariable("testTargetFramework")
		?? Argument<string>("ttf", null)
		?? "net452";

	return e.ToLower();
}

string GetTestConfiguration()
{
	var e = EnvironmentVariable("testConfiguration")
		??  Argument<string>("tc", "Release");

	return e;
}

string GetAccessToken()
{
	var e = EnvironmentVariable("access_token")
		?? Argument<string>("gitpwd", null);

	return e;
}

bool PatchPackage()
{
	return IsAppVeyorBuild() || Argument<string>("patch", null) != null;
}

bool SkipTests()
{
	return Argument<string>("skiptests", null) != null;
}

bool PublishDocFx()
{
	var publish = GetAccessToken() != null;
	if (IsAppVeyorBuild())
		publish &= AppVeyor.Environment.Repository.Branch.ToLower() == "release";

	return publish;
}

string RcVersion()
{
	var arg = Argument<string>("patch", null);
	if (arg != null)
		return arg;

	if (IsAppVeyorBuild())
		return AppVeyor.Environment.Build.Number.ToString();

	return "0";
}

void UploadTestResults(FilePath file, AppVeyorTestResultsType type)
{
	Console.WriteLine("Uplaoding test result: {0}", file.FullPath);

	if (IsAppVeyorBuild())
	{
		Console.WriteLine("Uploading test results to AppVeyor");

		AppVeyor.UploadTestResults(file, type);
	}
	else
	{
		Console.WriteLine("Nowhere to upload");
	}
}

void UploadTestResults()
{
	var rootDir = new System.IO.DirectoryInfo("./");

	Console.WriteLine("Looking for NUnit");
	var testResults = "TestResult.xml";

	if (FileExists(testResults))
	{
		UploadTestResults(testResults, AppVeyorTestResultsType.NUnit3);
		//DeleteFile(testResults);
	}
	else
		Console.WriteLine("No test results (expected at {0})", testResults);


	Console.WriteLine("Looking for TRX");

	foreach(var f in rootDir.GetFiles("*.trx", SearchOption.AllDirectories))
	{
		Console.WriteLine("Found: {0}", f.FullName);
		UploadTestResults(f.FullName, AppVeyorTestResultsType.MSTest);
		//DeleteFile(f.FullName);
	}

}

void UploadArtifact(FilePath file)
{
	if (IsAppVeyorBuild())
	{
		Console.WriteLine("Uploading file to AppVeyor");
		var settings = new AppVeyorUploadArtifactsSettings()
		{
			DeploymentName = file.GetFilename().ToString(),
			ArtifactType = AppVeyorUploadArtifactType.Auto
		};

		AppVeyor.UploadArtifact(file, settings);
	}
}

string GetPackageSuffix()
{
	if (!IsRelease())
		return "rc" + RcVersion();

	return "";
}

string GetDocFxCheckout()
{
	return "./linq2db.github.io";
}

string GetDocFxSite()
{
	return "./Doc/_site";
}

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does(() =>
{
	DotNetCoreBuild(GetSolutionName(),
		new DotNetCoreBuildSettings (){
			Configuration = GetConfiguration()
		}
	);
});

Task("DocFxBuild")
	.IsDependentOn("Clean")
	.Does(() =>
{
	DocFxMetadata("./Doc/docfx.json");
	DocFxBuild("./Doc/docfx.json");

	CopyFile("./Doc/_site/images/icon.ico", "./Doc/_site/favicon.ico");
});

Task("DocFxPublish")
	.IsDependentOn("Clean")
	.IsDependentOn("DocFxBuild")
	.WithCriteria(PublishDocFx())
	.Does(() =>
{
	var accessToken         = GetAccessToken();
	var docFxCheckout       = GetDocFxCheckout();
	var docFxSite           = GetDocFxSite();

	GitClone("https://github.com/linq2db/linq2db.github.io.git", "linq2db.github.io");
	CopyDirectory(docFxCheckout+"/.git", docFxSite+"/.git");
	GitAddAll(docFxSite);
	GitCommit(docFxSite, "DocFx", "docfx@linq2db.com", "CI DocFx update");

	GitPush(docFxSite, "ili", accessToken);
});

Task("RunTests")
	.IsDependentOn("Restore")
	.IsDependentOn("Clean")
	.WithCriteria(() => SkipTests() == false)
	.Does(() =>
{
	var testLogger          = GetTestLogger();
	var testFilter          = GetTestFilter();
	var testConfiguration   = GetTestConfiguration();
	var testTargetFramework = GetTestTargetFramework();
	var projects            = new [] { File("./Tests/Linq/Tests.csproj").Path };

	Console.WriteLine("Filter:        {0}", testFilter);
	Console.WriteLine("Logger:        {0}", testLogger);
	Console.WriteLine("Framework:     {0}", testTargetFramework);
	Console.WriteLine("Configuration: {0}", testConfiguration);

	var settings = new DotNetCoreTestSettings
	{
		// ArgumentCustomization = args => args.Append("--result=TestResult.xml"),
		Configuration = testConfiguration,
		Framework     = testTargetFramework,
		Filter        = testFilter,
		Logger        = testLogger
	};

	foreach(var project in projects)
	{
		Console.WriteLine(project.FullPath);

		DotNetCoreTest(project.FullPath, settings);

		UploadTestResults();
	}
})
.OnError(ex =>
{
	Console.WriteLine("Tests failed: {0}", ex.Message);

	UploadTestResults();

	throw ex;
});

Task("Pack")
	.IsDependentOn("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{
	var suffix = GetPackageSuffix();

	Console.WriteLine("Package  version: {0}", GetPackageVersion());
	Console.WriteLine("Package  suffix:  {0}", suffix);
	Console.WriteLine("Assembly version: {0}", GetAssemblyVersion());

	var settings = new DotNetCorePackSettings
	{
		Configuration   = GetConfiguration(),
		OutputDirectory = GetBuildArtifacts(),
		VersionSuffix   = suffix
	};

	DotNetCorePack(GetPackPath(), settings);
});

Task("Clean")
	.Does(() =>
{
	CleanDirectories(new DirectoryPath[] { GetBuildArtifacts() });

	if(DirectoryExists(GetDocFxCheckout()))
		DeleteDirectory(GetDocFxCheckout(), true);

	if(DirectoryExists(GetDocFxSite()))
		DeleteDirectory(GetDocFxSite(), true);

});

Task("Restore")
	.Does(() =>
{
	DotNetCoreRestore();
});

Task("Addins")
	.Does(()=>
{
	if(IsAppVeyorBuild())
	{
		Information("AppVeyor addin loading");
		CakeExecuteScript("./av.cake");
	}
});

Task("DocFx")
  .IsDependentOn("Addins")
  .IsDependentOn("DocFxBuild")
  .IsDependentOn("DocFxPublish");

Task("Default")
  .IsDependentOn("Addins")
  .IsDependentOn("RunTests")
  .IsDependentOn("Pack");

Task("Tests")
  .IsDependentOn("RunTests");

RunTarget(GetTarget());
