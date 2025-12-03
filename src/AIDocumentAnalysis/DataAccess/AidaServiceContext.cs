using Microsoft.EntityFrameworkCore;

namespace AIDocumentAnalysis.DataAccess
{
    public class AidaServiceContext: DbContext
    {
        public AidaServiceContext(DbContextOptions<AidaServiceContext> options)
            : base(options)
        {
        }
    }
}
