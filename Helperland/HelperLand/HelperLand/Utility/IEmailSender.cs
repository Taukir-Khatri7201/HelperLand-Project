using System.Threading.Tasks;

namespace HelperLand.Utility
{
    public interface IEmailSender
    {
        Task SendEmail(Message message);
    }
}
