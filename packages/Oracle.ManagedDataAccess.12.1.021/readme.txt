Release Notes
=============
Oracle Data Provider for .NET, Managed Driver
Oracle Data Provider for .NET, Managed Driver for Entity Framework

Release 12.1.0.2.1 for ODAC 12c Release 3 Production

January 2015


Copyright (c) Oracle Corporation 2015

This document provides information that supplements the Oracle Data Provider for .NET (ODP.NET) documentation. 
You have downloaded Oracle Data Provider for .NET from Oracle, the license agreement to which is available at 
http://www.oracle.com/technetwork/licenses/distribution-license-152002.html

TABLE OF CONTENTS
*Installation and Configuration Steps
*Installation Changes
*Documentation Corrections and Additions
*ODP.NET, Managed Driver Tips, Limitations, and Known Issues
*Entity Framework Tips, Limitations, and Known Issues

Note: The 32-bit "Oracle Developer Tools for Visual Studio" download from http://otn.oracle.com/dotnet is 
required for Entity Framework design-time features and for other Visual Studio designers such as the 
TableAdapter Wizard. This NuGet download does not enable design-time tools; it only provides run-time support. 
This version of ODP.NET supports Oracle Database version 10.2 and higher.



Installation and Configuration Steps
====================================
The downloads are NuGet packages that can be installed with the NuGet Package Manager. These instructions apply 
to install ODP.NET, Managed Driver.

1. Un-GAC any existing ODP.NET 12.1.0.2 versions you have installed. For example, if you plan to use only the 
ODP.NET, Managed Driver, only un-GAC existing managed ODP.NET 12.1.0.2 versions then.

2. In Visual Studio 2010, 2012, or 2013, open NuGet Package Manager from an existing Visual Studio project. 
NOTE: NuGet package for Oracle Data Provider for .NET, Managed Driver for Entity Framework requires Visual Studio 
2012 or higher.

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
   A. In the Search box in the upper right, search for the packages with id, "Oracle.ManagedDataAccess" and/or 
   "Oracle.ManagedDataAccess.EntityFramework". Verify that the packages use these unique IDs to ensure they are the 
   offical Oracle Data Provider for .NET, Managed Driver downloads.

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

The Oracle.ManagedDataAccessIOP.dll assembly is ONLY needed if you are using Kerberos. Kerberos users will need 
to download MIT Kerberos for Windows 4.0.1 or higher from 
  http://web.mit.edu/kerberos/dist/ 
to utilize ODP.NET, Managed Driver's support of Kerberos.

These asssemblies are located under
      packages\Oracle.ManagedDataAccess.12.1.021\bin\x64
and
      packages\Oracle.ManagedDataAccess.12.1.021\bin\x86
depending on the platform.

If these assemblies are required by your application, your Visual Studio project requires additional changes.

Use the following steps for your application to use the 64-bit version of Oracle.ManagedDataAccessDTC.dll:

1. Right click on the Visual Studio project.
2. Select Add -> New Folder
3. Name the folder x64.
4. Right click on the newly created x64 folder
5. Select Add -> Existing Item
6. Browse to packages\Oracle.ManagedDataAccess.12.1.021\bin\x64 under your project solution directory.
7. Choose Oracle.ManagedDataAccessDTC.dll
8. Click the 'Add' button
9. Left click the newly added Oracle.ManagedDataAccessDTC.dll in the x64 folder
10. In the properties window, set 'Copy To Output Directory' to 'Copy Always'.

For x86 targeted applications, name the folder x86 and add assemblies from the 
packages\Oracle.ManagedDataAccess.12.1.021\bin\x86 folder.

Use the same steps for adding Oracle.ManagedDataAccessIOP.dll.

To make your application platform independent even if it depends on Oracle.ManagedDataAccessDTC.dll and/or 
Oracle.ManagedDataAccessIOP.dll, create both x64 and x86 folders with the necessary assemblies added to them.



Installation Changes
====================
The following app/web.config entries are added by including the "ODP.NET, Managed Driver - Official" NuGet package 
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

The following app/web.config entries are added by including the "ODP.NET, Managed Entity Framework Driver - Official" 
NuGet package to your application.

1) Entity Framework

The following entry is added to enable Entity Framework to use Oracle.ManagedDataAccess.dll for executing Entity 
Framework related-operations, such as Entity Framework Code First and Entity Framework Code First Migrations against 
the Oracle Database.

<configuration>
  <entityFramework>
    <providers>
      <provider invariantName="Oracle.ManagedDataAccess.Client" type="Oracle.ManagedDataAccess.EntityFramework.EFOracleProviderServices, Oracle.ManagedDataAccess.EntityFramework, Version=6.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342" />
    </providers>
  </entityFramework>
</configuration>

2) Connection String

The following entry is added to enable the classes that are derived from DbContext to be associated with a connection 
string instead to associating the derived class with a connection string programmatically by passing it via its 
constructor. The name of "OracleDbContext" should be changed to the class name of your class that derives from DbContext. 
In addition, the connectionString attribute should be modified properly to set the "User Id", "Password", and 
"Data Source" appropriately with valid values.

<configuration>
  <connectionStrings>
    <add name="OracleDbContext" providerName="Oracle.ManagedDataAccess.Client" connectionString="User Id=oracle_user;Password=oracle_user_password;Data Source=oracle" />
  </connectionStrings>
</configuration>



Documentation Corrections and Additions
=======================================
This section contains information that corrects or adds to existing ODP.NET documentation, which can be found here:
http://docs.oracle.com/cd/E56485_01/index.htm

Custom Entity Data Model (EDM) Type Mapping Not Applied to Generated Complex Types 
---
When using the EDM wizard to create a complex type from a function import, any custom EDM type mappings specified will 
not be applied. The EDM wizard uses the default type mappings and the only known workaround is to manually edit the 
resulting complex type. After the complex type is generated any type declaration (field, property, constructor parameter, 
etc.) in the complex object which has an undesired type (such as Decimal rather than Boolean) should be manually edited 
to be of the desired type. 


ODP.NET, Managed Driver Support for Oracle Database 12c Implicit Ref Cursor 
---
ODP.NET, Managed Driver introduces support for the new Oracle Database 12c Implicit Ref Cursor. Configuration occurs 
using the <implicitrefcursor> .NET configuration section. When using database implicit ref cursors, the bindInfo element 
should be specified with a mode of "Implicit": 

<bindinfo mode="Implicit" /> 

For additional information refer to the implicitRefCursor section in Chapter 2 of the Oracle Data Provider for .NET 
Developer's Guide. 


Entity Framework Code First: Code-Based Migrations With No Supporting Code Migration File 
---
When using code-based migrations with the Entity Framework provider, the migration history table may be dropped if no 
supporting code migration file existed prior to updating the database. 

Workaround: Ensure the supporting code migration file has been added prior to updating the database. 

The following steps can remove the migration history table:
1. Execute application to create database objects
2. Enable-Migrations
3. Make code change to POCO
4. Update-Database

The workaround is to ensure code file is created:
1. Execute application to create database objects
2. Enable-Migrations
3. Make code change to POCO
4. Add-Migration (This step will create the necessary code migration file).
5. Update-Database


Session Time Zone Hour Offset in ODP.NET Managed and Unmanaged Drivers 
---
ODP.NET managed and unmanaged drivers set the default session time zone differently. While the session time zone for 
unmanaged ODP.NET uses an hour offset, managed ODP.NET uses the region identifier for setting its session time zone. 
As a result, managed ODP.NET is sensitive to daylight savings in scenarios where the timestamp LTZ values have to be 
converted from/to the session time zone.

There are two methods to resolve this difference if needed. For ODP.NET, Unmanaged Driver, the application explicitly 
sets the region identifier with the environment variable 'ORA_SDTZ' (e.g. 'set ORA_SDTZ = <Region ID>'). If ORA_SDTZ 
variable is set, Oracle Client considers this value as the session time zone. The second method is to execute an alter 
session command to set the session time zone property to the region identifier. 


ODP.NET, Managed Driver with NTS Authentication 
---
ODP.NET, Managed Driver supports NTS authentication to the database, except when the Windows domain is constrained to 
only support Kerberos-based domain authentication. 


