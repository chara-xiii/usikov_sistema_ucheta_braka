using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для partii.xaml — партии поступления материалов.
    /// </summary>
    public partial class partii : Window
    {
        public string ConnectString;
        private readonly DataTable _table = new DataTable();

        public partii(string connectString)
        {
            InitializeComponent();
            ConnectString = connectString;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCombos();
            RefreshDataGrid();
        }

        private void LoadCombos()
        {
            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                {
                    List<IdName> post = new List<IdName>();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id_postavshchika, Naimenovanie FROM UP03_Postavshchiki ORDER BY Naimenovanie", con))
                    using (SqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            post.Add(new IdName { Id = Convert.ToInt32(r[0]), Name = r[1].ToString() });
                    cmbPostavshchik.ItemsSource = post;

                    List<IdName> mat = new List<IdName>();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id_materiala, Naimenovanie FROM UP03_Materialy ORDER BY Naimenovanie", con))
                    using (SqlDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            mat.Add(new IdName { Id = Convert.ToInt32(r[0]), Name = r[1].ToString() });
                    cmbMaterial.ItemsSource = mat;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshDataGrid()
        {
            try
            {
                _table.Clear();
                string query = @"
                    SELECT pa.Id_partii,
                           p.Naimenovanie AS Postavshchik,
                           m.Naimenovanie AS Material,
                           pa.Data_postupleniya,
                           pa.Kolichestvo,
                           pa.Cena_postavki,
                           pa.Nomer_nakladnoi
                    FROM UP03_Partii pa
                    JOIN UP03_Postavshchiki p ON pa.Id_postavshchika = p.Id_postavshchika
                    JOIN UP03_Materialy m ON pa.Id_materiala = m.Id_materiala
                    ORDER BY pa.Data_postupleniya DESC, pa.Id_partii DESC";

                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    _table.Load(reader);

                // Форматируем дату
                foreach (DataRow row in _table.Rows)
                    if (row["Data_postupleniya"] != DBNull.Value)
                        row["Data_postupleniya"] = Convert.ToDateTime(row["Data_postupleniya"]).ToString("dd.MM.yyyy");

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
            cmbPostavshchik.SelectedIndex = -1;
            cmbMaterial.SelectedIndex = -1;
            dpData.SelectedDate = null;
            txtKol.Clear();
            txtCena.Clear();
            txtNakl.Clear();
            txtID.Clear();
            dg.SelectedItem = null;
        }

        private void clear_Click(object sender, RoutedEventArgs e) => ClearFields();

        private bool Validate()
        {
            if (cmbPostavshchik.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите поставщика.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (cmbMaterial.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите материал.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!dpData.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату поступления.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (MessageBox.Show("Добавить партию поступления?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO UP03_Partii (Id_postavshchika, Id_materiala, Data_postupleniya, Kolichestvo, Cena_postavki, Nomer_nakladnoi)
                      VALUES (@post, @mat, @data, @kol, @cena, @nakl)", con))
                {
                    cmd.Parameters.AddWithValue("@post", (int)cmbPostavshchik.SelectedValue);
                    cmd.Parameters.AddWithValue("@mat", (int)cmbMaterial.SelectedValue);
                    cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.Value.Date);
                    cmd.Parameters.AddWithValue("@kol", Utils.DecimalOrZero(txtKol.Text));
                    cmd.Parameters.AddWithValue("@cena", Utils.DecimalOrNull(txtCena.Text));
                    cmd.Parameters.AddWithValue("@nakl", (object)txtNakl.Text.Trim() ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Партия добавлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Выберите партию для изменения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Validate()) return;
            if (MessageBox.Show("Изменить партию?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE UP03_Partii
                      SET Id_postavshchika = @post, Id_materiala = @mat, Data_postupleniya = @data,
                          Kolichestvo = @kol, Cena_postavki = @cena, Nomer_nakladnoi = @nakl
                      WHERE Id_partii = @id", con))
                {
                    cmd.Parameters.AddWithValue("@post", (int)cmbPostavshchik.SelectedValue);
                    cmd.Parameters.AddWithValue("@mat", (int)cmbMaterial.SelectedValue);
                    cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.Value.Date);
                    cmd.Parameters.AddWithValue("@kol", Utils.DecimalOrZero(txtKol.Text));
                    cmd.Parameters.AddWithValue("@cena", Utils.DecimalOrNull(txtCena.Text));
                    cmd.Parameters.AddWithValue("@nakl", (object)txtNakl.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Партия изменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Выберите партию для удаления.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить партию? Действие необратимо.", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand("DELETE FROM UP03_Partii WHERE Id_partii = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Партия удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении (возможно, есть связанный брак): " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView row)
            {
                txtID.Text = row["Id_partii"].ToString();
                SelectByName(cmbPostavshchik, row["Postavshchik"].ToString());
                SelectByName(cmbMaterial, row["Material"].ToString());
                txtKol.Text = row["Kolichestvo"] == DBNull.Value ? "" : Convert.ToDecimal(row["Kolichestvo"]).ToString("0.##");
                txtCena.Text = row["Cena_postavki"] == DBNull.Value ? "" : Convert.ToDecimal(row["Cena_postavki"]).ToString("0.##");
                txtNakl.Text = row["Nomer_nakladnoi"].ToString();
                if (row["Data_postupleniya"] != DBNull.Value)
                    dpData.SelectedDate = DateTime.Parse(row["Data_postupleniya"].ToString());
                else
                    dpData.SelectedDate = null;
            }
        }

        private static void SelectByName(ComboBox cmb, string name)
        {
            if (cmb.ItemsSource is List<IdName> items)
            {
                foreach (IdName it in items)
                {
                    if (it.Name == name)
                    {
                        cmb.SelectedItem = it;
                        return;
                    }
                }
            }
        }
    }
}