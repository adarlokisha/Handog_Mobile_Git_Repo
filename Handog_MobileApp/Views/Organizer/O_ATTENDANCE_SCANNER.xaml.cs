using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Data.SqlClient;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls; // Added the Controls namespace here!
using Microsoft.Maui.ApplicationModel;
using Handog_MobileApp; // Allows access to EventDetailsViewModel and AppConfig

namespace Handog_MobileApp.Views.Organizer;

public partial class O_ATTENDANCE_SCANNER : ContentPage
{
    private readonly int _eventNum;
    private readonly EventDetailsViewModel _parentViewModel;
    private bool _isProcessing = false;

    public O_ATTENDANCE_SCANNER(int eventNum, EventDetailsViewModel parentViewModel)
    {
        InitializeComponent();
        _eventNum = eventNum;
        _parentViewModel = parentViewModel; // We need this to tell the EventDetails page to refresh its table!

        // Tell ZXing to specifically look for QR codes to save processing power
        barcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        barcodeReader.IsDetecting = true; // Turn camera on
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        barcodeReader.IsDetecting = false; // Turn camera off
    }

    private async void CloseScanner_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // This fires the millisecond a QR code is recognized on screen
    private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        // Prevent double-scanning the same code repeatedly
        if (_isProcessing) return;
        _isProcessing = true;

        var firstHit = e.Results?.FirstOrDefault();
        if (firstHit != null)
        {
            string scannedAccountId = firstHit.Value;

            // UI changes and database calls must be sent back to the Main Thread
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                barcodeReader.IsDetecting = false; // Pause camera while we process
                await ValidateAndMarkAttendance(scannedAccountId);
            });
        }
    }

    private async Task ValidateAndMarkAttendance(string accountId)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(AppConfig.DbConnectionString))
            {
                await conn.OpenAsync();

                // We check if they are registered AND Approved for this specific event.
                // If yes, update to Present. If no, 0 rows are affected.
                string query = @"
                    UPDATE ER
                    SET ER.AttendanceStatus = 'Present'
                    FROM EVENTREGISTRATION ER
                    INNER JOIN ACCOUNT A ON ER.AccountNum = A.AccountNum
                    WHERE ER.EventNum = @EventNum 
                      AND A.Account_ID = @ScannedAccountId
                      AND ER.RegistrationStatus = 'Approved';
                    
                    SELECT @@ROWCOUNT;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EventNum", _eventNum);
                    cmd.Parameters.AddWithValue("@ScannedAccountId", accountId);

                    int rowsAffected = (int)await cmd.ExecuteScalarAsync();

                    if (rowsAffected > 0)
                    {
                        await DisplayAlert("Success!", $"Attendance recorded for User {accountId}.", "OK");
                        // Tell the Event Details page behind us to refresh the table!
                        await _parentViewModel.LoadAttendeesAsync();
                    }
                    else
                    {
                        // THE VALIDATION CATCH
                        await DisplayAlert("Scan Failed", "This volunteer is not registered or approved for this event.", "OK");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            // After they click "OK" on the alert, turn the camera back on for the next person
            _isProcessing = false;
            barcodeReader.IsDetecting = true;
        }
    }
}