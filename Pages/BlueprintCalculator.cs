using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SEUtilityTools.API.Data;
using SEUtilityTools.API.Extensions;
using SEUtilityTools.API.Helpers;
using SEUtilityTools.API.Interface;
using ic = Projektanker.Icons.Avalonia;

namespace SEUtilityTools.Pages
{
    public class BlueprintCalculator : Page
    {
        public override string PageName => nameof(BlueprintCalculator);
        public override string Description => "Blueprint Calculator page";

        private TextBlock? _loadingText;
        private ProgressBar? _progressBar;
        private StackPanel? _blueprintListPanel;
        private Panel? _detailArea;
        private BlueprintData? _selectedBlueprint;
        private readonly Dictionary<BlueprintData, Border> _cardMap = [];

        public override Control CreateContent()
        {
            _cardMap.Clear();
            _selectedBlueprint = null;

            Grid root = new()
            {
                ColumnDefinitions = new ColumnDefinitions("280,*")
            };

            Border left = BuildLeftPanel();
            Grid.SetColumn(left, 0);
            root.Children.Add(left);

            _detailArea = new Grid
            {
                Background = new SolidColorBrush(Color.Parse("#0f0f0f"))
            };

            Grid.SetColumn(_detailArea, 1);
            root.Children.Add(_detailArea);

            RefreshBlueprintList();
            ShowPlaceholder();

            return root;
        }

        private Border BuildLeftPanel()
        {
            Grid leftGrid = new()
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*")
            };

            Grid headerRow = new()
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin = new Thickness(16, 16, 8, 10)
            };

            TextBlock header = new()
            {
                Text = "Blueprints",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(header, 0);
            headerRow.Children.Add(header);

            Button reloadBtn = new()
            {
                Content = new ic.Icon
                {
                    Value = "fa-solid fa-rotate",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.Parse("#707070"))
                },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8),
                Cursor = new Cursor(StandardCursorType.Hand),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(reloadBtn, 1);
            headerRow.Children.Add(reloadBtn);

            Grid.SetRow(headerRow, 0);
            leftGrid.Children.Add(headerRow);

