using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для brak.xaml — реестр учёта брака.
    /// </summary>
    public partial class brak : Window
    {
        public string ConnectString;
        public int UserId;
        private readonly DataTable _table = new DataTable();
        private List<IdName> _allPartii = new List<IdName>();

        public brak(string connectString, int userId)
        {
            InitializeComponent();
            ConnectString = connectString;
            UserId = userId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cmbPrichina.ItemsSource = new List<string>
            {
                "Коррозия", "Геометрия", "Трещины", "Дефект резьбы",
                "Комплектность", "Маркировка", "Повреждение упаковки", "Прочее"
            };
            cmbStatus.ItemsSource = new List<string> { "Зарегистрирован", "Списан", "Отклонён" };
            cmbStatus.SelectedIndex = 0;

            LoadMaterials();
            LoadPartii();
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

        private void LoadPartii()
        {
            _allPartii = new List<IdName>();
            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT pa.Id_partii, pa.Id_materiala, pa.Nomer_nakladnoi, pa.Data_postupleniya
                  FROM UP03_Partii pa ORDER BY pa.Data_postupleniya DESC", con))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string nakl = r["Nomer_nakladnoi"] == DBNull.Value ? "" : r["Nomer_nakladnoi"].ToString();
                    string data = r["Data_postupleniya"] == DBNull.Value ? "" : Convert.ToDateTime(r["Data_postupleniya"]).ToString("dd.MM.yyyy");
                    int matId = Convert.ToInt32(r["Id_materiala"]);
                    _allPartii.Add(new IdName
                    {
                        Id = Convert.ToInt32(r["Id_partii"]),
                        Name = "№" + r["Id_partii"] + " " + nakl + " (" + data + ")",
                        MaterialId = matId
                    });
                }
            }
            ApplyPartiaFilter(null);
        }

        private void ApplyPartiaFilter(int? materialId)
        {
            List<IdName> filtered = new List<IdName>();
            foreach (IdName p in _allPartii)
                if (materialId == null || p.MaterialId == materialId)
                    filtered.Add(p);
            cmbPartia.ItemsSource = filtered;
        }

        private void cmbMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int? mid = null;
            if (cmbMaterial.SelectedItem is IdName item) mid = item.Id;
            ApplyPartiaFilter(mid);
        }

        private void RefreshDataGrid()
        {
            try
            {
                _table.Clear();
                string query = @"
                    SELECT b.Id_braka,
                           b.Id_materiala,
                           b.Id_partii,
                           b.Data_vyyavleniya,
                           m.Naimenovanie AS Material,
                           p.Naimenovanie AS Postavshchik,
                           b.Kolichestvo,
                           b.Prichina,
                           b.Opisanie,
                           b.Status
                    FROM UP03_Brak b
                    JOIN UP03_Materialy m ON b.Id_materiala = m.Id_materiala
                    LEFT JOIN UP03_Partii pa ON b.Id_partii = pa.Id_partii
                    LEFT JOIN UP03_Postavshchiki p ON pa.Id_postavshchika = p.Id_postavshchika
                    ORDER BY b.Data_vyyavleniya DESC, b.Id_braka DESC";

                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    _table.Load(reader);

                foreach (DataRow row in _table.Rows)
                    if (row["Data_vyyavleniya"] != DBNull.Value)
                        row["Data_vyyavleniya"] = Convert.ToDateTime(row["Data_vyyavleniya"]).ToString("dd.MM.yyyy");

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
            cmbPartia.SelectedIndex = -1;
            dpData.SelectedDate = null;
            txtKol.Clear();
            cmbPrichina.Text = "";
            cmbStatus.SelectedIndex = 0;
            txtOpisanie.Clear();
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
                MessageBox.Show("Укажите дату выявления.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (MessageBox.Show("Зарегистрировать факт брака?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO UP03_Brak (Id_partii, Id_materiala, Id_polzovatelya, Data_vyyavleniya, Kolichestvo, Prichina, Opisanie, Status)
                      VALUES (@partia, @mat, @user, @data, @kol, @prich, @op, @status)", con))
                {
                    cmd.Parameters.AddWithValue("@partia", cmbPartia.SelectedValue ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@mat", (int)cmbMaterial.SelectedValue);
                    cmd.Parameters.AddWithValue("@user", UserId);
                    cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.Value.Date);
                    cmd.Parameters.AddWithValue("@kol", Utils.DecimalOrZero(txtKol.Text));
                    cmd.Parameters.AddWithValue("@prich", (object)cmbPrichina.Text ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@op", (object)txtOpisanie.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", cmbStatus.Text);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Факт брака зарегистрирован.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Выберите запись для изменения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Validate()) return;
            if (MessageBox.Show("Изменить запись о браке?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE UP03_Brak
                      SET Id_partii = @partia, Id_materiala = @mat, Data_vyyavleniya = @data,
                          Kolichestvo = @kol, Prichina = @prich, Opisanie = @op, Status = @status
                      WHERE Id_braka = @id", con))
                {
                    cmd.Parameters.AddWithValue("@partia", cmbPartia.SelectedValue ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@mat", (int)cmbMaterial.SelectedValue);
                    cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.Value.Date);
                    cmd.Parameters.AddWithValue("@kol", Utils.DecimalOrZero(txtKol.Text));
                    cmd.Parameters.AddWithValue("@prich", (object)cmbPrichina.Text ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@op", (object)txtOpisanie.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", cmbStatus.Text);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Запись изменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Выберите запись для удаления.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить запись о браке? Действие необратимо.", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand("DELETE FROM UP03_Brak WHERE Id_braka = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Запись удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении (возможно, есть связанное списание): " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView row)
            {
                txtID.Text = row["Id_braka"].ToString();

                int matId = row["Id_materiala"] == DBNull.Value ? 0 : Convert.ToInt32(row["Id_materiala"]);
                SelectById(cmbMaterial, matId);
                ApplyPartiaFilter(matId);

                if (row["Id_partii"] != DBNull.Value)
                    SelectById(cmbPartia, Convert.ToInt32(row["Id_partii"]));
                else
                    cmbPartia.SelectedIndex = -1;

                txtKol.Text = row["Kolichestvo"] == DBNull.Value ? "" : Convert.ToDecimal(row["Kolichestvo"]).ToString("0.##");
                cmbPrichina.Text = row["Prichina"].ToString();
                cmbStatus.Text = row["Status"].ToString();
                txtOpisanie.Text = row["Opisanie"].ToString();

                if (row["Data_vyyavleniya"] != DBNull.Value)
                    dpData.SelectedDate = DateTime.Parse(row["Data_vyyavleniya"].ToString());
                else
                    dpData.SelectedDate = null;
            }
        }

        private static void SelectById(ComboBox cmb, int id)
        {
            if (cmb.ItemsSource is IEnumerable<IdName> items)
            {
                foreach (IdName it in items)
                {
                    if (it.Id == id)
                    {
                        cmb.SelectedItem = it;
                        return;
                    }
                }
            }
        }
    }
}