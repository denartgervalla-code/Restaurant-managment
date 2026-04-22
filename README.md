# Restaurant Management System

Classic C# Windows Forms restaurant management system targeting .NET Framework 4.7.2 for Windows 7 SP1 compatibility.

## Requirements

- Visual Studio 2017 or newer with .NET desktop development workload
- .NET Framework 4.7.2 Developer Pack
- SQL Server or SQL Server Express reachable from each POS/kitchen/admin computer over LAN

## Setup

1. Open `RestaurantManagementSystem.sln` in Visual Studio.
2. Run `Database/CreateDatabase.sql` on SQL Server.
3. Edit `RestaurantManagementSystem/App.config` and set the `RestaurantDb` connection string.
4. Build and run the project.

Default RFID demo users:

- Admin: `ADMIN001`
- Waiter: `WAITER001`

## Notes

- The app uses only standard Windows Forms controls.
- Database access is implemented with ADO.NET classes: `SqlConnection`, `SqlCommand`, `SqlDataReader`, and `SqlDataAdapter`.
- Products are cached in memory for POS speed and refreshed after admin product changes.
- Kitchen orders refresh every 3 seconds using a WinForms `Timer`.
- Receipt printing uses `PrintDocument`.
