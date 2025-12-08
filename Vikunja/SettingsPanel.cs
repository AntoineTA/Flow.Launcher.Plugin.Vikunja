using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.Vikunja
{
    public partial class SettingsPanel : UserControl
    {
        private readonly PluginInitContext _context;
        private readonly Settings _settings;

        public SettingsPanel(PluginInitContext context, Settings settings)
        {
            _context = context;
            _settings = settings;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Background = System.Windows.Media.Brushes.White;
            
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Orientation = Orientation.Vertical
            };

            // Server URL
            stackPanel.Children.Add(new Label 
            { 
                Content = "Server URL:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0)
            });
            
            var serverUrlTextBox = new TextBox 
            { 
                Name = "ServerUrlTextBox", 
                Height = 25,
                Width = 400,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(serverUrlTextBox);
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "The URL of your Vikunja instance (e.g., https://vikunja.example.com)",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            });

            // API Token
            stackPanel.Children.Add(new Label 
            { 
                Content = "API Token:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0)
            });
            
            var apiTokenTextBox = new TextBox 
            { 
                Name = "ApiTokenTextBox", 
                Height = 25,
                Width = 400,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(apiTokenTextBox);
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Your Vikunja API token (create one in Vikunja Settings → API Tokens)\nThis will be stored as plaintext in the user data directory, please ensure it's not publicly accessible",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap
            });
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Required API Permissions:",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.DarkBlue,
                Margin = new Thickness(0, 5, 0, 3),
                TextWrapping = TextWrapping.Wrap
            });
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "• labels: create, read all\n• projects: read all, read one\n• tasks: create\n• tasksLabels: create",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(15, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            });

            // Default Project ID
            stackPanel.Children.Add(new Label 
            { 
                Content = "Default Project ID:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0)
            });
            
            var defaultProjectTextBox = new TextBox 
            { 
                Name = "DefaultProjectTextBox", 
                Height = 25,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(defaultProjectTextBox);
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Project ID to use when no project is specified (1 = Inbox)",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            });

            // Parsing Mode
            stackPanel.Children.Add(new Label 
            { 
                Content = "Parsing Mode:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(0)
            });
            
            var parsingModeComboBox = new ComboBox 
            { 
                Name = "ParsingModeComboBox", 
                Height = 30,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            parsingModeComboBox.Items.Add("Vikunja");
            parsingModeComboBox.Items.Add("Todoist");
            stackPanel.Children.Add(parsingModeComboBox);
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Vikunja: +project *label !priority | Todoist: #project @label p1-p3",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap
            });

            // Save button
            var saveButton = new Button 
            { 
                Content = "Save", 
                Width = 80, 
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = System.Windows.Media.Brushes.LightBlue
            };
            saveButton.Click += SaveButton_Click;
            stackPanel.Children.Add(saveButton);

            Content = stackPanel;
        }

        private void LoadSettings()
        {
            if (Content is StackPanel stackPanel)
            {
                var serverUrlTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "ServerUrlTextBox");
                var apiTokenTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "ApiTokenTextBox");
                var defaultProjectTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "DefaultProjectTextBox");
                var parsingModeComboBox = stackPanel.Children.OfType<ComboBox>().FirstOrDefault(t => t.Name == "ParsingModeComboBox");

                if (serverUrlTextBox != null) serverUrlTextBox.Text = _settings.ServerUrl;
                if (apiTokenTextBox != null) apiTokenTextBox.Text = _settings.ApiToken;
                if (defaultProjectTextBox != null) defaultProjectTextBox.Text = _settings.DefaultProjectId.ToString();
                if (parsingModeComboBox != null) parsingModeComboBox.SelectedIndex = (int)_settings.ParsingMode;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Content is StackPanel stackPanel)
            {
                var serverUrlTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "ServerUrlTextBox");
                var apiTokenTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "ApiTokenTextBox");
                var defaultProjectTextBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "DefaultProjectTextBox");
                var parsingModeComboBox = stackPanel.Children.OfType<ComboBox>().FirstOrDefault(t => t.Name == "ParsingModeComboBox");

                if (serverUrlTextBox != null) _settings.ServerUrl = serverUrlTextBox.Text.Trim();
                if (apiTokenTextBox != null) _settings.ApiToken = apiTokenTextBox.Text.Trim();
                
                if (defaultProjectTextBox != null && int.TryParse(defaultProjectTextBox.Text, out int projectId))
                    _settings.DefaultProjectId = projectId;

                if (parsingModeComboBox != null)
                    _settings.ParsingMode = (ParsingMode)parsingModeComboBox.SelectedIndex;

                _context.API.SavePluginSettings();
                MessageBox.Show("Settings saved successfully!", "Vikunja Plugin");
            }
        }
    }
}