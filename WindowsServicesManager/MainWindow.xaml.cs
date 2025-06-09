using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceProcess;
using System.Management;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;

namespace WindowsServicesManager
{
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<ServiceInfo> services = new ObservableCollection<ServiceInfo>();
        private bool sortAscending = true;

        // --- Column visibility properties ---
        private bool _showServiceName = true;
        private bool _showDescription = true;
        private bool _showStatus = true;
        private bool _showStartType = true;
        private bool _showLogOnAs = true;
        private bool _showPathName = false;
        private bool _showServiceType = false;
        private bool _showCanPauseAndContinue = false;
        private bool _showCanStop = false;
        private bool _showErrorControl = false;
        private bool _showDescriptionWmi = false;

        public bool ShowServiceName
        {
            get => _showServiceName;
            set
            {
                _showServiceName = value;
                OnPropertyChanged();
                if (ServiceNameColumn != null)
                    ServiceNameColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowDescription
        {
            get => _showDescription;
            set
            {
                _showDescription = value;
                OnPropertyChanged();
                if (DescriptionColumn != null)
                    DescriptionColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowStatus
        {
            get => _showStatus;
            set
            {
                _showStatus = value;
                OnPropertyChanged();
                if (StatusColumn != null)
                    StatusColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowStartType
        {
            get => _showStartType;
            set
            {
                _showStartType = value;
                OnPropertyChanged();
                if (StartTypeColumn != null)
                    StartTypeColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowLogOnAs
        {
            get => _showLogOnAs;
            set
            {
                _showLogOnAs = value;
                OnPropertyChanged();
                if (LogOnAsColumn != null)
                    LogOnAsColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowPathName
        {
            get => _showPathName;
            set
            {
                _showPathName = value;
                OnPropertyChanged();
                if (PathNameColumn != null)
                    PathNameColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowServiceType
        {
            get => _showServiceType;
            set
            {
                _showServiceType = value;
                OnPropertyChanged();
                if (ServiceTypeColumn != null)
                    ServiceTypeColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowCanPauseAndContinue
        {
            get => _showCanPauseAndContinue;
            set
            {
                _showCanPauseAndContinue = value;
                OnPropertyChanged();
                if (CanPauseAndContinueColumn != null)
                    CanPauseAndContinueColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowCanStop
        {
            get => _showCanStop;
            set
            {
                _showCanStop = value;
                OnPropertyChanged();
                if (CanStopColumn != null)
                    CanStopColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowErrorControl
        {
            get => _showErrorControl;
            set
            {
                _showErrorControl = value;
                OnPropertyChanged();
                if (ErrorControlColumn != null)
                    ErrorControlColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public bool ShowDescriptionWmi
        {
            get => _showDescriptionWmi;
            set
            {
                _showDescriptionWmi = value;
                OnPropertyChanged();
                if (DescriptionWmiColumn != null)
                    DescriptionWmiColumn.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Service selection for actions
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

            // Set initial column visibility after XAML is loaded
            this.ServicesDataGrid.Loaded += (s, e) => SetAllColumnVisibilities();
        }

        private void SetAllColumnVisibilities()
        {
            if (ServiceNameColumn != null) ServiceNameColumn.Visibility = ShowServiceName ? Visibility.Visible : Visibility.Collapsed;
            if (DescriptionColumn != null) DescriptionColumn.Visibility = ShowDescription ? Visibility.Visible : Visibility.Collapsed;
            if (StatusColumn != null) StatusColumn.Visibility = ShowStatus ? Visibility.Visible : Visibility.Collapsed;
            if (StartTypeColumn != null) StartTypeColumn.Visibility = ShowStartType ? Visibility.Visible : Visibility.Collapsed;
            if (LogOnAsColumn != null) LogOnAsColumn.Visibility = ShowLogOnAs ? Visibility.Visible : Visibility.Collapsed;
            if (PathNameColumn != null) PathNameColumn.Visibility = ShowPathName ? Visibility.Visible : Visibility.Collapsed;
            if (ServiceTypeColumn != null) ServiceTypeColumn.Visibility = ShowServiceType ? Visibility.Visible : Visibility.Collapsed;
            if (CanPauseAndContinueColumn != null) CanPauseAndContinueColumn.Visibility = ShowCanPauseAndContinue ? Visibility.Visible : Visibility.Collapsed;
            if (CanStopColumn != null) CanStopColumn.Visibility = ShowCanStop ? Visibility.Visible : Visibility.Collapsed;
            if (ErrorControlColumn != null) ErrorControlColumn.Visibility = ShowErrorControl ? Visibility.Visible : Visibility.Collapsed;
            if (DescriptionWmiColumn != null) DescriptionWmiColumn.Visibility = ShowDescriptionWmi ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void LoadServices()
        {
            services.Clear();
            var list = await Task.Run(() => GetAllServices());
            foreach (var s in list)
                services.Add(s);

            ApplySorting();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadServices();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text?.ToLower() ?? "";
            var filtered = GetAllServices().Where(s =>
                s.ServiceName.ToLower().Contains(query) ||
                s.DisplayName.ToLower().Contains(query));
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
                });
            }
            return result;
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySorting();
        }

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

        // --- Action Buttons ---
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedService == null) return;
            await Task.Run(() =>
            {
                using (var controller = new ServiceController(SelectedService.ServiceName))
                {
                    if (controller.Status == ServiceControllerStatus.Stopped)
                        controller.Start();
                }
            });
            LoadServices();
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedService == null) return;
            await Task.Run(() =>
            {
                using (var controller = new ServiceController(SelectedService.ServiceName))
                {
                    if (controller.Status == ServiceControllerStatus.Running)
                        controller.Stop();
                }
            });
            LoadServices();
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedService == null) return;
            await Task.Run(() =>
            {
                using (var controller = new ServiceController(SelectedService.ServiceName))
                {
                    if (controller.CanPauseAndContinue && controller.Status == ServiceControllerStatus.Running)
                        controller.Pause();
                }
            });
            LoadServices();
        }

        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}