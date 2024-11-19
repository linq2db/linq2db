<!-- omit in toc -->
# LINQ to DB CLI tools

***
> **NOTE**: This is not a library you could reference from your project, but command line utility, installed using `dotnet tool` command (see [installation notes](#installation)).
***

> See [this page](https://linq2db.github.io/articles/CLI.html) for more detailed help.

- [Installation](#installation)
- [Use](#use)
  - [Usage Examples](#usage-examples)
    - [Generate SQLite database model in current folder](#generate-sqlite-database-model-in-current-folder)
    - [Generate SQLite database model using response file](#generate-sqlite-database-model-using-response-file)

## Installation

> Requires .NET 6 or higher.

Install as global tool:

`dotnet tool install -g linq2db.cli`

Update:

`dotnet tool update -g linq2db.cli`

General information on .NET Tools could be found [here](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)

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