            _progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 1,
                Value = 0,
                Height = 3,
                IsVisible = false,
                Margin = new Thickness(8, 0, 8, 4),
                Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                Foreground = new SolidColorBrush(Color.Parse("#4a9eff")),
                CornerRadius = new CornerRadius(2)
            };

            Grid.SetRow(_progressBar, 1);
            leftGrid.Children.Add(_progressBar);

            _loadingText = new TextBlock
            {
                Text = "0 / 0",
                Foreground = new SolidColorBrush(Color.Parse("#505050")),
                FontSize = 11,
                IsVisible = false,
                Margin = new Thickness(8, 0, 8, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Grid.SetRow(_loadingText, 2);
            leftGrid.Children.Add(_loadingText);

            _blueprintListPanel = new StackPanel
            {
                Spacing = 2,
                Margin = new Thickness(8, 0)
            };

            ScrollViewer scroll = new()
            {
                Content = _blueprintListPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(scroll, 3);
            leftGrid.Children.Add(scroll);

            reloadBtn.Click += async (_, _) =>
            {
                reloadBtn.IsEnabled = false;
                BlueprintManager.Clear();
                string bpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers", "Blueprints", "local");
                int total = Directory.Exists(bpPath) ? Directory.GetDirectories(bpPath).Length : 1;

                if (_progressBar is not null)
                {
                    _progressBar.Maximum = total;
                    _progressBar.Value = 0;
                    _progressBar.IsVisible = true;
                }

                if (_loadingText is not null)
                {
                    _loadingText.Text = $"0 / {total}";
                    _loadingText.IsVisible = true;
                }

                Progress<int> progress = new(value =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _progressBar?.Value = value;
                        _loadingText?.Text = $"{value} / {total}";
                    });
                });

                await BlueprintManager.Init(promptOnLarge: false, progress: progress);

                _progressBar?.IsVisible = false;
                _loadingText?.IsVisible = false;

                RefreshBlueprintList();
                ShowPlaceholder();
                reloadBtn.IsEnabled = true;
            };

            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#161616")),
                BorderBrush = new SolidColorBrush(Color.Parse("#252525")),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Child = leftGrid
            };
        }

        private void RefreshBlueprintList()
        {
            if (_blueprintListPanel is null)
                return;

            _blueprintListPanel.Children.Clear();
            _cardMap.Clear();

            foreach (BlueprintData bp in BlueprintManager.Blueprints)
            {
                Border card = BuildBlueprintCard(bp);
                _cardMap[bp] = card;
                _blueprintListPanel.Children.Add(card);
            }

            if (_selectedBlueprint is not null && _cardMap.ContainsKey(_selectedBlueprint))
                ApplyHighlight(_selectedBlueprint);
        }

        private Border BuildBlueprintCard(BlueprintData bp)
        {
            Bitmap? roundedIcon = null;
            try
            {
                roundedIcon = bp.Icon.Scale(40).Round(20);
            }
            catch { }

            int totalPcu = bp.Blocks.Sum(b => b.PCU);
            int blockCount = bp.Blocks.Count;

            Border card = new()
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                Cursor = new Cursor(StandardCursorType.Hand),
                Background = Brushes.Transparent,
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        new Border
                        {
                            Width = 40,
                            Height = 40,
                            CornerRadius = new CornerRadius(6),
                            ClipToBounds = true,
                            Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                            Child = roundedIcon is not null
                                ? new Image { Source = roundedIcon, Stretch = Stretch.UniformToFill }
                                : new ic.Icon { Value = "fa-solid fa-cube", FontSize = 18, Foreground = new SolidColorBrush(Color.Parse("#404040")), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                        },
                        new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = bp.Name,
                                    Foreground = Brushes.White,
                                    FontSize = 13,
                                    FontWeight = FontWeight.Medium,
                                    TextTrimming = TextTrimming.CharacterEllipsis,
                                    MaxWidth = 170
                                },
                                new TextBlock
                                {
                                    Text = $"{totalPcu:N0} PCU  •  {blockCount:N0} blocks",
                                    Foreground = new SolidColorBrush(Color.Parse("#707070")),
                                    FontSize = 11,
                                    Margin = new Thickness(0, 2, 0, 0)
                                }
                            }
                        }
                    }
                }
            };

            card.PointerEntered += (_, _) =>
            {
                if (_selectedBlueprint != bp)
                    card.Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
            };

            card.PointerExited += (_, _) =>
            {
                if (_selectedBlueprint != bp)
                    card.Background = Brushes.Transparent;
            };

            card.PointerPressed += (_, _) => SelectBlueprint(bp);

            return card;
        }

        private void SelectBlueprint(BlueprintData bp)
        {
            _selectedBlueprint = bp;
            ApplyHighlight(bp);
            ShowBlueprintDetail(bp);
        }

        private void ApplyHighlight(BlueprintData bp)
        {
            foreach ((BlueprintData? key, Border? card) in _cardMap)
            {
                card.Background = key == bp ? new SolidColorBrush(Color.Parse("#252525")) : Brushes.Transparent;
            }
        }

        private void ShowPlaceholder()
        {
            if (_detailArea is null)
                return;

            _detailArea.Children.Clear();
            _detailArea.Children.Add(new TextBlock
            {
                Text = "Select a blueprint to view its details",
                Foreground = new SolidColorBrush(Color.Parse("#404040")),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        private void ShowBlueprintDetail(BlueprintData bp)
        {
            if (_detailArea is null)
                return;

            _detailArea.Children.Clear();

            int totalPcu = bp.Blocks.Sum(b => b.PCU);
            int blockCount = bp.Blocks.Count;
            int largeBlocks = bp.Blocks.Count(b => b.GridSize == "Large");
            int smallBlocks = bp.Blocks.Count(b => b.GridSize == "Small");

            Dictionary<string, int> materials = new();
            foreach (BlockData block in bp.Blocks)
            {
                foreach ((string? mat, int count) in block.Material)
                {
                    materials[mat] = materials.GetValueOrDefault(mat) + count;
                }

            }

            Grid root = new()
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
                Margin = new Thickness(28, 24, 28, 0)
            };

            StackPanel headerPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 16,
                Margin = new Thickness(0, 0, 0, 20)
            };

            Bitmap? roundedIcon = null;
            try
            {
                roundedIcon = bp.Icon.Scale(60).Round(20);
            }
            catch { }

            headerPanel.Children.Add(new Border
            {
                Width = 60,
                Height = 60,
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                Child = roundedIcon is not null
                    ? new Image
                    {
                        Source = roundedIcon,
                        Stretch = Stretch.UniformToFill
                    }
                    : new ic.Icon
                    {
                        Value = "fa-solid fa-cube",
                        FontSize = 28, Foreground = new SolidColorBrush(Color.Parse("#404040")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
            });

            StackPanel titleStack = new()
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            titleStack.Children.Add(new TextBlock
            {
                Text = bp.Name,
                Foreground = Brushes.White,
                FontSize = 22,
                FontWeight = FontWeight.Bold
            });

            titleStack.Children.Add(new TextBlock
            {
                Text = bp.Folder,
                Foreground = new SolidColorBrush(Color.Parse("#505050")),
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            headerPanel.Children.Add(titleStack);

            Grid.SetRow(headerPanel, 0);
            root.Children.Add(headerPanel);

            WrapPanel pills = new()
            {
                Margin = new Thickness(0, 0, 0, 24)
            };

            void AddPill(string label, string value)
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
                                Text = label,
                                Foreground = new SolidColorBrush(Color.Parse("#707070")),
                                FontSize = 12
                            },
                            new TextBlock
                            {
                                Text = value,
                                Foreground = Brushes.White,
                                FontSize = 12,
                                FontWeight = FontWeight.SemiBold
                            }
                        }
                    }
                });
            }

            AddPill("Total PCU", $"{totalPcu:N0}");
            AddPill("Blocks", $"{blockCount:N0}");
            AddPill("Large Grid", $"{largeBlocks:N0}");
            AddPill("Small Grid", $"{smallBlocks:N0}");
            AddPill("Unique Types", $"{bp.Blocks.Select(b => b.GridSize + b.PCU).Distinct().Count():N0}");

            Grid.SetRow(pills, 1);
            root.Children.Add(pills);

            Border blockSection = BuildBlockTable(bp);
            Grid.SetRow(blockSection, 2);
            root.Children.Add(blockSection);

            Border materialsSection = BuildCollapsibleMaterials(materials);
            Grid.SetRow(materialsSection, 3);
            root.Children.Add(materialsSection);

            _detailArea.Children.Add(root);
        }

        private static Border BuildBlockTable(BlueprintData bp)
        {
            Grid tableGrid = new()
            {
                RowDefinitions = new RowDefinitions("Auto,*")
            };

            Grid colHeaders = new()
            {
                ColumnDefinitions = new ColumnDefinitions("*,120,100"),
                Margin = new Thickness(12, 0, 12, 8)
            };

            void AddColHeader(string text, int col)
            {
                TextBlock tb = new()
                {
                    Text = text.ToUpperInvariant(),
                    Foreground = new SolidColorBrush(Color.Parse("#505050")),
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    LetterSpacing = 0.5
                };

                Grid.SetColumn(tb, col);
                colHeaders.Children.Add(tb);
            }

            AddColHeader("Block", 0);
            AddColHeader("Size", 1);
            AddColHeader("PCU", 2);

            Border headerBorder = new()
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#252525")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 8),
                Child = colHeaders
            };

            Grid.SetRow(headerBorder, 0);
            tableGrid.Children.Add(headerBorder);

            List<(string GridSize, int PCU, string IconPath, int Count, string Name)> grouped = bp.Blocks
                .GroupBy(b => (b.GridSize, b.PCU, b.IconPath, b.Name))
                .Select(g => (g.Key.GridSize, g.Key.PCU, g.Key.IconPath, Count: g.Count(), g.Key.Name))
                .OrderByDescending(x => x.Count)
                .ToList();

            StackPanel rowsPanel = new();
            foreach ((string gridSize, int pcu, string iconPath, int count, string name) in grouped)
            {
                Grid rowGrid = new()
                {
                    ColumnDefinitions = new ColumnDefinitions("*,120,100")
                };

                StackPanel nameCell = new()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (File.Exists(iconPath))
                {
                    try
                    {
                        nameCell.Children.Add(new Image
                        {
                            Source = new Bitmap(iconPath).Scale(20).Round(10),
                            Width = 20,
                            Height = 20
                        });
                    }
                    catch { }
                }

                nameCell.Children.Add(new TextBlock
                {
                    Text = $"×{count}",
                    Foreground = new SolidColorBrush(Color.Parse("#c0c0c0")),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                });

                Grid.SetColumn(nameCell, 0);
                rowGrid.Children.Add(nameCell);

                void AddCell(string text, int col, bool muted = false)
                {
                    TextBlock tb = new()
                    {
                        Text = text,
                        Foreground = muted ? new SolidColorBrush(Color.Parse("#555555")) : new SolidColorBrush(Color.Parse("#c0c0c0")),
                        FontSize = 13,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Grid.SetColumn(tb, col);
                    rowGrid.Children.Add(tb);
                }

                AddCell(gridSize, 1, muted: gridSize == "Small");
                AddCell($"{pcu:N0}", 2);

                rowsPanel.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush(Color.Parse("#1c1c1c")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(12, 9),
                    Child = rowGrid
                });
            }

            ScrollViewer scroll = new()
            {
                Content = rowsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(scroll, 1);
            tableGrid.Children.Add(scroll);

            return new Border
            {
                Child = tableGrid
            };
        }

        private static Border BuildCollapsibleMaterials(Dictionary<string, int> materials)
        {
            bool expanded = false;

            ic.Icon chevron = new()
            {
                Value = "fa-solid fa-chevron-right",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#707070")),
                RenderTransformOrigin = RelativePoint.Center,
                RenderTransform = new RotateTransform(0)
            };

            Button headerBtn = new()
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Cursor = new Cursor(StandardCursorType.Hand),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        chevron,
                        new TextBlock
                        {
                            Text = "Required Materials",
                            Foreground = new SolidColorBrush(Color.Parse("#c0c0c0")),
                            FontSize = 13,
                            FontWeight = FontWeight.SemiBold,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new Border
                        {
                            Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
                            BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(10),
                            Padding = new Thickness(8, 2),
                            VerticalAlignment = VerticalAlignment.Center,
                            Child = new TextBlock
                            {
                                Text = $"{materials.Count} types",
                                Foreground = new SolidColorBrush(Color.Parse("#707070")),
                                FontSize = 11
                            }
                        }
                    }
                }
            };

            StackPanel matPanel = new();

            ScrollViewer matScroll = new()
            {
                Content = matPanel,
                IsVisible = false,
                MaxHeight = 220,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            foreach ((string? mat, int count) in materials.OrderByDescending(m => m.Value))
            {
                matPanel.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush(Color.Parse("#1c1c1c")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(28, 8),
                    Child = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                        Children =
                        {
                            new TextBlock
                            {
                                Text = mat,
                                Foreground = new SolidColorBrush(Color.Parse("#c0c0c0")),
                                FontSize = 13
                            },
                            new TextBlock
                            {
                                Text = $"{count:N0}",
                                Foreground = new SolidColorBrush(Color.Parse("#707070")),
                                FontSize = 13,
                                HorizontalAlignment = HorizontalAlignment.Right
                            }
                        }
                    }
                });

                Grid.SetColumn(((Grid)((Border)matPanel.Children.Last()).Child!).Children[1], 1);
            }

            headerBtn.Click += (_, _) =>
            {
                expanded = !expanded;
                matScroll.IsVisible = expanded;

                double from = expanded ? 0 : 90;
                double to = expanded ? 90 : 0;
                RotateTransform rt = (RotateTransform)chevron.RenderTransform!;
                DateTime start = DateTime.UtcNow;
                TimeSpan duration = TimeSpan.FromMilliseconds(150);

                DispatcherTimer timer = new()
                {
                    Interval = TimeSpan.FromMilliseconds(8)
                };

                timer.Tick += (s, _) =>
                {
                    double t = Math.Clamp((DateTime.UtcNow - start).TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
                    rt.Angle = from + (to - from) * (1 - Math.Pow(1 - t, 3));
                    if (t >= 1)
                    {
                        rt.Angle = to;
                        ((DispatcherTimer)s!).Stop();
                    }
                };
                timer.Start();
            };

            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#252525")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(-28, 0, -28, 0),
                Child = new StackPanel
                {
                    Children =
                    {
                        new Border
                        {
                            Padding = new Thickness(28, 14),
                            Child = headerBtn
                        },
                        matScroll
                    }
                }
            };
        }


        public override void OnClicked()
        {
            RefreshBlueprintList();
            if (_selectedBlueprint is null)
                ShowPlaceholder();
        }
    }
}