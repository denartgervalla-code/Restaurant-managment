using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using RestaurantManagementSystem.DataAccess;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.UI
{
    public sealed class WebApiServer : IDisposable
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private Thread _thread;
        private volatile bool _running;

        public string Url { get { return "http://localhost:5055/"; } }

        public void Start()
        {
            if (_running)
                return;

            _listener.Prefixes.Clear();
            _listener.Prefixes.Add(Url);
            _listener.Start();
            _running = true;
            _thread = new Thread(ListenLoop) { IsBackground = true };
            _thread.Start();
        }

        public void Dispose()
        {
            _running = false;
            if (_listener.IsListening)
                _listener.Stop();
            _listener.Close();
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(delegate { HandleRequest(context); });
                }
                catch
                {
                    if (_running)
                        Thread.Sleep(200);
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                AddCorsHeaders(context.Response);
                if (context.Request.HttpMethod == "OPTIONS")
                {
                    WriteJson(context.Response, 200, new { ok = true });
                    return;
                }

                string path = context.Request.Url.AbsolutePath.Trim('/');
                string method = context.Request.HttpMethod.ToUpperInvariant();
                string[] parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (path.Equals("api/health", StringComparison.OrdinalIgnoreCase))
                {
                    string error;
                    bool ok = Database.EnsureCreated(out error);
                    WriteJson(context.Response, 200, new { ok = ok, error = error });
                    return;
                }

                if (path.Equals("api/bootstrap", StringComparison.OrdinalIgnoreCase) && method == "GET")
                {
                    WriteJson(context.Response, 200, BuildBootstrapPayload());
                    return;
                }

                if (path.Equals("api/login", StringComparison.OrdinalIgnoreCase) && method == "POST")
                {
                    LoginRequest request = ReadJson<LoginRequest>(context.Request);
                    if (request == null || string.IsNullOrWhiteSpace(request.Rfid))
                    {
                        WriteError(context.Response, 400, "RFID code is required.");
                        return;
                    }

                    User user = new UserRepository().GetByRFID(request.Rfid.Trim());
                    if (user == null)
                    {
                        WriteError(context.Response, 404, "Invalid RFID code.");
                        return;
                    }

                    WriteJson(context.Response, 200, new
                    {
                        user.Id,
                        user.Name,
                        user.Role,
                        user.RFIDCode
                    });
                    return;
                }

                if (path.Equals("api/products", StringComparison.OrdinalIgnoreCase) && method == "GET")
                {
                    WriteJson(context.Response, 200, new ProductRepository().GetAll().Select(ToProductDto).ToList());
                    return;
                }

                if (path.Equals("api/tables", StringComparison.OrdinalIgnoreCase) && method == "GET")
                {
                    WriteJson(context.Response, 200, BuildTablesPayload());
                    return;
                }

                if (path.Equals("api/orders", StringComparison.OrdinalIgnoreCase) && method == "GET")
                {
                    WriteJson(context.Response, 200, BuildOrdersPayload());
                    return;
                }

                if (path.Equals("api/orders", StringComparison.OrdinalIgnoreCase) && method == "POST")
                {
                    CreateOrderRequest request = ReadJson<CreateOrderRequest>(context.Request);
                    if (request == null || request.TableId <= 0 || request.WaiterId <= 0 || request.Items == null || request.Items.Count == 0)
                    {
                        WriteError(context.Response, 400, "Table, waiter, and at least one item are required.");
                        return;
                    }

                    List<OrderItem> items = request.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity <= 0 ? 1 : i.Quantity,
                        Comment = i.Comment ?? string.Empty
                    }).ToList();

                    int orderId = new OrderRepository().CreateOrder(request.TableId, request.WaiterId, items);
                    WriteJson(context.Response, 200, new { ok = true, orderId = orderId });
                    return;
                }

                if (parts.Length == 4 && parts[0] == "api" && parts[1] == "orders" && parts[3] == "status" && method == "PUT")
                {
                    int orderId;
                    if (!int.TryParse(parts[2], out orderId))
                    {
                        WriteError(context.Response, 400, "Invalid order id.");
                        return;
                    }

                    UpdateStatusRequest request = ReadJson<UpdateStatusRequest>(context.Request);
                    if (request == null || !IsAllowedStatus(request.Status))
                    {
                        WriteError(context.Response, 400, "Status must be Pending, Preparing, or Ready.");
                        return;
                    }

                    new OrderRepository().UpdateStatus(orderId, request.Status);
                    WriteJson(context.Response, 200, new { ok = true });
                    return;
                }

                if (parts.Length == 4 && parts[0] == "api" && parts[1] == "orders" && parts[3] == "close" && method == "POST")
                {
                    int orderId;
                    if (!int.TryParse(parts[2], out orderId))
                    {
                        WriteError(context.Response, 400, "Invalid order id.");
                        return;
                    }

                    CloseOrderRequest request = ReadJson<CloseOrderRequest>(context.Request);
                    if (request == null || request.Total < 0 || string.IsNullOrWhiteSpace(request.PaymentMethod))
                    {
                        WriteError(context.Response, 400, "Payment method and total are required.");
                        return;
                    }

                    new OrderRepository().CloseOrder(orderId, request.Total, request.PaymentMethod.Trim());
                    WriteJson(context.Response, 200, new { ok = true });
                    return;
                }

                if (parts.Length == 3 && parts[0] == "api" && parts[1] == "admin" && method == "GET")
                {
                    WriteJson(context.Response, 200, BuildAdminEntity(parts[2]));
                    return;
                }

                if (parts.Length == 3 && parts[0] == "api" && parts[1] == "admin" && method == "POST")
                {
                    SaveAdminRecord(parts[2], ReadJson<Dictionary<string, object>>(context.Request));
                    WriteJson(context.Response, 200, new { ok = true });
                    return;
                }

                if (parts.Length == 4 && parts[0] == "api" && parts[1] == "admin" && method == "DELETE")
                {
                    int id;
                    if (!int.TryParse(parts[3], out id))
                    {
                        WriteError(context.Response, 400, "Invalid record id.");
                        return;
                    }

                    new AdminRepository().Delete(NormalizeEntity(parts[2]), id);
                    ProductCache.Refresh();
                    WriteJson(context.Response, 200, new { ok = true });
                    return;
                }

                WriteError(context.Response, 404, "Endpoint not found.");
            }
            catch (Exception ex)
            {
                WriteError(context.Response, 500, ex.Message);
            }
        }

        private object BuildBootstrapPayload()
        {
            string dbError;
            bool databaseReady = Database.EnsureCreated(out dbError);
            return new
            {
                databaseReady = databaseReady,
                databaseError = dbError,
                restaurantName = "Restaurant Management System",
                demoUsers = new[]
                {
                    new { rfid = "ADMIN001", role = "Admin" },
                    new { rfid = "WAITER001", role = "Waiter" }
                },
                products = new ProductRepository().GetAll().Select(ToProductDto).ToList(),
                tables = BuildTablesPayload(),
                orders = BuildOrdersPayload(),
                categories = BuildAdminEntity("categories"),
                halls = BuildAdminEntity("halls")
            };
        }

        private List<object> BuildTablesPayload()
        {
            List<Order> orders = new OrderRepository().GetActiveOrders();
            HashSet<int> busyTableIds = new HashSet<int>(orders.Select(o => o.TableId));
            HashSet<int> readyTableIds = new HashSet<int>(orders.Where(o => o.Status == "Ready").Select(o => o.TableId));

            return new OrderRepository().GetTables().Select(t => (object)new
            {
                t.Id,
                t.Name,
                t.HallId,
                t.HallName,
                Status = readyTableIds.Contains(t.Id) ? "Ready" : busyTableIds.Contains(t.Id) ? "Busy" : "Free"
            }).ToList();
        }

        private List<object> BuildOrdersPayload()
        {
            return new OrderRepository().GetActiveOrders().Select(ToOrderDto).ToList();
        }

        private object BuildAdminEntity(string entityName)
        {
            string entity = NormalizeEntity(entityName);
            DataTable table = new AdminRepository().GetTable(entity);
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            List<string> columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            foreach (DataRow row in table.Rows)
            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                foreach (string column in columns)
                    values[column] = row[column];
                rows.Add(values);
            }

            return new
            {
                entity = entity,
                columns = columns,
                rows = rows
            };
        }

        private void SaveAdminRecord(string entityName, Dictionary<string, object> data)
        {
            if (data == null)
                throw new InvalidOperationException("No record data received.");

            string entity = NormalizeEntity(entityName);
            int id = ReadInt(data, "Id");
            string name = ReadString(data, "Name");
            AdminRepository repository = new AdminRepository();

            switch (entity)
            {
                case "Categories":
                    repository.SaveCategory(id, name);
                    break;
                case "Halls":
                    repository.SaveHall(id, name);
                    break;
                case "Tables":
                    repository.SaveTable(id, name, ReadInt(data, "HallId"));
                    break;
                case "Users":
                    repository.SaveUser(id, name, ReadString(data, "Role"), ReadString(data, "RFIDCode"));
                    break;
                case "Products":
                    repository.SaveProduct(id, name, ReadDecimal(data, "Price"), ReadDecimal(data, "VAT"), ReadInt(data, "CategoryId"));
                    ProductCache.Refresh();
                    break;
                default:
                    throw new InvalidOperationException("Unsupported entity.");
            }
        }

        private static object ToProductDto(Product product)
        {
            return new
            {
                product.Id,
                product.Name,
                product.Price,
                product.VAT,
                product.CategoryId,
                product.CategoryName,
                product.PriceWithVat
            };
        }

        private static object ToOrderDto(Order order)
        {
            return new
            {
                order.Id,
                order.TableId,
                order.TableName,
                order.WaiterId,
                order.WaiterName,
                order.Status,
                CreatedAt = order.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Items = order.Items.Select(i => new
                {
                    i.Id,
                    i.OrderId,
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.Comment,
                    i.UnitPrice,
                    i.VAT,
                    i.LineTotal
                }).ToList(),
                Total = order.Items.Sum(i => i.LineTotal)
            };
        }

        private T ReadJson<T>(HttpListenerRequest request)
        {
            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(body))
                    return default(T);
                return _serializer.Deserialize<T>(body);
            }
        }

        private void WriteJson(HttpListenerResponse response, int statusCode, object value)
        {
            string json = _serializer.Serialize(value);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Flush();
            response.Close();
        }

        private void WriteError(HttpListenerResponse response, int statusCode, string message)
        {
            WriteJson(response, statusCode, new { error = message });
        }

        private static void AddCorsHeaders(HttpListenerResponse response)
        {
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        }

        private static string NormalizeEntity(string raw)
        {
            string value = (raw ?? string.Empty).Trim().ToLowerInvariant();
            switch (value)
            {
                case "products":
                    return "Products";
                case "categories":
                    return "Categories";
                case "tables":
                    return "Tables";
                case "halls":
                    return "Halls";
                case "users":
                    return "Users";
                default:
                    throw new InvalidOperationException("Invalid entity.");
            }
        }

        private static bool IsAllowedStatus(string status)
        {
            return status == "Pending" || status == "Preparing" || status == "Ready";
        }

        private static int ReadInt(Dictionary<string, object> data, string key)
        {
            object value;
            if (!data.TryGetValue(key, out value) || value == null || string.IsNullOrWhiteSpace(Convert.ToString(value)))
                return 0;
            return Convert.ToInt32(value);
        }

        private static decimal ReadDecimal(Dictionary<string, object> data, string key)
        {
            object value;
            if (!data.TryGetValue(key, out value) || value == null || string.IsNullOrWhiteSpace(Convert.ToString(value)))
                return 0m;
            return Convert.ToDecimal(value);
        }

        private static string ReadString(Dictionary<string, object> data, string key)
        {
            object value;
            return data.TryGetValue(key, out value) && value != null ? Convert.ToString(value).Trim() : string.Empty;
        }

        private sealed class LoginRequest
        {
            public string Rfid { get; set; }
        }

        private sealed class CreateOrderRequest
        {
            public int TableId { get; set; }
            public int WaiterId { get; set; }
            public List<CreateOrderItemRequest> Items { get; set; }
        }

        private sealed class CreateOrderItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public string Comment { get; set; }
        }

        private sealed class UpdateStatusRequest
        {
            public string Status { get; set; }
        }

        private sealed class CloseOrderRequest
        {
            public decimal Total { get; set; }
            public string PaymentMethod { get; set; }
        }
    }
}
