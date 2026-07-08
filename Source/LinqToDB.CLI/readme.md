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

> **Install requires .NET 10 SDK or higher** — `linq2db.cli` ships as per-RID tool packages (a .NET 10 SDK feature; older `dotnet tool install` clients don't understand the pointer-package selection and would install an empty shell).
>
> **Runtime: .NET 8 or higher** — once installed, the tool runs on .NET 8 / 9 / 10.

Install as global tool:

`dotnet tool install -g linq2db.cli`

Update:

`dotnet tool update -g linq2db.cli`

General information on .NET Tools could be found [here](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)

### Choosing 32-bit vs 64-bit (Windows)

The tool ships as per-RID packages (`win-x64`, `win-x86`, `win-arm64`, plus Linux + macOS variants). A bare `dotnet tool install -g linq2db.cli` picks one based on your SDK architecture (usually **x64**).

The default x64 install works for most providers; you need a specific variant only when a database driver constrains the bitness:

**Always 32-bit** (must install `win-x86`):

- `Microsoft.Jet.OLEDB` (legacy Access `.mdb` databases — Jet has no 64-bit build)

**Bitness must match the installed driver** (install the matching variant of `linq2db.cli`):

- `Microsoft.ACE.OLEDB.12.0` / `.16.0` — must match the installed Office bitness
- SQL Server Compact Edition — must match the installed SQL CE runtime bitness
- SAP HANA — must match the installed HANA driver bitness; the HANA ODBC driver ships under different names for x86 vs x64, and the native dotnet client is also bitness-specific

Install the x86 variant explicitly:

```
dotnet tool install -g linq2db.cli --arch x86
```

A single tool ID can only have one architecture installed under `-g`. To switch architectures, either use `dotnet tool update -g linq2db.cli --arch <x86|x64>`, or uninstall first:

```
dotnet tool uninstall -g linq2db.cli
dotnet tool install -g linq2db.cli --arch x86
```

To keep both x86 and x64 available, install each to a separate path and manage `PATH` order yourself:

```
dotnet tool install linq2db.cli --tool-path C:\tools\linq2db-x64 --arch x64
dotnet tool install linq2db.cli --tool-path C:\tools\linq2db-x86 --arch x86
```

Use any paths you like; if the path contains spaces, quote it (PowerShell: `"C:\My Tools\linq2db-x86"`; cmd: `"%USERPROFILE%\tools\x86"`).

## Use

To invoke tool use `dotnet-linq2db <PARAMETERS>` or `dotnet linq2db <PARAMETERS>` command.

Available commands:

- `dotnet linq2db help`: prints general help
- `dotnet linq2db help <command>`: prints help for a specific command
- `dotnet linq2db scaffold <options>`: performs database model scaffolding
- `dotnet linq2db template [-o template_path]`: creates base T4 template file for scaffolding customization code
- `dotnet linq2db query <options>`: executes a single read-oriented SQL query and writes JSON, JSON table, or CSV output
- `dotnet linq2db mcp <options>`: runs a STDIO Model Context Protocol server exposing the `linq2db_query` tool
- `dotnet linq2db skill`: prints agent-oriented CLI usage instructions

For MCP-capable agent hosts, `mcp` is the intended integration mode. Use `query` for lighter direct invocation when MCP is unavailable, not allowed by policy, or not needed for a specific environment.

The MCP server exposes `linq2db_info` for non-secret runtime discovery of available profiles, providers, SQL dialects, defaults, and safety rules. Use it before `linq2db_query` when the active provider or dialect is unknown.

For list of available options, use `dotnet linq2db help <command>` command.

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
