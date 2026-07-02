using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Data.SqlClient;
using Handog_MobileApp.Models;

namespace Handog_MobileApp
{
    public class EventDetailsViewModel : BaseViewModel
    {
        private readonly int _eventNum;

        // --- Properties ---
        private EventModel _currentEvent;
        public EventModel CurrentEvent
        {
            get => _currentEvent;
            set { _currentEvent = value; OnPropertyChanged(); }
        }

        public ObservableCollection<AttendeeModel> AttendeesList { get; set; } = new ObservableCollection<AttendeeModel>();

        // --- Commands ---
        public ICommand ConfirmAttendanceCommand { get; }
        public ICommand ConfirmEventCommand { get; }
        public ICommand GoBackCommand { get; }

        public EventDetailsViewModel(EventModel selectedEvent)
        {
            _eventNum = selectedEvent.EventID; // Maps to EventNum in DB
            CurrentEvent = selectedEvent;

            ConfirmAttendanceCommand = new Command<int>(async (regNum) => await ExecuteConfirmAttendance(regNum));
            ConfirmEventCommand = new Command(async () => await ExecuteConfirmEvent());
            GoBackCommand = new Command(async () => await Application.Current.MainPage.Navigation.PopAsync());
        }

        public async Task LoadAttendeesAsync()
        {
            try
            {
                AttendeesList.Clear();
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    // Join Registration table with Account table to get Volunteer Names
                    string sql = @"SELECT ER.RegistrationNum, ER.AccountNum, ER.RegistrationDate, ER.AttendanceStatus, 
                                          A.Firstname + ' ' + A.Lastname AS VolunteerName
                                   FROM EVENTREGISTRATION ER
                                   INNER JOIN ACCOUNT A ON ER.AccountNum = A.AccountNum
                                   WHERE ER.EventNum = @EvtNum AND ER.RegistrationStatus = 'Approved'";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@EvtNum", _eventNum);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                AttendeesList.Add(new AttendeeModel
                                {
                                    RegistrationNum = Convert.ToInt32(reader["RegistrationNum"]),
                                    AccountNum = Convert.ToInt32(reader["AccountNum"]),
                                    VolunteerName = reader["VolunteerName"].ToString(),
                                    RegistrationDate = Convert.ToDateTime(reader["RegistrationDate"]).ToString("MMM dd, yyyy"),
                                    AttendanceStatus = reader["AttendanceStatus"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
        }

        private async Task ExecuteConfirmAttendance(int registrationNum)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = "UPDATE EVENTREGISTRATION SET AttendanceStatus = 'Present' WHERE RegistrationNum = @RegNum";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@RegNum", registrationNum);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                // Refresh list to update UI colors and hide the confirm button
                await LoadAttendeesAsync();
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
        }

        private async Task ExecuteConfirmEvent()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Finish Event", "Are you sure you want to mark this event as Completed?", "Yes", "No");
            if (!confirm) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = "UPDATE EVENT SET EventStatus = 'Completed' WHERE EventNum = @EvtNum";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@EvtNum", _eventNum);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await Application.Current.MainPage.DisplayAlert("Success", "Event successfully completed.", "OK");
                await Application.Current.MainPage.Navigation.PopAsync(); // Send user back to events list
            }
            catch (Exception ex) { await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK"); }
        }
    }
}