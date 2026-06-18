using System;
using Microsoft.Data.SqlClient;

namespace CheckUser
{
    class Program
    {
        static void Main(string[] args)
        {
            var cb = new SqlConnectionStringBuilder();
            cb.DataSource = "clinic-app-server-123.database.windows.net";
            cb.UserID = "clinic_admin";
            cb.Password = "Clinic@2024!";
            cb.InitialCatalog = "free-sql-db-2049830";
            cb.TrustServerCertificate = true;

            using (var conn = new SqlConnection(cb.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Email, PasswordHash FROM Users WHERE Email = 'msami11095@gmail.com'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine($"Email: {reader.GetString(0)}, Hash: {reader.GetString(1)}");
                            bool verified = BCrypt.Net.BCrypt.Verify("Sami@11095", reader.GetString(1));
                            Console.WriteLine($"Verified Sami@11095: {verified}");
                        }
                    }
                }
            }
        }
    }
}
