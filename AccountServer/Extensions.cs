using AccountServer.DB;

namespace AccountServer;

public static class Extensions
{
    public static bool SaveChangesExtended(this AppDbContext dbContext)
    {
        try
        {
            dbContext.SaveChanges();
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}