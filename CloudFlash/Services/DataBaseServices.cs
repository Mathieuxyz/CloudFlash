using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using Renci.SshNet;

namespace SGS.Services
{
    // ================================================================
    // MODEL CLASSES - one per table
    // ================================================================

    public class Customer
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public DateTime CreationDate { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Pending";
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public bool DepositPaid { get; set; }
        public DateTime? InvoiceDate { get; set; }
    }

    public class Cabinet
    {
        public int Id { get; set; }
        public string AngleIronColor { get; set; } = "";
        public decimal? AngleIronHeight { get; set; }
        public DateTime CreationDate { get; set; }
    }

    public class OrderCabinet
    {
        public int OrderId { get; set; }
        public int CabinetId { get; set; }
        public int Quantity { get; set; }
    }

    public class Locker
    {
        public int Id { get; set; }
        public int CabinetId { get; set; }
        public int Position { get; set; }
        public decimal Height { get; set; }
        public decimal Width { get; set; }
        public decimal Depth { get; set; }
        public string Color { get; set; } = "";
        public bool HasDoors { get; set; }
        public string? DoorColor { get; set; }
    }

    public class Part
    {
        public string Code { get; set; } = "";
        public string Kind { get; set; } = "";
        public string? Color { get; set; }
        public decimal? Height { get; set; }
        public decimal? Width { get; set; }
        public decimal? Depth { get; set; }
        public decimal CustomerPrice { get; set; }
        public int InStock { get; set; }
        public int MinStock { get; set; }
        public int? NbPartsByLocker { get; set; }
    }

    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class CatalogEntry
    {
        public string PartCode { get; set; } = "";
        public int SupplierId { get; set; }
        public decimal Price { get; set; }
        public int DeliveryTime { get; set; }
    }

    public class LockerPart
    {
        public int LockerId { get; set; }
        public string PartCode { get; set; } = "";
        public int Quantity { get; set; }
    }

    public class CabinetPart
    {
        public int CabinetId { get; set; }
        public string PartCode { get; set; } = "";
        public int Quantity { get; set; }
    }

    // ================================================================
    // DATABASE SERVICE
    // ================================================================

    public class DataBaseServices : IDisposable
    {
        // SSH Configuration
        private const string SshHost = "pat.infolab.ecam.be";
        private const int SshPort = 62221;
        private const string SshUser = "student-admin";
        private const string SshPass = "£r&49Tf2~3£@";

        // Database Configuration
        private const string DbUser = "clovis";
        private const string DbPass = "SGS_db_password";
        private const string DbName = "SGS_db";
        private const int LocalPort = 3307;

        private SshClient? _sshClient;
        private ForwardedPortLocal? _forwardedPort;
        private MySqlConnection? _dbConnection;

        // ----------------------------------------------------------------
        // CONNECTION
        // ----------------------------------------------------------------

        private async Task EnsureConnectedAsync()
        {
            if (_sshClient == null || !_sshClient.IsConnected)
            {
                _sshClient = new SshClient(SshHost, SshPort, SshUser, SshPass);
                _sshClient.Connect();
                _forwardedPort = new ForwardedPortLocal("127.0.0.1", LocalPort, "127.0.0.1", 3306);
                _sshClient.AddForwardedPort(_forwardedPort);
                _forwardedPort.Start();
            }

            if (_dbConnection == null || _dbConnection.State != ConnectionState.Open)
            {
                string connString = $"Server=127.0.0.1;Port={LocalPort};Database={DbName};User={DbUser};Password={DbPass};";
                _dbConnection = new MySqlConnection(connString);
                await _dbConnection.OpenAsync();
            }
        }

