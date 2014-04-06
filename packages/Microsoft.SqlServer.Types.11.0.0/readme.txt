To deploy an application that uses spatial data types to a machine that does not have 'System CLR Type for SQL Server' installed you also need to deploy the native assembly SqlServerSpatial110.dll.

Both x86 (32 bit) and x64 (64 bit) versions of this assembly have been added to your project under the SqlServerSpatial\x86 and SqlServerSpatial\x64 subdirectories. You need to setup the correct one of these assemblies to be deployed with your application (depending on the architecture of the target machine).


The easiest way to do this is:

 1. Copy the appropriate assembly to the root level of your project
    - In 'Solution Explorer' right-click on the appropriate assembly and select 'Copy'
    - Right click on your project and select 'Paste'

 2. Set the assembly to be copied to the output directory 
    - Right-click on the assembly in the root level of your project (not the one in the SqlServerSpatial subdirectory)
    - Select 'Properties'
    - Set the 'Copy to Output Directory' property to 'Copy Always'