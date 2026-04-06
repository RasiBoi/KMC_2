using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace KMCEvent.Api.Data
{
    public class DatabaseManager
    {
        public string GetConnectionString()
        {
            return @"Server=.;Database=KMC_CityEventsDB;Trusted_Connection=True;";
        }

        public DataTable Execute(SqlCommand command)
        {
            var dt = new DataTable();
            using (var con = new SqlConnection(GetConnectionString()))
            {
                command.Connection = con;
                con.Open();
                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        public int ExecuteNonQuery(SqlCommand command)
        {
            using (var con = new SqlConnection(GetConnectionString()))
            {
                command.Connection = con;
                con.Open();
                return command.ExecuteNonQuery();
            }
        }

        public object ExecuteScalar(SqlCommand command)
        {
            using (var con = new SqlConnection(GetConnectionString()))
            {
                command.Connection = con;
                con.Open();
                return command.ExecuteScalar();
            }
        }
    }
}
