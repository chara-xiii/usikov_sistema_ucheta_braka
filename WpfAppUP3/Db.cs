using System;
using System.Data.SqlClient;

namespace WpfAppUP3
{
    /// <summary>
    /// Вспомогательный класс для работы с базой данных.
    /// Учётные данные подключения заданы по заданию.
    /// </summary>
    public static class Db
    {
        public const string User = "YOUR_DB_USER";
        public const string Password = "YOUR_DB_PASSWORD";
        public const string Server = "YOUR_SQL_SERVER";
        public const string Database = "YOUR_DATABASE";

        /// <summary>
        /// Формирует строку подключения к БД по заданному заданию.
        /// </summary>
        public static string ConnectString(string server = null, string database = null)
        {
            if (string.IsNullOrWhiteSpace(server)) server = Server;
            if (string.IsNullOrWhiteSpace(database)) database = Database;

            return "Integrated Security=false; User Id=" + User + "; Password=" + Password +
                   "; Initial Catalog=" + database + ";Server=" + server;
        }

        /// <summary>
        /// Открывает новое соединение с БД.
        /// </summary>
        public static SqlConnection Open(string connectString)
        {
            SqlConnection con = new SqlConnection(connectString);
            con.Open();
            return con;
        }
    }
}
