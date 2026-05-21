using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SEUtilityTools.API.Helpers;
using SEUtilityTools.API.Interface;
using SEUtilityTools.API.Struct;
using ic = Projektanker.Icons.Avalonia;

namespace SEUtilityTools.Pages
{
    public class GPSTriangulator : Page
    {
        public class TriangulatorData : PageData
        {
            public List<GPS> Points { get; set; } = [];
        }

        public TriangulatorData Data { get; set; } = new();

        public override string PageName => nameof(GPSTriangulator);
        public override string Description => "Calculate a position based on GPS coordinates";

        private StackPanel? _pointsListPanel;
        private Panel? _resultArea;

        public override Control CreateContent()
        {
            Grid root = new()
            {
                ColumnDefinitions = new ColumnDefinitions("280,*")
            };

            Grid.SetColumn(BuildLeftPanel(), 0);
            root.Children.Add(BuildLeftPanel());

            _resultArea = new Panel { Background = new SolidColorBrush(Color.Parse("#0f0f0f")) };
            Grid.SetColumn(_resultArea, 1);
            root.Children.Add(_resultArea);

            RefreshPointsList();
            ShowResultArea();

            return root;
        }

        private Border BuildLeftPanel()
        {
            Grid leftGrid = new()
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };

            TextBlock header = new()
            {
                Text = "GPS Points",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(16, 16, 16, 10)
            };

            Grid.SetRow(header, 0);
            leftGrid.Children.Add(header);

            _pointsListPanel = new StackPanel
            {
                Spacing = 2,
                Margin = new Thickness(8, 0)
            };

            ScrollViewer scroll = new()
            {
                Content = _pointsListPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(scroll, 1);
            leftGrid.Children.Add(scroll);

            Border inputArea = BuildInputArea();
            Grid.SetRow(inputArea, 2);
            leftGrid.Children.Add(inputArea);

            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#161616")),
                BorderBrush = new SolidColorBrush(Color.Parse("#252525")),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Child = leftGrid
            };
        }

