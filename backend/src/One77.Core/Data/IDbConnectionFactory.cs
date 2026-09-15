using System.Data;
using System.Threading.Tasks;

namespace One77.Core.Data
{
    /// <summary>
    /// Creates open ADO.NET connections to the ONE77 SQL Server database.
    /// Kept as a plain interface (no ORM-specific types) so this project stays
    /// consumable by .NET Framework 4.8 and .NET Core/.NET 5+ alike.
    /// </summary>
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> OpenConnectionAsync();
    }
}
