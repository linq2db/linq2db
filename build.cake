#addin "MagicChunks"
#addin "Cake.DocFx"
#addin "Cake.Git"
#tool "docfx.console"
#tool "nuget:?package=NUnit.ConsoleRunner"

string GetSolutionName()
{
	return "./linq2db.core.sln";
}

string GetNugetProject()
{
	return "./Source/Source.csproj";
}

ConvertableDirectoryPath GetPackPath()
{
	return Directory("./Source/Source.csproj");
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
	if(Argument<string>("Release", null) != null)
		return true;

	if (IsAppVeyorBuild())
		return AppVeyor.Environment.Repository.Branch.ToLower() == "release";

	return false;
}

string GetBuildConfiguration()
{
	var e = EnvironmentVariable("buildConfiguration") 
		?? Argument<string>("bc", null)
		?? "net45";

	Console.WriteLine("Build Configuration: {0}", e);

	return e.ToLower();
}

string GetTarget()
{
	if (GetBuildConfiguration() == "docfx")
	{
		Console.WriteLine("Setting Target to DocFx because of buildConfiguration");
		return "DocFx";
	}

	return Argument("target", "Default");
}

string GetConfiguration()
{
	var e = EnvironmentVariable("configuration") 
		??  Argument<string>("configuration", "Release");

	Console.WriteLine("Configuration: {0}", e);

	return e;
}

string GetRegularProviders()
{
	var e = EnvironmentVariable("regularProviders") 
		?? Argument<string>("rp", null)
		?? "";

	Console.WriteLine("UserDataProviders.txt: {0}", e);

	return e;
}

string GetCoreProviders()
{
	var e = EnvironmentVariable("coreProviders") 
		?? Argument<string>("cp", null)
		?? "";

	Console.WriteLine("UserDataProviders.Core.txt: {0}", e);

	return e;
}

string GetPackageVersion()
{
	var e = EnvironmentVariable("packageVersion")
		?? Argument<string>("nv", null)
		?? "2.0.0";

	Console.WriteLine("Package Version: {0}", e);

	return e;

}

string GetTestRunner()
{
	var e = EnvironmentVariable("testRunner")
		?? Argument<string>("testRunner", null)
		?? "NUnit";

	Console.WriteLine("Test runner: {0}", e);

	return e;

}