        private Border BuildInputArea()
        {
            TextBox gpsInput = new()
            {
                Watermark = "GPS:Name:X:Y:Z  —  e.g. GPS:Base:12345:67890:-12345",
                Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                FontSize = 12,
            };

            Button addBtn = new()
            {
                Content = "Add GPS",
                Background = new SolidColorBrush(Color.Parse("#00d084")),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(0, 10),
                FontWeight = FontWeight.SemiBold,
                FontSize = 13,
            };

            addBtn.Click += (_, _) =>
            {
                if (TryParseGPS(gpsInput.Text?.Trim() ?? "", out GPS gps))
                {
                    Data.Points.Add(gps);
                    RefreshPointsList();
                    ShowResultArea();
                    gpsInput.Text = string.Empty;
                }
            };

            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#252525")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Child = new StackPanel
                {
                    Spacing = 8,
                    Margin = new Thickness(8, 10, 8, 16),
                    Children =
                    {
                        gpsInput,
                        addBtn
                    }
                }
            };
        }

        private static bool TryParseGPS(string input, out GPS gps)
        {
            gps = default;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input.StartsWith("GPS:", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = input.Split(':');
                if (parts.Length >= 5 &&
                    float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                {
                    gps = new GPS(parts[1], x, y, z);
                    return true;
                }
            }

            string[] tokens = input.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 3 &&
                float.TryParse(tokens[^3], NumberStyles.Float, CultureInfo.InvariantCulture, out float sx) &&
                float.TryParse(tokens[^2], NumberStyles.Float, CultureInfo.InvariantCulture, out float sy) &&
                float.TryParse(tokens[^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float sz))
            {
                string name = tokens.Length > 3 ? string.Join(" ", tokens[..^3]) : "Unknown";
                gps = new GPS(name, sx, sy, sz);
                return true;
            }

            return false;
        }

        private void RefreshPointsList()
        {
            if (_pointsListPanel is null)
                return;

            _pointsListPanel.Children.Clear();

            for (int i = 0; i < Data.Points.Count; i++)
            {
                GPS point = Data.Points[i];
                Border card = BuildPointCard(point, i);
                _pointsListPanel.Children.Add(card);
            }
        }

        private Border BuildPointCard(GPS point, int index)
        {
            Button deleteBtn = new()
            {
                Content = new ic::Icon
                {
                    Value = "fa-solid fa-trash",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#606060"))
                },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6),
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                IsVisible = false
            };

            deleteBtn.Click += (_, e) =>
            {
                e.Handled = true;
                Data.Points.RemoveAt(index);
                RefreshPointsList();
                ShowResultArea();
            };

            StackPanel textStack = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = point.Name,
                        Foreground = Brushes.White,
                        FontSize = 13,
                        FontWeight = FontWeight.Medium,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    },
                    new TextBlock
                    {
                        Text = point.Coordinates.ToString(),
                        Foreground = new SolidColorBrush(Color.Parse("#707070")),
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    }
                }
            };

            Grid cardContent = new()
            {
                Children =
                {
                    textStack,
                    deleteBtn
                }
            };

            Border card = new()
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10),
                Cursor = new Cursor(StandardCursorType.Hand),
                Background = Brushes.Transparent,
                Child = cardContent
            };

            card.PointerEntered += (_, _) =>
            {
                deleteBtn.IsVisible = true;
                card.Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
            };

            card.PointerExited += (_, _) =>
            {
                deleteBtn.IsVisible = false;
                card.Background = Brushes.Transparent;
            };

            return card;
        }

        private void ShowResultArea()
        {
            if (_resultArea is null)
                return;

            _resultArea.Children.Clear();

            if (Data.Points.Count == 0)
            {
                _resultArea.Children.Add(new TextBlock
                {
                    Text = "Add GPS points to begin triangulation",
                    Foreground = new SolidColorBrush(Color.Parse("#404040")),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                
                return;
            }

            Grid root = new()
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*"),
                Margin = new Thickness(28, 24, 28, 24)
            };

            StackPanel headerPanel = new() { Margin = new Thickness(0, 0, 0, 20) };
            headerPanel.Children.Add(new TextBlock
            {
                Text = "GPS Triangulation",
                Foreground = Brushes.White,
                FontSize = 22,
                FontWeight = FontWeight.Bold
            });

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"{Data.Points.Count} reference point{(Data.Points.Count == 1 ? "" : "s")} loaded",
                Foreground = new SolidColorBrush(Color.Parse("#707070")),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0)
            });

            Grid.SetRow(headerPanel, 0);
            root.Children.Add(headerPanel);

            WrapPanel pills = new() { Margin = new Thickness(0, 0, 0, 24) };
            foreach (GPS point in Data.Points)
            {
                pills.Children.Add(new Border
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#2e2e2e")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 6),
                    Child = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = point.Name,
                                Foreground = new SolidColorBrush(Color.Parse("#707070")),
                                FontSize = 12
                            },
                            new TextBlock
                            {
                                Text = point.Coordinates.ToString(),
                                Foreground = Brushes.White,
                                FontSize = 12,
                                FontWeight = FontWeight.SemiBold
                            }
                        }
                    }
                });
            }
            Grid.SetRow(pills, 1);
            root.Children.Add(pills);

            Button computeBtn = new()
            {
                Content = "Compute Triangulation",
                Background = new SolidColorBrush(Color.Parse("#00d084")),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(0, 12),
                FontWeight = FontWeight.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16)
            };

            Grid.SetRow(computeBtn, 2);
            root.Children.Add(computeBtn);

            StackPanel resultPanel = new() { Spacing = 8 };
            resultPanel.Children.Add(new TextBlock
            {
                Text = "RESULT",
                Foreground = new SolidColorBrush(Color.Parse("#505050")),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                LetterSpacing = 0.5
            });

            TextBox resultBox = new()
            {
                Watermark = "Press compute to generate...",
                Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                FontSize = 13,
                IsReadOnly = true,
            };

            resultPanel.Children.Add(resultBox);

            Button copyBtn = new()
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        new ic::Icon
                        {
                            Value = "fa-solid fa-copy",
                            FontSize = 11,
                            Foreground = Brushes.White
                        },
                        new TextBlock
                        {
                            Text = "Copy",
                            Foreground = Brushes.White,
                            FontSize = 12,
                            FontWeight = FontWeight.SemiBold
                        }
                    }
                },
                Background = new SolidColorBrush(Color.Parse("#252525")),
                BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                IsVisible = false
            };

            copyBtn.Click += (_, _) =>
            {
                if (!string.IsNullOrEmpty(resultBox.Text))
                {
                    copyBtn.Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 6,
                        Children =
                        {
                            new ic::Icon
                            {
                                Value = "fa-solid fa-check",
                                FontSize = 11,
                                Foreground = new SolidColorBrush(Color.Parse("#00d084"))
                            },
                            new TextBlock
                            {
                                Text = "Copied!",
                                Foreground = new SolidColorBrush(Color.Parse("#00d084")),
                                FontSize = 12,
                                FontWeight = FontWeight.SemiBold
                            }
                        }
                    };
                }
            };

            resultPanel.Children.Add(copyBtn);
            Grid.SetRow(resultPanel, 3);
            root.Children.Add(resultPanel);

            computeBtn.Click += (_, _) =>
            {
                if (Data.Points.Count == 0)
                    return;

                float x = Data.Points.Sum(p => p.Coordinates.X) / Data.Points.Count;
                float y = Data.Points.Sum(p => p.Coordinates.Y) / Data.Points.Count;
                float z = Data.Points.Sum(p => p.Coordinates.Z) / Data.Points.Count;

                GPS result = new("Triangulated", x, y, z);
                resultBox.Text = result.ToString();
                copyBtn.IsVisible = true;
            };

            _resultArea.Children.Add(root);
        }

        public override void OnClicked()
        {
            LogManager.Debug($"Opened GPSTriangulator");
            Data = DataManager.LoadData<TriangulatorData>(this) ?? new TriangulatorData();
            RefreshPointsList();
            ShowResultArea();
        }

        public override void OnClosed()
        {
            LogManager.Debug($"Closed GPSTriangulator");
            DataManager.SaveData(this, Data);
        }
    }
}