using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.API.Controllers;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinic.UnitTests.Controllers;

public class ProcurementTests
{
    private ClinicDbContext GetMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        return new ClinicDbContext(options);
    }

    [Fact]
    public async Task REQ_PROC_01_CreateDraftPO_CalculatesTotal()
    {
        // Arrange
        var context = GetMemoryContext();
        var controller = new ProcurementController(context);
        
        var dto = new CreatePurchaseOrderDto
        {
            ClinicId = "clinic-1",
            SupplierId = "supp-1",
            Items = new List<PurchaseOrderItemDto>
            {
                new PurchaseOrderItemDto { MaterialId = "mat-1", Quantity = 10, UnitPrice = 5.0m },
                new PurchaseOrderItemDto { MaterialId = "mat-2", Quantity = 2, UnitPrice = 100.0m }
            }
        };

        // Act
        var result = await controller.CreateDraftPO(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var responseObj = createdResult.Value;
        var po = (PurchaseOrder)responseObj!.GetType().GetProperty("data")!.GetValue(responseObj, null)!;

        Assert.Equal("Draft", po.Status);
        Assert.Equal(250.0m, po.TotalAmount); // 10*5 + 2*100 = 50 + 200 = 250
        Assert.Equal(2, po.Items.Count);
    }

    [Fact]
    public async Task REQ_PROC_02_ReceiveOrder_UpdatesInventory_And_Settles()
    {
        // Arrange
        var context = GetMemoryContext();
        
        var material = new Material { Id = "mat-1", Name = "Implants", Quantity = 5 };
        context.Materials.Add(material);
        
        var po = new PurchaseOrder { Id = "po-1", Status = "Sent" };
        po.Items.Add(new PurchaseOrderItem { MaterialId = "mat-1", Quantity = 10, UnitPrice = 100 });
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var controller = new ProcurementController(context);

        // Act
        var result = await controller.ReceiveOrder("po-1");

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var dbPo = await context.PurchaseOrders.FindAsync("po-1");
        var dbMat = await context.Materials.FindAsync("mat-1");

        Assert.Equal("Settled", dbPo!.Status);
        Assert.Equal(15, dbMat!.CurrentStock); // 5 initial + 10 received
    }
}
