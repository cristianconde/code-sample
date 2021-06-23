using Staks.Commons.Enums;
using Staks.Data.Entities;
using Staks.Data.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Staks.Notifications.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Returns all the notifications for the given User.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="skip"></param>
        /// <param name="size"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        Task<PagedList<Notification>> GetByUserIdAsync(int userId, int skip, int size, ApplicationEnum application);

        /// <summary>
        /// Marks all notifications as read.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        Task MarkAllAsReadAsync(int userId, ApplicationEnum application, int? maxId);

        /// <summary>
        /// Returns the unread notifications count for the given User
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<int> GetUnreadNotificationsCount(int userId, ApplicationEnum application);

        /// <summary>
        /// Sends multiple notifications at once
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        Task SendMultipleAsync(IEnumerable<Notification> notifications);

        /// <summary>
        /// Sends a notification to all the devices subscribed to the given topic
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        Task SendToTopicAsync(Notification notification);

        /// <summary>
        /// Sends notification to the User that was added to the band
        /// </summary>
        /// <param name="user"></param>
        /// <param name="band"></param>
        /// <returns></returns>
        Task SendAddedToBandAsync(User user, Business band);

        /// <summary>
        /// Sends a notifiaction to the User removed from the Band
        /// </summary>
        /// <param name="member"></param>
        /// <param name="band"></param>
        /// <returns></returns>
        Task SendRemovedFromBandAsync(Member member, Business band);

        /// <summary>
        /// Sends a notification with the resolution of the request. Only for results accepted or rejected.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task SendPhilanthropistRequestResolutionAsync(PatronRequest request);
    }
}
