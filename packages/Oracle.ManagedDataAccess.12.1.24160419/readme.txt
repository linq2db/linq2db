Oracle.ManagedDataAccess NuGet Package 12.1.24160419 README
===========================================================

for ODAC 12c Release 4

Release Notes: Oracle Data Provider for .NET, Managed Driver

April 2016

Copyright (c) Oracle Corporation 2016

This document provides information that supplements the Oracle Data Provider for .NET (ODP.NET) documentation. 
You have downloaded Oracle Data Provider for .NET from Oracle, the license agreement to which is available at 
http://www.oracle.com/technetwork/licenses/distribution-license-152002.html

TABLE OF CONTENTS
*New Features
*Installation and Configuration Steps
*Installation Changes
*Documentation Corrections and Additions
*ODP.NET, Managed Driver Tips, Limitations, and Known Issues

Note: The 32-bit "Oracle Developer Tools for Visual Studio" download from http://otn.oracle.com/dotnet is 
required for Entity Framework design-time features and for other Visual Studio designers such as the 
TableAdapter Wizard. This NuGet download does not enable design-time tools; it only provides run-time support. 
This version of ODP.NET supports Oracle Database version 10.2 and higher.



New Features since Oracle.ManagedDataAccess NuGet Package 12.1.2400
===================================================================
1. Data Integrity
ODP.NET, Managed Driver supports cryptographic hash functions to better ensure data integrity between the 
database server and client. The algorithms supported include MD5, SHA-1, and SHA-2 (SHA-256, SHA-384, and 
SHA-512).

To enable ODP.NET, Managed Driver data integrity, use the following .NET configuration file (i.e. 
web.config, machine.config) properties available in the oracle.manageddataaccess.client section:

* SQLNET.CRYPTO_CHECKSUM_CLIENT = Specifies the desired data integrity behavior when this client connects 
to a server. Supported values are accepted, rejected, requested, or required. Default = accepted.
More info: http://docs.oracle.com/database/121/NETRF/sqlnet.htm#NETRF200

* SQLNET.CRYPTO_CHECKSUM_TYPES_CLIENT = Specifies the data integrity algorithms that this client uses. 
Supported values are SHA512, SHA384, SHA256, SHA1, and MD5.
More info: http://docs.oracle.com/database/121/NETRF/sqlnet.htm#NETRF202


2. Secure Sockets Layer (SSL) and Transport Layer Security (TLS)
    ODP.NET, Managed Driver has added support for TLS 1.1 and 1.2, to go along with our previous support for
    SSL 3.0 and TLS 1.0.

    To utilize a specific version of SSL/TLS, use the SSL_VERSION .NET Application configuration setting parameter.
    By default the SSL_VERSION is set to all supported versions, in the order 3.0, 1.0, 1.1, and 1.2.

    The client and server negotiate to the highest version among the common conversions
    specified in the client and server configurations.  The versions from lowest to highest are:
    3.0 (lowest), 1.0, 1.1, and 1.2 (highest).

    Please reference the following documentation for a more thorough discussion of the SSL_VERSION parameter:

https://docs.oracle.com/cd/E11882_01/network.112/e10835/sqlnet.htm#NETRF235


3. Configuration Settings with Relative Windows Path and Windows Environment Variables

The following managed ODP.NET configuration settings support relative Windows path and environment 
variables:

a. TraceFileLocation
b. WALLET_LOCATION

File locations for the above config parameters can now be set using relative Windows paths. The 
"." notation informs ODP.NET to use the current working directory. Sub-directories can be added by 
appending them. For example, ".\mydir" refers to the sub-directory "mydir" in the current working 
directory.  To navigate to a parent directory, use the ".." notation.

For web applications, the current working directory is the application directory. For Windows
applications, the .EXE location is the current working directory.

Windows paths can also be set using Windows environment variable names within "%" characters.
(e.g. "%tns_admin%", "c:\%dir%\my_app_location", "c:\%top_level_dir%\%bottom_level_dir%")

Please note the following for Windows environment variables:
- If the environment variable that is used by the configuration parameter is not set to anything, 
an exception will be thrown.
- A directory name cannot partially be using an environment variable (i.e. "c:\my_app_%id%")
- Multiple variables can used in given directory location. 
(i.e. "c:\%top_level_dir%\%bottom_level_dir%")


4. .ORA File Search Order
For Windows applications, ODP.NET will now search for tnsnames.ora, sqlnet.ora. and ldap.ora 
files first in the .EXE directory before looking in the current working directory.


5. NuGet Package Versioning - new versioning scheme
Oracle .NET NuGet packages will use a new versioning scheme as NuGet releases are expected to be more 
frequent than ODAC releases. This scheme will help customers distinguish the version they are using.

[DB major version].[DB minor version].[DB patchset version][ODAC version][6-digit patchset version]

