using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace DevelopersHub.Unity.Networking
{
    public class Database
    {

        public static MySqlConnection connection;
        public static string server = "127.0.0.1";
        public static string username = "root";
        public static string password = "";
        public static string database = "example";

        public static bool CheckConnection()
        {
            try
            {
                if (connection == null || connection.State == ConnectionState.Closed)
                {
                    connection = new MySqlConnection("SERVER=" + server + "; DATABASE=" + database + "; UID=" + username + "; PASSWORD=" + password + ";");
                    connection.Open();
                    proccessing = false;
                }
                else if (connection.State == ConnectionState.Broken)
                {
                    connection.Close();
                    connection = new MySqlConnection("SERVER=" + server + "; DATABASE=" + database + "; UID=" + username + "; PASSWORD=" + password + ";");
                    connection.Open();
                    proccessing = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        #region Task Manager
        private class Task
        {
            public string method;
            public object[] args;
        }

        private static List<Task> tasks = new List<Task>();
        private static bool proccessing = false;

        public static void CheckTasks()
        {
            if (proccessing)
            {
                return;
            }
            if (tasks.Count > 0)
            {
                if (tasks[0] != null)
                {
                    proccessing = true;
                    CallMethod(tasks[0].method, tasks[0].args);
                }
                tasks.RemoveAt(0);
            }
        }

        public static void AddMethod(string methodName, params object[] args)
        {
            Task task = new Task();
            task.method = methodName;
            task.args = args;
            tasks.Add(task);
        }

        private static void CallMethod(string methodName, params object[] args)
        {
            Type type = typeof(Database);
            MethodInfo info = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            object value = info.Invoke(null, args);
        }
        #endregion

        #region Process Methods
        public static void ClientConnected(int id)
        {

        }

        public static void ClientDisconnected(int id)
        {

        }

        public static void ExampleMethod(int id)
        {
            if (!CheckConnection())
            {
                return;
            }
            try
            {
                string query = "";

                #region Insert Example
                string insertStringValue = "whatever";
                int insertIntValue = 99;
                query = "INSERT INTO table (intValue, stringValue, ...) VALUES(" + insertIntValue + ", '" + insertStringValue + "' , ...);";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                #endregion

                #region Update Example
                string updateStringValue = "whatever";
                int updateIntValue = 99;
                query = "UPDATE table SET intValue = " + updateIntValue + ", stringValue = '" + updateStringValue + "', ... WHERE condition;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                #endregion

                #region Select Example
                query = "SELECT intValue, stringValue, ... FROM table WHERE condition;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int selectIntValue = int.Parse(reader["intValue"].ToString());
                                string selectStringValue = reader["stringValue"].ToString();
                            }
                        }
                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // Allways make sure to set proccessing value to false when exiting a method
            proccessing = false;

        }
        #endregion

    }
}