        // ----------------------------------------------------------------
        // CUSTOMERS
        // ----------------------------------------------------------------

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var list = new List<Customer>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand("SELECT * FROM Customers;", _dbConnection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(ReadCustomer(reader));
            return list;
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand("SELECT * FROM Customers WHERE ID = @id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadCustomer(reader) : null;
        }

        public async Task<int> AddCustomerAsync(Customer c)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                INSERT INTO Customers (FirstName, LastName, Email, Phone, Address, City, ZipCode, Country)
                VALUES (@fn, @ln, @email, @phone, @addr, @city, @zip, @country);
                SELECT LAST_INSERT_ID();", _dbConnection);
            cmd.Parameters.AddWithValue("@fn", c.FirstName);
            cmd.Parameters.AddWithValue("@ln", c.LastName);
            cmd.Parameters.AddWithValue("@email", (object?)c.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", (object?)c.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@addr", (object?)c.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@city", (object?)c.City ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@zip", (object?)c.ZipCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@country", (object?)c.Country ?? DBNull.Value);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateCustomerAsync(Customer c)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                UPDATE Customers SET FirstName=@fn, LastName=@ln, Email=@email,
                Phone=@phone, Address=@addr, City=@city, ZipCode=@zip, Country=@country
                WHERE ID=@id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", c.Id);
            cmd.Parameters.AddWithValue("@fn", c.FirstName);
            cmd.Parameters.AddWithValue("@ln", c.LastName);
            cmd.Parameters.AddWithValue("@email", (object?)c.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", (object?)c.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@addr", (object?)c.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@city", (object?)c.City ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@zip", (object?)c.ZipCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@country", (object?)c.Country ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        private static Customer ReadCustomer(MySqlDataReader r) => new()
        {
            Id           = r.GetInt32("ID"),
            FirstName    = r.GetString("FirstName"),
            LastName     = r.GetString("LastName"),
            Email        = r.IsDBNull("Email") ? null : r.GetString("Email"),
            Phone        = r.IsDBNull("Phone") ? null : r.GetString("Phone"),
            Address      = r.IsDBNull("Address") ? null : r.GetString("Address"),
            City         = r.IsDBNull("City") ? null : r.GetString("City"),
            ZipCode      = r.IsDBNull("ZipCode") ? null : r.GetString("ZipCode"),
            Country      = r.IsDBNull("Country") ? null : r.GetString("Country"),
            CreationDate = r.GetDateTime("CreationDate")
        };

        // ----------------------------------------------------------------
        // ORDERS
        // ----------------------------------------------------------------

        public async Task<List<Order>> GetOrdersByCustomerAsync(int customerId)
        {
            var list = new List<Order>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand("SELECT * FROM Orders WHERE CustomerID = @id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", customerId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(ReadOrder(reader));
            return list;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand("SELECT * FROM Orders WHERE ID = @id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadOrder(reader) : null;
        }

        public async Task<int> AddOrderAsync(Order o)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                INSERT INTO Orders (CustomerID, Status, TotalAmount, DepositAmount, DepositPaid)
                VALUES (@cid, @status, @total, @deposit, @paid);
                SELECT LAST_INSERT_ID();", _dbConnection);
            cmd.Parameters.AddWithValue("@cid", o.CustomerId);
            cmd.Parameters.AddWithValue("@status", o.Status);
            cmd.Parameters.AddWithValue("@total", o.TotalAmount);
            cmd.Parameters.AddWithValue("@deposit", o.DepositAmount);
            cmd.Parameters.AddWithValue("@paid", o.DepositPaid ? 1 : 0);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand("UPDATE Orders SET Status=@status WHERE ID=@id;", _dbConnection);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", orderId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MarkOrderInvoicedAsync(int orderId)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "UPDATE Orders SET Status='Invoiced', InvoiceDate=NOW() WHERE ID=@id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", orderId);
            await cmd.ExecuteNonQueryAsync();
        }

        private static Order ReadOrder(MySqlDataReader r) => new()
        {
            Id            = r.GetInt32("ID"),
            CustomerId    = r.GetInt32("CustomerID"),
            OrderDate     = r.GetDateTime("OrderDate"),
            Status        = r.GetString("Status"),
            TotalAmount   = r.GetDecimal("TotalAmount"),
            DepositAmount = r.GetDecimal("DepositAmount"),
            DepositPaid   = r.GetInt32("DepositPaid") == 1,
            InvoiceDate   = r.IsDBNull("InvoiceDate") ? null : r.GetDateTime("InvoiceDate")
        };

        // ----------------------------------------------------------------
        // CABINETS
        // ----------------------------------------------------------------

        public async Task<int> AddCabinetAsync(Cabinet cab)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                INSERT INTO Cabinets (AngleIronColor, AngleIronHeight)
                VALUES (@color, @height);
                SELECT LAST_INSERT_ID();", _dbConnection);
            cmd.Parameters.AddWithValue("@color", cab.AngleIronColor);
            cmd.Parameters.AddWithValue("@height", (object?)cab.AngleIronHeight ?? DBNull.Value);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<Cabinet?> GetCabinetByIdAsync(int id)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand("SELECT * FROM Cabinets WHERE ID = @id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadCabinet(reader) : null;
        }

        public async Task UpdateCabinetHeightAsync(int cabinetId, decimal totalHeight)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "UPDATE Cabinets SET AngleIronHeight=@h WHERE ID=@id;", _dbConnection);
            cmd.Parameters.AddWithValue("@h", totalHeight);
            cmd.Parameters.AddWithValue("@id", cabinetId);
            await cmd.ExecuteNonQueryAsync();
        }

        private static Cabinet ReadCabinet(MySqlDataReader r) => new()
        {
            Id              = r.GetInt32("ID"),
            AngleIronColor  = r.GetString("AngleIronColor"),
            AngleIronHeight = r.IsDBNull("AngleIronHeight") ? null : r.GetDecimal("AngleIronHeight"),
            CreationDate    = r.GetDateTime("CreationDate")
        };

        // ----------------------------------------------------------------
        // ORDER CABINETS
        // ----------------------------------------------------------------

        public async Task AddOrderCabinetAsync(int orderId, int cabinetId, int quantity)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                INSERT INTO OrderCabinets (OrderID, CabinetID, Quantity)
                VALUES (@oid, @cid, @qty);", _dbConnection);
            cmd.Parameters.AddWithValue("@oid", orderId);
            cmd.Parameters.AddWithValue("@cid", cabinetId);
            cmd.Parameters.AddWithValue("@qty", quantity);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<OrderCabinet>> GetCabinetsByOrderAsync(int orderId)
        {
            var list = new List<OrderCabinet>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "SELECT * FROM OrderCabinets WHERE OrderID = @id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", orderId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new OrderCabinet
                {
                    OrderId   = reader.GetInt32("OrderID"),
                    CabinetId = reader.GetInt32("CabinetID"),
                    Quantity  = reader.GetInt32("Quantity")
                });
            return list;
        }

        // ----------------------------------------------------------------
        // LOCKERS
        // ----------------------------------------------------------------

        public async Task<int> AddLockerAsync(Locker l)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                INSERT INTO Lockers (CabinetID, Position, Height, Width, Depth, Color, HasDoors, DoorColor)
                VALUES (@cid, @pos, @h, @w, @d, @color, @doors, @doorcolor);
                SELECT LAST_INSERT_ID();", _dbConnection);
            cmd.Parameters.AddWithValue("@cid", l.CabinetId);
            cmd.Parameters.AddWithValue("@pos", l.Position);
            cmd.Parameters.AddWithValue("@h", l.Height);
            cmd.Parameters.AddWithValue("@w", l.Width);
            cmd.Parameters.AddWithValue("@d", l.Depth);
            cmd.Parameters.AddWithValue("@color", l.Color);
            cmd.Parameters.AddWithValue("@doors", l.HasDoors ? 1 : 0);
            cmd.Parameters.AddWithValue("@doorcolor", (object?)l.DoorColor ?? DBNull.Value);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<Locker>> GetLockersByCabinetAsync(int cabinetId)
        {
            var list = new List<Locker>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "SELECT * FROM Lockers WHERE CabinetID = @id ORDER BY Position;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", cabinetId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(ReadLocker(reader));
            return list;
        }

        private static Locker ReadLocker(MySqlDataReader r) => new()
        {
            Id        = r.GetInt32("ID"),
            CabinetId = r.GetInt32("CabinetID"),
            Position  = r.GetInt32("Position"),
            Height    = r.GetDecimal("Height"),
            Width     = r.GetDecimal("Width"),
            Depth     = r.GetDecimal("Depth"),
            Color     = r.GetString("Color"),
            HasDoors  = r.GetInt32("HasDoors") == 1,
            DoorColor = r.IsDBNull("DoorColor") ? null : r.GetString("DoorColor")
        };

        // ----------------------------------------------------------------
        // LOCKER PARTS
        // ----------------------------------------------------------------

        public async Task AddLockerPartAsync(int lockerId, string partCode, int quantity)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                INSERT INTO LockerParts (LockerID, PartCode, Quantity)
                VALUES (@lid, @code, @qty);", _dbConnection);
            cmd.Parameters.AddWithValue("@lid", lockerId);
            cmd.Parameters.AddWithValue("@code", partCode);
            cmd.Parameters.AddWithValue("@qty", quantity);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<LockerPart>> GetPartsByLockerAsync(int lockerId)
        {
            var list = new List<LockerPart>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "SELECT * FROM LockerParts WHERE LockerID = @id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", lockerId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new LockerPart
                {
                    LockerId = reader.GetInt32("LockerID"),
                    PartCode = reader.GetString("PartCode"),
                    Quantity = reader.GetInt32("Quantity")
                });
            return list;
        }

