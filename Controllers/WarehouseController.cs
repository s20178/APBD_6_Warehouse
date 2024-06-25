using Microsoft.AspNetCore.Mvc;
using System;
using Warehouse.Models;
using Warehouse.Services;

namespace Warehouse.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost("add-product")]
        public IActionResult AddProduct([FromBody] ProductWarehouseRequest request)
        {
            if (request == null || request.Amount <= 0)
            {
                return BadRequest("Invalid input data.");
            }

            try
            {
                var result = _warehouseService.AddProductToWarehouse(request);
                return Ok(new { Id = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
        [HttpPost("add-product-stored-procedure")]
        public IActionResult AddProductStoredProcedure([FromBody] ProductWarehouseRequest request)
        {
            if (request == null || request.Amount <= 0)
            {
                return BadRequest("Invalid input data.");
            }

            try
            {
                var result = _warehouseService.AddProductToWarehouseStoredProcedure(request);
                return Ok(new { Id = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }
}
