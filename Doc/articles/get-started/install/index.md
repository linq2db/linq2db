---
title: Installing LINQ To DB
author: sdanyliv

---
# Installing LINQ To DB

<a name="visual-studio"></a>
### Visual Studio development

You can develop many different types of applications that target .NET Core, .NET Framework, or other platforms supported by LINQ To DB using Visual Studio.

There are two ways you can install the LINQ To DB database provider in your application from Visual Studio:

#### Using NuGet's [Package Manager User Interface](https://docs.microsoft.com/nuget/tools/package-manager-ui)

* Select on the menu **Project > Manage NuGet Packages**

* Click on the **Browse** or the **Updates** tab

* Select the `linq2db.SqlServer` package and the desired version and confirm [list of supported databases](/articles/general/databases.html)

> [!TIP]  
> `linq2db` package contains all data providers in bundle. It dynamically loads specific data provider based on configuration.
> For simplyfying usage LINQ To DB has many helper packages `linq2db.*` that just reference required libraries for specific database provider.


#### Using NuGet's [Package Manager Console (PMC)](https://docs.microsoft.com/nuget/tools/package-manager-console)

* Select on the menu **Tools > NuGet Package Manager > Package Manager Console**

* Type and run the following command in the PMC:

  ``` PowerShell  
  Install-Package linq2db.SqlServer
  ```
* You can use the `Update-Package` command instead to update a package that is already installed to a more recent  version

* To specify a specific version, you can use the `-Version` modifier. For example, to install LINQ To DB packages, append `-Version 2.1.0` to the commands

