using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using WebApplication3.Areas.Admin.Controllers;
using WebApplication3.Data;
using WebApplication3.Migrations;
using WebApplication3.Models;
using WebApplication3.Repository.IRepository;


namespace AppDevp
{
    public class Tests
    {
        [Test]
        public void TestControllerIndexAsync_InMemory()
        {
            var mockProductRepo = new Mock<IProductRepository>();
            mockProductRepo.Setup(repo => repo.GetAll("Category"))
                .Returns(new List<Product>
                {
                        new Product { Id = 1, Name = "Harry Potter", Price = 100, CategoryId = 1},
                        new Product { Id = 2, Name = "Captain Hook", Price = 300, CategoryId = 2 },
                        new Product { Id = 3, Name = "Star Trek", Price = 600, CategoryId = 3 }
                }.AsQueryable());

            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(u => u.Product).Returns(mockProductRepo.Object);

            // Test controller logic
            var controller = new ProductController(mockUnitOfWork.Object);
            var result = controller.Index() as ViewResult;
            var model = result?.Model as List<Product>;

            // Assertions
            Assert.IsNotNull(result);
            Assert.AreEqual(3, model?.Count);
            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsInstanceOf<List<Product>>(model);
            Assert.IsNotNull(model);
        }
    }

}
