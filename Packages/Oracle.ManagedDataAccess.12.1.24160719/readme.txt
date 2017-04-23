Oracle.ManagedDataAccess NuGet Package 12.1.24160719 README
===========================================================

Release Notes: Oracle Data Provider for .NET, Managed Driver

September 2016

Copyright (c) 2016, Oracle and/or its affiliates. All rights reserved.

This document provides information that supplements the Oracle Data Provider for .NET (ODP.NET) documentation. 
You have downloaded Oracle Data Provider for .NET from Oracle, the license agreement to which is available at 
http://www.oracle.com/technetwork/licenses/distribution-license-152002.html

TABLE OF CONTENTS
*New Features
*Bug Fixes
*Installation and Configuration Steps
*Installation Changes
*Documentation Corrections and Additions
*ODP.NET, Managed Driver Tips, Limitations, and Known Issues

Note: The 32-bit "Oracle Developer Tools for Visual Studio" download from http://otn.oracle.com/dotnet is 
required for Entity Framework design-time features and for other Visual Studio designers such as the 
TableAdapter Wizard. This NuGet download does not enable design-time tools; it only provides run-time support. 
This version of ODP.NET supports Oracle Database version 10.2 and higher.



New Features since Oracle.ManagedDataAccess NuGet Package 12.1.24160419
=======================================================================
1. ODP.NET can connect to Oracle Database Exadata Express Cloud Service using the following instructions. 
http://www.oracle.com/technetwork/topics/dotnet/tech-info/dotnetcloudexaexpress-3112654.html


Bug Fixes since Oracle.ManagedDataAccess NuGet Package 12.1.24160419
====================================================================

21111355 LDAP: CONNECTION PERFORMANCE ISSUE WITH LDAP CONFIGURATION
22652577 CHECKSUM: HIT "ORA-12599" WHILE IT SHOULD BE "ORA-01013" AFTER CANCEL COMMAND
22936067 ODPMANAGED SSL DOESN'T SUPPORT DN MATCHING
22995665 ODPM - INCORRECT VALUE OF DATACOLUMN'S READONLY PROPERTY
23040870 ODPM DOES NOT HANDLE PROMOTION PROPERLY
23059650 SSL: NTS DOESN'T WORK WITH SQLNET.AUTHENTICATION_SERVICES=(NTS,TCPS)
23102388 ORA-01461: CAN BIND A LONG VALUE ONLY FOR INSERT INTO A LONG COLUMN MANAGED ODP
23135026 TTC_HARDEN: BEHAVIOR DIFFERENCE FOR TRANSACTION RESTRICTION IN ODPU&ODPM
23136980 ODPM: ADAPTER FILL FAIL WITH XMLTYPE WHEN RETURNPROVIDERSPECIFICTYPES=TRUE
23168763 REFCURSORS IN OUTPUT ARRAY BIND DO NOT RETURN ANY ROWS
23263802 ODPM: CONNECTION IS NOT LOCKED BEFORE DOING COMMIT/ROLLBACK RPC FOR LOCAL TXN
23265098 IMPLICITLY RETURNED RESULTSET MISSING VALID REFCURSOR WHEN CONTAINS EMPTY REFCUR
23317774 ODPM : CURSORS NOT FREED WHEN THE CONNECTION IS BEING CLOSED
23323754 ODPM: CONNECTIONS DO NOT DRAIN PROPERLY IN DTXN/HA SCENARIO
23342504 ORA-03137: MALFORMED TTC PACKET FROM CLIENT REJECTED
23559078 ODPM: UOPF_BER FLAG SHOULD NOT BE SET FOR NON-DML ARRAY BIND OPERATIONS


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
