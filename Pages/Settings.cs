using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SEUtilityTools.API.Helpers;
using SEUtilityTools.API.Interface;
using ic = Projektanker.Icons.Avalonia;

namespace SEUtilityTools.Pages
{
    public class Settings : Page
    {
        public override string PageName => nameof(Settings);
        public override string Description => "Settings page";

        private Panel? _contentArea;

        public override Control CreateContent()
        {
            Grid root = new()
            {
                RowDefinitions = new RowDefinitions("Auto,*")
            };

            Border header = new()
            {
                Background = new SolidColorBrush(Color.Parse("#0f0f0f")),
                BorderBrush = new SolidColorBrush(Color.Parse("#1e1e1e")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(28, 20),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Settings",
                            Foreground = Brushes.White,
                            FontSize = 20,
                            FontWeight = FontWeight.Bold
                        },
                        new TextBlock
                        {
                            Text = "Manage application configuration",
                            Foreground = new SolidColorBrush(Color.Parse("#505050")),
                            FontSize = 12
                        }
                    }
                }
            };

            Grid.SetRow(header, 0);
            root.Children.Add(header);

            StackPanel body = new()
            {
                Margin = new Thickness(28, 24),
                Spacing = 8
            };

            body.Children.Add(SectionHeader("General"));

            body.Children.Add(BoolRow(
                icon: "fa-solid fa-bug",
                title: "Debug Mode",
                description: "Enable verbose logging to console",
                getValue: () => Program.Config.Debug,
                setValue: v => Program.Config.Debug = v));

            body.Children.Add(BoolRow(
                icon: "fa-solid fa-images",
                title: "Convert Icons on Start",
                description: "Convert block icons from DDS to PNG at launch",
                getValue: () => Program.Config.ConvertIconsOnStart,
                setValue: v => Program.Config.ConvertIconsOnStart = v));

            body.Children.Add(new Border
            {
                Height = 8
            });

            body.Children.Add(SectionHeader("Paths"));

            body.Children.Add(StringRow(
                icon: "fa-solid fa-folder-open",
                title: "Space Engineers Directory",
                description: "Path to the Space Engineers installation",
                getValue: () => Program.Config.SpaceEngineersDirectory,
                setValue: v => Program.Config.SpaceEngineersDirectory = v));

            body.Children.Add(new Border
            {
                Height = 16
            });
            body.Children.Add(ReloadButton());

            ScrollViewer scroll = new() { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            return root;
        }

        private Border BoolRow(string icon, string title, string description, Func<bool> getValue, Action<bool> setValue)
        {
            Border badge = MakeBadge(getValue().ToString(), isBool: true);
            Border row = MakeRow(icon, title, description, badge);
            row.PointerPressed += (_, _) =>
            {
                ShowBoolPopup(title, description, getValue, v =>
                {
                    setValue(v);
                    ConfigManager.SaveConfig();
                    RefreshBadge(badge, v.ToString(), isBool: true);
                });
            };
            return row;
        }

        private Border StringRow(string icon, string title, string description, Func<string> getValue, Action<string> setValue)
        {
            Border badge = MakeBadge(getValue(), isBool: false);

            Border row = MakeRow(icon, title, description, badge);
            row.PointerPressed += (_, _) =>
            {
                ShowStringPopup(title, description, getValue, v =>
                {
                    setValue(v);
                    ConfigManager.SaveConfig();
                    RefreshBadge(badge, v, isBool: false);
                });
            };
            return row;
        }

        private static Border MakeRow(string icon, string title, string description, Border badge)
        {
            Border row = new()
            {
                Background = new SolidColorBrush(Color.Parse("#141414")),
                BorderBrush = new SolidColorBrush(Color.Parse("#252525")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 14),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            Grid layout = new()
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            Border iconBox = new()
            {
                Width = 34,
                Height = 34,
                Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                BorderBrush = new SolidColorBrush(Color.Parse("#2e2e2e")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Child = new ic.Icon
                {
                    Value = icon,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.Parse("#707070")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            Grid.SetColumn(iconBox, 0);
            layout.Children.Add(iconBox);

            StackPanel text = new()
            {
                Margin = new Thickness(14, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            text.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeight.Medium
            });

            text.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Color.Parse("#505050")),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });

            Grid.SetColumn(text, 1);
            layout.Children.Add(text);

            StackPanel right = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };

            right.Children.Add(badge);
            right.Children.Add(new ic.Icon
            {
                Value = "fa-solid fa-chevron-right",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#404040"))
            });

            Grid.SetColumn(right, 2);
            layout.Children.Add(right);

            row.Child = layout;
            row.PointerEntered += (_, _) => row.Background = new SolidColorBrush(Color.Parse("#1a1a1a"));
            row.PointerExited += (_, _) => row.Background = new SolidColorBrush(Color.Parse("#141414"));
            return row;
        }

