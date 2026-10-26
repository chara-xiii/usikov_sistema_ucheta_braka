using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для otcet.xaml — отчётность.
    /// </summary>
    public partial class otcet : Window
    {
        public string ConnectString;
        private DataTable _current;

        public otcet(string connectString)
        {
            InitializeComponent();
            ConnectString = connectString;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbType.ItemsSource = new List<string>
            {
                "Отчёт по браку",
                "Отчёт по качеству товаров",
                "Инвентаризационная ведомость"
            };
            cmbType.SelectedIndex = 0;
            dg.AutoGenerateColumns = true;
            Build();
        }

        private void Build_Click(object sender, RoutedEventArgs e) => Build();

        private void Build()
        {
            try
            {
                string type = cmbType.SelectedItem as string;
                DateTime from = dpFrom.SelectedDate ?? new DateTime(1900, 1, 1);
                DateTime to = dpTo.SelectedDate ?? new DateTime(2100, 1, 1);

                string query;
                if (type == "Отчёт по качеству товаров")
                {
                    query = @"
                        SELECT p.Naimenovanie AS [Поставщик],
                               ISNULL(s.Postavleno, 0) AS [Поставлено],
                               ISNULL(br.Brak, 0) AS [Брак],
                               CASE WHEN ISNULL(s.Postavleno, 0) = 0 THEN CAST(0 AS DECIMAL(18,2))
                                    ELSE CAST(ISNULL(br.Brak, 0) * 100.0 / ISNULL(s.Postavleno, 0) AS DECIMAL(18,2)) END AS [Вероятность брака, %],
                               CASE WHEN ISNULL(s.Postavleno, 0) = 0 THEN N'Нет данных'
                                    WHEN ISNULL(br.Brak, 0) * 100.0 / ISNULL(s.Postavleno, 0) < 2 THEN N'Надёжный'
                                    WHEN ISNULL(br.Brak, 0) * 100.0 / ISNULL(s.Postavleno, 0) < 5 THEN N'Средний'
                                    ELSE N'Рискованный' END AS [Оценка]
                        FROM UP03_Postavshchiki p
                        LEFT JOIN (SELECT Id_postavshchika, SUM(Kolichestvo) AS Postavleno FROM UP03_Partii GROUP BY Id_postavshchika) s
                               ON s.Id_postavshchika = p.Id_postavshchika
                        LEFT JOIN (SELECT pa.Id_postavshchika, SUM(b.Kolichestvo) AS Brak
                                   FROM UP03_Brak b JOIN UP03_Partii pa ON b.Id_partii = pa.Id_partii
                                   GROUP BY pa.Id_postavshchika) br
                               ON br.Id_postavshchika = p.Id_postavshchika
                        ORDER BY [Вероятность брака, %]";
                    _current = Query(query, null, null);
                }
                else if (type == "Инвентаризационная ведомость")
                {
                    query = @"
                        SELECT m.Naimenovanie AS [Материал],
                               m.Edinica_izmereniya AS [Ед. изм.],
                               ISNULL(s.Postavleno, 0) AS [Поступило],
                               ISNULL(br.Brak, 0) AS [Брак],
                               ISNULL(s.Postavleno, 0) - ISNULL(br.Brak, 0) AS [Годный остаток],
                               ISNULL(z.Zayavleno, 0) AS [Заявлено производством]
                        FROM UP03_Materialy m
                        LEFT JOIN (SELECT Id_materiala, SUM(Kolichestvo) AS Postavleno FROM UP03_Partii GROUP BY Id_materiala) s
                               ON s.Id_materiala = m.Id_materiala
                        LEFT JOIN (SELECT Id_materiala, SUM(Kolichestvo) AS Brak FROM UP03_Brak GROUP BY Id_materiala) br
                               ON br.Id_materiala = m.Id_materiala
                        LEFT JOIN (SELECT Id_materiala, SUM(Kolichestvo) AS Zayavleno FROM UP03_Zayavki GROUP BY Id_materiala) z
                               ON z.Id_materiala = m.Id_materiala
                        ORDER BY m.Naimenovanie";
                    _current = Query(query, null, null);
                }
                else
                {
                    query = @"
                        SELECT b.Id_braka AS [№],
                               b.Data_vyyavleniya AS [Дата],
                               m.Naimenovanie AS [Материал],
                               p.Naimenovanie AS [Поставщик],
                               b.Kolichestvo AS [Количество],
                               b.Prichina AS [Причина],
                               b.Status AS [Статус],
                               u.FIO AS [Зарегистрировал]
                        FROM UP03_Brak b
                        JOIN UP03_Materialy m ON b.Id_materiala = m.Id_materiala
                        LEFT JOIN UP03_Partii pa ON b.Id_partii = pa.Id_partii
                        LEFT JOIN UP03_Postavshchiki p ON pa.Id_postavshchika = p.Id_postavshchika
                        LEFT JOIN UP03_Polzovateli u ON b.Id_polzovatelya = u.Id_polzovatelya
                        WHERE b.Data_vyyavleniya >= @from AND b.Data_vyyavleniya <= @to
                        ORDER BY b.Data_vyyavleniya, b.Id_braka";
                    _current = Query(query, from, to);
                }

                dg.ItemsSource = _current.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка формирования отчёта: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable Query(string query, DateTime? from, DateTime? to)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                if (from.HasValue) cmd.Parameters.AddWithValue("@from", from.Value.Date);
                if (to.HasValue) cmd.Parameters.AddWithValue("@to", to.Value.Date);
                using (SqlDataReader r = cmd.ExecuteReader())
                    dt.Load(r);
            }
            return dt;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null || _current.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для выгрузки. Сформируйте отчёт.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string fileName = "Отчет_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    for (int i = 0; i < _current.Columns.Count; i++)
                    {
                        sw.Write(_current.Columns[i].ColumnName);
                        if (i < _current.Columns.Count - 1) sw.Write(";");
                    }
                    sw.WriteLine();

                    foreach (DataRow row in _current.Rows)
                    {
                        for (int i = 0; i < _current.Columns.Count; i++)
                        {
                            sw.Write(row[i].ToString());
                            if (i < _current.Columns.Count - 1) sw.Write(";");
                        }
                        sw.WriteLine();
                    }
                }

                MessageBox.Show("Отчёт сохранён на рабочем столе:\n" + filePath, "Экспорт выполнен",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}