using System;
using System.Data;
using System.Data.SqlClient;
using Warehouse.Models;

namespace Warehouse.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly string _connectionString;
        public WarehouseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int AddProductToWarehouse(ProductWarehouseRequest request)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    if (!ProductExists(connection, transaction, request.ProductId))
                    {
                        throw new Exception("Product not found.");
                    }

                    if (!WarehouseExists(connection, transaction, request.WarehouseId))
                    {
                        throw new Exception("Warehouse not found.");
                    }

                    var order = GetValidOrder(connection, transaction, request.ProductId, request.Amount, request.CreatedAt);
                    if (order == null)
                    {
                        throw new Exception("No valid order found for the product.");
                    }

                    if (IsOrderFulfilled(connection, transaction, order.OrderId))
                    {
                        throw new Exception("Order already fulfilled.");
                    }

                    UpdateOrderAsFulfilled(connection, transaction, order.OrderId);

                    var insertedId = InsertProductWarehouse(connection, transaction, request, order.Price);

                    transaction.Commit();
                    return insertedId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        public int AddProductToWarehouseStoredProcedure(ProductWarehouseRequest request)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("AddProductToWarehouse", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdProduct", request.ProductId);
                    command.Parameters.AddWithValue("@IdWarehouse", request.WarehouseId);
                    command.Parameters.AddWithValue("@Amount", request.Amount);
                    command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

                    var outputIdParam = new SqlParameter("@InsertedId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputIdParam);

                    command.ExecuteNonQuery();

                    return (int)outputIdParam.Value;
                }
            }
        }

        private bool ProductExists(SqlConnection connection, SqlTransaction transaction, int productId)
        {
            using (SqlCommand command = new SqlCommand("SELECT COUNT(1) FROM Product WHERE Id = @ProductId", connection, transaction))
            {
                command.Parameters.AddWithValue("@ProductId", productId);
                return (int)command.ExecuteScalar() > 0;
            }
        }

        private bool WarehouseExists(SqlConnection connection, SqlTransaction transaction, int warehouseId)
        {
            using (SqlCommand command = new SqlCommand("SELECT COUNT(1) FROM Warehouse WHERE Id = @WarehouseId", connection, transaction))
            {
                command.Parameters.AddWithValue("@WarehouseId", warehouseId);
                return (int)command.ExecuteScalar() > 0;
            }
        }

        private Order? GetValidOrder(SqlConnection connection, SqlTransaction transaction, int productId, int amount, DateTime createdAt)
        {
            using (SqlCommand command = new SqlCommand("SELECT TOP 1 Id, Price FROM [Order] WHERE ProductId = @ProductId AND Amount = @Amount AND CreatedAt < @CreatedAt", connection, transaction))
            {
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@CreatedAt", createdAt);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Order
                        {
                            OrderId = reader.GetInt32(0),
                            Price = reader.GetDecimal(1)
                        };
                    }
                }
            }
            return null;
        }

        private bool IsOrderFulfilled(SqlConnection connection, SqlTransaction transaction, int orderId)
        {
            using (SqlCommand command = new SqlCommand("SELECT COUNT(1) FROM Product_Warehouse WHERE OrderId = @OrderId", connection, transaction))
            {
                command.Parameters.AddWithValue("@OrderId", orderId);
                return (int)command.ExecuteScalar() > 0;
            }
        }

        private void UpdateOrderAsFulfilled(SqlConnection connection, SqlTransaction transaction, int orderId)
        {
            using (SqlCommand command = new SqlCommand("UPDATE [Order] SET FullfilledAt = @FullfilledAt WHERE Id = @OrderId", connection, transaction))
            {
                command.Parameters.AddWithValue("@FullfilledAt", DateTime.Now);
                command.Parameters.AddWithValue("@OrderId", orderId);
                command.ExecuteNonQuery();
            }
        }

        private int InsertProductWarehouse(SqlConnection connection, SqlTransaction transaction, ProductWarehouseRequest request, decimal price)
        {
            using (SqlCommand command = new SqlCommand("INSERT INTO Product_Warehouse (ProductId, WarehouseId, Amount, Price, CreatedAt) OUTPUT INSERTED.Id VALUES (@ProductId, @WarehouseId, @Amount, @Price, @CreatedAt)", connection, transaction))
            {
                command.Parameters.AddWithValue("@ProductId", request.ProductId);
                command.Parameters.AddWithValue("@WarehouseId", request.WarehouseId);
                command.Parameters.AddWithValue("@Amount", request.Amount);
                command.Parameters.AddWithValue("@Price", price * request.Amount);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                return (int)command.ExecuteScalar();
            }
        }
    }
}
