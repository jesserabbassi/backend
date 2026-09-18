using System;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main()
    {
        var cs = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                 ?? "Host=db.zwnxeihqokalcndpsspk.supabase.co;Port=5432;Username=postgres;Password=Jesser56098980;Database=postgres;SSL Mode=Require;Trust Server Certificate=true";
        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();
            Console.WriteLine("Connected, server version: " + conn.PostgresVersion);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connection failed:");
            Console.WriteLine(ex);
        }
    }
}