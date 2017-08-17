#addin "MagicChunks"
#addin "Cake.DocFx"
#addin "Cake.Git"
#tool "docfx.console"

var target          = Argument("target", "Default");
var configuration   = Argument<string>("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var isAppVeyorBuild     = AppVeyor.IsRunningOnAppVeyor;
var buildConfiguration  = GetBuildConfiguration();

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
	var e = EnvironmentVariable("nugetVersion")
		?? Argument<string>("nv", null)
		?? "2.0.0";

	Console.WriteLine("Package Version: {0}", e);

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

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does(() =>
{

	// Patch Version for CI builds
	if (PatchPackage())
	{
		var assemblyVersion = packageVersion + ".0";

		if (!IsRelease())
		{
			packageSuffix      = "rc" + AppVeyor.Environment.Build.Number.ToString();
			fullPackageVersion = packageVersion + "-" + packageSuffix;
		}

		Console.WriteLine("Package  Version: {0}", packageVersion);
		Console.WriteLine("Package  Suffix : {0}", packageSuffix);
		Console.WriteLine("Assembly Version: {0}", assemblyVersion);


		TransformConfig(nugetProject, nugetProject,
		new TransformationCollection {
			{ "Project/PropertyGroup/Version",         fullPackageVersion },
			{ "Project/PropertyGroup/VersionPrefix",   packageVersion },
			{ "Project/PropertyGroup/VersionSuffix",   packageSuffix },
			{ "Project/PropertyGroup/AssemblyVersion", assemblyVersion },
			{ "Project/PropertyGroup/FileVersion",     assemblyVersion },
		 });

	}

	if (buildConfiguration == "docfx")
	{
		DocFxBuild("./Doc/docfx.json");

		GitClone("https://github.com/linq2db/linq2db.github.io.git", "linq2db.github.io");
		CopyDirectory(docFxCheckout+"/.git", docFxSite+"/.git");
		GitAddAll(docFxSite);
		GitCommit(docFxSite, "DocFx", "docfx@linq2db.com", "CI DocFx update");

		if(accessToken != null)
		{
			GitPush(docFxSite, "ili", accessToken);
		}
	}
	else
	{
		MSBuild(solutionName, cfg => cfg
			.SetConfiguration("Release")
			.UseToolVersion(MSBuildToolVersion.VS2017)
			);
	}

});

Task("RunTests")
	.IsDependentOn("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{
	if(buildConfiguration == "docfx")
		return;

	if (!string.IsNullOrEmpty(regularProviders))
	{
		CopyFile("./Tests/Linq/" + regularProviders, "./Tests/Linq/UserDataProviders.txt");
	}

	if (!string.IsNullOrEmpty(coreProviders))
	{
		CopyFile("./Tests/Linq/" + coreProviders, "./Tests/Linq/UserDataProviders.Core.txt");
	}

	var projects = new [] {File("./Tests/Linq/Linq.csproj").Path};

	foreach(var project in projects)
	{
		var settings = new DotNetCoreTestSettings
		{
			Configuration = configuration,
			NoBuild = true, 
			Framework = buildConfiguration
		};

		Console.WriteLine(project.FullPath);

		DotNetCoreTest(project.FullPath, settings);
	}
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

	NuGetRestore(solutionName);
});

Task("Default")
  .IsDependentOn("Build")
  .IsDependentOn("RunTests")
  .IsDependentOn("Pack");

RunTarget(target);