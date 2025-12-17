using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MuhasebeAPI.Helpers
{
    public class Baglanti
    {

        public SqlConnection GetConnection()
        {
            // 1. ÖNCE MSSQL_URL'yi kontrol et
            var mssqlUrl = Environment.GetEnvironmentVariable("MSSQL_URL");

            if (!string.IsNullOrEmpty(mssqlUrl))
            {
                Console.WriteLine($"Railway SQL Server'a baðlanýyor: {mssqlUrl}");
                return new SqlConnection(mssqlUrl);
            }

            // 2. Railway'de ama MSSQL_URL yoksa - DEFAULT SQL Server
            var railwayEnv = Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT");
            if (!string.IsNullOrEmpty(railwayEnv))
            {
                // Railway'de çalýþýyor ama MSSQL_URL yok
                Console.WriteLine("Railway'de default SQL Server baðlantýsý kullanýlýyor");
                var defaultConn = "Server=localhost,1433;Database=master;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;";
                return new SqlConnection(defaultConn);
            }

            // 3. Lokalde - LocalDB
            Console.WriteLine("Lokal LocalDB'ye baðlanýyor...");
            return new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\VeriTabani\MuhasebeDB.mdf;Integrated Security=True;");
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
