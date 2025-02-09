# LINQ to DB Compat

**linq2db.Compat** is an additional package that ensures compatibility of **linq2db** with applications using configuration via `System.Configuration.ConfigurationManager` (classic `app.config`/`web.config`).

## 📦 Installation

You can install the package via NuGet:

### Using NuGet Package Manager Console:
```sh
Install-Package linq2db.Compat
```

### Using .NET CLI:
```sh
dotnet add package linq2db.Compat
```

### Using PackageReference in `csproj`:
Add the following line inside the `<ItemGroup>` section of your `.csproj` file:
```xml
<PackageReference Include="linq2db.Compat" Version="6.0.0" />
```

## 🚀 Usage

This package allows using connection settings from `System.Configuration.ConfigurationManager`. To initialize linq2db with settings from `app.config` or `web.config`, add the following code:

```csharp
using LinqToDB;
using LinqToDB.Configuration;

DataConnection.DefaultSettings = LinqToDBSection.Instance;
```

### 📌 Example `app.config`:

```xml
<configuration>
  <configSections>
    <section name="linq2db" type="LinqToDB.Configuration.LinqToDBSection, linq2db.Compat" />
  </configSections>
  
  <linq2db defaultConfiguration="DefaultConnection" />
  
  <connectionStrings>
    <add name="DefaultConnection" connectionString="Data Source=.;Initial Catalog=MyDatabase;Integrated Security=True" providerName="System.Data.SqlClient" />
    <add name="SecondaryConnection" connectionString="Data Source=remote_server;Initial Catalog=OtherDatabase;User Id=user;Password=pass;" providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

## 🎯 Compatibility

This package is designed to support applications using `System.Configuration.ConfigurationManager` for linq2db configuration (classic `app.config`/`web.config`).

## 📜 License

This project is distributed under the [MIT](https://github.com/linq2db/linq2db/blob/master/MIT-LICENSE.txt) license.
