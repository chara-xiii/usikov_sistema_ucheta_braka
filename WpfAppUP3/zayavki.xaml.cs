using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для zayavki.xaml — заявки производства.
    /// </summary>
    public partial class zayavki : Window
    {
        public string ConnectString;
        public int UserId;
        private readonly DataTable _table = new DataTable();

        public zayavki(string connectString, int userId)
        {
            InitializeComponent();
            ConnectString = connectString;
            UserId = userId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbStatus.ItemsSource = new List<string> { "Новая", "В работе", "Выполнена", "Отклонена" };
            cmbStatus.SelectedIndex = 0;
            LoadMaterials();
            RefreshDataGrid();
        }

        private void LoadMaterials()
        {
            List<IdName> mat = new List<IdName>();
            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand("SELECT Id_materiala, Naimenovanie FROM UP03_Materialy ORDER BY Naimenovanie", con))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                    mat.Add(new IdName { Id = Convert.ToInt32(r[0]), Name = r[1].ToString() });
            cmbMaterial.ItemsSource = mat;
        }

        private void RefreshDataGrid()
        {
            try
            {
                _table.Clear();
                string query = @"
                    SELECT z.Id_zayavki, m.Naimenovanie AS Material, z.Data_zayavki,
                           z.Kolichestvo, z.Kommentarii, z.Status, p.FIO, z.Id_materiala
                    FROM UP03_Zayavki z
                    JOIN UP03_Materialy m ON z.Id_materiala = m.Id_materiala
                    LEFT JOIN UP03_Polzovateli p ON z.Id_polzovatelya = p.Id_polzovatelya
                    ORDER BY z.Data_zayavki DESC, z.Id_zayavki DESC";

                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    _table.Load(reader);

                foreach (DataRow row in _table.Rows)
                    if (row["Data_zayavki"] != DBNull.Value)
                        row["Data_zayavki"] = Convert.ToDateTime(row["Data_zayavki"]).ToString("dd.MM.yyyy");

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
            cmbMaterial.SelectedIndex = -1;
            dpData.SelectedDate = null;
            txtKol.Clear();
            txtKomment.Clear();
            cmbStatus.SelectedIndex = 0;
            txtID.Clear();
            dg.SelectedItem = null;
        }

        private void clear_Click(object sender, RoutedEventArgs e) => ClearFields();

        private bool Validate()
        {
            if (cmbMaterial.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите материал.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!dpData.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату заявки.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (Utils.DecimalOrZero(txtKol.Text) <= 0)
            {
                MessageBox.Show("Введите количество больше нуля.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            if (MessageBox.Show("Создать заявку на материал?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO UP03_Zayavki (Id_materiala, Id_polzovatelya, Data_zayavki, Kolichestvo, Kommentarii, Status)
                      VALUES (@mat, @user, @data, @kol, @komm, @status)", con))
                {
                    cmd.Parameters.AddWithValue("@mat", (int)cmbMaterial.SelectedValue);
                    cmd.Parameters.AddWithValue("@user", UserId);
                    cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.Value.Date);
                    cmd.Parameters.AddWithValue("@kol", Utils.DecimalOrZero(txtKol.Text));
                    cmd.Parameters.AddWithValue("@komm", (object)txtKomment.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", cmbStatus.Text);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Заявка создана.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void upd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtID.Text))
            {
                MessageBox.Show("Выберите заявку для изменения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Validate()) return;
            if (MessageBox.Show("Изменить заявку?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE UP03_Zayavki
                      SET Id_materiala = @mat, Data_zayavki = @data, Kolichestvo = @kol,
                          Kommentarii = @komm, Status = @status
                      WHERE Id_zayavki = @id", con))
                {
                    cmd.Parameters.AddWithValue("@mat", (int)cmbMaterial.SelectedValue);
                    cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.Value.Date);
                    cmd.Parameters.AddWithValue("@kol", Utils.DecimalOrZero(txtKol.Text));
                    cmd.Parameters.AddWithValue("@komm", (object)txtKomment.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", cmbStatus.Text);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Заявка изменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void del_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtID.Text))
            {
                MessageBox.Show("Выберите заявку для удаления.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить заявку?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand("DELETE FROM UP03_Zayavki WHERE Id_zayavki = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Заявка удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView row)
            {
                txtID.Text = row["Id_zayavki"].ToString();
                int matId = row["Id_materiala"] == DBNull.Value ? 0 : Convert.ToInt32(row["Id_materiala"]);
                if (cmbMaterial.ItemsSource is IEnumerable<IdName> items)
                    foreach (IdName it in items)
                        if (it.Id == matId) { cmbMaterial.SelectedItem = it; break; }

                txtKol.Text = row["Kolichestvo"] == DBNull.Value ? "" : Convert.ToDecimal(row["Kolichestvo"]).ToString("0.##");
                txtKomment.Text = row["Kommentarii"].ToString();
                cmbStatus.Text = row["Status"].ToString();
                if (row["Data_zayavki"] != DBNull.Value)
                    dpData.SelectedDate = DateTime.Parse(row["Data_zayavki"].ToString());
                else
                    dpData.SelectedDate = null;
            }
        }
    }
}