For example, this NuGet version is 12.1.24160419, which is equivalent to
[DB major version] = 12
[DB minor version] = 1
[DB patchset version] = 2
[ODAC version] = 4
[6-digit patchset version] = 160419



Installation and Configuration Steps
====================================
The downloads are NuGet packages that can be installed with the NuGet Package Manager. These instructions apply 
to install ODP.NET, Managed Driver.

1. Un-GAC and un-configure any existing assembly (i.e. Oracle.ManagedDataAccess.dll) and policy DLL 
(i.e. Policy.4.121.Oracle.ManagedDataAccess.dll) for the ODP.NET, Managed Driver, version 12.1.0.2
that exist in the GAC. Remove all references of Oracle.ManagedDataAccess from machine.config file, if any exists.

2. In Visual Studio 2010, 2012, 2013, or 2015 open NuGet Package Manager from an existing Visual Studio project. 

3. Install the NuGet package from an OTN-downloaded local package source or from nuget.org.


   From Local Package Source
   -------------------------
   A. Click on the Settings button in the lower left of the dialog box.

   B. Click the "+" button to add a package source. In the Source field, enter in the directory location where the 
   NuGet package(s) were downloaded to. Click the Update button, then the Ok button.

   C. On the left side, under the Online root node, select the package source you just created. The ODP.NET NuGet 
   packages will appear.


   From Nuget.org
   --------------
   A. In the Search box in the upper right, search for the package with id, "Oracle.ManagedDataAccess". Verify 
   that the package uses this unique ID to ensure it is the official Oracle Data Provider for .NET, Managed Driver 
   download.

   B. Select the package you wish to install.


4. Click on the Install button to select the desired NuGet package(s) to include with the project. Accept the 
license agreement and Visual Studio will continue the setup.

5. Open the app/web.config file to configure the ODP.NET connection string and local naming parameters 
(i.e. tnsnames.ora). Below is an example of configuring the local naming parameters:

  <oracle.manageddataaccess.client>
    <version number="*">
      <dataSources>
        <!-- Customize these connection alias settings to connect to Oracle DB -->
        <dataSource alias="MyDataSource" descriptor="(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL))) " />
      </dataSources>
    </version>
  </oracle.manageddataaccess.client>

After following these instructions, ODP.NET is now configured and ready to use.

NOTE: ODP.NET, Managed Driver comes with two platform specific assemblies:

        i.  Oracle.ManagedDataAccessDTC.dll (for Distributed Transaction Support)
        ii. Oracle.ManagedDataAccessIOP.dll (for Kerberos Support)

The Oracle.ManagedDataAccessDTC.dll assembly is ONLY needed if you are using Distributed Trasactions and the 
.NET Framework being used is 4.5.1 or lower. If you are using .NET Framework 4.5.2 or higher, this assembly does 
not need to be referenced by your application.

The Oracle.ManagedDataAccessIOP.dll assembly is ONLY needed if you are using Kerberos5 based external 
authentication. Kerberos5 users will need to download MIT Kerberos for Windows version 4.0.1 from 
	http://web.mit.edu/kerberos/dist/
to utilize ODP.NET, Managed Driver's support of Kerberos5.

These asssemblies are located under
      packages\Oracle.ManagedDataAccess.<version>\bin\x64
and
      packages\Oracle.ManagedDataAccess.<version>\bin\x86
depending on the platform.

If these assemblies are required by your application, your Visual Studio project requires additional changes.

Use the following steps for your application to use the 64-bit version of Oracle.ManagedDataAccessDTC.dll:

1. Right click on the Visual Studio project.
2. Select Add -> New Folder
3. Name the folder x64.
4. Right click on the newly created x64 folder
5. Select Add -> Existing Item
6. Browse to packages\Oracle.ManagedDataAccess.<version>\bin\x64 under your project solution directory.
7. Choose Oracle.ManagedDataAccessDTC.dll
8. Click the 'Add' button
9. Left click the newly added Oracle.ManagedDataAccessDTC.dll in the x64 folder
10. In the properties window, set 'Copy To Output Directory' to 'Copy Always'.

For x86 targeted applications, name the folder x86 and add assemblies from the 
packages\Oracle.ManagedDataAccess.<version>\bin\x86 folder.

Use the same steps for adding Oracle.ManagedDataAccessIOP.dll.

To make your application platform independent even if it depends on Oracle.ManagedDataAccessDTC.dll and/or 
Oracle.ManagedDataAccessIOP.dll, create both x64 and x86 folders with the necessary assemblies added to them.



Installation Changes
====================
The following app/web.config entries are added by including the "Official Oracle ODP.NET, Managed Driver" NuGet package 
to your application:

1) Configuration Section Handler

The following entry is added to the app/web.config to enable applications to add an <oracle.manageddataaccess.client> 
section for ODP.NET, Managed Driver-specific configuration:

