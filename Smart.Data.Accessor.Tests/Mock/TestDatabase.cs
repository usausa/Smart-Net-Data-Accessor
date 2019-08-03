namespace Smart.Mock
{
    using System.Data.Common;
    using System.IO;

    using Microsoft.Data.Sqlite;

    public static class TestDatabase
    {
        private const string FileName = "test.db";
        private const string FileName2 = "test2.db";

        private const string ConnectionString = "Data Source=" + FileName;
        private const string ConnectionString2 = "Data Source=" + FileName2;

        public static DbConnection Initialize()
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            return CreateConnection();
        }

        public static DbConnection Initialize2()
        {
            if (File.Exists(FileName2))
            {
                File.Delete(FileName2);
            }

            return CreateConnection2();
        }

        public static DbConnection CreateConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        public static DbConnection CreateConnection2()
        {
            return new SqliteConnection(ConnectionString2);
        }

        public static DbConnection CreateMemory()
        {
            return new SqliteConnection("Data Source=:memory:");
        }
    }
}
