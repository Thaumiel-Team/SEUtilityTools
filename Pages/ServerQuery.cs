using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SEUtilityTools.API.Data;
using SEUtilityTools.API.Helpers;
using SEUtilityTools.API.Interface;
using ic = Projektanker.Icons.Avalonia;

namespace SEUtilityTools.Pages
{
    public class ServerQuery : Page
    {
        public class QueryData : PageData
        {
            public List<ServerDataDto> Data { get; set; } = [];
        }

        public QueryData Data { get; set; } = new();

        public override string PageName => nameof(ServerQuery);
        public override string Description => "Server query page";

        private StackPanel? _serverListPanel;
        private Panel? _detailArea;
        private ServerDataDto? _selectedServer;
        private readonly Dictionary<ServerDataDto, Border> _cardMap = [];


        public override Control CreateContent()
        {
            _cardMap.Clear();
            _selectedServer = null;

            Grid root = new()
            {
                ColumnDefinitions = new ColumnDefinitions("280,*")
            };

            Grid.SetColumn(BuildLeftPanel(), 0);
            root.Children.Add(BuildLeftPanel());

            _detailArea = new Panel { Background = new SolidColorBrush(Color.Parse("#0f0f0f")) };
            Grid.SetColumn(_detailArea, 1);
            root.Children.Add(_detailArea);

            RefreshServerList();
            ShowPlaceholder();

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
                Text = "Queried Servers",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(16, 16, 16, 10)
            };

            Grid.SetRow(header, 0);
            leftGrid.Children.Add(header);
            _serverListPanel = new StackPanel
            {
                Spacing = 2,
                Margin = new Thickness(8, 0)
            };

