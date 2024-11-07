using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace AbakConfigurator
{
    /// <summary>
    /// Делега lambla функции вызываемой на каждую полученную из таблицы строку
    /// </summary>
    /// <param name="reader"></param>
    delegate void SelectDelegate(NpgsqlDataReader reader);

    /// <summary>
    /// Класс хранящий пару, SQL запрос и делегат для выполнения этого запроса
    /// </summary>
    class CSQlPair
    {
        private SelectDelegate func;
        private String sql;

        public CSQlPair(String sql, SelectDelegate func)
        {
            this.sql = sql;
            this.func = func;
        }

        public SelectDelegate Func { get => this.func; }
        public String SQL { get => this.sql; }
    }

    /// <summary>
    /// Набор вспомогательных функция для работы с базой данных
    /// </summary>
    class CPostgresAuxil
    {
        static public void Select(NpgsqlConnection connection, SelectDelegate func, String sql)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();

            try
            {
                while (reader.Read())
                    func(reader);
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Функция выполняет сразу несколько запросов
        /// </summary>
        /// <param name="connection">Экземпляр класса соединения с базой</param>
        /// <param name="sqlQueue">Очередь SQL запросов для выполнения</param>
        static public void Select(NpgsqlConnection connection, Queue<CSQlPair> sqlQueue)
        {
            String sql = "";
            foreach (CSQlPair pair in sqlQueue)
                sql = sql + pair.SQL + ";";

            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();

            try
            {
                foreach (CSQlPair pair in sqlQueue)
                {
                    pair.Func(reader);
                    reader.NextResult();
                }
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Функция выполняет команду которая не возвращает набор данных
        /// </summary>
        static public void ExecuteNonQuery(NpgsqlConnection connection, String sql)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Выполняет сразу несколько команд в рамках одной транзакции
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        static public void ExecuteNonQuery(NpgsqlConnection connection, List<String> sqlList)
        {
            NpgsqlTransaction transaction = connection.BeginTransaction();
            try
            {
                foreach(String sql in sqlList)
                {
                    NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                    cmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
