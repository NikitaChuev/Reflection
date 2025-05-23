using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Reflection;

namespace FileReflection
{
    public interface IReflectable { }

    public class MainViewModel : INotifyPropertyChanged, IReflectable
    {
        public ObservableCollection<FileSystemViewModel> FileSystemItems { get; } = new ObservableCollection<FileSystemViewModel>();

        private RelayCommand _copyCommand;
        private RelayCommand _moveCommand;

        public ICommand CopyCommand => _copyCommand;
        public ICommand MoveCommand => _moveCommand;

        private FileSystemViewModel _firstSelectedItem;
        public FileSystemViewModel FirstSelectedItem
        {
            get => _firstSelectedItem;
            set
            {
                if (_firstSelectedItem != value)
                {
                    _firstSelectedItem = value;
                    OnPropertyChanged(nameof(FirstSelectedItem));
                    UpdateSelectedItemSize();
                    UpdateCommandsCanExecute();
                }
            }
        }

        private FolderViewModel _secondSelectedItem;
        public FolderViewModel SecondSelectedItem
        {
            get => _secondSelectedItem;
            set
            {
                if (_secondSelectedItem != value)
                {
                    _secondSelectedItem = value;
                    OnPropertyChanged(nameof(SecondSelectedItem));
                    UpdateCommandsCanExecute();
                }
            }
        }

        private long _selectedItemSize;
        public long SelectedItemSize
        {
            get => _selectedItemSize;
            set
            {
                if (_selectedItemSize != value)
                {
                    _selectedItemSize = value;
                    OnPropertyChanged(nameof(SelectedItemSize));
                }
            }
        }

        private string _assemblyPath;
        public string AssemblyPath
        {
            get => _assemblyPath;
            set
            {
                if (_assemblyPath != value)
                {
                    _assemblyPath = value;
                    OnPropertyChanged(nameof(AssemblyPath));
                }
            }
        }

        public ObservableCollection<Type> ReflectedTypes { get; } = new ObservableCollection<Type>();
        private Type _selectedReflectedType;
        public Type SelectedReflectedType
        {
            get => _selectedReflectedType;
            set
            {
                bool changed = _selectedReflectedType != value;
                _selectedReflectedType = value;
                OnPropertyChanged(nameof(SelectedReflectedType));
                LoadMethodsForSelectedType();
            }
        }

        public ObservableCollection<MethodInfo> ReflectedMethods { get; } = new ObservableCollection<MethodInfo>();
        private MethodInfo _selectedReflectedMethod;
        public MethodInfo SelectedReflectedMethod
        {
            get => _selectedReflectedMethod;
            set
            {
                bool changed = _selectedReflectedMethod != value;
                _selectedReflectedMethod = value;
                OnPropertyChanged(nameof(SelectedReflectedMethod));
                LoadParametersForSelectedMethod();
            }
        }

        public ObservableCollection<ParameterViewModel> ReflectedParameters { get; } = new ObservableCollection<ParameterViewModel>();

        private string _reflectionResult;
        public string ReflectionResult
        {
            get => _reflectionResult;
            set
            {
                if (_reflectionResult != value)
                {
                    _reflectionResult = value;
                    OnPropertyChanged(nameof(ReflectionResult));
                }
            }
        }

        private RelayCommand _loadAssemblyCommand;
        public ICommand LoadAssemblyCommand => _loadAssemblyCommand;

        private RelayCommand _executeReflectedMethodCommand;
        public ICommand ExecuteReflectedMethodCommand => _executeReflectedMethodCommand;

        public MainViewModel()
        {
            _copyCommand = new RelayCommand(Copy, CanCopy);
            _moveCommand = new RelayCommand(Move, CanMove);

            _loadAssemblyCommand = new RelayCommand(_ => LoadAssembly());
            _executeReflectedMethodCommand = new RelayCommand(_ => ExecuteReflectedMethod(), _ => SelectedReflectedMethod != null);

            InitializeFileSystem();
        }

