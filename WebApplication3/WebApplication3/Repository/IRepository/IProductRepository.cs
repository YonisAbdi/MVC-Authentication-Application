using WebApplication3.Models;

namespace WebApplication3.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        IEnumerable<Product> GetAll();
        void GetFirstOrDefault();
        void Update(Product obj);
    }
}
