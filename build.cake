#addin "MagicChunks"
#addin "Cake.DocFx"
#addin "Cake.Git"
#addin "Cake.AppVeyor"
#tool "docfx.console"
#tool "nuget:?package=NUnit.ConsoleRunner"


var buildConfiguration  = GetBuildConfiguration();
var target              = GetTarget();
var configuration       = GetConfiguration();

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var isAppVeyorBuild     = AppVeyor.IsRunningOnAppVeyor;

var packPath            = Directory("./Source/Source.csproj");
var buildArtifacts      = Directory("./artifacts/packages");

var solutionName        = "./linq2db.core.sln";
var nugetProject        = "./Source/Source.csproj";
var argRelease          = Argument<string>("Release", null);

var packageSuffix       = "";
var packageVersion      = GetPackageVersion();
var fullPackageVersion  = "";

var regularProviders    = GetRegularProviders();
var coreProviders       = GetCoreProviders();

var accessToken         = GetAccessToken();
var docFxCheckout       = "./linq2db.github.io";
var docFxSite           = "./Doc/_site";

var testRunner          = GetTestRunner();

bool IsRelease()
{
	if(argRelease != null)
		return true;

	if (isAppVeyorBuild)
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
	if (buildConfiguration == "docfx")
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
string GetAccessToken()
{
	var e = EnvironmentVariable("access_token")
		?? Argument<string>("gitpwd", null);

	return e;

}

bool PatchPackage()
{
	return isAppVeyorBuild || Argument<string>("patch", null) != null;
}

bool SkipTests()
{
	return buildConfiguration == "docfx" || Argument<string>("skiptests", null) != null;
}

bool PublishDocFx()
{
	var publish = accessToken != null;
	if (isAppVeyorBuild)
		publish &= AppVeyor.Environment.Repository.Branch.ToLower() == "release";

	return publish;
}

string RcVersion()
{
	var arg = Argument<string>("patch", null);
	if (arg != null)
		return arg;

	if (isAppVeyorBuild)
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

void UploadTestResults(FilePath file)
{
	if (isAppVeyorBuild)
	{
		Console.WriteLine("Uploading test results to AppVeyor");
		AppVeyor.UploadTestResults(file, AppVeyorTestResultsType.NUnit3);
	}
}

void UploadArtifact(FilePath file)
{
	if (isAppVeyorBuild)
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

Task("PatchPackage")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.WithCriteria(PatchPackage())
	.Does(() =>
{
	var assemblyVersion = packageVersion + ".0";

	if (!IsRelease())
	{
		packageSuffix      = "rc" + RcVersion();
		fullPackageVersion = packageVersion + "-" + packageSuffix;
	}

	Console.WriteLine("Full Package  Version: {0}", fullPackageVersion);
	Console.WriteLine("Package  Version     : {0}", packageVersion);
	Console.WriteLine("Package  Suffix      : {0}", packageSuffix);
	Console.WriteLine("Assembly Version     : {0}", assemblyVersion);


	TransformConfig(nugetProject, nugetProject,
		new TransformationCollection {
			{ "Project/PropertyGroup/Version",         fullPackageVersion },
			{ "Project/PropertyGroup/VersionPrefix",   packageVersion },
			{ "Project/PropertyGroup/VersionSuffix",   packageSuffix },
			{ "Project/PropertyGroup/AssemblyVersion", assemblyVersion },
			{ "Project/PropertyGroup/FileVersion",     assemblyVersion },
	 });
});


Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("PatchPackage")
	.Does(() =>
{
	MSBuild(solutionName, cfg => cfg
			.SetVerbosity(Verbosity.Minimal)
			.SetConfiguration(configuration)
			.UseToolVersion(MSBuildToolVersion.VS2017)
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

	if (!string.IsNullOrEmpty(regularProviders))
	{
		CopyFile("./Tests/Linq/" + regularProviders, "./Tests/Linq/UserDataProviders.txt");
	}

	if (!string.IsNullOrEmpty(coreProviders))
	{
		CopyFile("./Tests/Linq/" + coreProviders, "./Tests/Linq/UserDataProviders.Core.txt");
	}

	var projects = testRunner == "NUnit"
		? new [] { File("./Tests/Linq/Bin/Release/" + buildConfiguration + "/linq2db.Tests.dll").Path }
		: new [] { File("./Tests/Linq/Linq.csproj").Path };
	
	var testFilter = GetTestFilter();
	Console.WriteLine("Filter: {0}", testFilter);

	var settings = new DotNetCoreTestSettings
	{
		// ArgumentCustomization = args => args.Append("--result=TestResult.xml"),
		Configuration = configuration,
		NoBuild = true, 
		Framework = buildConfiguration,
		Filter = testFilter
	};

	var testResults = "TestResult.xml";

	var nunitSettings = new NUnit3Settings 
	{
		Configuration = configuration,
		X86 = true,
		Results = testResults,
		//Framework = buildConfiguration,
		Where = testFilter
	};


	foreach(var project in projects)
	{
		Console.WriteLine(project.FullPath);

		if (testRunner == "NUnit")
			NUnit3(project.FullPath, nunitSettings);
		else
			DotNetCoreTest(project.FullPath, settings);

		if (FileExists(testResults))
		{
			UploadTestResults(testResults);
			DeleteFile(testResults);
		}
		else 
			Console.WriteLine("No test results (expected at {0})", testResults);
	}
})
.OnError(ex => 
{
	Console.WriteLine("Tests failed: {0}", ex.Message);
	var fileName = "TestResult.xml";

	if (FileExists(fileName))
	{
		Console.WriteLine("Test results exists: {0}", fileName);
		UploadTestResults(fileName);
	}
	else 
		Console.WriteLine("No test results (expected at {0})", fileName);
	
	throw ex;
});

Task("Pack")
	.IsDependentOn("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{
	if(buildConfiguration == "docfx")
		return;

	var settings = new DotNetCorePackSettings
	{
		Configuration = configuration,
		OutputDirectory = buildArtifacts,
		NoBuild = true,
		VersionSuffix = packageSuffix
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

	DotNetCorePack(packPath, settings);
});

Task("Clean")
	.Does(() =>
{
	CleanDirectories(new DirectoryPath[] { buildArtifacts });

	if(DirectoryExists(docFxCheckout))
		DeleteDirectory(docFxCheckout, true);

	if(DirectoryExists(docFxSite))
		DeleteDirectory(docFxSite, true);

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

	NuGetRestore(solutionName, new NuGetRestoreSettings()
	{
		Verbosity = NuGetVerbosity.Quiet,
	});
});

Task("DocFx")
  .IsDependentOn("DocFxBuild")
  .IsDependentOn("DocFxPublish");

Task("Default")
  .IsDependentOn("Build")
  .IsDependentOn("RunTests")
  .IsDependentOn("Pack");

RunTarget(target);