using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WindowsServicesManager
{
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<ServiceInfo> services { get; } = new ObservableCollection<ServiceInfo>();
        private bool sortAscending = true;

        // --- Column chooser properties ---
        public bool ShowServiceName { get => _showServiceName; set { _showServiceName = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showServiceName = true;
        public bool ShowDescription { get => _showDescription; set { _showDescription = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showDescription = true;
        public bool ShowStatus { get => _showStatus; set { _showStatus = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showStatus = true;
        public bool ShowStartType { get => _showStartType; set { _showStartType = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showStartType = true;
        public bool ShowLogOnAs { get => _showLogOnAs; set { _showLogOnAs = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showLogOnAs = true;
        public bool ShowPathName { get => _showPathName; set { _showPathName = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showPathName = true;
        public bool ShowServiceType { get => _showServiceType; set { _showServiceType = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showServiceType = true;
        public bool ShowCanPauseAndContinue { get => _showCanPauseAndContinue; set { _showCanPauseAndContinue = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showCanPauseAndContinue = true;
        public bool ShowCanStop { get => _showCanStop; set { _showCanStop = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showCanStop = true;
        public bool ShowErrorControl { get => _showErrorControl; set { _showErrorControl = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showErrorControl = true;
        public bool ShowDescriptionWmi { get => _showDescriptionWmi; set { _showDescriptionWmi = value; OnPropertyChanged(); UpdateColumnVisibility(); } }
        private bool _showDescriptionWmi = true;

        private ServiceInfo _selectedService;
        public ServiceInfo SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsServiceSelected));
            }
        }
        public bool IsServiceSelected => SelectedService != null;

        public MainWindow DataContext { get; private set; }

        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = this;
            SearchBox.TextChanged += SearchBox_TextChanged;
            LoadServices();
            UpdateColumnVisibility();
        }

        private async void LoadServices()
        {
            services.Clear();
            var list = await Task.Run(GetAllServices);
            foreach (var s in list)
                services.Add(s);
            ApplySorting();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadServices();

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text?.ToLower() ?? "";
            var filtered = GetAllServices().Where(s =>
                (s.ServiceName ?? "").ToLower().Contains(query) ||
                (s.DisplayName ?? "").ToLower().Contains(query));
            services.Clear();
            foreach (var s in filtered)
                services.Add(s);

            ApplySorting();
        }

        public static ObservableCollection<ServiceInfo> GetAllServices()
        {
            var result = new ObservableCollection<ServiceInfo>();
            var services = ServiceController.GetServices();

            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
            var wmiData = searcher.Get()
                .Cast<ManagementObject>()
                .ToDictionary(mo => mo["Name"] as string);

            foreach (var svc in services)
            {
                string startType = "", logOnAs = "", pathName = "", serviceType = "",
                       canPauseAndContinue = "", canStop = "", errorControl = "", description = "";
                if (wmiData.TryGetValue(svc.ServiceName, out var mo))
                {
                    startType = mo["StartMode"]?.ToString() ?? "";
                    logOnAs = mo["StartName"]?.ToString() ?? "";
                    pathName = mo["PathName"]?.ToString() ?? "";
                    serviceType = mo["ServiceType"]?.ToString() ?? "";
                    canPauseAndContinue = mo["AcceptPause"]?.ToString() ?? "";
                    canStop = mo["AcceptStop"]?.ToString() ?? "";
                    errorControl = mo["ErrorControl"]?.ToString() ?? "";
                    description = mo["Description"]?.ToString() ?? "";
                }

                result.Add(new ServiceInfo
                {
                    ServiceName = svc.ServiceName,
                    DisplayName = svc.DisplayName,
                    Status = svc.Status.ToString(),
                    StartType = startType,
                    LogOnAs = logOnAs,
                    PathName = pathName,
                    ServiceType = serviceType,
                    CanPauseAndContinue = canPauseAndContinue,
                    CanStop = canStop,
                    ErrorControl = errorControl,
                    Description = description,
                    PendingStartupType = "",
                });
            }
            return result;
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplySorting();

        private void SortOrderToggle_Checked(object sender, RoutedEventArgs e)
        {
            sortAscending = SortOrderToggle.IsChecked != false;
            var rotate = new RotateTransform
            {
                Angle = sortAscending ? 0 : 180,
                CenterX = 12,
                CenterY = 12
            };
            SortOrderIcon.RenderTransform = rotate;
            ApplySorting();
        }

        private void ApplySorting()
        {
            var selected = (SortComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (string.IsNullOrEmpty(selected))
                return;

            var sorted = sortAscending
                ? services.OrderBy(s => GetSortValue(s, selected)).ToList()
                : services.OrderByDescending(s => GetSortValue(s, selected)).ToList();

            services.Clear();
            foreach (var svc in sorted)
                services.Add(svc);
        }

        private string GetSortValue(ServiceInfo s, string selected)
        {
            return selected switch
            {
                "ServiceName" => s.ServiceName,
                "Status" => s.Status,
                "StartType" => s.StartType,
                _ => s.ServiceName
            };
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedService == null) return;
            LaunchSCCommand("start", SelectedService.ServiceName);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedService == null) return;
            LaunchSCCommand("stop", SelectedService.ServiceName);
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedService == null) return;
            LaunchSCCommand("pause", SelectedService.ServiceName);
        }

        private void LaunchSCCommand(string action, string serviceName)
        {
            string scCommand = $"sc {action} \"{serviceName}\"";
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {scCommand}",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            try
            {
                var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.EnableRaisingEvents = true;
                    proc.Exited += (s, e) =>
                    {
                        DispatcherQueue.TryEnqueue(() => LoadServices());
                    };
                }
            }
            catch (Exception ex)
            {
                var _ = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to execute command. {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                }.ShowAsync();
            }
        }

        private void LaunchSCStartupTypeCommand(string serviceName, string startupType)
        {
            string scCommand = $"sc config \"{serviceName}\" start= {startupType}";
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {scCommand}",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            try
            {
                var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.EnableRaisingEvents = true;
                    proc.Exited += (s, e) =>
                    {
                        DispatcherQueue.TryEnqueue(() => LoadServices());
                    };
                }
            }
            catch (Exception ex)
            {
                var _ = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to execute command. {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                }.ShowAsync();
            }
        }

        private void SetStartupType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ServiceInfo info)
            {
                ChangeStartupType(info);
            }
        }

        private void ChangeStartupType(ServiceInfo service)
        {
            if (service == null) return;
            string newType = service.PendingStartupType;
            if (string.IsNullOrEmpty(newType))
                newType = "auto";
            LaunchSCStartupTypeCommand(service.ServiceName, newType);
        }

        private void UpdateColumnVisibility()
        {
            if (ServiceNameColumn != null) ServiceNameColumn.Visibility = ShowServiceName ? Visibility.Visible : Visibility.Collapsed;
            if (DescriptionColumn != null) DescriptionColumn.Visibility = ShowDescription ? Visibility.Visible : Visibility.Collapsed;
            if (StatusColumn != null) StatusColumn.Visibility = ShowStatus ? Visibility.Visible : Visibility.Collapsed;
            if (StartTypeColumn != null) StartTypeColumn.Visibility = ShowStartType ? Visibility.Visible : Visibility.Collapsed;
            if (SetStartupTypeColumn != null) SetStartupTypeColumn.Visibility = ShowStartType ? Visibility.Visible : Visibility.Collapsed;
            if (LogOnAsColumn != null) LogOnAsColumn.Visibility = ShowLogOnAs ? Visibility.Visible : Visibility.Collapsed;
            if (PathNameColumn != null) PathNameColumn.Visibility = ShowPathName ? Visibility.Visible : Visibility.Collapsed;
            if (ServiceTypeColumn != null) ServiceTypeColumn.Visibility = ShowServiceType ? Visibility.Visible : Visibility.Collapsed;
            if (CanPauseAndContinueColumn != null) CanPauseAndContinueColumn.Visibility = ShowCanPauseAndContinue ? Visibility.Visible : Visibility.Collapsed;
            if (CanStopColumn != null) CanStopColumn.Visibility = ShowCanStop ? Visibility.Visible : Visibility.Collapsed;
            if (ErrorControlColumn != null) ErrorControlColumn.Visibility = ShowErrorControl ? Visibility.Visible : Visibility.Collapsed;
            if (DescriptionWmiColumn != null) DescriptionWmiColumn.Visibility = ShowDescriptionWmi ? Visibility.Visible : Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ServiceInfo : INotifyPropertyChanged
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
        public string StartType { get; set; }
        public string LogOnAs { get; set; }
        public string PathName { get; set; }
        public string ServiceType { get; set; }
        public string CanPauseAndContinue { get; set; }
        public string CanStop { get; set; }
        public string ErrorControl { get; set; }
        public string Description { get; set; }

        private string _pendingStartupType;
        public string PendingStartupType
        {
            get => _pendingStartupType;
            set { _pendingStartupType = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}