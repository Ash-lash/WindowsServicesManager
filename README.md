# Windows Services Manager (WinUI 3)

A modern Windows desktop application built with WinUI 3 for viewing, searching, sorting, and managing Windows services with a user-friendly interface.

## Features

- **Service Overview:** List all Windows services with key properties (name, display name, status, startup type, logon user, etc).
- **Column Chooser:** Show/hide any service property column instantly with simple checkboxes.
- **Search & Filter:** Quickly find services by typing in the search box.
- **Sorting:** Sort services by name, status, or startup type, ascending or descending.
- **Service Actions:** Start, stop, and pause selected services directly from the interface.
- **Modern DataGrid:** Uses CommunityToolkit DataGrid for a clean, tabular display.
- **Responsive UI:** Built with the latest WinUI 3 controls and styles.
- **WMI Integration:** Displays additional service info fetched via WMI, such as the executable path and full description.

## Requirements

- **Windows 10/11** with [WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/) support
- .NET 6/7/8 Desktop
- Visual Studio 2022 or newer

## Setup

1. **Clone the repo and open in Visual Studio.**
2. **Install NuGet Packages:**
   - `Microsoft.WindowsAppSDK`
   - `System.Management`
   - `CommunityToolkit.WinUI.UI.Controls.DataGrid`
3. **Build and run the project** (Run as Administrator for service control).

## Screenshots

<!-- Add screenshots here if available -->

## Usage

- Use the **search bar** to find services by name.
- Toggle columns in the **Column Chooser** expander.
- Select a service row, then use the **Start**, **Stop**, or **Pause** buttons to control the service.
- Sort the list using the dropdown and sort order toggle.

## Notes

- **Administrative privileges** are required to start, stop, or pause most Windows services.
- Some services may not support pause or stop actions.

## License

MIT License

---

*Built with ❤️ using WinUI 3 and .NET.*
