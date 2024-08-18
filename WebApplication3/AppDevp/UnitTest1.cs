using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using WebApplication3.Areas.Admin.Controllers;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Repository.IRepository;
using WebApplication3.Models.ViewModels;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp.Tests
{
    [TestFixture]
    public class ProductControllerTests
    {
        private DbContextOptions<ApplicationDbContext> _options;
        private Mock<IProductRepository> _mockProductRepo;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private ProductController _controller;

        [SetUp]
        public void Setup()
        {
            // Configure in-memory database
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Mock the Product repository
            _mockProductRepo = new Mock<IProductRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUnitOfWork.Setup(u => u.Product).Returns(_mockProductRepo.Object);

            // Instantiate the controller
            _controller = new ProductController(_mockUnitOfWork.Object);
        }

        [Test]
        public async Task Index_ReturnsViewResult_WithProductList()
        {
            // Seed data
            using (var context = new ApplicationDbContext(_options))
            {
                context.Products.AddRange(new List<Product>
                {
                    new Product { Id = 1, Name = "Harry Potter", Price = 100, CategoryId = 1},
                    new Product { Id = 2, Name = "Captain Hook", Price = 300, CategoryId = 2},
                    new Product { Id = 3, Name = "Star Trek", Price = 600, CategoryId = 3}
                });
                await context.SaveChangesAsync();
            }

            // Mock repository to return the same data as in-memory DB
            _mockProductRepo.Setup(repo => repo.GetAll(It.IsAny<string>())).Returns(
                new List<Product>
                {
                    new Product { Id = 1, Name = "Harry Potter", Price = 100, CategoryId = 1},
                    new Product { Id = 2, Name = "Captain Hook", Price = 300, CategoryId = 2},
                    new Product { Id = 3, Name = "Star Trek", Price = 600, CategoryId = 3}
                }.AsQueryable());

            // Act
            var result = _controller.Index() as ViewResult;
            var model = result?.Model as List<Product>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, model?.Count);
        }

        [Test]
        public async Task Create_Post_AddsProduct_AndRedirectsToIndex()
        {
            await Task.Run(async () =>
            {
                var newProduct = new Product { Id = 4, Name = "New Product", Price = 600, CategoryId = 2 };

                var result = _controller.Create(new ProductVM { Product = newProduct }) as RedirectToActionResult;

                Assert.IsNotNull(result);
                Assert.AreEqual("Index", result.ActionName);
                _mockProductRepo.Verify(repo => repo.Add(It.IsAny<Product>()), Times.Once);
                _mockUnitOfWork.Verify(u => u.Save(), Times.Once);
            });
        }

        [Test]
        public async Task Create_Post_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            // Arrange
            var newProduct = new Product { Id = 4, Name = "", Price = 600, CategoryId = 2 }; // Invalid Name
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = _controller.Create(new ProductVM { Product = newProduct }) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
            _mockProductRepo.Verify(repo => repo.Add(It.IsAny<Product>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Save(), Times.Never);
        }

        [Test]
        public async Task Edit_Post_UpdatesProduct_AndRedirectsToIndex()
        {
            try
            {
                // Arrange
                var product = new Product { Id = 1, Name = "Harry Potter Updated", Price = 500, CategoryId = 2 };

                _mockProductRepo.Setup(repo => repo.GetFirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
                                .Returns(product);

                _mockProductRepo.Setup(repo => repo.Update(It.IsAny<Product>())).Verifiable();
                _mockUnitOfWork.Setup(u => u.Save()).Verifiable();

                // Act
                var result = _controller.Edit(product) as RedirectToActionResult;

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("Index", result.ActionName);
                _mockProductRepo.Verify(repo => repo.Update(It.IsAny<Product>()), Times.Once);
                _mockUnitOfWork.Verify(u => u.Save(), Times.Once);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught during test execution: {ex.Message}");
                throw; // Rethrow the exception to allow the test framework to handle it
            }
        }


        [Test]
        public async Task Edit_Post_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "", Price = 500, CategoryId = 2 }; // Invalid Name
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = _controller.Edit(product) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
            _mockProductRepo.Verify(repo => repo.Update(It.IsAny<Product>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Save(), Times.Never);
        }

        [Test]
        public async Task Delete_Post_DeletesProduct_AndRedirectsToIndex()
        {
            // Arrange
            var product = new Product { Id = 1, Name = "Harry Potter", Price = 100, CategoryId = 1 };

            _mockProductRepo.Setup(repo => repo.GetFirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
                            .Returns(product);

            _mockProductRepo.Setup(repo => repo.Remove(It.IsAny<Product>()))
                            .Verifiable();

            _mockUnitOfWork.Setup(u => u.Save())
                           .Verifiable();

            // Act
            var result = _controller.DeletePOST(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            _mockProductRepo.Verify(repo => repo.Remove(It.IsAny<Product>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.Save(), Times.Once);
        }

        [Test]
        public async Task Delete_Post_ReturnsNotFoundResult_WhenProductDoesNotExist()
        {
            // Arrange
            _mockProductRepo.Setup(repo => repo.GetFirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<System.Func<Product, bool>>>()))
                            .Returns((Product)null);

            // Act
            var result = _controller.DeletePOST(99) as NotFoundResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<NotFoundResult>(result);
            _mockProductRepo.Verify(repo => repo.Remove(It.IsAny<Product>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Save(), Times.Never);
        }
    }
}
