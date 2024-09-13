IMPORTANT: We removed dependencies on `linq2db` and any data provider packages. If you need to use it, please add it manually.

Follow the next steps to create a data model from your existing database:

1. Create new *.tt file (e.g. MyDatabase.tt) in the folder where you would like to generate your data model. For example:

	MyProject
		DataModels
			MyDatabase.tt

2. Copy content from the CopyMe.<DB_NAME>.tt.txt file located in the LinqToDB.Templates folder.

3. Find the following methods and provide connection parameters:

	Load<DB_NAME>Metadata("MyServer", "MyDatabase", "root", "TestPassword");
//	Load<DB_NAME>Metadata(connectionString);

4. See how to configure T4 template in the LinqToDB.Templates folder / README.md file
   or at https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB.Templates/README.md

5. See more at https://linq2db.github.io/articles/T4.html


If you use database scaffolding, consider migration to the scaffolding tool https://www.nuget.org/packages/linq2db.cli
Discussion thread: https://github.com/linq2db/linq2db/discussions/3531
