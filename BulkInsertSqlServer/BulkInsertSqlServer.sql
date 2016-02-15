CREATE DATABASE BulkInsertSqlServer
GO

-----

USE BulkInsertSqlServer
GO

CREATE TABLE TestTable (
	Id INT NOT NULL IDENTITY(0, 1),
	Name VARCHAR(50) NOT NULL,
	Quantity INT,
	Description VARCHAR(MAX)
)

CREATE TABLE TestTableXml (Id INT IDENTITY(0, 1), XmlCol xml);
GO

-----

CREATE PROCEDURE [dbo].[InsertXQueryProcedure] @File VARCHAR(MAX)
AS
BEGIN
	DECLARE @Sql VARCHAR(MAX)

	SET @Sql = 'INSERT INTO TestTableXml(XmlCol)
		SELECT * FROM OPENROWSET(
	BULK''' + @File + ''', SINGLE_BLOB) AS x';

	DECLARE @Xml XML

	SELECT TOP 1 @Xml = XmlCol FROM TestTableXml

	INSERT INTO TestTable (Name, Quantity, Description)
	SELECT 
		t.c.value('(Name/text())[1]', 'VARCHAR(50)') as Name,
		t.c.value('(Quantity/text())[1]', 'INT') as Quantity, 
		t.c.value('(Description/text())[1]', 'VARCHAR(MAX)') as [Description] 
		FROM @Xml.nodes('/Root/TestTable') t(c)

	EXEC(@Sql)
END
GO

-----

CREATE TYPE [dbo].[TestTableType] AS TABLE(
	Name VARCHAR(50) NOT NULL,
	Quantity INT,
	Description VARCHAR(MAX)
)
GO

CREATE PROCEDURE [dbo].[InsertProcedure] @Records [TestTableType] READONLY
AS
BEGIN
	INSERT INTO TestTable (Name, Quantity, Description) SELECT Name, Quantity, [Description] FROM @Records
END
GO