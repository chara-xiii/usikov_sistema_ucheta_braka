using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для spisanie.xaml — списание брака.
    /// </summary>
    public partial class spisanie : Window
    {
        public string ConnectString;
        public int UserId;
        private readonly DataTable _table = new DataTable();

        public spisanie(string connectString, int userId)
        {
            InitializeComponent();
            ConnectString = connectString;
            UserId = userId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBrakCombo();
            RefreshDataGrid();
        }

        private void LoadBrakCombo()
        {
            List<IdName> list = new List<IdName>();
            string query = @"
                SELECT b.Id_braka, m.Naimenovanie AS Material, b.Data_vyyavleniya, b.Kolichestvo
                FROM UP03_Brak b
                JOIN UP03_Materialy m ON b.Id_materiala = m.Id_materiala
                WHERE NOT EXISTS (SELECT 1 FROM UP03_Spisanie s WHERE s.Id_braka = b.Id_braka)
                ORDER BY b.Data_vyyavleniya DESC";

            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string data = r["Data_vyyavleniya"] == DBNull.Value ? "" : Convert.ToDateTime(r["Data_vyyavleniya"]).ToString("dd.MM.yyyy");
                    decimal kol = r["Kolichestvo"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Kolichestvo"]);
                    list.Add(new IdName
                    {
                        Id = Convert.ToInt32(r["Id_braka"]),
                        Name = "№" + r["Id_braka"] + " — " + r["Material"] + " — " + data + " (" + kol.ToString("0.##") + ")"
                    });
                }
            }
            cmbBrak.ItemsSource = list;
        }

        private void cmbBrak_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBrak.SelectedItem is IdName item)
            {
                // Подставляем количество из выбранного брака
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand("SELECT Kolichestvo FROM UP03_Brak WHERE Id_braka = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", item.Id);
                    object v = cmd.ExecuteScalar();
                    if (v != null && v != DBNull.Value)
                        txtKol.Text = Convert.ToDecimal(v).ToString("0.##");
                }
                if (!dpData.SelectedDate.HasValue)
                    dpData.SelectedDate = DateTime.Today;
            }
        }

        private void RefreshDataGrid()
        {
            try
            {
                _table.Clear();
                string query = @"
                    SELECT s.Id_spisaniya, s.Data_spisaniya, s.Id_braka,
                           m.Naimenovanie AS Material, s.Kolichestvo, s.Prichina,
                           p.FIO
                    FROM UP03_Spisanie s
                    JOIN UP03_Brak b ON s.Id_braka = b.Id_braka
                    JOIN UP03_Materialy m ON b.Id_materiala = m.Id_materiala
                    LEFT JOIN UP03_Polzovateli p ON s.Id_polzovatelya = p.Id_polzovatelya
                    ORDER BY s.Data_spisaniya DESC, s.Id_spisaniya DESC";

                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    _table.Load(reader);

                foreach (DataRow row in _table.Rows)
                    if (row["Data_spisaniya"] != DBNull.Value)
                        row["Data_spisaniya"] = Convert.ToDateTime(row["Data_spisaniya"]).ToString("dd.MM.yyyy");

                dg.ItemsSource = _table.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFields()
        {
            cmbBrak.SelectedIndex = -1;
            dpData.SelectedDate = null;
            txtKol.Clear();
            txtPrichina.Clear();
            dg.SelectedItem = null;
        }

        private void clear_Click(object sender, RoutedEventArgs e) => ClearFields();

        private void add_Click(object sender, RoutedEventArgs e)
        {
            if (!(cmbBrak.SelectedItem is IdName item))
            {
                MessageBox.Show("Выберите запись о браке.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!dpData.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату списания.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Utils.DecimalOrZero(txtKol.Text) <= 0)
            {
                MessageBox.Show("Введите количество больше нуля.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Оформить списание выбранного брака?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                {
                    using (SqlCommand cmd = new SqlCommand(
                        @"INSERT INTO UP03_Spisanie (Id_braka, Id_polzovatelya, Data_spisaniya, Kolichestvo, Prichina)
                          VALUES (@brak, @user, @data, @kol, @prich)", con))
                    {
                        cmd.Parameters.AddWithValue("@brak", item.Id);
                        cmd.Parameters.AddWithValue("@user", UserId);
                        cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.Value.Date);
                        cmd.Parameters.AddWithValue("@kol", Utils.DecimalOrZero(txtKol.Text));
                        cmd.Parameters.AddWithValue("@prich", (object)txtPrichina.Text.Trim() ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    // Обновляем статус брака
                    using (SqlCommand cmd = new SqlCommand("UPDATE UP03_Brak SET Status = N'Списан' WHERE Id_braka = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", item.Id);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Брак списан.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadBrakCombo();
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при списании: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void del_Click(object sender, RoutedEventArgs e)
        {
            if (!(dg.SelectedItem is DataRowView row))
            {
                MessageBox.Show("Выберите акт списания для удаления.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int idSpis = Convert.ToInt32(row["Id_spisaniya"]);
            int idBrak = Convert.ToInt32(row["Id_braka"]);

            if (MessageBox.Show("Удалить акт списания? Статус брака вернётся в «Зарегистрирован».",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                {
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM UP03_Spisanie WHERE Id_spisaniya = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", idSpis);
                        cmd.ExecuteNonQuery();
                    }
                    using (SqlCommand cmd = new SqlCommand("UPDATE UP03_Brak SET Status = N'Зарегистрирован' WHERE Id_braka = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", idBrak);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Акт списания удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadBrakCombo();
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}