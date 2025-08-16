ECHO ON
ECHO Packing %2

SET RELDIR=%1..\..\..\..\releases
SET RESDIR=%1..\..\..\..\..\NuGet\LINQPad

DEL %RELDIR%\linq2db.LINQPad.%2
DEL %RELDIR%\linq2db.LINQPad.%2.zip

REM LINQPad 5 driver archive generation
IF %2 EQU lpx (
	REM remove resource satellite assemblies
	RD /S /Q %1cs
	RD /S /Q %1de
	RD /S /Q %1es
	RD /S /Q %1fr
	RD /S /Q %1it
	RD /S /Q %1ja
	RD /S /Q %1ko
	RD /S /Q %1pl
	RD /S /Q %1pt
	RD /S /Q %1pt-BR
	RD /S /Q %1ru
	RD /S /Q %1tr
	RD /S /Q %1zh-Hans
	RD /S /Q %1zh-Hant

	REM remove not needed files
	DEL /Q %1linq2db*.xml
	DEL /Q %1*.pdb

	"C:\Program Files\7-Zip\7z.exe" -r a %RELDIR%\linq2db.LINQPad.%2.zip %1*.* %RESDIR%\Connection.png %RESDIR%\FailedConnection.png %RESDIR%\header.xml
)

REM LINQPad 7 driver archive generation
IF %2 EQU lpx6 ("C:\Program Files\7-Zip\7z.exe" a %RELDIR%\linq2db.LINQPad.%2.zip %1linq2db.LINQPad.dll %RESDIR%\Connection.png %RESDIR%\FailedConnection.png %1linq2db.LINQPad.deps.json)

REN %RELDIR%\linq2db.LINQPad.%2.zip linq2db.LINQPad.%2

