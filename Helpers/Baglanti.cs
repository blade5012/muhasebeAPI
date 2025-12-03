using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MuhasebeAPI.Helpers
{
    public class Baglanti
    {
        string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\VeriTabani\MuhasebeDB.mdf;Integrated Security=True;";
        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }

    //public class Baglanti
    //{
    //    private string connectionString = @"Server=.\SQLEXPRESS;Database=MuhasebeDB;Trusted_Connection=True;";

    //    public SqlConnection GetConnection()
    //    {
    //        return new SqlConnection(connectionString);
    //    }
    //}

}