        private static Border MakeBadge(string value, bool isBool)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 4),
                Child = new TextBlock
                {
                    Text = BadgeText(value, isBool),
                    Foreground = BadgeForeground(value, isBool),
                    FontSize = 12,
                    FontFamily = new FontFamily("Consolas,Menlo,monospace")
                }
            };
        }

        private static void RefreshBadge(Border badge, string value, bool isBool)
        {
            if (badge.Child is not TextBlock tb)
                return;

            tb.Text = BadgeText(value, isBool);
            tb.Foreground = BadgeForeground(value, isBool);
        }

        private static string BadgeText(string value, bool isBool) => isBool ? (value.Equals("True", StringComparison.OrdinalIgnoreCase) ? "Enabled" : "Disabled") : (value.Length > 55 ? "..." + value[^52..] : value);

        private static IBrush BadgeForeground(string value, bool isBool) => isBool ? (value.Equals("True", StringComparison.OrdinalIgnoreCase) ? new SolidColorBrush(Color.Parse("#4ade80")) : new SolidColorBrush(Color.Parse("#f87171"))) : new SolidColorBrush(Color.Parse("#a0a0a0"));

        private void ShowBoolPopup(string title, string description, Func<bool> getValue, Action<bool> onSave)
        {
            bool edited = getValue();

            ToggleSwitch toggle = new()
            {
                IsChecked = edited,
                OnContent = "Enabled",
                OffContent = "Disabled",
                Foreground = Brushes.White,
                FontSize = 13
            };

            toggle.IsCheckedChanged += (_, _) => edited = toggle.IsChecked == true;
            ShowPopup(title, description, toggle, () => onSave(edited));
        }

        private void ShowStringPopup(string title, string description, Func<string> getValue, Action<string> onSave)
        {
            string edited = getValue();

            TextBox tb = new()
            {
                Text = edited,
                Background = new SolidColorBrush(Color.Parse("#0f0f0f")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10),
                FontSize = 13,
                FontFamily = new FontFamily("Consolas,Menlo,monospace"),
                CaretBrush = Brushes.White,
                SelectionBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
            };

            tb.TextChanged += (_, _) => edited = tb.Text ?? string.Empty;
            ShowPopup(title, description, tb, () => onSave(edited));
        }

        private void ShowPopup(string title, string description, Control editor, Action onSave)
        {
            if (_contentArea is null)
                return;

            Border overlay = new()
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Border card = new()
            {
                Width = 460,
                Background = new SolidColorBrush(Color.Parse("#161616")),
                BorderBrush = new SolidColorBrush(Color.Parse("#2e2e2e")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                BoxShadow = BoxShadows.Parse("0 20 60 0 #000000")
            };

            Button closeBtn = new()
            {
                Content = new ic.Icon
                {
                    Value = "fa-solid fa-xmark",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.Parse("#707070"))
                },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            Grid titleRow = new()
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };

            TextBlock titleTb = new()
            {
                Text = title,
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeight.SemiBold
            };

            Grid.SetColumn(titleTb, 0);
            Grid.SetColumn(closeBtn, 1);
            titleRow.Children.Add(titleTb);
            titleRow.Children.Add(closeBtn);

            Button cancelBtn = new()
            {
                Content = new TextBlock
                {
                    Text = "Cancel",
                    Foreground = new SolidColorBrush(Color.Parse("#a0a0a0")),
                    FontSize = 13
                },
                Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            Button saveBtn = new()
            {
                Content = new TextBlock
                {
                    Text = "Save",
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold
                },
                Background = new SolidColorBrush(Color.Parse("#252525")),
                BorderBrush = new SolidColorBrush(Color.Parse("#404040")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            StackPanel btnRow = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            btnRow.Children.Add(cancelBtn);
            btnRow.Children.Add(saveBtn);

            card.Child = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    titleRow,
                    new TextBlock
                    {
                        Text = description,
                        Foreground = new SolidColorBrush(Color.Parse("#606060")),
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Color.Parse("#252525"))
                    },
                    editor,
                    btnRow
                }
            };

            overlay.Child = card;

            void Close()
            {
                if (_contentArea.Children.Contains(overlay))
                    _contentArea.Children.Remove(overlay);
            }

            closeBtn.Click += (_, _) => Close();
            cancelBtn.Click += (_, _) => Close();
            saveBtn.Click += (_, _) =>
            {
                onSave();
                Close();
            };

            overlay.PointerPressed += (_, e) =>
            {
                Point pos = e.GetPosition(card);
                if (pos.X < 0 || pos.Y < 0 || pos.X > card.Bounds.Width || pos.Y > card.Bounds.Height)
                    Close();
            };

            _contentArea.Children.Add(overlay);
        }


        private static TextBlock SectionHeader(string text) => new()
        {
            Text = text.ToUpperInvariant(),
            Foreground = new SolidColorBrush(Color.Parse("#505050")),
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 0.8,
            Margin = new Thickness(0, 0, 0, 4)
        };

        private Border ReloadButton()
        {
            Button btn = new()
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new ic.Icon
                        {
                            Value = "fa-solid fa-rotate",
                            FontSize = 12,
                            Foreground = Brushes.White
                        },
                        new TextBlock
                        {
                            Text = "Reload Config",
                            Foreground = Brushes.White,
                            FontSize = 13,
                            FontWeight = FontWeight.SemiBold
                        }
                    }
                },
                Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 10),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            btn.Click += (_, _) =>
            {
                ConfigManager.LoadConfig();
                if (_contentArea is not null)
                {
                    _contentArea.Children.Clear();
                    FillContent(_contentArea);
                }
            };

            return new Border
            {
                Child = btn
            };
        }

        public override void FillContent(Panel area)
        {
            _contentArea = area;
            base.FillContent(area);
        }
    }
}