        // ----------------------------------------------------------------
        // CABINET PARTS (angle irons)
        // ----------------------------------------------------------------

        public async Task AddCabinetPartAsync(int cabinetId, string partCode, int quantity = 4)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                INSERT INTO CabinetParts (CabinetID, PartCode, Quantity)
                VALUES (@cid, @code, @qty);", _dbConnection);
            cmd.Parameters.AddWithValue("@cid", cabinetId);
            cmd.Parameters.AddWithValue("@code", partCode);
            cmd.Parameters.AddWithValue("@qty", quantity);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<CabinetPart>> GetPartsByCabinetAsync(int cabinetId)
        {
            var list = new List<CabinetPart>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "SELECT * FROM CabinetParts WHERE CabinetID = @id;", _dbConnection);
            cmd.Parameters.AddWithValue("@id", cabinetId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new CabinetPart
                {
                    CabinetId = reader.GetInt32("CabinetID"),
                    PartCode  = reader.GetString("PartCode"),
                    Quantity  = reader.GetInt32("Quantity")
                });
            return list;
        }

        // ----------------------------------------------------------------
        // PARTS & STOCK
        // ----------------------------------------------------------------

        public async Task<List<Part>> GetAllPartsAsync()
        {
            var list = new List<Part>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand("SELECT * FROM Parts;", _dbConnection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(ReadPart(reader));
            return list;
        }

        public async Task<List<Part>> GetPartsByKindAsync(string kind)
        {
            var list = new List<Part>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "SELECT * FROM Parts WHERE Kind = @kind;", _dbConnection);
            cmd.Parameters.AddWithValue("@kind", kind);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(ReadPart(reader));
            return list;
        }

