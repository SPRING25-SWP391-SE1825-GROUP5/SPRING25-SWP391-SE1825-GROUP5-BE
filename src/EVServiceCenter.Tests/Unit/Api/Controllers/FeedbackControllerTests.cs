using System;
using System.Linq;
using System.Threading.Tasks;
using Allure.Xunit.Attributes;
using EVServiceCenter.Api.Controllers;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;
using Xunit;

namespace EVServiceCenter.Tests.Unit.Api.Controllers;

public class FeedbackControllerTests
{
    private static EVDbContext CreateInMemoryDb(string name)
    {
        var options = new DbContextOptionsBuilder<EVDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;
        return new EVDbContext(options);
    }

    [Fact]
    [AllureSuite("Feedback Controller")]
    [AllureLabel("layer", "api")]
    public async Task CreateForOrderPart_Should_Validate_And_Create()
    {
        using var db = CreateInMemoryDb(nameof(CreateForOrderPart_Should_Validate_And_Create));
        db.Orders.Add(new Order { OrderId = 10, Status = "COMPLETED", CustomerId = 1 });
        db.Parts.Add(new Part { PartId = 5, PartName = "Oil Filter", PartNumber = "OF-1", Price = 10, Rating = 0 });
        db.OrderItems.Add(new OrderItem { OrderId = 10, PartId = 5, UnitPrice = 10 });
        await db.SaveChangesAsync();

        var customerRepo = new Mock<ICustomerRepository>(MockBehavior.Strict);
        customerRepo.Setup(r => r.GetCustomerByIdAsync(1)).ReturnsAsync(new Customer { CustomerId = 1, UserId = 1 });

        var controller = new FeedbackController(db, customerRepo.Object);

        var req = new CreateOrderPartFeedbackRequest { CustomerId = 1, Rating = 5, Comment = "Great!", IsAnonymous = false };
        var result = await controller.CreateForOrderPart(10, 5, req) as OkObjectResult;

        Assert.NotNull(result);
        Assert.True(db.Feedbacks.Any());
        var fb = await db.Feedbacks.FirstAsync();
        Assert.Equal(10, fb.OrderId);
        Assert.Equal(5, fb.PartId);
        customerRepo.VerifyAll();
    }

