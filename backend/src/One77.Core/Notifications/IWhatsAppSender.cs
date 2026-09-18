using System.Threading.Tasks;

namespace One77.Core.Notifications
{
    public interface IWhatsAppSender
    {
        Task SendOrderConfirmationAsync(OrderConfirmation order);
    }
}