        public async Task<Part?> GetPartByCodeAsync(string code)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "SELECT * FROM Parts WHERE Code = @code;", _dbConnection);
            cmd.Parameters.AddWithValue("@code", code);
            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadPart(reader) : null;
        }

        public async Task UpdateStockAsync(string partCode, int newStock)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "UPDATE Parts SET InStock=@stock WHERE Code=@code;", _dbConnection);
            cmd.Parameters.AddWithValue("@stock", newStock);
            cmd.Parameters.AddWithValue("@code", partCode);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Returns all parts where InStock is below MinStock — used for restocking alerts.
        /// </summary>
        public async Task<List<Part>> GetLowStockPartsAsync()
        {
            var list = new List<Part>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(
                "SELECT * FROM Parts WHERE InStock < MinStock;", _dbConnection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(ReadPart(reader));
            return list;
        }

        private static Part ReadPart(MySqlDataReader r) => new()
        {
            Code            = r.GetString("Code"),
            Kind            = r.GetString("Kind"),
            Color           = r.IsDBNull("Color") ? null : r.GetString("Color"),
            Height          = r.IsDBNull("Height") ? null : r.GetDecimal("Height"),
            Width           = r.IsDBNull("Width") ? null : r.GetDecimal("Width"),
            Depth           = r.IsDBNull("Depth") ? null : r.GetDecimal("Depth"),
            CustomerPrice   = r.GetDecimal("CustomerPrice"),
            InStock         = r.GetInt32("InStock"),
            MinStock        = r.GetInt32("MinStock"),
            NbPartsByLocker = r.IsDBNull("NbPartsByLocker") ? null : r.GetInt32("NbPartsByLocker")
        };

        // ----------------------------------------------------------------
        // SUPPLIERS & CATALOG
        // ----------------------------------------------------------------

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            var list = new List<Supplier>();
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand("SELECT * FROM Suppliers;", _dbConnection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new Supplier
                {
                    Id   = reader.GetInt32("ID"),
                    Name = reader.GetString("Name")
                });
            return list;
        }

        /// <summary>
        /// Returns the best supplier for a part: cheapest price, then fastest delivery on tie.
        /// Implements the PDF rule directly in SQL.
        /// </summary>
        public async Task<CatalogEntry?> GetBestSupplierForPartAsync(string partCode)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                SELECT * FROM Catalog
                WHERE PartCode = @code
                ORDER BY Price ASC, DeliveryTime ASC
                LIMIT 1;", _dbConnection);
            cmd.Parameters.AddWithValue("@code", partCode);
            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? new CatalogEntry
            {
                PartCode     = reader.GetString("PartCode"),
                SupplierId   = reader.GetInt32("SupplierID"),
                Price        = reader.GetDecimal("Price"),
                DeliveryTime = reader.GetInt32("DeliveryTime")
            } : null;
        }

        public async Task UpdateCatalogPriceAsync(string partCode, int supplierId, decimal newPrice, int newDeliveryTime)
        {
            await EnsureConnectedAsync();
            using var cmd = new MySqlCommand(@"
                UPDATE Catalog SET Price=@price, DeliveryTime=@time
                WHERE PartCode=@code AND SupplierID=@sid;", _dbConnection);
            cmd.Parameters.AddWithValue("@price", newPrice);
            cmd.Parameters.AddWithValue("@time", newDeliveryTime);
            cmd.Parameters.AddWithValue("@code", partCode);
            cmd.Parameters.AddWithValue("@sid", supplierId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ----------------------------------------------------------------
        // DISPOSE
        // ----------------------------------------------------------------

        public void Dispose()
        {
            _dbConnection?.Dispose();
            _forwardedPort?.Stop();
            _sshClient?.Disconnect();
            _sshClient?.Dispose();
        }
    }
}