namespace Smart.Mock
{
    using System.Data.Common;
    using System.IO;

    using Microsoft.Data.Sqlite;

    public static class TestDatabase
    {
        private const string FileName = "test.db";

        private const string ConnectionString = "Data Source=" + FileName;

        public static DbConnection Initialize()
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }

            return CreateConnection();
        }

        public static DbConnection CreateConnection()
        {
            return new SqliteConnection(ConnectionString);
        }
    }
}
