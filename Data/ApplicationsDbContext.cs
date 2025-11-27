using Microsoft.EntityFrameworkCore;

namespace BeautyCare_API.Data
{
    public class AplicationsDbContext : DbContext
    {


        public AplicationsDbContext(DbContextOptions<AplicationsDbContext> options) : base(options)
        {

        }
       
    }
}
