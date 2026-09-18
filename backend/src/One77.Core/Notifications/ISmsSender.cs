using System.Threading.Tasks;

namespace One77.Core.Notifications
{
    public interface ISmsSender
    {
        Task SendOrderConfirmationAsync(OrderConfirmation order);
    }
}
