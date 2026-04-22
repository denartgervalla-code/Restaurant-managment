using System;
using System.Collections.Generic;
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
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private Thread _thread;
        private bool _running;

        public string Url { get { return "http://localhost:5055/"; } }

        public void Start()
        {
            if (_running)
                return;

            _listener.Prefixes.Add(Url);
            _listener.Start();
            _running = true;
            _thread = new Thread(ListenLoop);
            _thread.IsBackground = true;
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
                    ThreadPool.QueueUserWorkItem(delegate { Handle(context); });
                }
                catch
                {
                    if (_running)
                        Thread.Sleep(250);
                }
            }
        }

        private void Handle(HttpListenerContext context)
        {
            try
            {
                AddCorsHeaders(context.Response);

                if (context.Request.HttpMethod == "OPTIONS")
                {
                    WriteJson(context.Response, new { ok = true });
                    return;
                }

                string path = context.Request.Url.AbsolutePath.Trim('/').ToLowerInvariant();

                if (path == "api/health")
                {
                    string error;
                    WriteJson(context.Response, new { ok = Database.EnsureCreated(out error), error = error });
                    return;
                }

                if (path == "api/products" && context.Request.HttpMethod == "GET")
                {
                    WriteJson(context.Response, new ProductRepository().GetAll());
                    return;
                }

                if (path == "api/tables" && context.Request.HttpMethod == "GET")
                {
                    WriteJson(context.Response, new OrderRepository().GetTables());
                    return;
                }

                if (path == "api/orders" && context.Request.HttpMethod == "GET")
                {
                    WriteJson(context.Response, new OrderRepository().GetActiveOrders().Select(ToOrderDto).ToList());
                    return;
                }

                if (path == "api/orders" && context.Request.HttpMethod == "POST")
                {
                    CreateOrderRequest request = ReadJson<CreateOrderRequest>(context.Request);
                    if (request == null || request.TableId <= 0 || request.Items == null || request.Items.Count == 0)
                    {
                        WriteError(context.Response, 400, "Select a table and add at least one product.");
                        return;
                    }

                    List<OrderItem> items = request.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity <= 0 ? 1 : i.Quantity,
                        Comment = i.Comment ?? string.Empty
                    }).ToList();
                    int waiterId = request.WaiterId <= 0 ? 2 : request.WaiterId;
                    int orderId = new OrderRepository().CreateOrder(request.TableId, waiterId, items);
                    WriteJson(context.Response, new { orderId = orderId });
                    return;
                }

                if (path.StartsWith("api/orders/") && path.EndsWith("/status") && context.Request.HttpMethod == "PUT")
                {
                    int orderId;
                    if (!TryGetOrderId(path, out orderId))
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
                    WriteJson(context.Response, new { ok = true });
                    return;
                }

                if (path.StartsWith("api/orders/") && path.EndsWith("/close") && context.Request.HttpMethod == "POST")
                {
                    int orderId;
                    if (!TryGetOrderId(path, out orderId))
                    {
                        WriteError(context.Response, 400, "Invalid order id.");
                        return;
                    }

                    CloseOrderRequest request = ReadJson<CloseOrderRequest>(context.Request);
                    new OrderRepository().CloseOrder(orderId, request == null ? 0m : request.Total, request == null || string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod);
                    WriteJson(context.Response, new { ok = true });
                    return;
                }

                WriteError(context.Response, 404, "Endpoint not found.");
            }
            catch (Exception ex)
            {
                WriteError(context.Response, 500, ex.Message);
            }
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
                return string.IsNullOrWhiteSpace(body) ? default(T) : _serializer.Deserialize<T>(body);
            }
        }

        private void WriteJson(HttpListenerResponse response, object value)
        {
            string json = _serializer.Serialize(value);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode = 200;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private void WriteError(HttpListenerResponse response, int statusCode, string message)
        {
            response.StatusCode = statusCode;
            WriteJson(response, new { error = message });
        }

        private static void AddCorsHeaders(HttpListenerResponse response)
        {
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        }

        private static bool TryGetOrderId(string path, out int orderId)
        {
            orderId = 0;
            string[] parts = path.Split('/');
            return parts.Length >= 3 && int.TryParse(parts[2], out orderId);
        }

        private static bool IsAllowedStatus(string status)
        {
            return status == "Pending" || status == "Preparing" || status == "Ready";
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