        private void InitializeFileSystem()
        {
            var rootFolder = new FolderViewModel { Name = "Root" };
            var subFolder = new FolderViewModel { Name = "Subfolder" };
            var file1 = new FileViewModel("File1.txt", 1024);
            var file2 = new FileViewModel("File2.txt", 2048);
            subFolder.AddItem(file1);
            subFolder.AddItem(file2);
            var subFolder2 = new FolderViewModel { Name = "Subfolder2" };
            var file2_1 = new FileViewModel("File2_1.txt", 8000);
            subFolder2.AddItem(file2_1);
            rootFolder.AddItem(subFolder);
            rootFolder.AddItem(subFolder2);

            FileSystemItems.Add(rootFolder);
        }

        private void UpdateSelectedItemSize()
        {
            if (FirstSelectedItem is FileViewModel file)
            {
                SelectedItemSize = file.Size;
            }
            else if (FirstSelectedItem is FolderViewModel folder)
            {
                SelectedItemSize = CalculateFolderSize(folder);
            }
            else
            {
                SelectedItemSize = 0;
            }
        }

        private void UpdateCommandsCanExecute()
        {
            _copyCommand.RaiseCanExecuteChanged();
            _moveCommand.RaiseCanExecuteChanged();
        }

        private long CalculateFolderSize(FolderViewModel folder)
        {
            long size = 0;
            foreach (var item in folder.Items)
            {
                if (item is FileViewModel file)
                {
                    size += file.Size;
                }
                else if (item is FolderViewModel subFolder)
                {
                    size += CalculateFolderSize(subFolder);
                }
            }
            return size;
        }

        private void Copy(object parameter)
        {
            if (FirstSelectedItem == null || SecondSelectedItem == null) return;

            if (FirstSelectedItem is FolderViewModel folder)
            {
                CopyFolder(folder, SecondSelectedItem);
            }
            else if (FirstSelectedItem is FileViewModel file)
            {
                CopyFile(file, SecondSelectedItem);
            }
            RefreshFileSystemItems();
        }

        private void CopyFolder(FolderViewModel folder, FolderViewModel destinationFolder)
        {
            var copyFolder = new FolderViewModel { Name = folder.Name };

            foreach (var item in folder.Items)
            {
                if (item is FolderViewModel subfolder)
                {
                    CopyFolder(subfolder, copyFolder);
                }
                else if (item is FileViewModel file)
                {
                    var copyFile = new FileViewModel(file.Name, file.Size);
                    copyFolder.AddItem(copyFile);
                }
            }

            destinationFolder.AddItem(copyFolder);
        }

        private void CopyFile(FileViewModel file, FolderViewModel destinationFolder)
        {
            if (!destinationFolder.Items.Any(item => item.Name == file.Name))
            {
                var copyFile = new FileViewModel(file.Name, file.Size);
                destinationFolder.AddItem(copyFile);
            }
        }

        private bool CanCopy(object parameter)
        {
            return FirstSelectedItem != null &&
                   SecondSelectedItem != null &&
                   FirstSelectedItem != SecondSelectedItem;
        }

        private bool CanMove(object parameter)
        {
            return FirstSelectedItem != null &&
                   SecondSelectedItem != null &&
                   FirstSelectedItem != SecondSelectedItem &&
                   (FirstSelectedItem is FileViewModel ||
                   (FirstSelectedItem is FolderViewModel folder && !folder.IsAncestorOf(SecondSelectedItem)));
        }

        private void Move(object parameter)
        {
            if (FirstSelectedItem == null || SecondSelectedItem == null) return;

            if (FirstSelectedItem is FolderViewModel folder)
            {
                MoveFolder(folder, SecondSelectedItem);
            }
            else if (FirstSelectedItem is FileViewModel file)
            {
                MoveFile(file, SecondSelectedItem);
            }
            RefreshFileSystemItems();
        }

        private void MoveFolder(FolderViewModel folder, FolderViewModel destinationFolder)
        {
            if (folder != destinationFolder && !folder.IsAncestorOf(destinationFolder))
            {
                folder.ParentFolder?.RemoveItem(folder);
                destinationFolder.AddItem(folder);
            }
        }

