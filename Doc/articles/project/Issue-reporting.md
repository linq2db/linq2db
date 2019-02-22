---
uid: newissue
---
# How to report an issue

To help you with your problem we need to know:

* linq2db version you are using
* Database you are using
* Code sample, demonstrating the problem & result SQL query (if any)
* Explain what is wrong

Certainly, the best way of reporting an issue would be the Pull Request with test, demonstrating an issue and fix. Or just the test. Please, when making such PR use data model from [Tests.Model](https://github.com/linq2db/linq2db/tree/master/Tests/Model) project.

If your query is not obvious and it is not clear how to write minimal reproducing sample, please read above about how to generate test sample.

## Generating the test

This page describes how to generate NUnit test, demonstrating your issue.

1. Cleanup `C:\Users\[username]\AppData\Local\Temp\linq2db` (if exists)
1. Set `LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;` before your failing query, and `LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = false;` after.
1. Execute your failing query.
1. `ExpressionTest.0.cs` file would be generated in `C:\Users\[username]\AppData\Local\Temp\linq2db`. This would contain unit test with your query and POCO model. Attach this file to the issue.

For example:

```cs
LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;

// Don't forget to trigger query execution by calling e.g. ToList()
var q = db.GetTable<MyTable>().Where(_ => _.Id > 111).ToList();

LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = false;
```