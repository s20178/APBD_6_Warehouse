using Warehouse.Models;

namespace Warehouse.Services
{
    public interface IWarehouseService
    {
        int AddProductToWarehouse(ProductWarehouseRequest request);
        int AddProductToWarehouseStoredProcedure(ProductWarehouseRequest request);
    }
}