        private void MoveFile(FileViewModel file, FolderViewModel destinationFolder)
        {
            if (!destinationFolder.Items.Any(item => item.Name == file.Name))
            {
                file.ParentFolder?.RemoveItem(file);
                destinationFolder.AddItem(file);
            }
        }

        private void RefreshFileSystemItems()
        {
            OnPropertyChanged(nameof(FileSystemItems));
        }

        private void LoadAssembly()
        {
            ReflectedTypes.Clear();
            ReflectedMethods.Clear();
            ReflectedParameters.Clear();

            if (SelectedReflectedType != null)
            {
                SelectedReflectedType = null;
            }
            if (SelectedReflectedMethod != null)
            {
                SelectedReflectedMethod = null;
            }
            ReflectionResult = "";

            if (string.IsNullOrWhiteSpace(AssemblyPath)) return;

            try
            {
                var assembly = Assembly.LoadFrom(AssemblyPath);

                var interfaceType = typeof(IReflectable);
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic && interfaceType.IsAssignableFrom(t))
                    .ToList();

                foreach (var t in types)
                    ReflectedTypes.Add(t);

                SelectedReflectedType = null;
                if (ReflectedTypes.Count > 0)
                    SelectedReflectedType = ReflectedTypes[0];
            }
            catch (Exception ex)
            {
                ReflectionResult = $"Error loading assembly: {ex.Message}";
            }
        }

        private void LoadMethodsForSelectedType()
        {
            ReflectedMethods.Clear();
            ReflectedParameters.Clear();

            if (SelectedReflectedMethod != null)
            {
                SelectedReflectedMethod = null;
            }
            if (SelectedReflectedType == null) return;

            var methods = SelectedReflectedType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .ToList();

            foreach (var m in methods)
                ReflectedMethods.Add(m);

            SelectedReflectedMethod = null;
            if (ReflectedMethods.Count > 0)
                SelectedReflectedMethod = ReflectedMethods[0];
        }

        private void LoadParametersForSelectedMethod()
        {
            ReflectedParameters.Clear();
            if (SelectedReflectedMethod == null) return;

            foreach (var p in SelectedReflectedMethod.GetParameters())
                ReflectedParameters.Add(new ParameterViewModel(p));
        }

        private void ExecuteReflectedMethod()
        {
            if (SelectedReflectedType == null || SelectedReflectedMethod == null) return;

            try
            {
                var ctor = SelectedReflectedType.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                {
                    ReflectionResult = "No parameterless constructor found.";
                    return;
                }
                var instance = ctor.Invoke(null);

                var paramValues = ReflectedParameters.Select(p => p.GetValue()).ToArray();
                var result = SelectedReflectedMethod.Invoke(instance, paramValues);

                ReflectionResult = result != null ? result.ToString() : "Method executed (void)";
            }
            catch (Exception ex)
            {
                ReflectionResult = $"Error: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ParameterViewModel : INotifyPropertyChanged
    {
        public ParameterInfo ParameterInfo { get; }
        public string Name => ParameterInfo.Name;
        public string TypeName => ParameterInfo.ParameterType.Name;
        private string _inputValue;
        public string InputValue
        {
            get => _inputValue;
            set
            {
                if (_inputValue != value)
                {
                    _inputValue = value;
                    OnPropertyChanged(nameof(InputValue));
                }
            }
        }

        public ParameterViewModel(ParameterInfo parameterInfo)
        {
            ParameterInfo = parameterInfo;
        }

        public object GetValue()
        {
            var type = ParameterInfo.ParameterType;
            if (type == typeof(string)) return InputValue;
            if (type == typeof(int)) return int.TryParse(InputValue, out var i) ? i : 0;
            if (type == typeof(double)) return double.TryParse(InputValue, out var d) ? d : 0;
            if (type == typeof(bool)) return bool.TryParse(InputValue, out var b) ? b : false;
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}