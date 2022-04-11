# LINQ to DB CLI tools

***
> **NOTE**: This is not a library you could reference from your project, but command line utility, installed using `dotnet tool` command (see [installation notes](#installation)).
***

## Installation

> Requres .NET Core 3.1 or higher.

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

### Customize Scaffold with Code

For more advanced scaffolding configuration you can use scaffold interceptor class (inherited from `ScaffoldInterceptors` class), passed as pre-built assembly (don't forget that scaffold utility use .net core 3.1+, so don't target it with .NET Framework TFM) or T4 template.

Main difference between assembly and T4 approach is:

- with assembly you can write customization in any .net language but need to build it to .net assembly to use
- with T4 you can use only C#, but you will have ready-to-use T4 template to modify and compilation will be done by cli tool

To invoke scaffolding with code-based customization use `--customize path_to_file` option:

`dotnet linq2db scaffold -i database.json -c "Data Source=c:\Databases\MyDatabase.sqlite" --customize CustomAssembly.dll`

`dotnet linq2db scaffold -i database.json -c "Data Source=c:\Databases\MyDatabase.sqlite" --customize CustomTemplate.t4`

CLI tool will detect custmization approach using file extension:

- `.dll`: referenced file will be loaded as assembly
- any other extension: referenced file will be treated as T4 template

#### Customization with assembly

1. Create new .net library project and reference `linq2db.Tools` nuget
2. Add class, inherited from `LinqToDB.Scaffold.ScaffoldInterceptors` and override required customization methods
3. Build assembly and use it with `--custmize` option

#### Customization with T4 template

1. Generate initial T4 template file using `dotnet linq2db template` command
2. Edit `Interceptors` class methods in template with required customization logic
3. Use template file with `--custmize` option
