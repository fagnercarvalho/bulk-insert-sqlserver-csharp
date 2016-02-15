# Testing bulk insert data into SQL Server using CSharp

This is a simple Console Application to test bulk insert methods in C#.

## Requirements

- Visual Studio 2013
- SQL Server

## Setup

- Run `BulkInsertSqlServer.sql`  in your SQL server instance;
- Change the test iterations and rows to be inserted in each iteration changing the numbers in the `Main` method in the `Program.cs` file;
- Run the solution and wait for the results.

You can also run tests for the `bcp` utility running the file `InsertBcp.ps1` inside Powershell.

