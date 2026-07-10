using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Handog_MobileApp.ViewModels.Organizer; // Adjust based on where AppNotification is located

namespace Handog_MobileApp.Services
{
    public class NotificationService
    {
        // Centralized fetch for either Organizer or Volunteer
        public async Task<List<AppNotification>> GetNotificationsAsync(int accountNum, int topCount = 10)
        {
            var list = new List<AppNotification>();

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string query = $@"
                        SELECT TOP {topCount} NotificationID, Title, Message, IsRead 
                        FROM NOTIFICATION 
                        WHERE AccountNum = @AccountNum 
                        ORDER BY CreatedAt DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AccountNum", accountNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new AppNotification
                                {
                                    NotificationID = Convert.ToInt32(reader["NotificationID"]),
                                    Title = reader["Title"]?.ToString() ?? "",
                                    Message = reader["Message"]?.ToString() ?? "",
                                    IsRead = Convert.ToBoolean(reader["IsRead"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching notifications: {ex.Message}");
                // Optional: Fallback item if database goes down
                list.Add(new AppNotification { NotificationID = -1, Title = "Offline", Message = "Could not sync updates.", IsRead = true });
            }

            return list;
        }

        // Centralized update
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string query = "UPDATE NOTIFICATION SET IsRead = 1 WHERE NotificationID = @ID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", notificationId);
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking read in service: {ex.Message}");
                return false;
            }
        }
    }
}