<configuration>
  <configSections>
    <section name="oracle.manageddataaccess.client" type="OracleInternal.Common.ODPMSectionHandler, Oracle.ManagedDataAccess, Version=4.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342" />
  </configSections>
</configuration>

Note: If your application is a web application and the above entry was added to a web.config and the same config 
section handler for "oracle.manageddataaccess.client" also exists in machine.config but the "Version" attribute values 
are different, an error message of "There is a duplicate 'oracle.manageddataaccess.client' section defined." may be 
observed at runtime.  If so, the config section handler entry in the machine.config for 
"oracle.manageddataaccess.client" has to be removed from the machine.config for the web application to not encounter 
this error.  But given that there may be other applications on the machine that depended on this entry in the 
machine.config, this config section handler entry may need to be moved to all of the application's .NET config file on 
that machine that depend on it.

2) DbProviderFactories

The following entry is added for applications that use DbProviderFactories and DbProviderFactory classes. Also, any 
DbProviderFactories entry for "Oracle.ManagedDataAccess.Client" in the machine.config will be ignored with the following 
entry:

<configuration>
  <system.data>
    <DbProviderFactories>
      <remove invariant="Oracle.ManagedDataAccess.Client" />
      <add name="ODP.NET, Managed Driver" invariant="Oracle.ManagedDataAccess.Client" description="Oracle Data Provider for .NET, Managed Driver" type="Oracle.ManagedDataAccess.Client.OracleClientFactory, Oracle.ManagedDataAccess, Version=4.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342" />
    </DbProviderFactories>
  </system.data>
</configuration>

3) Dependent Assembly

The following entry is created to ignore policy DLLs for Oracle.ManagedDataAccess.dll and always use the 
Oracle.ManagedDataAccess.dll version that is specified by the newVersion attribute in the <bindingRedirect> element.  
The newVersion attribute corresponds to the Oracle.ManagedDataAccess.dll version which came with the NuGet package 
associated with the application.

<configuration>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <publisherPolicy apply="no" />
        <assemblyIdentity name="Oracle.ManagedDataAccess" publicKeyToken="89b483f429c47342" culture="neutral" />
        <bindingRedirect oldVersion="4.121.0.0 - 4.65535.65535.65535" newVersion="4.121.2.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>

4) Data Sources

The following entry is added to provide a template on how a data source can be configured in the app/web.config. 
Simply rename "MyDataSource" to an alias of your liking and modify the PROTOCOL, HOST, PORT, SERVICE_NAME as required 
and un-comment the <dataSource> element. Once that is done, the alias can be used as the "data source" attribute in 
your connection string when connecting to an Oracle Database through ODP.NET, Managed Driver.

<configuration>
  <oracle.manageddataaccess.client>
    <version number="*">
      <dataSources>
        <dataSource alias="SampleDataSource" descriptor="(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL))) " />
      </dataSources>
    </version>
  </oracle.manageddataaccess.client>
</configuration>



Documentation Corrections and Additions
=======================================
This section contains information that corrects or adds to existing ODP.NET documentation, which can be found here:
http://www.oracle.com/technetwork/topics/dotnet/tech-info/index.html

1. ODP.NET Entity Framework Database First and Model First applications using Entity Framework 6 requires .NET 
Framework 4.5 or higher.

2. All Oracle database clients support interrupting database query execution, such as through an ODP.NET command 
timeout. The database server can be interrupted via either TCP/IP urgent data or normal TCP/IP data, called out of band 
(OOB) or in band data, respectively. Windows-based database servers only support in band breaks, whereas all other 
(predominantly UNIX-based) database servers can support OOB or in band breaks. ODP.NET, Managed Driver uses OOB breaks 
by default with database servers that support it. For certain network topologies, the routers or firewalls involved in 
the route to the database may have been configured to drop urgent data or in band the data. If the routers or firewalls 
can not be changed to handle urgent data appropriately, then the ODP.NET, Managed Driver can be configured to utilize 
in band breaks by setting the .NET configuration parameter disable_oob to "on". The default value for disable_oob is 
"off". disable_oob can be set in the <settings> of the .NET config file for <oracle.manageddataaccess.client>. As with 
all ODP.NET, Managed Driver settings, disable_oob can be set in either the .NET config or sqlnet.ora files, whereas it 
can only be set for ODP.NET, Unmanaged Driver in the sqlnet.ora file.



ODP.NET, Managed Driver Tips, Limitations, and Known Issues
===========================================================
This section contains information that is specific to ODP.NET, Managed Driver. 

1. OracleConnection object's OpenWithNewPassword() method invocation will result in an ORA-1017 error with 11.2.0.3.0 
and earlier versions of the database. [Bug 12876992]

2. Stored functions/procedures in a PDB cannot be added to a .NET Entity Framework model. [Bug 17344899]
