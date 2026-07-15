using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Handog_MobileApp.Models;

namespace Handog_MobileApp.Services
{
    public class NotificationService
    {
        public async Task<List<NotificationModel>> GetNotificationsAsync(int accountNum)
        {
            var notifications = new List<NotificationModel>();
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();
                string query = @"SELECT TOP 10 NotificationID, Title, Message, IsRead 
                                 FROM NOTIFICATION 
                                 WHERE AccountNum = @AccountNum 
                                 ORDER BY CreatedAt DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@AccountNum", System.Data.SqlDbType.Int).Value = accountNum;
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            notifications.Add(new NotificationModel
                            {
                                NotificationID = Convert.ToInt32(reader["NotificationID"]),
                                Title = reader["Title"].ToString(),
                                Message = reader["Message"].ToString(),
                                IsRead = Convert.ToBoolean(reader["IsRead"])
                            });
                        }
                    }
                }
            }
            return notifications;
        }

        public async Task CreateNotificationAsync(int accountNum, string title, string message)
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();
                string query = @"INSERT INTO NOTIFICATION (AccountNum, Title, Message, IsRead, CreatedAt) 
                         VALUES (@AccountNum, @Title, @Message, 0, GETDATE())";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AccountNum", accountNum);
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Message", message);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();
                string query = "UPDATE NOTIFICATION SET IsRead = 1 WHERE NotificationID = @ID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@ID", System.Data.SqlDbType.Int).Value = notificationId;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }


    }
}