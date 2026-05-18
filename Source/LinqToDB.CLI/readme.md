<!-- omit in toc -->
# LINQ to DB CLI tools

***
> **NOTE**: This is not a library you could reference from your project, but command line utility, installed using `dotnet tool` command (see [installation notes](#installation)).
***

> See [this page](https://linq2db.github.io/articles/CLI.html) for more detailed help.

- [Installation](#installation)
  - [Choosing 32-bit vs 64-bit (Windows)](#choosing-32-bit-vs-64-bit-windows)
- [Use](#use)
  - [Usage Examples](#usage-examples)
    - [Generate SQLite database model in current folder](#generate-sqlite-database-model-in-current-folder)
    - [Generate SQLite database model using response file](#generate-sqlite-database-model-using-response-file)

## Installation

> Requires .NET 8 or higher.

Install as global tool:

`dotnet tool install -g linq2db.cli`

Update:

`dotnet tool update -g linq2db.cli`

General information on .NET Tools could be found [here](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)

### Choosing 32-bit vs 64-bit (Windows)

The tool ships as per-RID packages (`win-x64`, `win-x86`, `win-arm64`, plus Linux + macOS variants). A bare `dotnet tool install -g linq2db.cli` picks one based on your SDK architecture (usually **x64**).

The following providers REQUIRE a **32-bit** process — install the `win-x86` variant if you use any of them:

- `Microsoft.Jet.OLEDB` (legacy Access `.mdb` databases — Jet is 32-bit-only)
- `Microsoft.ACE.OLEDB.12.0` / `.16.0` when matching **32-bit Microsoft Office** is installed (provider bitness must match Office bitness)
- SQL Server Compact Edition — driver must match process bitness
- SAP HANA — driver must match process bitness; the HANA ODBC driver ships under different names for x86 vs x64, and the native dotnet client is also bitness-specific

Install the x86 variant explicitly:

```
dotnet tool install -g linq2db.cli --arch x86
```

A single tool ID can only have one architecture installed under `-g`; reinstalling replaces. To keep both x86 and x64 available, install each to a separate path and manage PATH order yourself:

```
dotnet tool install linq2db.cli --tool-path %USERPROFILE%\tools\x64 --arch x64
dotnet tool install linq2db.cli --tool-path %USERPROFILE%\tools\x86 --arch x86
```

## Use

To invoke tool use `dotnet-linq2db <PARAMETERS>` or `dotnet linq2db <PARAMETERS>` command.

Available commands:

- `dotnet linq2db help`: prints general help
- `dotnet linq2db help scaffold`: prints help for `scaffold` command
- `dotnet linq2db scaffold <options>`: performs database model scaffolding
- `dotnet linq2db template [-o template_path]`: creates base T4 template file for scaffolding customization code

For list of available options, use `dotnet linq2db help scaffold` command.

### Usage Examples

#### Generate SQLite database model in current folder

This command uses minimal set of options, required for scaffolding (database provider and connection string) and generates database model classes in current folder.

`dotnet linq2db scaffold -p SQLite -c "Data Source=c:\Databases\MyDatabase.sqlite"`

#### Generate SQLite database model using response file

This command demonstrates use of configuration file with scaffold options combined with command line options.

`dotnet linq2db scaffold -i database.json -c "Data Source=c:\Databases\MyDatabase.sqlite"`

database.json file:

```json
{
    "general": {
        "provider": "SQLite",
        "connection": "Data Source=c:\\Databases\\TestDatabase.sqlite",
        "output": "c:\\MyProject\\DbModel",
        "overwrite": true
    }
}
```

Here you can see that connection string passed using both command line and `json` config file. In such cases option passed in command line takes precedence.

Scaffold configs (response files) are convenient in many ways:

- you can store scaffolding options for your project in source control and share with other developers
- with many options it is hard to work with command line
- some options not available from CLI or hard to use due to CLI nature (e.g. various issues with escaping of parameters)
