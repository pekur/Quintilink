using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;

namespace Quintilink.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        private string version = string.Empty;

        [ObservableProperty]
        private string copyright = string.Empty;

        [ObservableProperty]
        private string licenseSummary = string.Empty;

        public AboutViewModel()
        {
            LoadVersionInfo();
            LoadLicenseInfo();
        }

        private void LoadVersionInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyVersion = assembly.GetName().Version;
            
            // Try to get informational version (includes git info from Nerdbank.GitVersioning)
            var infoVersionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (infoVersionAttr != null)
            {
                // Trim any metadata after + sign for cleaner display
                var fullVersion = infoVersionAttr.InformationalVersion;
                var plusIndex = fullVersion.IndexOf('+');
                Version = plusIndex > 0 ? $"v{fullVersion[..plusIndex]}" : $"v{fullVersion}";
            }
            else if (assemblyVersion != null)
            {
                Version = $"v{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
            }
            else
            {
                Version = "v1.0.0";
            }

            Copyright = "© 2026 Petr Kurka";
        }

        private void LoadLicenseInfo()
        {
            LicenseSummary = """
                This software is licensed under the Quintilink Non-Commercial License.
                
                ? Free for personal, educational, and research use
                ? You may copy, modify, and distribute for non-commercial purposes
                ? Commercial use requires a separate license
                """;
        }
    }
}
