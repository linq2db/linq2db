Pregenerated platform-specific apphost.exe files for linq2db.CLI

We can also generate them automatically using this code in liq2db.CLI.csproj, but it doesn't work everywhere (who knows why):

```xml
	<PropertyGroup Condition="$(RID) != ''">
		<AppHostRuntimeIdentifier>$(RID)</AppHostRuntimeIdentifier>
		<OutputPath>bin\$(Configuration)\apphosts\$(RID)</OutputPath>
	</PropertyGroup>

	<Target Name="BuildSpecific" AfterTargets="Build" Condition=" '$(RID)' == '' ">
		<!--we need to remove apphost.exe as Build target doesn't validate already existing file to be compatible with requested RID-->
		<Delete Files="obj\$(Configuration)\netcoreapp3.1\apphost.exe" />
		<MSBuild Projects="$(MSBuildProjectFullPath)" Properties="RID=win-x86;Platform=$(Platform);Configuration=$(Configuration)" Targets="Build" />
		<Delete Files="obj\$(Configuration)\netcoreapp3.1\apphost.exe" />
		<MSBuild Projects="$(MSBuildProjectFullPath)" Properties="RID=win-x64;Platform=$(Platform);Configuration=$(Configuration)" Targets="Build" />
	</Target>
	<Target Name="CopyHosts" AfterTargets="Build" Condition=" '$(RID)' != '' ">
		<Copy SourceFiles="bin\$(Configuration)\apphosts\$(RID)\netcoreapp3.1\$(AssemblyName).exe" DestinationFiles="bin\$(Configuration)\netcoreapp3.1\$(AssemblyName).$(RID).exe" />
	</Target>
```
