namespace BulkInsertSqlServer
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Dapper;

    using Simple.Data;

    class Program
    {
        private const string InsertStatement = "INSERT INTO TestTable (Name, Quantity, Description) VALUES (@Name, @Quantity, @Description)";

        private static List<TestTable> records;

        private static string connectionString;

        private static Dictionary<string, List<long>> times;

        private static void InitVariables(int startIndex, int endIndex)
        {
            records = new List<TestTable>();

            var builder = new StringBuilder();

            for (var i = startIndex; i < endIndex; i++)
            {
                records.Add(new TestTable
                {
                    Name = string.Format("Record: {0}", i),
                    Quantity = i,
                    Description = string.Format("Description: {0}", i)
                });

                builder.AppendLine(string.Format("{0}, {1}, {2}, {3}", i, records[i].Name, records[i].Quantity, records[i].Description));
            }

            File.WriteAllText("InsertBcp.csv", builder.ToString());

            connectionString = ConfigurationManager.ConnectionStrings["BulkInsertSqlServer"].ConnectionString;

            times = new Dictionary<string, List<long>>
            {
                { "InsertSimpleData", new List<long>() },
                { "InsertBulkCopy", new List<long>() },
                { "InsertProcedureTransaction", new List<long>() },
                { "InsertXQueryProcedure", new List<long>() },
                { "InsertSimple", new List<long>() },
                { "InsertBulk", new List<long>() },
                { "InsertBulkTransaction", new List<long>() },
                { "InsertParallel", new List<long>() }
            };
        }

        static void Main()
        {
            InitVariables(0, 10000);
            InitTest(0, 1);
        }

        private static void InitTest(int startIndex, int endIndex)
        {
            Console.WriteLine("RUNNING");

            for (var i = startIndex; i < endIndex; i++)
            {
                Console.WriteLine("Iteration {0}", i);

                InsertBulkCopy();

                InsertSimpleData();

                InsertProcedureTransaction();

                InsertXQueryProcedure();

                InsertSimple();

                InsertBulk();

                InsertBulkTransaction();

                InsertParallel();
            }

            foreach (var keyValue in times)
            {
                if (keyValue.Value.Any())
                {
                    Console.WriteLine("{0}: {1} ms", keyValue.Key, keyValue.Value.Average());
                }
            }

            Console.WriteLine("TEST ENDED. Press any key to continue.");

            Console.Read();
        }

        private static void InsertBulkCopy()
        {
            var watch = Stopwatch.StartNew();

            using (var bulkCopy = new SqlBulkCopy(connectionString))
            {
                bulkCopy.DestinationTableName = "TestTable";

                bulkCopy.BulkCopyTimeout = 0;

                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Name", "Name"));
                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Quantity", "Quantity"));
                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping("Description", "Description"));

                bulkCopy.WriteToServer(CreateDataTable());
            }

            watch.Stop();
            times["InsertBulkCopy"].Add(watch.ElapsedMilliseconds);

            DeleteRecords();
        }

        private static void InsertSimpleData()
        {
            var watch = Stopwatch.StartNew();

            Database.OpenNamedConnection("BulkInsertSqlServer").TestTables.Insert(records);

            watch.Stop();
            times["InsertSimpleData"].Add(watch.ElapsedMilliseconds);

            DeleteRecords();
        }

        private static void InsertProcedureTransaction()
        {
            var watch = Stopwatch.StartNew();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters =
                    new
                    {
                        Records =
                            CreateDataTable().AsTableValuedParameter("dbo.TestTableType")
                    };

                connection.Open();

                var transaction = connection.BeginTransaction();

                connection.Execute(
                    "InsertProcedure",
                    parameters,
                    transaction,
                    commandType: CommandType.StoredProcedure);

                transaction.Commit();
            }

            watch.Stop();
            times["InsertProcedureTransaction"].Add(watch.ElapsedMilliseconds);

            DeleteRecords();
        }

        private static void InsertXQueryProcedure()
        {
            var watch = Stopwatch.StartNew();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                connection.Execute(
                    "InsertXQueryProcedure",
                    new
                    {
                        File =
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InsertXQueryProcedure.xml")
                    },
                    commandType: CommandType.StoredProcedure);
            }

            watch.Stop();
            times["InsertXQueryProcedure"].Add(watch.ElapsedMilliseconds);

            DeleteRecords();
        }

        private static void InsertSimple()
        {
            var watch = Stopwatch.StartNew();

            foreach (var record in records)
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    connection.Execute(
                        InsertStatement,
                        record);
                }
            }

            watch.Stop();
            times["InsertSimple"].Add(watch.ElapsedMilliseconds);

            DeleteRecords();
        }

        private static void InsertBulk()
        {
            var watch = Stopwatch.StartNew();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                connection.Execute(
                    InsertStatement,
                    records);
            }

            watch.Stop();
            times["InsertBulk"].Add(watch.ElapsedMilliseconds);

            DeleteRecords();
        }

        private static void InsertBulkTransaction()
        {
            var watch = Stopwatch.StartNew();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var transaction = connection.BeginTransaction();

                connection.Execute(
                    InsertStatement,
                    records,
                    transaction);

                transaction.Commit();
            }

            watch.Stop();
            times["InsertBulkTransaction"].Add(watch.ElapsedMilliseconds);

            DeleteRecords();
        }

        private static void InsertParallel()
        {
            var watch = Stopwatch.StartNew();

            Parallel.ForEach(
                records,
                record =>
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        connection.Execute(
                            InsertStatement,
                            record);
                    }
                });

            watch.Stop();
            times["InsertParallel"].Add(watch.ElapsedMilliseconds);

            DeleteRecords();
        }

        private static DataTable CreateDataTable()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Quantity", typeof(int));
            dataTable.Columns.Add("Description", typeof(string));

            foreach (var record in records)
            {
                dataTable.Rows.Add(record.Name, record.Quantity, record.Description);
            }

            return dataTable;
        }

        private static void DeleteRecords()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                connection.Execute("TRUNCATE TABLE TestTable");
            }
        }
    }
}
