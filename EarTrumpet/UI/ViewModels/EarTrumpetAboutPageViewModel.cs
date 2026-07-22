using EarTrumpet.Interop.Helpers;
using EarTrumpet.UI.Helpers;
using System;
using System.Windows.Input;

namespace EarTrumpet.UI.ViewModels
{
    class EarTrumpetAboutPageViewModel : SettingsPageViewModel
    {
        public ICommand OpenDiagnosticsCommand { get; }
        public ICommand OpenEarTrumpetCommand { get; }
        public ICommand OpenForkCommand { get; }
        public string AboutText { get; }

        private readonly Action _openDiagnostics;

        public EarTrumpetAboutPageViewModel(Action openDiagnostics, AppSettings settings) : base(null)
        {
            _openDiagnostics = openDiagnostics;
            Glyph = "\xE946";
            Title = Properties.Resources.AboutTitle;
            AboutText = $"DeviceTrumpet 1.1.0 based on EarTrumpet {App.PackageVersion}";

            OpenEarTrumpetCommand = new RelayCommand(OpenEarTrumpet);
            OpenForkCommand = new RelayCommand(OpenFork);
            OpenDiagnosticsCommand = new RelayCommand(_openDiagnostics.Invoke);
        }

        private void OpenEarTrumpet() => ProcessHelper.StartNoThrow("https://github.com/File-New-Project/EarTrumpet");
        private void OpenFork() => ProcessHelper.StartNoThrow("https://github.com/Qwezy/DeviceTrumpet");
    }
}
