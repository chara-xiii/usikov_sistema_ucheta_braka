using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для menu.xaml — главное меню (панель управления).
    /// Набор разделов формируется в зависимости от роли пользователя.
    /// </summary>
    public partial class menu : Window
    {
        public string ConnectString;
        public int UserId;
        public string Fio;
        public string Role;

        private readonly List<Button> _navButtons = new List<Button>();

        public menu(string connectString, int userId, string fio, string role)
        {
            InitializeComponent();
            ConnectString = connectString;
            UserId = userId;
            Fio = fio;
            Role = role;
        }

        // ---------- Права доступа по ролям ----------
        private bool IsAdmin => Role == "Администратор";
        private bool IsManager => Role == "Менеджер по закупкам";
        private bool IsProduction => Role == "Производство";

        private bool CanRef => IsAdmin || IsManager;       // справочники и поступления
        private bool CanSpis => IsAdmin || IsManager;      // списание
        private bool CanProg => IsAdmin || IsManager;      // прогнозирование
        private bool CanReport => IsAdmin || IsManager;    // отчёты
        private bool CanUsers => IsAdmin;                  // управление пользователями

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblFio.Text = Fio;
            lblRole.Text = Role;
            lblTitle.Text = "Панель управления";
            lblSubtitle.Text = "Сводные показатели по учёту брака — " + Role;

            BuildNavigation();
            LoadKpi();
            LoadChart();
            LoadRecent();
        }

        // ================= Навигация =================
        private void BuildNavigation()
        {
            if (CanRef)
            {
                AddNav("Поставщики", () => new postavshchiki(ConnectString).Show());
                AddNav("Материалы", () => new materialy(ConnectString).Show());
                AddNav("Партии поступления", () => new partii(ConnectString).Show());
            }

            AddNav("Учёт брака", () => new brak(ConnectString, UserId).Show());

            if (CanSpis)
                AddNav("Списание брака", () => new spisanie(ConnectString, UserId).Show());

            if (CanProg)
                AddNav("Прогнозирование закупок", () => new prognoz(ConnectString).Show());

            AddNav("Заявки производства", () => new zayavki(ConnectString, UserId).Show());

            if (CanReport)
                AddNav("Отчёты", () => new otcet(ConnectString).Show());

            if (CanUsers)
                AddNav("Пользователи", () => new polzovateli(ConnectString).Show());
        }

        private void AddNav(string text, Action onClick)
        {
            Button b = new Button
            {
                Content = text,
                Style = (Style)FindResource("NavButton"),
                Margin = new Thickness(0, 2, 0, 2),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            b.Click += (s, e) =>
            {
                SetActive(b);
                onClick();
            };
            _navButtons.Add(b);
            pnlNav.Children.Add(b);
        }

        private void SetActive(Button active)
        {
            foreach (Button b in _navButtons)
                b.Style = (Style)FindResource("NavButton");
            active.Style = (Style)FindResource("NavButtonActive");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }

        // ================= KPI =================
        private void LoadKpi()
        {
            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                {
                    AddKpi("Поставщики", Scalar(con, "SELECT COUNT(*) FROM UP03_Postavshchiki"), "контрагентов");
                    AddKpi("Материалы", Scalar(con, "SELECT COUNT(*) FROM UP03_Materialy"), "позиций");
                    AddKpi("Партии", Scalar(con, "SELECT COUNT(*) FROM UP03_Partii"), "поступлений");
                    AddKpi("Записей о браке", Scalar(con, "SELECT COUNT(*) FROM UP03_Brak"), "фактов");
                    AddKpi("Объём брака", Scalar(con, "SELECT ISNULL(SUM(Kolichestvo),0) FROM UP03_Brak"), "единиц");
                    AddKpi("Списано актов", Scalar(con, "SELECT COUNT(*) FROM UP03_Spisanie"), "документов");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки показателей: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Scalar(SqlConnection con, string query)
        {
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                object v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value) return "0";
                if (v is decimal d) return d.ToString("0.##");
                return v.ToString();
            }
        }

        private void AddKpi(string title, string value, string hint)
        {
            Border card = new Border
            {
                Background = (Brush)FindResource("Surface"),
                BorderBrush = (Brush)FindResource("Border"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 12, 12),
                Width = 168
            };

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                Foreground = (Brush)FindResource("Khaki")
            });
            sp.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("Espresso"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            sp.Children.Add(new TextBlock
            {
                Text = hint,
                FontSize = 11,
                Foreground = (Brush)FindResource("Khaki")
            });

            card.Child = sp;
            pnlKpi.Children.Add(card);
        }

        // ================= Диаграмма =================
        private void LoadChart()
        {
            pnlChart.Children.Clear();
            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                {
                    string query = @"
                        SELECT LEFT(CONVERT(varchar(7), Data_vyyavleniya, 126), 7) AS Mes,
                               SUM(Kolichestvo) AS Kol
                        FROM UP03_Brak
                        GROUP BY LEFT(CONVERT(varchar(7), Data_vyyavleniya, 126), 7)
                        ORDER BY Mes";

                    Dictionary<string, decimal> data = new Dictionary<string, decimal>();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal kol = r["Kol"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Kol"]);
                            data[r["Mes"].ToString()] = kol;
                        }
                    }

                    if (data.Count == 0)
                    {
                        pnlChart.Children.Add(new TextBlock { Text = "Нет данных для отображения." });
                        return;
                    }

                    decimal max = 0;
                    foreach (var v in data.Values)
                        if (v > max) max = v;
                    if (max <= 0) max = 1;

                    foreach (var kv in data)
                    {
                        pnlChart.Children.Add(BuildBar(kv.Key, kv.Value, max));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки диаграммы: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UIElement BuildBar(string mes, decimal value, decimal max)
        {
            Grid row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });

            TextBlock lbl = new TextBlock
            {
                Text = mes,
                FontSize = 12,
                Foreground = (Brush)FindResource("Espresso"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 0);
            row.Children.Add(lbl);

            double width = (double)(value / max) * 230.0;
            if (width < 3) width = 3;

            Border bar = new Border
            {
                Background = (Brush)FindResource("AccentDark"),
                CornerRadius = new CornerRadius(4),
                Height = 18,
                Width = width,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(bar, 1);
            row.Children.Add(bar);

            TextBlock val = new TextBlock
            {
                Text = value.ToString("0.##"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Espresso"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(val, 2);
            row.Children.Add(val);

            return row;
        }

        // ================= Последние операции =================
        private void LoadRecent()
        {
            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                {
                    string query = @"
                        SELECT TOP 5
                            b.Data_vyyavleniya,
                            m.Naimenovanie AS Material,
                            p.Naimenovanie AS Postavshchik,
                            b.Kolichestvo,
                            b.Status
                        FROM UP03_Brak b
                        JOIN UP03_Materialy m ON b.Id_materiala = m.Id_materiala
                        LEFT JOIN UP03_Partii pa ON b.Id_partii = pa.Id_partii
                        LEFT JOIN UP03_Postavshchiki p ON pa.Id_postavshchika = p.Id_postavshchika
                        ORDER BY b.Data_vyyavleniya DESC, b.Id_braka DESC";

                    List<RecentRow> rows = new List<RecentRow>();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            rows.Add(new RecentRow
                            {
                                Data = r["Data_vyyavleniya"] == DBNull.Value ? "" : Convert.ToDateTime(r["Data_vyyavleniya"]).ToString("dd.MM.yyyy"),
                                Material = r["Material"].ToString(),
                                Postavshchik = r["Postavshchik"] == DBNull.Value ? "—" : r["Postavshchik"].ToString(),
                                Kolichestvo = r["Kolichestvo"] == DBNull.Value ? "" : Convert.ToDecimal(r["Kolichestvo"]).ToString("0.##"),
                                Status = r["Status"].ToString()
                            });
                        }
                    }
                    dgRecent.ItemsSource = rows;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки последних операций: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class RecentRow
        {
            public string Data { get; set; }
            public string Material { get; set; }
            public string Postavshchik { get; set; }
            public string Kolichestvo { get; set; }
            public string Status { get; set; }
        }
    }
}