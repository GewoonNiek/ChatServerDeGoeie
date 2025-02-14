﻿using MySql.Data.MySqlClient;
using System;

namespace ChatServer
{
    internal class database
    {
        // Connectionstring & connection to database variables
        public static string connectionString = "server=188.89.106.150;database=chatprogramma;user=chatprogramma;password=chatprogramma123;";
        static MySqlConnection connection = new MySqlConnection(connectionString);


        // Function to open connection to database
        public static void DatabaseConnect()
        {
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // Function to close database
        public static void DatabaseDisconnect()
        {
            connection.Close();
        }

        // Function to put groupmessage in database
        public static void putGRPInDB(string groupID, string message)
        {
            string putMsgInDBQuery = $"INSERT INTO `berichten` (`groep_id`, `bericht`) VALUES ({groupID}, '{message}');";
            MySqlCommand cmd = new MySqlCommand(putMsgInDBQuery, connection);

            try
            {
                cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
