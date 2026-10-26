using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для prognoz.xaml — прогнозирование закупок.
    /// Расчёт вероятности брака по поставщикам и построение линии тренда.
    /// </summary>
    public partial class prognoz : Window
    {
        public string ConnectString;

        // Отступы области построения графика
        private const double PadLeft = 52;
        private const double PadRight = 30;
        private const double PadTop = 20;
        private const double PadBottom = 46;

        public prognoz(string connectString)
        {
            InitializeComponent();
            ConnectString = connectString;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMaterials();
            LoadSuppliers();
        }

        private void LoadMaterials()
        {
            List<IdName> items = new List<IdName> { new IdName { Id = 0, Name = "Все материалы" } };
            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand("SELECT Id_materiala, Naimenovanie FROM UP03_Materialy ORDER BY Naimenovanie", con))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                    items.Add(new IdName { Id = Convert.ToInt32(r[0]), Name = r[1].ToString() });

            cmbMaterial.ItemsSource = items;
            cmbMaterial.SelectedIndex = 0;
        }

        private void LoadSuppliers()
        {
            List<IdName> items = new List<IdName> { new IdName { Id = 0, Name = "Все поставщики" } };
            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand("SELECT Id_postavshchika, Naimenovanie FROM UP03_Postavshchiki ORDER BY Naimenovanie", con))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                    items.Add(new IdName { Id = Convert.ToInt32(r[0]), Name = r[1].ToString() });

            cmbPostavshchik.ItemsSource = items;
            cmbPostavshchik.SelectedIndex = 0;
        }

        private void btnCalc_Click(object sender, RoutedEventArgs e)
        {
            int matId = 0;
            if (cmbMaterial.SelectedItem is IdName m) matId = m.Id;
            object matParam = matId == 0 ? (object)DBNull.Value : matId;
            int postId = 0;
            if (cmbPostavshchik.SelectedItem is IdName p) postId = p.Id;
            object postParam = postId == 0 ? (object)DBNull.Value : postId;

            try
            {
                LoadSupplierStatistics(matParam, postParam);
                DrawTrend(matParam);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка расчёта: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSupplierStatistics(object matParam, object postParam)
        {
            string query = @"
                SELECT p.Naimenovanie,
                       ISNULL(s.Postavleno, 0) AS Postavleno,
                       ISNULL(br.Brak, 0) AS Brak
                FROM UP03_Postavshchiki p
                LEFT JOIN (
                    SELECT Id_postavshchika, SUM(Kolichestvo) AS Postavleno
                    FROM UP03_Partii
                    WHERE (@mat IS NULL OR Id_materiala = @mat)
                    GROUP BY Id_postavshchika
                ) s ON s.Id_postavshchika = p.Id_postavshchika
                LEFT JOIN (
                    SELECT pa.Id_postavshchika, SUM(b.Kolichestvo) AS Brak
                    FROM UP03_Brak b
                    JOIN UP03_Partii pa ON b.Id_partii = pa.Id_partii
                    WHERE (@mat IS NULL OR b.Id_materiala = @mat)
                    GROUP BY pa.Id_postavshchika
                ) br ON br.Id_postavshchika = p.Id_postavshchika
                WHERE s.Postavleno IS NOT NULL
                  AND (@post IS NULL OR p.Id_postavshchika = @post)
                ORDER BY p.Naimenovanie";

            List<SupplierStat> stats = new List<SupplierStat>();
            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@mat", matParam);
                cmd.Parameters.AddWithValue("@post", postParam);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        decimal post = Convert.ToDecimal(r["Postavleno"]);
                        decimal brak = Convert.ToDecimal(r["Brak"]);
                        decimal prob = post > 0 ? Math.Round(brak / post * 100m, 2) : 0m;
                        stats.Add(new SupplierStat
                        {
                            Postavshchik = r["Naimenovanie"].ToString(),
                            Postavleno = post.ToString("0.##"),
                            Brak = brak.ToString("0.##"),
                            Veroyatnost = prob.ToString("0.##") + " %",
                            ProbValue = prob
                        });
                    }
                }
            }

            dg.ItemsSource = stats;

            if (stats.Count == 0)
            {
                lblRecomend.Text = "Недостаточно данных для расчёта.";
                return;
            }

            SupplierStat best = stats.OrderBy(s => s.ProbValue).First();
            SupplierStat worst = stats.OrderByDescending(s => s.ProbValue).First();
            lblRecomend.Text = "Наименьшая доля брака у поставщика «" + best.Postavshchik + "» (" +
                               best.Veroyatnost + "). Рекомендуется закупка у данного поставщика. " +
                               "Наибольший риск — «" + worst.Postavshchik + "» (" + worst.Veroyatnost + ").";
        }

        private void DrawTrend(object matParam)
        {
            chartCanvas.Children.Clear();

            Dictionary<string, decimal> supply = MonthSum(
                @"SELECT LEFT(CONVERT(varchar(7), Data_postupleniya, 126), 7) AS Mes, SUM(Kolichestvo) AS K
                  FROM UP03_Partii
                  WHERE (@mat IS NULL OR Id_materiala = @mat)
                  GROUP BY LEFT(CONVERT(varchar(7), Data_postupleniya, 126), 7)", matParam);

            Dictionary<string, decimal> brak = MonthSum(
                @"SELECT LEFT(CONVERT(varchar(7), Data_vyyavleniya, 126), 7) AS Mes, SUM(Kolichestvo) AS K
                  FROM UP03_Brak
                  WHERE (@mat IS NULL OR Id_materiala = @mat)
                  GROUP BY LEFT(CONVERT(varchar(7), Data_vyyavleniya, 126), 7)", matParam);

            List<string> months = supply.Keys.Union(brak.Keys).OrderBy(x => x).ToList();
            if (months.Count == 0)
            {
                AddText("Нет данных для построения графика.", PadLeft, PadTop + 40);
                return;
            }

            double plotW = chartCanvas.Width - PadLeft - PadRight;
            double plotH = chartCanvas.Height - PadTop - PadBottom;

            List<double> values = new List<double>();
            foreach (string mes in months)
            {
                decimal s = supply.ContainsKey(mes) ? supply[mes] : 0m;
                decimal b = brak.ContainsKey(mes) ? brak[mes] : 0m;
                values.Add(s > 0 ? (double)(b / s * 100m) : 0.0);
            }

            double max = values.Count > 0 ? values.Max() : 1;
            if (max <= 0) max = 1;
            max = Math.Ceiling(max * 1.2);

            // Оси
            AddLine(PadLeft, PadTop, PadLeft, PadTop + plotH, (Brush)FindResource("Border"), 1);
            AddLine(PadLeft, PadTop + plotH, PadLeft + plotW, PadTop + plotH, (Brush)FindResource("Border"), 1);

            // Сетка и подписи по Y
            int steps = 4;
            for (int i = 0; i <= steps; i++)
            {
                double y = PadTop + plotH - plotH * i / steps;
                AddLine(PadLeft, y, PadLeft + plotW, y, (Brush)FindResource("Border"), 0.6);
                double val = max * i / steps;
                AddText(val.ToString("0.#"), 8, y - 9, 42, (Brush)FindResource("Khaki"), 11, TextAlignment.Right);
            }

            // Точки
            int n = months.Count;
            double stepX = n > 1 ? plotW / (n - 1) : 0;
            Polyline line = new Polyline
            {
                Stroke = (Brush)FindResource("AccentDark"),
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };

            for (int i = 0; i < n; i++)
            {
                double x = PadLeft + (n > 1 ? stepX * i : plotW / 2);
                double y = PadTop + plotH - plotH * values[i] / max;

                line.Points.Add(new Point(x, y));

                Ellipse dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = (Brush)FindResource("Accent"),
                    Stroke = (Brush)FindResource("Espresso"),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(dot, x - 5);
                Canvas.SetTop(dot, y - 5);
                chartCanvas.Children.Add(dot);

                // Значение над точкой
                AddText(values[i].ToString("0.#") + "%", x - 22, y - 24, 44,
                    (Brush)FindResource("Espresso"), 11, TextAlignment.Center);

                // Подпись месяца
                string label = mesLabel(months[i]);
                AddText(label, x - 24, PadTop + plotH + 8, 48,
                    (Brush)FindResource("Espresso"), 11, TextAlignment.Center);
            }

            chartCanvas.Children.Insert(0, line);
        }

        private Dictionary<string, decimal> MonthSum(string query, object matParam)
        {
            Dictionary<string, decimal> result = new Dictionary<string, decimal>();
            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@mat", matParam);
                using (SqlDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        string mes = r["Mes"] == DBNull.Value ? "" : r["Mes"].ToString();
                        if (mes == "") continue;
                        result[mes] = r["K"] == DBNull.Value ? 0m : Convert.ToDecimal(r["K"]);
                    }
            }
            return result;
        }

        private static string mesLabel(string mes)
        {
            // "2026-01" -> "01.2026"
            if (DateTime.TryParseExact(mes, "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime dt))
                return dt.ToString("MM.yyyy");
            return mes;
        }

        private void AddLine(double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        {
            chartCanvas.Children.Add(new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = stroke,
                StrokeThickness = thickness
            });
        }

        private void AddText(string text, double left, double top, double width, Brush brush,
            double fontSize, TextAlignment align)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                Width = width,
                FontSize = fontSize,
                Foreground = brush,
                TextAlignment = align,
                FontFamily = (FontFamily)FindResource("AppFont")
            };
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, top);
            chartCanvas.Children.Add(tb);
        }

        private void AddText(string text, double left, double top)
        {
            AddText(text, left, top, 400, (Brush)FindResource("Espresso"), 13, TextAlignment.Left);
        }

        public class SupplierStat
        {
            public string Postavshchik { get; set; }
            public string Postavleno { get; set; }
            public string Brak { get; set; }
            public string Veroyatnost { get; set; }
            public decimal ProbValue { get; set; }
        }
    }
}