    [Fact]
    public async Task Update_Should_Allow_Admin_To_Update()
    {
        using var db = CreateInMemoryDb(nameof(Update_Should_Allow_Admin_To_Update));
        db.Customers.Add(new Customer { CustomerId = 1, UserId = 100 });
        db.Feedbacks.Add(new Feedback { FeedbackId = 5, CustomerId = 1, Rating = 3, Comment = "old", IsAnonymous = false, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, "ADMIN"),
                    new Claim(ClaimTypes.NameIdentifier, "100")
                }, "test"))
            }
        };

        var req = new UpdateFeedbackRequest { Rating = 5, Comment = "new", IsAnonymous = true };
        var result = await controller.Update(5, req) as OkObjectResult;
        Assert.NotNull(result);
        var fb = await db.Feedbacks.FirstAsync(f => f.FeedbackId == 5);
        Assert.Equal(5, fb.Rating);
        Assert.Equal("new", fb.Comment);
        Assert.True(fb.IsAnonymous);
    }

    [Fact]
    public async Task Update_Should_Reject_NonOwner_NonAdmin()
    {
        using var db = CreateInMemoryDb(nameof(Update_Should_Reject_NonOwner_NonAdmin));
        db.Customers.AddRange(
            new Customer { CustomerId = 1, UserId = 100 },
            new Customer { CustomerId = 2, UserId = 200 }
        );
        db.Feedbacks.Add(new Feedback { FeedbackId = 6, CustomerId = 2, Rating = 3, Comment = "x", IsAnonymous = false, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "100")
                }, "test"))
            }
        };

        var req = new UpdateFeedbackRequest { Rating = 2, Comment = "y", IsAnonymous = false };
        var result = await controller.Update(6, req) as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal(403, result!.StatusCode);
    }

    [Fact]
    public async Task Delete_Should_Remove_When_Exists()
    {
        using var db = CreateInMemoryDb(nameof(Delete_Should_Remove_When_Exists));
        db.Feedbacks.Add(new Feedback { FeedbackId = 8, CustomerId = 1, Rating = 4, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        var result = await controller.Delete(8) as OkObjectResult;
        Assert.NotNull(result);
        Assert.False(await db.Feedbacks.AnyAsync(f => f.FeedbackId == 8));
    }

    [Fact]
    public async Task ListByOrder_Should_Return_Feedbacks()
    {
        using var db = CreateInMemoryDb(nameof(ListByOrder_Should_Return_Feedbacks));
        db.Feedbacks.AddRange(
            new Feedback { FeedbackId = 1, OrderId = 10, Rating = 5, CreatedAt = DateTime.UtcNow },
            new Feedback { FeedbackId = 2, OrderId = 10, Rating = 3, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        var result = await controller.ListByOrder(10) as OkObjectResult;
        Assert.NotNull(result);
        var value = result!.Value!;
        var data = (System.Collections.IEnumerable)value.GetType().GetProperty("data")!.GetValue(value)!;
        Assert.Equal(2, data.Cast<object>().Count());
    }

    [Fact]
    public async Task ListByWorkOrder_Should_Return_Feedbacks()
    {
        using var db = CreateInMemoryDb(nameof(ListByWorkOrder_Should_Return_Feedbacks));
        db.Feedbacks.AddRange(
            new Feedback { FeedbackId = 1, WorkOrderId = 20, Rating = 5, CreatedAt = DateTime.UtcNow },
            new Feedback { FeedbackId = 2, WorkOrderId = 20, Rating = 3, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        var result = await controller.ListByWorkOrder(20) as OkObjectResult;
        Assert.NotNull(result);
        var value = result!.Value!;
        var data = (System.Collections.IEnumerable)value.GetType().GetProperty("data")!.GetValue(value)!;
        Assert.Equal(2, data.Cast<object>().Count());
    }

    [Fact]
    public async Task CreateForOrderPart_Should_Fail_When_Part_Not_In_Order()
    {
        using var db = CreateInMemoryDb(nameof(CreateForOrderPart_Should_Fail_When_Part_Not_In_Order));
        db.Orders.Add(new Order { OrderId = 30, Status = "COMPLETED", CustomerId = 1 });
        db.Parts.Add(new Part { PartId = 99, PartName = "X", PartNumber = "X-1", Price = 1, Rating = 0 });
        await db.SaveChangesAsync();

        var customerRepo = new Mock<ICustomerRepository>(MockBehavior.Strict);
        customerRepo.Setup(r => r.GetCustomerByIdAsync(1)).ReturnsAsync(new Customer { CustomerId = 1 });

        var controller = new FeedbackController(db, customerRepo.Object);
        var req = new CreateOrderPartFeedbackRequest { CustomerId = 1, Rating = 5 };
        var result = await controller.CreateForOrderPart(30, 99, req);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateForWorkOrderTechnician_Should_Fail_When_Tech_Mismatch()
    {
        using var db = CreateInMemoryDb(nameof(CreateForWorkOrderTechnician_Should_Fail_When_Tech_Mismatch));
        db.WorkOrders.Add(new WorkOrder { WorkOrderId = 40, Status = "COMPLETED", TechnicianId = 7 });
        db.Technicians.Add(new Technician { TechnicianId = 8, CenterId = 1, UserId = 2 });
        await db.SaveChangesAsync();

        var customerRepo = new Mock<ICustomerRepository>(MockBehavior.Strict);
        customerRepo.Setup(r => r.GetCustomerByIdAsync(1)).ReturnsAsync(new Customer { CustomerId = 1 });

        var controller = new FeedbackController(db, customerRepo.Object);
        var req = new CreateWorkOrderTechnicianFeedbackRequest { CustomerId = 1, Rating = 4 };
        var result = await controller.CreateForWorkOrderTechnician(40, 8, req);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Should_Fail_When_Customer_Not_Found()
    {
        using var db = CreateInMemoryDb(nameof(Create_Should_Fail_When_Customer_Not_Found));
        db.Orders.Add(new Order { OrderId = 55, Status = "COMPLETED", CustomerId = 2 });
        db.Parts.Add(new Part { PartId = 5, PartName = "Oil Filter", PartNumber = "OF-1", Price = 10, Rating = 0 });
        db.OrderItems.Add(new OrderItem { OrderId = 55, PartId = 5, UnitPrice = 10 });
        await db.SaveChangesAsync();

        var customerRepo = new Mock<ICustomerRepository>(MockBehavior.Strict);
        customerRepo.Setup(r => r.GetCustomerByIdAsync(1)).ReturnsAsync((Customer)null);

        var controller = new FeedbackController(db, customerRepo.Object);
        var req = new CreateOrderPartFeedbackRequest { CustomerId = 1, Rating = 5 };
        var result = await controller.CreateForOrderPart(55, 5, req);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateForOrderPart_Should_Return_BadRequest_When_Order_Not_Completed()
    {
        using var db = CreateInMemoryDb(nameof(CreateForOrderPart_Should_Return_BadRequest_When_Order_Not_Completed));
        db.Orders.Add(new Order { OrderId = 77, Status = "PENDING", CustomerId = 1 });
        db.Parts.Add(new Part { PartId = 9, PartName = "Brake Pad", PartNumber = "BP-1", Price = 20, Rating = 0 });
        db.OrderItems.Add(new OrderItem { OrderId = 77, PartId = 9, UnitPrice = 20 });
        await db.SaveChangesAsync();

        var customerRepo = new Mock<ICustomerRepository>(MockBehavior.Strict);
        customerRepo.Setup(r => r.GetCustomerByIdAsync(1)).ReturnsAsync(new Customer { CustomerId = 1, UserId = 1 });

        var controller = new FeedbackController(db, customerRepo.Object);
        var req = new CreateOrderPartFeedbackRequest { CustomerId = 1, Rating = 4 };
        var result = await controller.CreateForOrderPart(77, 9, req);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateForWorkOrderPart_Should_Validate_And_Create()
    {
        using var db = CreateInMemoryDb(nameof(CreateForWorkOrderPart_Should_Validate_And_Create));
        db.WorkOrders.Add(new WorkOrder { WorkOrderId = 50, Status = "COMPLETED", TechnicianId = 2 });
        db.Parts.Add(new Part { PartId = 3, PartName = "Air Filter", PartNumber = "AF-1", Price = 15, Rating = 0 });
        db.WorkOrderParts.Add(new WorkOrderPart { WorkOrderId = 50, PartId = 3 });
        await db.SaveChangesAsync();

        var customerRepo = new Mock<ICustomerRepository>(MockBehavior.Strict);
        customerRepo.Setup(r => r.GetCustomerByIdAsync(1)).ReturnsAsync(new Customer { CustomerId = 1, UserId = 1 });

        var controller = new FeedbackController(db, customerRepo.Object);
        var req = new CreateWorkOrderPartFeedbackRequest { CustomerId = 1, Rating = 5 };
        var result = await controller.CreateForWorkOrderPart(50, 3, req) as OkObjectResult;
        Assert.NotNull(result);
        Assert.True(db.Feedbacks.Any());
    }

    [Fact]
    public async Task CreateForWorkOrderTechnician_Should_Validate_Tech_And_Create()
    {
        using var db = CreateInMemoryDb(nameof(CreateForWorkOrderTechnician_Should_Validate_Tech_And_Create));
        db.WorkOrders.Add(new WorkOrder { WorkOrderId = 60, Status = "DONE", TechnicianId = 7 });
        db.Technicians.Add(new Technician { TechnicianId = 7, CenterId = 1, UserId = 2 });
        await db.SaveChangesAsync();

        var customerRepo = new Mock<ICustomerRepository>(MockBehavior.Strict);
        customerRepo.Setup(r => r.GetCustomerByIdAsync(1)).ReturnsAsync(new Customer { CustomerId = 1, UserId = 1 });

        var controller = new FeedbackController(db, customerRepo.Object);
        var req = new CreateWorkOrderTechnicianFeedbackRequest { CustomerId = 1, Rating = 4 };
        var result = await controller.CreateForWorkOrderTechnician(60, 7, req) as OkObjectResult;
        Assert.NotNull(result);
        Assert.True(db.Feedbacks.Any());
    }

    [Fact]
    public async Task List_Should_Filter_And_Paginate()
    {
        using var db = CreateInMemoryDb(nameof(List_Should_Filter_And_Paginate));
        db.Feedbacks.AddRange(
            new Feedback { FeedbackId = 1, CustomerId = 1, PartId = 2, Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Feedback { FeedbackId = 2, CustomerId = 2, TechnicianId = 7, Rating = 4, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        var result = await controller.List(customerId: 1, partId: null, technicianId: null, from: null, to: null, page: 1, pageSize: 10) as OkObjectResult;
        Assert.NotNull(result);
        var value = result!.Value!;
        var totalProp = value.GetType().GetProperty("total");
        Assert.NotNull(totalProp);
        var total = (int)totalProp!.GetValue(value)!;
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetById_Should_Return_NotFound_When_Missing()
    {
        using var db = CreateInMemoryDb(nameof(GetById_Should_Return_NotFound_When_Missing));
        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        var result = await controller.GetById(999);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PartSummary_Should_Return_Average_And_Count()
    {
        using var db = CreateInMemoryDb(nameof(PartSummary_Should_Return_Average_And_Count));
        db.Feedbacks.AddRange(
            new Feedback { PartId = 5, Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Feedback { PartId = 5, Rating = 3, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        var result = await controller.PartSummary(5) as OkObjectResult;
        Assert.NotNull(result);
        var data = result!.Value!.GetType().GetProperty("data")!.GetValue(result.Value!);
        var avg = (double)data!.GetType().GetProperty("avgRating")!.GetValue(data)!;
        var count = (int)data!.GetType().GetProperty("count")!.GetValue(data)!;
        Assert.Equal(2, count);
        Assert.InRange(avg, 3.9, 4.1);
    }

    [Fact]
    public async Task TechnicianSummary_Should_Return_Average_And_Count()
    {
        using var db = CreateInMemoryDb(nameof(TechnicianSummary_Should_Return_Average_And_Count));
        db.Feedbacks.AddRange(
            new Feedback { TechnicianId = 7, Rating = 4, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Feedback { TechnicianId = 7, Rating = 2, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var controller = new FeedbackController(db, new Mock<ICustomerRepository>().Object);
        var result = await controller.TechnicianSummary(7) as OkObjectResult;
        Assert.NotNull(result);
        var data = result!.Value!.GetType().GetProperty("data")!.GetValue(result.Value!);
        var avg = (double)data!.GetType().GetProperty("avgRating")!.GetValue(data)!;
        var count = (int)data!.GetType().GetProperty("count")!.GetValue(data)!;
        Assert.Equal(2, count);
        Assert.InRange(avg, 2.9, 3.1);
    }
}


