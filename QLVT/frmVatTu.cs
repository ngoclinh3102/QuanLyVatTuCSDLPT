﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QLVT
{
    public partial class frmVatTu : Form
    {
        int position = 0;
        Stack undolist = new Stack();

        public frmVatTu()
        {
            InitializeComponent();
        }

        private void FormVatTu_Load(object sender, EventArgs e)
        {
            dS.EnforceConstraints = false;

            this.vattuTableAdapter.Connection.ConnectionString = Program.constr;
            this.vattuTableAdapter.Fill(this.dS.Vattu);

            this.cTDDHTableAdapter.Connection.ConnectionString = Program.constr;
            this.cTDDHTableAdapter.Fill(this.dS.CTDDH);

            this.cTPNTableAdapter.Connection.ConnectionString = Program.constr;
            this.cTPNTableAdapter.Fill(this.dS.CTPN);

            this.cTPXTableAdapter.Connection.ConnectionString = Program.constr;
            this.cTPXTableAdapter.Fill(this.dS.CTPX);

            if (Program.mGroup == "CONGTY")
            {
                btnThem.Enabled = btnXoa.Enabled = btnGhi.Enabled = false;
            }

            txtMaVT.Enabled = btnUndo.Enabled = false;
        }

       
        private void BtnXoa_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            String maVT = "";
            DialogResult dr = MessageBox.Show("Bạn có chắc chắn muốn xóa vật tư này?", "Xác nhận",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (dr == DialogResult.OK)
            {
                // == Query tìm MAVT ==
                String mavattu = txtMaVT.Text.Trim();
                Program.conn = new SqlConnection(Program.constr);
                Program.conn.Open();
                String query_MAVT = "DECLARE @return_value int " +
                               "EXEC @return_value = [dbo].[SP_CHECKTT_MAVT] " +
                               "@p1 " +
                               "SELECT 'Return Value' = @return_value";
                SqlCommand sqlCommand = new SqlCommand(query_MAVT, Program.conn);
                sqlCommand.Parameters.AddWithValue("@p1", mavattu);
                SqlDataReader dataReader = null;

                try
                {
                    dataReader = sqlCommand.ExecuteReader();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Thực thi database thất bại!\n" + ex.Message, "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Đọc và lấy result
                dataReader.Read();
                int result_value_MAVT = int.Parse(dataReader.GetValue(0).ToString());
                dataReader.Close();
                if (result_value_MAVT == 1)
                {
                    MessageBox.Show("Vật tư đang được sử dụng ở chi chánh hiện tại!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (result_value_MAVT == 2)
                {
                    MessageBox.Show("Vật tư đang được sử dụng ở chi nhánh khác!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    try
                    {
                        String VT_info = txtMaVT.Text.Trim() + "#" + txtTenVT.Text.Trim() + "#" + txtDVT.Text.Trim() + "#" + numSLT.Text.Trim();
                        Console.WriteLine(VT_info);
                        maVT = ((DataRowView)bdsVatTu[bdsVatTu.Position])["MAVT"].ToString(); // Giữ lại mã để khi bị lỗi có thể quay về
                        bdsVatTu.RemoveCurrent();
                        btnUndo.Enabled = true;
                        undolist.Push(VT_info);
                        undolist.Push("DELETE");
                        this.vattuTableAdapter.Update(this.dS.Vattu);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi xảy ra trong quá trình xóa. Vui lòng thử lại!\n" + ex.Message, "Thông báo lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.vattuTableAdapter.Fill(this.dS.Vattu);
                        bdsVatTu.Position = bdsVatTu.Find("MAVT", maVT);
                        return;
                    }
                }
            }

            if (bdsVatTu.Count == 0) btnXoa.Enabled = false;
        }

        private void BtnThem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            position = bdsVatTu.Position;
            txtMaVT.Enabled = true;
            this.bdsVatTu.AddNew();
            btnThem.Enabled = btnXoa.Enabled = gridVatTu.Enabled = btnReload.Enabled = btnUndo.Enabled = false;
            btnGhi.Enabled = gcInfoVatTu.Enabled = true;
            numSLT.Value = 0;
            temp = 1;
        }
        private int temp = 0;
        private void BtnUndo_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            if (undolist.Count > 0)
            {
                String statement = undolist.Pop().ToString();
                if (statement.Equals("DELETE"))
                {
                    this.bdsVatTu.AddNew();
                    String TT = undolist.Pop().ToString();
                    Console.WriteLine(TT);
                    String[] TT_VT = TT.Split('#');

                    txtMaVT.Text = TT_VT[0];
                    txtTenVT.Text = TT_VT[1];
                    txtDVT.Text = TT_VT[2];
                    numSLT.Text = TT_VT[3];
                    this.bdsVatTu.EndEdit();
                    this.vattuTableAdapter.Update(this.dS.Vattu);
                }
                else if (statement.Equals("INSERT") && temp == 1)
                {
                    String maVT = undolist.Pop().ToString();
                    int vitrixoa = bdsVatTu.Find("MAVT", maVT);
                    bdsVatTu.Position = vitrixoa;
                    bdsVatTu.RemoveCurrent();
                    this.vattuTableAdapter.Update(this.dS.Vattu);
                    temp = 0;
                }
            }
            if (undolist.Count == 0) btnUndo.Enabled = false;
        }

        private void BtnReload_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.vattuTableAdapter.Fill(this.dS.Vattu);
        }

        private void BtnThoat_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.Close();
        }

        private void BtnGhi_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (txtMaVT.Text.Trim() == "")
            {
                MessageBox.Show("Mã vật tư không được thiếu!", "", MessageBoxButtons.OK);
                txtMaVT.Focus();
                return;
            }
            if (txtTenVT.Text.Trim() == "")
            {
                MessageBox.Show("Tên vật tư không được thiếu!", "", MessageBoxButtons.OK);
                txtTenVT.Focus();
                return;
            }
            if (txtDVT.Text.Trim() == "")
            {
                MessageBox.Show("Đơn vị tính không được thiếu!", "", MessageBoxButtons.OK);
                txtDVT.Focus();
                return;
            }

            if (float.Parse(numSLT.Text) < 0)
            {
                MessageBox.Show("Số lượng tồn không hợp lệ!", "", MessageBoxButtons.OK);
                numSLT.Focus();
                return;
            }
            if (ValidateChildren(ValidationConstraints.Enabled))
            {
                String mavattu = txtMaVT.Text.Trim();
                Program.conn = new SqlConnection(Program.constr);
                Program.conn.Open();
                String query_MAVT = "DECLARE @return_value int " +
                               "EXEC @return_value = [dbo].[SP_CHECKTT_MAVT] " +
                               "@p1 " +
                               "SELECT 'Return Value' = @return_value";
                SqlCommand sqlCommand1 = new SqlCommand(query_MAVT, Program.conn);
                sqlCommand1.Parameters.AddWithValue("@p1", mavattu);
                SqlDataReader dataReader1 = null;

                try
                {
                    dataReader1 = sqlCommand1.ExecuteReader();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Thực thi database thất bại!\n" + ex.Message, "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Đọc và lấy result
                dataReader1.Read();
                int result_value_MAVT = int.Parse(dataReader1.GetValue(0).ToString());
                dataReader1.Close();
                if (result_value_MAVT == 1)
                {
                    MessageBox.Show("Không thể sửa vì vật tư đang được sử dụng ở chi chánh hiện tại!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else if (result_value_MAVT == 2)
                {
                    MessageBox.Show("Không thể sửa vì vật tư đang được sử dụng ở chi nhánh khác!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int checkMaVT = bdsVatTu.Find("TENVT", txtTenVT.Text);
                if (checkMaVT != -1 && (checkMaVT != bdsVatTu.Position))
                {
                    MessageBox.Show("Tên vật tư trùng. Vui lòng chọn tên vật tư khác!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Có cần thiết chạy SP không trong khi VATTU là nhân bản???
                Program.conn = new SqlConnection(Program.constr);
                Program.conn.Open();
                String query = "DECLARE	@return_value int " +
                               "EXEC @return_value = [dbo].[SP_CHECKID] " +
                               "@p1, @p2 " +
                               "SELECT 'Return Value' = @return_value";
                SqlCommand sqlCommand = new SqlCommand(query, Program.conn);
                sqlCommand.Parameters.AddWithValue("@p1", txtMaVT.Text);
                sqlCommand.Parameters.AddWithValue("@p2", "MAVT");

                SqlDataReader dataReader = null;
                try
                {
                    dataReader = sqlCommand.ExecuteReader();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Thực thi database thất bại!\n" + ex.Message, "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Đọc và lấy result
                dataReader.Read();
                int result_value = int.Parse(dataReader.GetValue(0).ToString());
                dataReader.Close();

                int indexMaVT = bdsVatTu.Find("MAVT", txtMaVT.Text);
                int indexCurrent = bdsVatTu.Position;
                String maVT = txtMaVT.Text;
                if (result_value == 1 && (indexMaVT != indexCurrent))
                {
                    MessageBox.Show("Mã vật tư đã tồn tại!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    DialogResult dr = MessageBox.Show("Bạn có chắc muốn ghi dữ liệu vào Database không?", "Thông báo",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                    if (dr == DialogResult.OK)
                    {
                        try
                        {
                            btnThem.Enabled = btnXoa.Enabled = gridVatTu.Enabled = btnReload.Enabled = btnGhi.Enabled = gcInfoVatTu.Enabled = true;
                            btnUndo.Enabled = true;
                            this.bdsVatTu.EndEdit();
                            this.vattuTableAdapter.Update(this.dS.Vattu);
                            if(temp==1)
                            {
                                undolist.Push(maVT);
                                undolist.Push("INSERT");
                            }    
                            else
                            {
                                btnUndo.Enabled = false;
                            }    
                            bdsVatTu.Position = position;
                        }
                        catch (Exception ex)
                        {
                            // Khi Update database lỗi thì xóa record vừa thêm trong bds
                            bdsVatTu.RemoveCurrent();
                            MessageBox.Show("Thất bại. Vui lòng kiểm tra lại!\n" + ex.Message, "Lỗi",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private bool Validate(TextBox tb, string str)
        {
            if (tb.Text.Trim().Equals(""))
            {
                MessageBox.Show(str, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus();
                return false;
            }
            return true;
        }

        private void TxtMaVT_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaVT.Text))
            {
                e.Cancel = true;
                txtMaVT.Focus();
                errorProvider1.SetError(txtMaVT, "Mã vật tư không được để trống!");
            }
            else if (txtMaVT.Text.Trim().Contains(" "))
            {
                e.Cancel = true;
                txtMaVT.Focus();
                errorProvider1.SetError(txtMaVT, "Mã vật tư không được chứa khoảng trắng!");
            }
            else if (txtMaVT.Text.Trim().Contains("#"))
            {
                e.Cancel = true;
                txtMaVT.Focus();
                errorProvider1.SetError(txtMaVT, "Mã vật tư không được chứa ký tự đặc biệt!");
            }
            else
            {
                e.Cancel = false;
                errorProvider1.SetError(txtMaVT, "");
            }
        }

        private void TxtTenVT_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenVT.Text))
            {
                e.Cancel = true;
                txtTenVT.Focus();
                errorProvider1.SetError(txtTenVT, "Tên vật tư không được để trống!");
            }
            else if (txtTenVT.Text.Trim().Contains("#"))
            {
                e.Cancel = true;
                txtTenVT.Focus();
                errorProvider1.SetError(txtTenVT, "Tên vật tư không được chứa ký tự đặc biệt!");
            }
            else
            {
                e.Cancel = false;
                errorProvider1.SetError(txtTenVT, "");
            }
        }

        private void TxtDVT_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDVT.Text))
            {
                e.Cancel = true;
                txtDVT.Focus();
                errorProvider1.SetError(txtDVT, "Đơn vị tính không được để trống!");
            }
            else if (txtDVT.Text.Trim().Contains("#"))
            {
                e.Cancel = true;
                txtDVT.Focus();
                errorProvider1.SetError(txtDVT, "Đơn vị tính không được chứa ký tự đặc biệt!");
            }
            else
            {
                e.Cancel = false;
                errorProvider1.SetError(txtDVT, "");
            }
        }

        private void NumSLT_Validating(object sender, CancelEventArgs e)
        {
            if (numSLT.Value == 0)
            {
                e.Cancel = true;
                numSLT.Focus();
                errorProvider1.SetError(numSLT, "Vui lòng chọn số lượng tồn!");
            }
            else
            {
                e.Cancel = false;
                errorProvider1.SetError(numSLT, "");
            }
        }

        private void txtDVT_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsLetter(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