            ScrollViewer scroll = new()
            {
                Content = _serverListPanel,
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
            TextBox ipBox = new()
            {
                Watermark = "IP:Port  —  e.g. 64.20.60.18:27017",
                Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                FontSize = 12,
            };

            Button queryBtn = new()
            {
                Content = "Query Server",
                Background = new SolidColorBrush(Color.Parse("#00d084")),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(0, 10),
                FontWeight = FontWeight.SemiBold,
                FontSize = 13,
            };

            queryBtn.Click += async (_, _) =>
            {
                string raw = ipBox.Text?.Trim() ?? "";
                int lastColon = raw.LastIndexOf(':');
                if (lastColon <= 0 || lastColon == raw.Length - 1)
                    return;

                string host = raw[..lastColon];
                if (!int.TryParse(raw[(lastColon + 1)..], out int port))
                    return;

                queryBtn.IsEnabled = false;
                queryBtn.Content = "Querying…";

                ServerData? result = await ServerManager.QueryServerAsync(host, port);

                queryBtn.IsEnabled = true;
                queryBtn.Content = "Query Server";

                if (result is null)
                    return;

                Data.Data.RemoveAll(s => s.Ip == result.Ip && s.Port == result.Port);
                Data.Data.Add(result.ToDto());

                RefreshServerList();
                SelectServer(Data.Data.Last());
                ipBox.Text = string.Empty;
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
                        ipBox,
                        queryBtn
                    }
                }
            };
        }

        private void RefreshServerList()
        {
            if (_serverListPanel is null)
                return;

            _serverListPanel.Children.Clear();
            _cardMap.Clear();

            foreach (ServerDataDto server in Data.Data)
            {
                Border card = BuildServerCard(server);
                _cardMap[server] = card;
                _serverListPanel.Children.Add(card);
            }

            if (_selectedServer is not null && _cardMap.ContainsKey(_selectedServer))
                ApplyHighlight(_selectedServer);
        }

        private Border BuildServerCard(ServerDataDto server)
        {
            Button refreshBtn = new()
            {
                Content = new ic::Icon
                {
                    Value = "fa-solid fa-rotate",
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

            Button joinBtn = new()
            {
                Content = new ic::Icon
                {
                    Value = "fa-solid fa-arrow-right-to-bracket",
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

            joinBtn.Click += (_, e) =>
            {
                e.Handled = true;
                Process.Start(new ProcessStartInfo($"steam://connect/{server.Ip}:{server.Port}")
                {
                    UseShellExecute = true
                });
            };

            refreshBtn.Click += async (_, e) =>
            {
                e.Handled = true;
                refreshBtn.IsEnabled = false;

                ServerData? result = await ServerManager.QueryServerAsync(server.Ip, server.Port);
                if (result is not null)
                {
                    ServerDataDto updated = result.ToDto();
                    Data.Data[Data.Data.IndexOf(server)] = updated;
                    RefreshServerList();
                    SelectServer(updated);
                }

                refreshBtn.IsEnabled = true;
            };

            StackPanel textStack = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = server.Name,
                        Foreground = Brushes.White,
                        FontSize = 13,
                        FontWeight = FontWeight.Medium,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    },
                    new TextBlock
                    {
                        Text = $"{server.Ip}:{server.Port}  •  {server.PlayerCount}/{server.MaxPlayers} players",
                        Foreground = new SolidColorBrush(Color.Parse("#707070")),
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    }
                }
            };

            StackPanel actionButtons = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Children =
                {
                    joinBtn,
                    refreshBtn
                }
            };

            Grid cardContent = new()
            {
                Children =
                {
                    textStack,
                    actionButtons
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
                joinBtn.IsVisible = true;
                refreshBtn.IsVisible = true;
                if (_selectedServer != server)
                    card.Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
            };

            card.PointerExited += (_, _) =>
            {
                joinBtn.IsVisible = false;
                refreshBtn.IsVisible = false;
                if (_selectedServer != server)
                    card.Background = Brushes.Transparent;
            };

            card.PointerPressed += (_, _) => SelectServer(server);
            return card;
        }

        private void SelectServer(ServerDataDto server)
        {
            _selectedServer = server;
            ApplyHighlight(server);
            ShowServerDetail(server);
        }

        private void ApplyHighlight(ServerDataDto server)
        {
            foreach ((ServerDataDto? key, Border? card) in _cardMap)
            {
                card.Background = key == server ? new SolidColorBrush(Color.Parse("#252525")) : Brushes.Transparent;
            }
        }

        private void ShowPlaceholder()
        {
            if (_detailArea is null)
                return;

            _detailArea.Children.Clear();
            _detailArea.Children.Add(new TextBlock
            {
                Text = "Select a server or query a new one",
                Foreground = new SolidColorBrush(Color.Parse("#404040")),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        private void ShowServerDetail(ServerDataDto server)
        {
            if (_detailArea is null)
                return;

            _detailArea.Children.Clear();

            Grid root = new()
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,*"),
                Margin = new Thickness(28, 24, 28, 24)
            };

            StackPanel headerPanel = new()
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = server.Name,
                Foreground = Brushes.White,
                FontSize = 22,
                FontWeight = FontWeight.Bold
            });

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"{server.Ip}:{server.Port}",
                Foreground = new SolidColorBrush(Color.Parse("#707070")),
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 0)
            });

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

            AddPill("Players", $"{server.PlayerCount} / {server.MaxPlayers}");

            Grid.SetRow(pills, 1);
            root.Children.Add(pills);

            Grid tableGrid = new()
            {
                RowDefinitions = new RowDefinitions("Auto,*")
            };

            Grid colHeaders = new()
            {
                ColumnDefinitions = new ColumnDefinitions("*,100,130"),
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

            AddColHeader("Player", 0);
            AddColHeader("Time", 1);

            Border colHeaderBorder = new()
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#252525")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 8),
                Child = colHeaders
            };

            Grid.SetRow(colHeaderBorder, 0);
            tableGrid.Children.Add(colHeaderBorder);

            StackPanel rowsPanel = new();
            foreach (PlayerDto player in server.Players)
            {
                Grid rowGrid = new()
                {
                    ColumnDefinitions = new ColumnDefinitions("*,100,130")
                };

                void AddCell(string text, int col, bool muted = false)
                {
                    TextBlock tb = new()
                    {
                        Text = text,
                        Foreground = muted ? new SolidColorBrush(Color.Parse("#555555")) : new SolidColorBrush(Color.Parse("#c0c0c0")),
                        FontSize = 13,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Grid.SetColumn(tb, col);
                    rowGrid.Children.Add(tb);
                }

                AddCell(string.IsNullOrWhiteSpace(player.Name) ? "—" : player.Name, 0);
                AddCell(player.Duration.ToString(@"hh\:mm\:ss"), 1, true);

                rowsPanel.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush(Color.Parse("#1c1c1c")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(12, 10),
                    Child = rowGrid
                });
            }

            if (server.Players.Count == 0)
            {
                rowsPanel.Children.Add(new TextBlock
                {
                    Text = "No players currently online",
                    Foreground = new SolidColorBrush(Color.Parse("#484848")),
                    FontSize = 13,
                    Margin = new Thickness(12, 20)
                });
            }

            ScrollViewer rowScroll = new()
            {
                Content = rowsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(rowScroll, 1);
            tableGrid.Children.Add(rowScroll);

            Grid.SetRow(tableGrid, 2);
            root.Children.Add(tableGrid);

            _detailArea.Children.Add(root);
        }

        public override void OnClicked()
        {
            Data = DataManager.LoadData<QueryData>(this) ?? new QueryData();
            RefreshServerList();
        }

        public override void OnClosed()
        {
            DataManager.SaveData(this, Data);
        }
    }
}