using FirebaseAdmin;
using Microsoft.Extensions.Logging;
using Staks.Commons;
using Staks.Commons.Enums;
using Staks.Commons.Infrastructure;
using Staks.Data.Entities;
using Staks.Data.Infrastructure;
using Staks.Data.Interfaces;
using Staks.Notifications.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase = FirebaseAdmin.Messaging;

namespace Staks.Notifications.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IDeviceService _deviceService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            IDeviceService deviceService,
            FirebaseApp firebaseApp,
            ILogger<NotificationService> logger
            )
        {
            _notificationRepository = notificationRepository;
            _deviceService = deviceService;
            _logger = logger;
        }

        public async Task<PagedList<Notification>> GetByUserIdAsync(int userId, int skip, int size, ApplicationEnum application)
        {
            return await _notificationRepository.GetByUserIdAsync(userId, skip, size, application);
        }

        public async Task MarkAllAsReadAsync(int userId, ApplicationEnum application, int? maxId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId, application, maxId);
        }

        public async Task<int> GetUnreadNotificationsCount(int userId, ApplicationEnum application)
        {
            return await _notificationRepository.GetUnreadCount(userId, application);
        }

        public async Task<Notification> SendAsync(Notification notification)
        {
            await SendPushNotification(notification);

            notification.Timestamp = DateTime.UtcNow;

            await _notificationRepository.CreateAsync(notification);
            await _notificationRepository.SaveAsync();

            return notification;
        }

        public async Task SendMultipleAsync(IEnumerable<Notification> notifications)
        {
            foreach (var notification in notifications)
            {
                await SendPushNotification(notification);
            }

            await _notificationRepository.AddRangeAsync(notifications);
            await _notificationRepository.SaveAsync();
        }

        public async Task SendToTopicAsync(Notification notification)
        {
            var messaging = Firebase.FirebaseMessaging.DefaultInstance;
            var notificationData = new Dictionary<string, string>();

            notificationData.Add("type", notification.Type.ToDescriptionString());

            var message = new Firebase.Message()
            {
                Notification = new Firebase.Notification()
                {
                    Title = notification.Subject,
                    Body = notification.Body
                },
                Topic = notification.Application.ToDescriptionString(),
                Data = notificationData
            };

            await messaging.SendAsync(message);
            await SaveNotificationToProfiles(notification);
        }

        private async Task SendPushNotification(Notification notification)
        {
            var devices = await _deviceService.GetByUserIdAsync(notification.UserId, notification.Application);
            var messaging = Firebase.FirebaseMessaging.DefaultInstance;
            var notificationData = new Dictionary<string, string>();

            notificationData.Add("type", notification.Type.ToDescriptionString());

            foreach (var device in devices)
            {
                var message = new Firebase.Message()
                {
                    Notification = new Firebase.Notification()
                    {
                        Title = notification.Subject,
                        Body = notification.Body
                    },
                    Token = device.Token,
                    Data = notificationData
                };

                try
                {
                    await messaging.SendAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(eventId: LogEvent.PushNotificationSendFailed, exception: ex.GetBaseException(), message: $"Message {message.Notification.Title}: {message.Notification.Body}, failed to be sent to UserId { notification.UserId }");
                }
            }
        }

        private async Task SaveNotificationToProfiles(Notification notification)
        {
            await _notificationRepository.AddByProfile(notification);
        }

        public async Task SendAddedToBandAsync(User user, Business band)
        {
            await SendAsync(new Notification()
            {
                UserId = user.Id,
                Subject = $"You were added to {band.Name}",
                Body = $"You are now a a member of {band.Name}.",
                Type = NotificationTypeEnum.Band,
                Application = ApplicationEnum.Musician
            });
        }

        public async Task SendRemovedFromBandAsync(Member member, Business band)
        {
            await SendAsync(new Notification()
            {
                UserId = member.UserId,
                Subject = $"You were removed from {band.Name}",
                Body = $"You are no longer a member of {band.Name}.",
                Type = NotificationTypeEnum.Band,
                Application = ApplicationEnum.Musician
            });
        }

        public async Task SendPhilanthropistRequestResolutionAsync(PatronRequest request)
        {
            var notification = new Notification
            {
                Application = ApplicationEnum.Audience,
                UserId = request.UserId,
                Type = NotificationTypeEnum.Philanthropist
            };

            switch (request.Status)
            {
                case PatronRequestStatusEnum.Accepted:
                    notification.Subject = "Your philanthropist request was accepted.";
                    notification.Body = "You can start sending tips to more artists now.";
                    break;
                case PatronRequestStatusEnum.Rejected:
                    notification.Subject = "Your philanthropist request was rejected.";
                    notification.Body = request.RejectionReason;
                    break;
                default:
                    break;
            }

            if (request.Status == PatronRequestStatusEnum.Accepted || request.Status == PatronRequestStatusEnum.Rejected)
            {
                await SendAsync(notification);
            }
        }
    }
}