ODP.NET, Managed Driver SSL Connections with Firewalls 
---
ODP.NET, Managed Driver SSL connections require a redirect to a dynamic port on the database server side. If a firewall 
exists between the database client and server, then all firewall ports must be enabled or the dynamic firewall port 
Oracle chooses must be enabled at run-time.

 

ODP.NET, Managed Driver Tips, Limitations, and Known Issues
===========================================================
This section contains information that is specific to ODP.NET, Managed Driver. 

1. OracleConnection object's OpenWithNewPassword() method invocation will result in an ORA-1017 error with 11.2.0.3.0 
and earlier versions of the database. [Bug 12876992]

2. Stored functions/procedures in a PDB cannot be added to a .NET Entity Framework model. [Bug 17344899]



Entity Framework Tips, Limitations, and Known Issues
====================================================
This section contains Entity Framework related information that pertains to both ODP.NET, Managed Driver and ODP.NET, 
Unmanaged Driver. 

1. Interval Day to Second and Interval Year to Month column values cannot be compared to literals in a WHERE clause of 
a LINQ to Entities or an Entity SQL query.

2. LINQ to Entities and Entity SQL (ESQL) queries that require the usage of SQL APPLY in the generated queries will 
cause SQL syntax error(s) if the Oracle Database being used does not support APPLY. In such cases, the inner exception 
message will indicate that APPLY is not supported by the database.

3. ODP.NET does not currently support wildcards that accept character ranges for the LIKE operator in Entity SQL 
(i.e. [] and [^]). [Bug 11683837]

4. Executing LINQ or ESQL query against tables with one or more column names that are close to or equal to the maximum 
length of identifiers (30 bytes) may encounter "ORA-00972: identifier is too long" error, due to the usage of alias 
identifier(s) in the generated SQL that exceed the limit.

5. An "ORA-00932: inconsistent datatypes: expected - got NCLOB" error will be encountered when trying to bind a string 
that is equal to or greater than 2,000 characters in length to an XMLType column or parameter. [Bug 12630958]

6. An "ORA-00932 : inconsistent datatypes" error can be encountered if a string of 2,000 or more characters, or a byte 
array with 4,000 bytes or more in length, is bound in a WHERE clause of a LINQ/ESQL query. The same error can be 
encountered if an entity property that maps to a BLOB, CLOB, NCLOB, LONG, LONG RAW, XMLTYPE column is used in a WHERE 
clause of a LINQ/ESQL query.

7. An "Arithmetic operation resulted in an overflow" exception can be encountered when fetching numeric values that 
have more precision than what the .NET type can support. In such cases, the LINQ or ESQL query can "cast" the value 
to a particular .NET or EDM type to limit the precision and avoid the exception. This approach can be useful if the 
LINQ/ESQL query has computed/calculated columns which will store up to 38 precision in Oracle, which cannot be 
represented as .NET decimal unless the value is casted. 

8. Oracle Database treats NULLs and empty strings the same. When executing string related operations on NULLS or empty 
strings, the result will be NULL. When comparing strings with NULLs, use the equals operator (i.e. "x == NULL") in the 
LINQ query, which will in turn use the "IS NULL" condition in the generated SQL that will appropriately detect NULL-ness.

9. If an exception message of "The store provider factory type 'Oracle.ManagedDataAccess.Client.OracleClientFactory' 
does not implement the IServiceProvider interface." is encountered when executing an Entity Framework application with 
ODP.NET, the machine.config requires and entry for ODP.NET under the <DbProviderFactories> section. To resolve this 
issue by adding an entry in the machine.config, reinstall ODAC.

10. Creating a second instance of the context that derives from DbContext within an application and executing methods 
within the scope of that context that result in an interaction with the database may result in unexpected recreation of 
the database objects if the DropCreateDatabaseAlways database initializer is used.

More Informations: https://entityframework.codeplex.com/workitem/2362

Known Workarounds:
- Use a different database initializer,
- Use an operating system authenticated user for the connection, or 
- Include "Persist Security Info=true" in the connection string (Warning: Turning on "Persist Security Info" will cause 
the password to remain as part of the connection string).