string GetTestLogger()
{
	var e = EnvironmentVariable("testLogger")
		?? Argument<string>("testLogger", null);


	Console.WriteLine("Test logger: {0}", e);

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
	return GetBuildConfiguration() == "docfx" || Argument<string>("skiptests", null) != null;
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

string GetTestFilter()
{
	var arg = Argument<string>("testfilter", null);
	if (arg != null)
		return arg; 

	return EnvironmentVariable("testfilter");
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

string GetBuilder()
{
	return 
		EnvironmentVariable("builder")    ??
		Argument<string>("builder", null) ?? 
		"MSBuild";

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

Task("PatchPackage")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.WithCriteria(PatchPackage())
	.Does(() =>
{
	var packageVersion      = GetPackageVersion();
	var assemblyVersion     = packageVersion + ".0";
	var fullPackageVersion  = "";
	var packageSuffix       = GetPackageSuffix();

	if (!IsRelease())
	{
		fullPackageVersion = packageVersion + "-" + packageSuffix;
	}

	Console.WriteLine("Full Package  Version: {0}", fullPackageVersion);
	Console.WriteLine("Package  Version     : {0}", packageVersion);
	Console.WriteLine("Package  Suffix      : {0}", packageSuffix);
	Console.WriteLine("Assembly Version     : {0}", assemblyVersion);


	TransformConfig(GetNugetProject(), GetNugetProject(),
		new TransformationCollection {
			{ "Project/PropertyGroup/Version",         fullPackageVersion },
			{ "Project/PropertyGroup/VersionPrefix",   packageVersion },
			{ "Project/PropertyGroup/VersionSuffix",   packageSuffix },
			{ "Project/PropertyGroup/AssemblyVersion", assemblyVersion },
			{ "Project/PropertyGroup/FileVersion",     assemblyVersion },
	 });
});

Task("PatchTests")
	.WithCriteria(() => GetConfiguration() == "Travis")
	.Does(() =>
{
	TransformConfig("./Tests/Linq/Linq.csproj", "./Tests/Linq/Linq.csproj",
		new TransformationCollection {
			{ "Project/ItemGroup[@Condition=' '$(Configuration)' != 'Travis' ']", "" },
	 });
});

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("PatchPackage")
	.Does(() =>
{
	var builder = GetBuilder();

	if (builder == "MSBuild")
		MSBuild(GetSolutionName(), cfg => cfg
				.SetVerbosity(Verbosity.Minimal)
				.SetConfiguration(GetConfiguration())
				.UseToolVersion(MSBuildToolVersion.VS2017)
				);
	else 
		DotNetCoreBuild("./Tests/Linq/Linq.csproj", 
			new DotNetCoreBuildSettings
			{
				Configuration = GetConfiguration(),
				Framework = GetBuildConfiguration()
			});

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
	.Does(() =>
{
	if(SkipTests())
	{
		Console.WriteLine("Tests are skipped due to configuration");
		return;
	}

	var testRunner          = GetTestRunner();
	var testLogger          = GetTestLogger();
	var regularProviders    = GetRegularProviders();
	var coreProviders       = GetCoreProviders();

	if (!string.IsNullOrEmpty(regularProviders))
	{
		CopyFile("./Tests/Linq/" + regularProviders, "./Tests/Linq/UserDataProviders.txt");
	}

	if (!string.IsNullOrEmpty(coreProviders))
	{
		CopyFile("./Tests/Linq/" + coreProviders, "./Tests/Linq/UserDataProviders.Core.txt");
	}

	var projects = testRunner == "NUnit"
		? new [] { File("./Tests/Linq/Bin/Release/" + GetBuildConfiguration() + "/linq2db.Tests.dll").Path }
		: new [] { File("./Tests/Linq/Linq.csproj").Path };
	
	var testFilter = GetTestFilter();
	Console.WriteLine("Filter: {0}", testFilter);

	var settings = new DotNetCoreTestSettings
	{
		// ArgumentCustomization = args => args.Append("--result=TestResult.xml"),
		Configuration = GetConfiguration(),
		NoBuild = true, 
		Framework = GetBuildConfiguration(),
		Filter = testFilter,
		Logger = testLogger
	};

	var testResults = "TestResult.xml";

	var nunitSettings = new NUnit3Settings 
	{
		Configuration = GetConfiguration(),
		X86 = true,
		Results = testResults,
		//Framework = GetBuildConfiguration(),
		Where = testFilter
	};


	foreach(var project in projects)
	{
		Console.WriteLine(project.FullPath);

		if (testRunner == "NUnit")
			NUnit3(project.FullPath, nunitSettings);
		else
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
	if(GetBuildConfiguration() == "docfx")
		return;

	var settings = new DotNetCorePackSettings
	{
		Configuration = GetConfiguration(),
		OutputDirectory = GetBuildArtifacts(),
		NoBuild = true,
		VersionSuffix = GetPackageSuffix()
	};

/*	
	if (!string.IsNullOrEmpty(packageVersion))
		settings.ArgumentCustomization = b => 
		{
			Console.WriteLine("Package  Version: {0}", packageVersion);

			b.Append(" /p:VersionSuffix=" + "rc10");
			return b;
		};
*/

	DotNetCorePack(GetPackPath(), settings);
});

Task("Clean")
	.IsDependentOn("PatchTests")
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
{/*
	var settings = new DotNetCoreRestoreSettings
	{
		//Sources = new [] { "https://api.nuget.org/v3/index.json" }
	};
	*/
	//DotNetCoreRestore(solutionName, settings);

	NuGetRestore(GetSolutionName(), new NuGetRestoreSettings()
	{
		Verbosity = NuGetVerbosity.Quiet,
	});
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
  .IsDependentOn("Build")
  .IsDependentOn("RunTests")
  .IsDependentOn("Pack");

RunTarget(GetTarget());