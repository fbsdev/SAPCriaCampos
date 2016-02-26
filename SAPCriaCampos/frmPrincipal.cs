using SAPbobsCOM;
using System;
using System.Windows.Forms;

namespace SAPCriaCampos
{
    public partial class frmPrincipal : Form
    {
        public static string sErrMsg;
        public static int lErrCode;
        public static int lRetCode;

        public static Company oCompany = new Company();

        public frmPrincipal()
        {
            InitializeComponent();
            btnCriar.Enabled = btnExcluir.Enabled = false;
        }

        public void mensagem(string msg)
        {
            listBox1.Items.Add(String.Format("{0:d}-{0:t}: {1}", DateTime.Now, msg));
        }

        #region Criar Campo
        // baseado no post SAP Forum - http://scn.sap.com/thread/372248
        public void AddField(string tablename, string fieldname, string description, int type, int size)
        {
            SAPbobsCOM.UserFieldsMD oUserFieldsMD = null;
            oUserFieldsMD = ((SAPbobsCOM.UserFieldsMD)(oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oUserFields)));
            oUserFieldsMD.TableName = tablename;
            oUserFieldsMD.Name = fieldname;
            oUserFieldsMD.Description = description;

            switch (type)
            {
                case 1:
                    oUserFieldsMD.Type = SAPbobsCOM.BoFieldTypes.db_Alpha;
                    oUserFieldsMD.Size = size;
                    break;
                case 2:
                    oUserFieldsMD.Type = SAPbobsCOM.BoFieldTypes.db_Date;
                    oUserFieldsMD.Size = size;
                    break;
                case 3:
                    oUserFieldsMD.Type = SAPbobsCOM.BoFieldTypes.db_Float;
                    oUserFieldsMD.SubType = SAPbobsCOM.BoFldSubTypes.st_Rate;
                    break;

                case 4:
                    oUserFieldsMD.Type = SAPbobsCOM.BoFieldTypes.db_Memo;
                    oUserFieldsMD.Size = size;
                    break;
                case 5:
                    oUserFieldsMD.Type = SAPbobsCOM.BoFieldTypes.db_Numeric;
                    break;
            }

            lRetCode = oUserFieldsMD.Add();

            if (lRetCode != 0)
            {
                oCompany.GetLastError(out lErrCode, out sErrMsg);
                mensagem(sErrMsg);
            }
            else
            {
                mensagem("Campo: '" + oUserFieldsMD.Name + "' foi adicionado com sucesso na tabela " + oUserFieldsMD.TableName);
            }
        }
        #endregion

        #region Apagar Campo
        // Baseado no post https://scn.sap.com/thread/1445352
        private void DeleteUDF(string sTableID, string sFieldName)
        {
            SAPbobsCOM.UserFieldsMD sboField = (SAPbobsCOM.UserFieldsMD)oCompany.GetBusinessObject(BoObjectTypes.oUserFields);

            try
            {
                int iFieldID = GetFieldID(sTableID, sFieldName);
                if (sboField.GetByKey(sTableID, iFieldID))
                {
                    if (sboField.Remove() != 0)
                        mensagem("ERRO: Erro ao tentar remover o campo: " + oCompany.GetLastErrorDescription());

                    mensagem("Campo removido com sucesso: " + sFieldName);
                }                
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(sboField);
                sboField = null;
                GC.Collect();
            }
        }

        private int GetFieldID(string sTableID, string sAliasID)
        {
            int iRetVal = 0;
            SAPbobsCOM.Recordset sboRec = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                sboRec.DoQuery("select FieldID from CUFD where TableID = '" + sTableID + "' and AliasID = '" + sAliasID + "'");
                if (!sboRec.EoF) iRetVal = Convert.ToInt32(sboRec.Fields.Item("FieldID").Value.ToString());
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(sboRec);
                sboRec = null;
                GC.Collect();
            }
            return iRetVal;
        }
        #endregion



        private void btnConectar_Click(object sender, EventArgs e)
        {
            btnConectar.Enabled = false;
            mensagem("Aguarde .. conectando no banco de dados");

            this.Refresh();

            oCompany.Server = txtServidor.Text;
            oCompany.LicenseServer = txtLicenca.Text;
            oCompany.CompanyDB = txtCompanhia.Text;
            oCompany.UserName = txtUsuario.Text;
            oCompany.Password = txtSenha.Text;
            oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2008; // Banco de dados SQL 2008 Server
            oCompany.language = BoSuppLangs.ln_Portuguese_Br; // Define o idioma para portugues/br            

            string retStr;
            int retVal = oCompany.Connect();

            if (retVal != 0)
            {
                oCompany.GetLastError(out retVal, out retStr);
                mensagem("ERRO: " + retVal + " - " + retStr);
                btnConectar.Enabled = true;
                btnCriar.Enabled = btnExcluir.Enabled = false;
                return;
            }

            mensagem("Conectado com sucesso");
            btnConectar.Enabled = false;
            btnCriar.Enabled = btnExcluir.Enabled = true;
        }

        private void frmPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {            
            if (oCompany.Connected == true)
            {
                mensagem("Aguarde .. desconectando banco de dados");
                this.Refresh();
                oCompany.Disconnect();
            }
        }

        private void btnCriar_Click(object sender, EventArgs e)
        {            
            bool erro = false;

            int size = 0;
            int type = cbType.SelectedIndex + 1;

            try
            {
                size = Convert.ToInt32(txtSize.Text);
            }
            catch { };

            if (String.IsNullOrEmpty(txtTableName.Text))
            {
                mensagem("ERRO: Necessário nome da tabela que será criada o campo");
                erro = true;
            }

            if (String.IsNullOrEmpty(txtFieldName.Text))
            {
                mensagem("ERRO: Nome do Campo não preenchido");
                erro = true;
            }

            if (size > 255 || size == 0)
            {
                mensagem("ERRO: Tamanho deve ser entre 1 a 255");
                erro = true;
            }
            if (type == 0)
            {
                mensagem("ERRO: Necessário selecionar o tipo do campo");
                erro = true;
            }

            if (erro) return;

            AddField(txtTableName.Text, txtFieldName.Text, txtDescription.Text, type, size);

            // por segurança, desconectei o BD
            if (oCompany.Connected == true)
            {
                oCompany.Disconnect();
                mensagem("SAP Desconectado por segurança: " + oCompany.GetLastErrorDescription());
            }

            btnConectar.Enabled = true;
            btnCriar.Enabled = btnExcluir.Enabled = false;
        }

        private void btnExcluir_Click(object sender, EventArgs e)
        {
            bool erro = false;

            if (String.IsNullOrEmpty(txtTableName.Text))
            {
                mensagem("ERRO: Necessário nome da tabela");
                erro = true;
            }

            if (String.IsNullOrEmpty(txtFieldName.Text))
            {
                mensagem("ERRO: Nome do Campo não preenchido");
                erro = true;
            }

            if (erro) return;

            // Apaga o campo
            DeleteUDF(txtTableName.Text, txtFieldName.Text);

            // por segurança, desconectei o BD
            if (oCompany.Connected == true)
            {
                oCompany.Disconnect();
                mensagem("SAP Desconectado por segurança: " + oCompany.GetLastErrorDescription());
            }

            btnConectar.Enabled = true;
            btnCriar.Enabled = btnExcluir.Enabled = false;
        }
    }
}