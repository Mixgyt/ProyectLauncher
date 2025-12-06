using System.Threading.Tasks;
using Supabase;
using System;
using dotenv.net;

namespace PinoLauncher.Services;
static class SupabaseService
{
    private static readonly Client _client;

    static SupabaseService()
    {   
        DotEnv.Load();
            try{
                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = true,
                    AutoRefreshToken = true
                };
                Console.WriteLine(Environment.GetEnvironmentVariable("SUPABASE_URL"));

                _client = new Client(
                    "https://jjdreodsctelmrdrmfgj.supabase.co",
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImpqZHJlb2RzY3RlbG1yZHJtZmdqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ3OTgyMjksImV4cCI6MjA4MDM3NDIyOX0.rzOrPoAgpenQDMSh8Q5BjJF-AdDm4svYd5Em6oJNbeA",
                    options);
            }catch(Exception ex){
                Console.WriteLine($"Error initializing Supabase client: {ex.Message}");
                throw;
            }
    }

    public static async Task InitializeAsync()
    {
        await _client.InitializeAsync();
    }

    public static Client GetClient()
    {
        return _client;
    }

    // Otros métodos para interactuar con Supabase (CRUD, autenticación, etc.)
}