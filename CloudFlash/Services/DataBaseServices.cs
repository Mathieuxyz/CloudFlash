using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using Renci.SshNet;

namespace SGS.Services
{
    public class DataBaseServices : IDisposable
    {
        // SSH Configuration
        private const string SshHost = "pat.infolab.ecam.be:62221"; 
        private const string SshUser = "student-admin";
        private const string SshPass = "£r&49Tf2~3£@"; // Or use a Private Key

        // Database Configuration
        private const string DbUser = "clovis";
        private const string DbPass = "SGS_db_password";
        private const string DbName = "SGS_db";
        private const int LocalPort = 3307; // We bridge the server's 3306 to our 3307

        private SshClient? _sshClient;
        private ForwardedPortLocal? _forwardedPort;
        private MySqlConnection? _dbConnection;

        /// 
        /// Establishes the SSH Tunnel and opens the MariaDB connection if they aren't already active.
        /// 
        private async Task EnsureConnectedAsync()
        {
            // 1. Setup SSH Tunnel if not connected
            if (_sshClient == null || !_sshClient.IsConnected)
            {
                _sshClient = new SshClient(SshHost, SshUser, SshPass);
                _sshClient.Connect();

                _forwardedPort = new ForwardedPortLocal("127.0.0.1", LocalPort, "127.0.0.1", 3306);
                _sshClient.AddForwardedPort(_forwardedPort);
                _forwardedPort.Start();
            }

            // 2. Setup Database Connection if not open
            if (_dbConnection == null || _dbConnection.State != ConnectionState.Open)
            {
                string connString = $"Server=127.0.0.1;Port={LocalPort};Database={DbName};User={DbUser};Password={DbPass};";
                _dbConnection = new MySqlConnection(connString);
                await _dbConnection.OpenAsync();
            }
        }

        /// 
        /// Example Method: Get all data from a specific table
        /// 
        public async Task<List<string>> GetTableDataAsync(string tableName, string columnName)
        {
            var results = new List<string>();
            
            try 
            {
                await EnsureConnectedAsync();

                string query = $"SELECT {columnName} FROM {tableName};";
                using var cmd = new MySqlCommand(query, _dbConnection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(reader.GetValue(0).ToString() ?? "");
                }
            }
            catch (Exception ex)
            {
                // In a real app, log this error
                Console.WriteLine($"Database Service Error: {ex.Message}");
            }

            return results;

        }

        public async Task ExecuteNonQueryAsync(string sqlCommand)
        {
            try 
            {
                await EnsureConnectedAsync();
                using var cmd = new MySqlCommand(sqlCommand, _dbConnection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Execution Error: {ex.Message}");
            }
        }

        public async Task<List<string>> GetAllTablesAsync()
        {
            var tables = new List<string>();
            try
            {
                await EnsureConnectedAsync();
                using var cmd = new MySqlCommand("SHOW TABLES;", _dbConnection);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // Column 0 in "SHOW TABLES" is the table name
                    tables.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching tables: {ex.Message}");
            }
            return tables;
        }

        /// 
        /// Cleanup: Close the tunnel and database connection when the app closes
        /// 
        public void Dispose()
        {
            _dbConnection?.Dispose();
            _forwardedPort?.Stop();
            _sshClient?.Disconnect();
            _sshClient?.Dispose();
        }
    }
}