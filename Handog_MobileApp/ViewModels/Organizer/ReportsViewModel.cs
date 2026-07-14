    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Microsoft.Data.SqlClient;
    using Microsoft.Maui.Controls;
    using Handog_MobileApp.Models;

    namespace Handog_MobileApp.ViewModels.Organizer
    {
        public class ReportsViewModel : INotifyPropertyChanged
        {
            public EventModel CurrentEvent { get; set; }

            private string _reportExpected = "0";
            public string ReportExpected { get => _reportExpected; set { _reportExpected = value; OnPropertyChanged(); } }

            private string _reportPresent = "0";
            public string ReportPresent { get => _reportPresent; set { _reportPresent = value; OnPropertyChanged(); } }

            private string _reportRate = "0%";
            public string ReportRate { get => _reportRate; set { _reportRate = value; OnPropertyChanged(); } }

            private string _reportDate = "Loading...";
            public string ReportDate { get => _reportDate; set { _reportDate = value; OnPropertyChanged(); } }

            public ObservableCollection<ReportsModel> AttendeesList { get; set; } = new ObservableCollection<ReportsModel>();

            public ICommand GoBackCommand { get; }

            public ReportsViewModel(EventModel completedEvent)
            {
                CurrentEvent = completedEvent;

                // This handles your Back Button!
                GoBackCommand = new Command(async () => await Application.Current.MainPage.Navigation.PopAsync());

                _ = LoadReportDataAsync();
            }

            private async Task LoadReportDataAsync()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                    {
                        await conn.OpenAsync();
                        int expectedCapacity = 0;
                        int totalPresent = 0;

                        // 1. Fetch Capacity and the NEW EventCompleted Date
                        string eventSql = "SELECT VolunteerCapacity, EventCompleted FROM EVENT WHERE EventNum = @EvtNum";
                        using (SqlCommand cmd = new SqlCommand(eventSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@EvtNum", CurrentEvent.EventID);
                            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    expectedCapacity = reader["VolunteerCapacity"] != DBNull.Value ? Convert.ToInt32(reader["VolunteerCapacity"]) : 0;
                                    ReportExpected = expectedCapacity.ToString();

                                    // Format the new date you just added to the DB!
                                    if (reader["EventCompleted"] != DBNull.Value)
                                    {
                                        ReportDate = Convert.ToDateTime(reader["EventCompleted"]).ToString("MMMM dd, yyyy");
                                    }
                                    else
                                    {
                                        ReportDate = "Unknown";
                                    }
                                }
                            }
                        }

                        // 2. Fetch Volunteers and dynamically count attendance
                        string volunteersSql = @"SELECT a.Firstname + ' ' + a.Lastname AS VolunteerName, er.AttendanceStatus AS Status
                             FROM EVENTREGISTRATION er
                             INNER JOIN ACCOUNT a ON er.AccountNum = a.AccountNum
                             WHERE er.EventNum = @EvtNum
                             ORDER BY a.Firstname ASC";

                        using (SqlCommand cmd = new SqlCommand(volunteersSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@EvtNum", CurrentEvent.EventID);
                            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                            {
                                AttendeesList.Clear();
                                while (await reader.ReadAsync())
                                {
                                    string status = reader["Status"]?.ToString();
                                    if (status != "Present") status = "Absent";

                                    // Tally up the present volunteers
                                    if (status == "Present") totalPresent++;

                                    AttendeesList.Add(new ReportsModel
                                    {
                                        VolunteerName = reader["VolunteerName"].ToString(),
                                        AttendanceStatus = status
                                    });
                                }
                            }
                        }

                        // 3. Calculate Final Metrics
                        ReportPresent = totalPresent.ToString();
                        if (expectedCapacity > 0)
                        {
                            double rate = ((double)totalPresent / expectedCapacity) * 100;
                            ReportRate = $"{Math.Round(rate)}%";
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading report: {ex.Message}");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }