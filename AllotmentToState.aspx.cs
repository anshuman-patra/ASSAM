using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using DataAccessLayer;
using System.IO;
using System.Text;
using System.Web.Configuration;
using System.Text.RegularExpressions;

public partial class NHM_Allotment_AllotmentToState : System.Web.UI.Page
{
    IDBManager db = new DBManager(DataProvider.SqlServer, ConfigurationManager.ConnectionStrings["DataConnString"].ConnectionString);
    UtilityLibrary utl = new UtilityLibrary();

    #region//Global Variables Declaration..!
    decimal _ProjAAP_Amt, PCountZeroAAP_Amt = 0;
    decimal _CompAAP_Amt, CCountZeroAAP_Amt = 0;
    decimal _SubCompAAP_Amt, SCountZeroAAP_Amt = 0;
    decimal _AllotAAP_Amt = 0;
    int _ProjNoOfTarget, _CompNoOfTarget, _SubCompNoOfTarget, _AllotNoOfTarget = 0;
    decimal num = new decimal(0);

    string OfficeType_Chk = "";
    #endregion




    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserLoginDetails"] != null && Session["AuthToken"] != null && Request.Cookies["AuthToken"] != null)
        {
            if (!Session["AuthToken"].ToString().Equals(Request.Cookies["AuthToken"].Value))
                return;
            if (!Page.IsPostBack)
            {
                UserLoginDetails objUserLoginDetails = new UserLoginDetails();
                objUserLoginDetails = (UserLoginDetails)Session["UserLoginDetails"];
                hfUserID.Value = objUserLoginDetails.UserID;
                hfUserType.Value = objUserLoginDetails.UserType;
                hfDeptCode.Value = objUserLoginDetails.DeptCode;
                hfDeptName.Value = objUserLoginDetails.DeptName;
                hfOfficeCode.Value = objUserLoginDetails.OfficeCode;
                hfOffName.Value = objUserLoginDetails.OffName;
                hfOfficeType.Value = objUserLoginDetails.OfficeType;
                hfF_Year.Value = objUserLoginDetails.F_Year;
                hfFirstLogin.Value = objUserLoginDetails.FirstLogin;
                hfSchemeCode.Value = ConfigurationManager.AppSettings["KeySchemeCode"].ToString();
                lblDept.Text = hfDeptName.Value;
                lblDept1.Text = hfDeptName.Value;

                if ((hfUserType.Value == "A") && hfFirstLogin.Value == "N")
                {
                    bindGrid();
                    PopulateYear();
                    PopulateYear1();
                    bind_totalAmount();
                    bind_ActionPlanAmount();
                    bind_totalOB();
                    PopulateScheme(hfDeptCode.Value);
                    utl.SetSessionCookie();

                    if (hfOfficeType.Value == "D")
                    {
                        rblOfficeTypeH.Items[0].Enabled = false;
                        rblOfficeTypeH.Items[1].Enabled = true;
                        rblOfficeTypeH.Items[1].Selected = true;
                        PopulateDistrictsH();
                    }

                    hfSession.Value = Session["AuthTokenPage"].ToString();
                    lblMsg.Text = "";

                    GetAllQuarters_FromFnYear(FnYear: ddlYear.SelectedValue);
                    GetTotalAndAvail_Balance(office_code: hfOfficeCode.Value);
                    PopulateScheme1(hfDeptCode.Value);
                    Populate_AllotmentBalance(office_code: hfOfficeCode.Value);




                }
            }
            //Response.Redirect("~/Unauthorised.aspx", true);
        }

        else { return; }
    }

    #region allotment to state

    #region//Header Details..!

    protected void PopulateYear()
    {
        DataSet dsYear = new DataSet();
        try
        {
            db.Open();
            dsYear = db.ExecuteDataSet(CommandType.Text, "SELECT Fyr FROM Financial_Year ORDER BY Fyr DESC");
            ddlYear.DataSource = dsYear;
            ddlYear.DataValueField = "Fyr";
            ddlYear.DataTextField = "Fyr";
            ddlYear.DataBind();
            ddlYear.SelectedValue = hfF_Year.Value;
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {
            db.Close();
            dsYear.Clear();
            dsYear.Dispose();
            ddlYear.SelectedIndex = utl.ddlSelIndex(ddlYear, hfF_Year.Value);
        }
    }

    protected void PopulateScheme(string deptCode)
    {
        DataSet dsScheme = new DataSet();
        try
        {
            db.Open();
            Regex regDept = new Regex(@"^\d{2}$");
            Regex regYear = new Regex(@"^\d{4}-\d{2}$");
            if (regDept.IsMatch(deptCode))
            {
                dsScheme = db.ExecuteDataSet(CommandType.Text, "SELECT Scheme_Code,Scheme_Name FROM NHM_Schemes WHERE Dept_Code='" + deptCode + "' AND Active_Status='A' ORDER BY Scheme_Name");
                ddlScheme.DataSource = dsScheme;
                ddlScheme.DataValueField = "Scheme_Code";
                ddlScheme.DataTextField = "Scheme_Name";
                ddlScheme.DataBind();
                ddlScheme.Items.Insert(0, "Select Scheme");
                if (hfSchemeCode.Value != "")
                {
                    ddlScheme.SelectedValue = hfSchemeCode.Value;
                }
            }
            else
                throw new ApplicationException("Invalid Characters!");
        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {
            db.Close();
            dsScheme.Clear();
            dsScheme.Dispose();
        }
    }

    protected void bind_totalOB()
    {
        DataSet dsScheme = new DataSet();
        string SQL = string.Empty;
        try
        {

            Regex regDept = new Regex(@"^\d{2}$");
            Regex regYear = new Regex(@"^\d{4}-\d{2}$");
            db.Open();
            SQL = "select  sum(OB_Amt) Total_Available from SNA_FnYearWise_OB where F_Year='" + hfF_Year.Value + "'";
            dsScheme = db.ExecuteDataSet(CommandType.Text, SQL);
            db.Close();
            if (dsScheme.Tables[0].Rows.Count > 0)
            {
                string amt = dsScheme.Tables[0].Rows[0]["Total_Available"].ToString();
                if (amt != "")
                {
                    lbl_ob.Text = amt;
                    HF_ob.Value = lbl_ob.Text;
                }
                else
                {
                    lbl_ob.Text = "0.00000";
                    HF_ob.Value = "0.00000";
                }

            }
            else
            {
                lbl_ob.Text = "0.00000";
                HF_ob.Value = "0.00000";
            }



        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {

            dsScheme.Clear();
            dsScheme.Dispose();
        }
    }
    protected void bind_totalAmount()
    {
        DataSet dsScheme = new DataSet();
        string SQL = string.Empty;
        try
        {

            Regex regDept = new Regex(@"^\d{2}$");
            Regex regYear = new Regex(@"^\d{4}-\d{2}$");
            db.Open();
            SQL = "SELECT sum(Sanction_Amt) 'Total_Available' from  SNA_Plan_Sanction where F_Year='" + hfF_Year.Value + "' ";
            dsScheme = db.ExecuteDataSet(CommandType.Text, SQL);
            db.Close();
            if (dsScheme.Tables[0].Rows.Count > 0)
            {
                string amt = dsScheme.Tables[0].Rows[0]["Total_Available"].ToString();
                if (amt != "")
                {
                    lbl_amt.Text = amt;
                    hf_Sanction_OB.Value = lbl_amt.Text;
                }
                else
                {
                    lbl_amt.Text = "0.00000";
                    hf_Sanction_OB.Value = "0.00000";
                }
            }
            else
            {
                lbl_amt.Text = "0.00000";
                hf_Sanction_OB.Value = "0.00000";
            }



        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {

            dsScheme.Clear();
            dsScheme.Dispose();
        }
    }
    protected void bind_ActionPlanAmount()
    {
        DataSet dsScheme = new DataSet();
        string SQL = string.Empty;
        try
        {

            Regex regDept = new Regex(@"^\d{2}$");
            Regex regYear = new Regex(@"^\d{4}-\d{2}$");
            db.Open();
            SQL = "select sum(convert(decimal(18,5),AAP_Amt)) 'Total_AAP_Amt' from SNA_AnnualActionPlan_ProgrammeWise_Dtls where F_Year='" + hfF_Year.Value + "'";
            dsScheme = db.ExecuteDataSet(CommandType.Text, SQL);
            db.Close();
            if (dsScheme.Tables[0].Rows.Count > 0)
            {
                string amt = dsScheme.Tables[0].Rows[0]["Total_AAP_Amt"].ToString();
                if (amt != "")
                {
                    lbl_actionAmt.Text = amt;
                    hf_actionAMT.Value = lbl_actionAmt.Text;
                }
                else
                {
                    lbl_actionAmt.Text = "0.00000";
                    hf_actionAMT.Value = "0.00000";
                }
            }
            else
            {
                lbl_actionAmt.Text = "0.00000";
                hf_actionAMT.Value = "0.00000";
            }

        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {

            dsScheme.Clear();
            dsScheme.Dispose();
        }
    }
    protected void bindGrid()
    {
        DataSet dsScheme = new DataSet();
        string SQL = string.Empty;
        try
        {

            Regex regDept = new Regex(@"^\d{2}$");
            Regex regYear = new Regex(@"^\d{4}-\d{2}$");
            db.Open();
            SQL = "select t2.Fn_Year,t1.district_name,t1.District_code,t2.Allotment_Amt from(select district_code+'00'  'District_code',district_name from MASTER_DISTRICT where district_code = '1800' )as t1 " +
                "inner join(select Office_Code, Allotment_Amt, Fn_Year from SNA_Allotment where ProgramName is null) as t2 on t1.District_code = t2.Office_Code";
            dsScheme = db.ExecuteDataSet(CommandType.Text, SQL);
            db.Close();
            if (dsScheme.Tables[0].Rows.Count > 0)
            {
                hf_lstAmt.Value = dsScheme.Tables[0].Rows[0]["Allotment_Amt"].ToString();
                gdv_altmnt.DataSource = dsScheme;
                gdv_altmnt.DataBind();
                gdv_altmnt.Visible = true;
            }
            else
            {
                gdv_altmnt.DataSource = dsScheme;
                gdv_altmnt.DataBind();
                gdv_altmnt.Visible = false;
            }



        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {

            dsScheme.Clear();
            dsScheme.Dispose();
        }
    }
    protected void ddlScheme_Changed(object sender, System.EventArgs e)
    {
        panelAction.Visible = false;
    }
    protected void rblOfficeTypeH_Changed(object sender, EventArgs e)
    {
        if (rblOfficeTypeH.SelectedValue == "H")
        {
            ViewState["OfficeType"] = "H";
            panelAction.Visible = false;
            lblOfficeType.Text = "State Office";
            PopulateDistrictsH();
        }
        if (rblOfficeTypeH.SelectedValue == "D")
        {
            ViewState["OfficeType"] = "D";
            panelAction.Visible = false;
            lblOfficeType.Text = "District";
            PopulateDistrictsH();
        }
        if (rblOfficeTypeH.SelectedValue == "B")
        {
            ViewState["OfficeType"] = "B";
            panelAction.Visible = false;
            lblOfficeType.Text = "District";
            PopulateDistrictsH();
        }

    }

    #endregion
    protected void btnCancel1_Click(object sender, System.EventArgs e)
    {
        ClearFields_OB();
    }
    protected void ClearFields_OB()
    {
        ddlScheme.SelectedIndex = 0;
        rblOfficeTypeH.SelectedIndex = -1;
        txtOB_Amt_AbsoluteValue.Text = "0";
        txtOB_Amt.Text = "";
        txtOB_Amt_Crore.Text = "0";
        txt_remarks.Text = "";
        panelAction.Visible = false;
    }
    protected void txtOB_Amt_AbsoluteValue_TextChanged(object sender, EventArgs e)
    {
        decimal sacn = Convert.ToDecimal(hf_Sanction_OB.Value);
        decimal OB = Convert.ToDecimal(HF_ob.Value);
        decimal ob = sacn + OB;
        decimal action_amt = Convert.ToDecimal(hf_actionAMT.Value);
        txtOB_Amt_Crore.Text = "0";
        txtOB_Amt.Text = "";
        if (txtOB_Amt_AbsoluteValue.Text != "" || txtOB_Amt_AbsoluteValue.Text != "0")
        {
            decimal Rec_Amount = Convert.ToDecimal(txtOB_Amt_AbsoluteValue.Text);
            decimal Rec_Amount_AbsoluteValue = Rec_Amount / Convert.ToDecimal("100000");
            if (Rec_Amount_AbsoluteValue > ob)
            {
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Allotment Amount should not be greater than Sanction Amount and Opening Balance !');", true);
                ScriptManager.GetCurrent(Page).SetFocus(txtOB_Amt_AbsoluteValue);
            }
            //else if(Rec_Amount_AbsoluteValue > action_amt)
            //{
            //    ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Allotment Amount should not be greater than Action Plan Amount!');", true);
            //    ScriptManager.GetCurrent(Page).SetFocus(txtOB_Amt_AbsoluteValue);
            //}
            else
            {
                txtOB_Amt.Text = Rec_Amount_AbsoluteValue.ToString();
                //lbl_totalAlt_Balance.Text = txtOB_Amt.Text;
                //HftotalAlt_Balance.Value = lbl_totalAlt_Balance.Text;

                if (Rec_Amount_AbsoluteValue != 0)
                {
                    decimal Crore_Amt = Rec_Amount / Convert.ToDecimal("10000000");
                    txtOB_Amt_Crore.Text = Crore_Amt.ToString();
                    txt_remarks.Focus();
                }
            }


        }
    }
    protected void txtOB_Amt_TextChanged(object sender, EventArgs e)
    {
        txtOB_Amt_Crore.Text = "0";
        txtOB_Amt_AbsoluteValue.Text = "";
        if (txtOB_Amt.Text != "" || txtOB_Amt.Text != "0")
        {
            decimal Rec_Amount = Convert.ToDecimal(txtOB_Amt.Text);
            decimal Rec_Amount_AbsoluteValue = Rec_Amount * Convert.ToDecimal("100000");
            txtOB_Amt_AbsoluteValue.Text = Rec_Amount_AbsoluteValue.ToString();

            if (Rec_Amount_AbsoluteValue != 0)
            {
                decimal Crore_Amt = Rec_Amount_AbsoluteValue / Convert.ToDecimal("10000000");
                txtOB_Amt_Crore.Text = Crore_Amt.ToString();
                txt_remarks.Focus();
            }

        }
    }
    protected void txtOB_Amt_Crore_TextChanged(object sender, EventArgs e)
    {
        txtOB_Amt.Text = "0";
        txtOB_Amt_AbsoluteValue.Text = "";
        if (txtOB_Amt_Crore.Text != "" || txtOB_Amt_Crore.Text != "0")
        {
            decimal Rec_Amount = Convert.ToDecimal(txtOB_Amt_Crore.Text);
            decimal Rec_Amount_AbsoluteValue = Rec_Amount * Convert.ToDecimal("10000000");
            txtOB_Amt_AbsoluteValue.Text = Rec_Amount_AbsoluteValue.ToString();

            if (Rec_Amount_AbsoluteValue != 0)
            {
                decimal Lakhs_Amt = Rec_Amount_AbsoluteValue / Convert.ToDecimal("100000");
                txtOB_Amt.Text = Lakhs_Amt.ToString();
                txt_remarks.Focus();
            }

        }
    }
    protected void btnSubmit1_Click(object sender, System.EventArgs e)
    {
        int VAL= 0;
        VAL = ddlScheme.SelectedIndex;
        if (VAL == 1)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Please Select Scheme!');", true);
            ScriptManager.GetCurrent(Page).SetFocus(ddlScheme);
        }

        if (rblOfficeTypeH.SelectedIndex == -1)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Please Select Annual ActionPlan for!');", true);
            ScriptManager.GetCurrent(Page).SetFocus(rblOfficeTypeH);
        }
        else if (txtOB_Amt_AbsoluteValue.Text == "0")
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Please Enter Allotment Amount!');", true);
            ScriptManager.GetCurrent(Page).SetFocus(txtOB_Amt);
        }
        else if (txt_remarks.Text == "")
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Please Enter Remarks!');", true);
            ScriptManager.GetCurrent(Page).SetFocus(txt_remarks);
        }

        else
        {
            try
            {
                Regex regDept = new Regex(@"^\d{2}$");
                Regex regSch = new Regex(@"^\d{3}$");
                Regex regOff = new Regex(@"^\d{6}$");
                Regex regYear = new Regex(@"^\d{4}-\d{2}$");
                string dept = hfDeptCode.Value;
                string sch = ddlScheme.SelectedValue;
                string fyear = ddlYear.SelectedValue;
                string OfficeCode = hfOfficeCode.Value;
                string OfficeType = "";
                if (rblOfficeTypeH.SelectedValue == "H")
                {
                    OfficeType = "H";
                    OfficeCode = hfOfficeCode.Value;

                }
                if (rblOfficeTypeH.SelectedValue == "D")
                {
                    OfficeType = "D";
                    OfficeCode = hfOfficeCode.Value;

                }
                if (rblOfficeTypeH.SelectedValue == "B")
                {
                    OfficeType = "B";
                    OfficeCode = hfOfficeCode.Value;

                }



                decimal last_amountAlt = 0;
                if (hf_lstAmt.Value != "")
                {
                    last_amountAlt = Convert.ToDecimal(hf_lstAmt.Value);
                }


                decimal amt = Convert.ToDecimal(lbl_amt.Text);
                decimal lksAmt = Convert.ToDecimal(txtOB_Amt.Text.Trim());
                decimal avil_amt = amt - last_amountAlt;
                if (lksAmt > amt)
                {
                    ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert(' Allotment Amount Should not be greater than Available Balance!');", true);
                    ScriptManager.GetCurrent(Page).SetFocus(txtOB_Amt_AbsoluteValue);
                }
                else if (lksAmt > avil_amt)
                {
                    ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert(' Insufficient Balance !!!');", true);
                    ScriptManager.GetCurrent(Page).SetFocus(txtOB_Amt_AbsoluteValue);
                }
                else
                {
                    db.CreateInParameters(8);
                    db.AddInParameters(0, "@Dept_Code", hfDeptCode.Value);
                    db.AddInParameters(1, "@Scheme_Code", ddlScheme.SelectedItem.Value);
                    db.AddInParameters(2, "@Office_Code", OfficeCode);
                    db.AddInParameters(3, "@Fn_Year", ddlYear.SelectedItem.Value);
                    db.AddInParameters(4, "@Allotment_Amt", Convert.ToDecimal(txtOB_Amt.Text.Trim()));
                    db.AddInParameters(5, "@Office_Type", rblOfficeTypeH.SelectedValue);
                    db.AddInParameters(6, "@remarks", txt_remarks.Text.Trim());
                    db.AddInParameters(7, "@Entry_By", hfUserID.Value);

                    db.CreateOutParameters(1);
                    db.AddOutParameters(0, "@msg", 1, 100);
                    db.Open();
                    db.ExecuteNonQuery(CommandType.StoredProcedure, "SNA_Allotment_To_State");

                    // MAINTAIN ACTIVITY LOG ON ACCESSING PAGE
                    //
                    string msg = db.outParameters[0].Value.ToString();
                    db.Close();
                    if (msg.ToString() == "Allotment Submited Successfully")
                    {
                        bindGrid();
                        int activityid;
                        ActivityLog activity = new ActivityLog();
                        activity.UserID = hfUserID.Value;
                        activity.UserIP = Convert.ToString(HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
                        activity.ActivityType = "Action";
                        activity.Activity = "Opening Balance Entry";
                        activity.PageURL = System.Web.HttpContext.Current.Request.Url.ToString();
                        activity.Remark = db.outParameters[0].Value.ToString(); ;
                        activityid = ActivityLog.InsertActivityLog(activity);
                        ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + msg + "');", true);
                        //ClearFields_OB();
                        panelAction.Visible = true;
                       // pnlGrid_OB.Visible = true;

                    }
                    else
                    {
                        ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + msg + "');", true);
                        panelAction.Visible = true;
                        //PnlGrid_OB.Visible = true;
                    }
                }

                ClearFields_OB();



            }
            catch (ApplicationException exception)
            {
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
                string errorString = ExceptionHandler.CreateErrorMessage(ex);
                ExceptionHandler.WriteLog(errorString);
            }
            finally
            {

            }


        }
    }
    protected decimal ConvertText_To_Decimal(string value)
    {
        try
        {
            return Convert.ToDecimal(value);
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return 0;
        }
    }


    protected void gdv_altmnt_RowDataBound(object sender, GridViewRowEventArgs e)
    {

    }

    protected void lnkdelete_Click(object sender, EventArgs e)
    {
        if (hfSession.Value == Session["AuthTokenPage"].ToString())
        {
            LinkButton lnkdelete = (LinkButton)sender;
            GridViewRow gridRow = (GridViewRow)lnkdelete.NamingContainer;
            Label ofc_code = (Label)gridRow.FindControl("lblDistrict_code");
            Label amt = (Label)gridRow.FindControl("lblAllotment_Amt");


            try
            {

                db.CreateInParameters(4);
                db.AddInParameters(0, "@Dept_Code", hfDeptCode.Value);
                db.AddInParameters(1, "@Scheme_Code", hfSchemeCode.Value);
                db.AddInParameters(2, "@office_code", ofc_code.Text);
                db.AddInParameters(3, "@alt_amt", amt.Text);



                db.CreateOutParameters(1);
                db.AddOutParameters(0, "@msg", 1, 100);
                db.Open();
                db.ExecuteNonQuery(CommandType.StoredProcedure, "SNA_Allotment_To_State_Delete");
                string msg = db.outParameters[0].Value.ToString();
                db.Close();


                // MAINTAIN ALLOTMENT LOG
                if (msg == "Allotment Deleted Successfully")
                {
                    bindGrid();
                    int activityid;
                    ActivityLog activity = new ActivityLog();
                    activity.UserID = hfUserID.Value;
                    activity.UserIP = Convert.ToString(HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
                    activity.ActivityType = "Action";
                    activity.Activity = "Delete Allotment";
                    activity.PageURL = System.Web.HttpContext.Current.Request.Url.ToString();
                    activity.Remark = "Delete Allotment of State Office by Admin.";
                    activityid = ActivityLog.InsertActivityLog(activity);
                }
                else
                {
                    int activityid;
                    ActivityLog activity = new ActivityLog();
                    activity.UserID = hfUserID.Value;
                    activity.UserIP = Convert.ToString(HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
                    activity.ActivityType = "Action";
                    activity.Activity = "Delete Allotment";
                    activity.PageURL = System.Web.HttpContext.Current.Request.Url.ToString();
                    // activity.Remark = "Delete Majorhead" + txthead.Text + " could not be entered.";
                    activityid = ActivityLog.InsertActivityLog(activity);
                }
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + msg + "');", true);

            }
            catch (ApplicationException exception)
            {
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
                string message = ExceptionHandler.CreateErrorMessage(ex);
                ExceptionHandler.WriteLog(message);
            }
            finally
            {

            }
        }
        else
        {
            ExceptionHandler.WriteException("Session Value in Cookie And Hidden Field Does not Match");
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Error Executing the Request. Please Contact Site Administrator for Details!');", true);
        }
    }
    #endregion

    #region allotment to state Programme Wise
    protected void GetTotalAndAvail_Balance(string office_code)
    {
        DataSet dsHeadwiseDtls = new DataSet();
        try
        {
            db.CreateInParameters(1);
            db.AddInParameters(0, "@office_code", office_code);
            db.CreateOutParameters(3);
            db.AddOutParameters(0, "@available_amt", 1, 100);
            db.AddOutParameters(1, "@total_allotment", 1, 100);
            db.AddOutParameters(2, "@exep_Amt", 1, 100);
            db.Open();
            dsHeadwiseDtls = db.ExecuteDataSet(CommandType.StoredProcedure, "SNA_retrive_amt");
            string Avail_amt = db.outParameters[0].Value.ToString();
            string total_amt = db.outParameters[1].Value.ToString();
            db.Close();

            // lbl_totalAlt_Balance.Text = total_amt;
            // lbl_AvailAlt_Balance.Text = Avail_amt;
            // HftotalAlt_Balance.Value = lbl_totalAlt_Balance.Text;
            //  HfAvailAlt_Balance.Value = lbl_AvailAlt_Balance.Text;
        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string message = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(message);
            ExceptionHandler.WriteException(ex.Message);
        }
        finally
        {

            dsHeadwiseDtls.Clear();
            dsHeadwiseDtls.Dispose();
        }
    }
    protected void PopulateYear1()
    {
        DataSet dsYear = new DataSet();
        try
        {
            db.Open();
            dsYear = db.ExecuteDataSet(CommandType.Text, "SELECT Fyr FROM Financial_Year ORDER BY Fyr DESC");
            ddlYear1.DataSource = dsYear;
            ddlYear1.DataValueField = "Fyr";
            ddlYear1.DataTextField = "Fyr";
            ddlYear1.DataBind();
            ddlYear1.SelectedValue = hfF_Year.Value;
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {
            db.Close();
            dsYear.Clear();
            dsYear.Dispose();
            ddlYear.SelectedIndex = utl.ddlSelIndex(ddlYear, hfF_Year.Value);
        }
    }
    private void GetAllQuarters_FromFnYear(string FnYear)
    {
        DataSet dsQuarter = new DataSet();
        try
        {
            db.CreateInParameters(1);
            db.AddInParameters(0, "@FnYear", FnYear);
            db.AddInParameters(1, "@action", "getDDMMYYYY");
            db.Open();
            dsQuarter = db.ExecuteDataSet(CommandType.StoredProcedure, "SNA_GetAllQuarters_FromFnYear");
            db.Close();
            if (dsQuarter.Tables[0].Rows.Count > 0)
            {
                ddlQuarterly.DataSource = dsQuarter;
                ddlQuarterly.DataValueField = "quarter_number";
                ddlQuarterly.DataTextField = "Quarters";
                ddlQuarterly.DataBind();
                ddlQuarterly.Items.Insert(0, "Select Quarter");
            }
        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string message = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(message);
        }
        finally
        {
            dsQuarter.Clear();
            dsQuarter.Dispose();
        }
    }
    protected void PopulateScheme1(string deptCode)
    {
        DataSet dsScheme = new DataSet();
        try
        {
            db.Open();
            Regex regDept = new Regex(@"^\d{2}$");
            Regex regYear = new Regex(@"^\d{4}-\d{2}$");
            if (regDept.IsMatch(deptCode))
            {
                dsScheme = db.ExecuteDataSet(CommandType.Text, "SELECT Scheme_Code,Scheme_Name FROM NHM_Schemes WHERE Dept_Code='" + deptCode + "' AND Active_Status='A' ORDER BY Scheme_Name");
                ddlScheme1.DataSource = dsScheme;
                ddlScheme1.DataValueField = "Scheme_Code";
                ddlScheme1.DataTextField = "Scheme_Name";
                ddlScheme1.DataBind();
                ddlScheme1.Items.Insert(0, "Select Scheme");
                if (hfSchemeCode.Value != "")
                {
                    ddlScheme1.SelectedValue = hfSchemeCode.Value;
                }
            }
            else
                throw new ApplicationException("Invalid Characters!");
        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {
            db.Close();
            dsScheme.Clear();
            dsScheme.Dispose();
        }
    }
    protected void Populate_AllotmentBalance(string office_code)
    {
        DataSet dsScheme = new DataSet();
        try
        {
            db.Open(); 
            Regex regDept = new Regex(@"^\d{6}$");
            Regex regYear = new Regex(@"^\d{4}-\d{2}$");
            if (regDept.IsMatch(office_code))
            {
               
                dsScheme = db.ExecuteDataSet(CommandType.Text, "select Sum(isnull(Allotment_Amt,0)) 'amt' from SNA_Allotment where Office_Code='" + office_code + "' and  Fn_Year='" + hfF_Year.Value + "'");
                if (dsScheme.Tables[0].Rows.Count > 0)
                {
                    string Avail_amt_state = dsScheme.Tables[0].Rows[0]["amt"].ToString();
                    if (Avail_amt_state != "")
                    {
                        lbl_totalAlt_Balance.Text = Avail_amt_state;
                    }
                    else
                    {
                        lbl_totalAlt_Balance.Text = "0.00000";
                    }

                }
                else
                {
                    lbl_totalAlt_Balance.Text = "0.00000";
                }

            }
            else
                throw new ApplicationException("Invalid Characters!");
        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string errorString = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(errorString);
        }
        finally
        {
            db.Close();
            dsScheme.Clear();
            dsScheme.Dispose();
        }
    }
    protected void ddlScheme1_Changed(object sender, System.EventArgs e)
    {
        panelAction1.Visible = false;
    }
    protected void rblOfficeTypeH1_Changed(object sender, EventArgs e)
    {
        //if (rblOfficeTypeH1.SelectedValue == "H")
        //{
        //    ViewState["OfficeType"] = "H";
        //    panelAction1.Visible = false;
        //}
        if (rblOfficeTypeH1.SelectedValue == "H")
        {
            ViewState["OfficeType"] = "H";
            panelAction.Visible = false;
            lblOfficeType.Text = "State Office";
            trDistrict.Visible = true;
            rowBlock.Visible = false;
            PopulateDistrictsH();
        }
        if (rblOfficeTypeH1.SelectedValue == "D")
        {
            ViewState["OfficeType"] = "D";
            panelAction.Visible = false;
            lblOfficeType.Text = "District";
            trDistrict.Visible = true;
            rowBlock.Visible = false;
            PopulateDistrictsH();
        }
        if (rblOfficeTypeH1.SelectedValue == "B")
        {
            ViewState["OfficeType"] = "B";
            panelAction.Visible = false;
            lblOfficeType.Text = "District";
            trDistrict.Visible = true;
            rowBlock.Visible = false;
            PopulateDistrictsH();
        }
    }
    protected void PopulateDistrictsH()
    {
        DataSet dsDistrict = new DataSet();
        string strQry = string.Empty;
        try
        {
            this.db.Open();
            //if (rblOfficeTypeH.SelectedValue == "D")
            //    dsDistrict = this.db.ExecuteDataSet(CommandType.Text, "SELECT district_code,district_name FROM MASTER_DISTRICT ORDER BY district_name");//WHERE district_code!='2400' 
            //else
            //    dsDistrict = this.db.ExecuteDataSet(CommandType.Text, "SELECT district_code,district_name FROM MASTER_DISTRICT WHERE district_code!='2400'  ORDER BY district_name");

            if (hfOfficeType.Value == "H")
            {
                if (rblOfficeTypeH1.SelectedValue == "H")
                {
                    strQry = "SELECT district_code,district_name FROM MASTER_DISTRICT WHERE district_code='2400'  ORDER BY district_code";
                }
                else if (rblOfficeTypeH1.SelectedValue == "D" || rblOfficeTypeH1.SelectedValue == "B")
                {
                    strQry = "SELECT district_code,district_name FROM MASTER_DISTRICT WHERE district_code!='2400'  ORDER BY district_code";
                }
            }
            dsDistrict = db.ExecuteDataSet(CommandType.Text, strQry);
            this.ddlDistrict.DataSource = dsDistrict;
            this.ddlDistrict.DataValueField = "district_code";
            this.ddlDistrict.DataTextField = "district_name";
            this.ddlDistrict.DataBind();

            if (rblOfficeTypeH.SelectedValue == "H")
                this.ddlDistrict.Items.Insert(0, "Select Office");
            if (rblOfficeTypeH.SelectedValue == "D")
                this.ddlDistrict.Items.Insert(0, "===ALL===");
            if (rblOfficeTypeH.SelectedValue == "B")
                this.ddlDistrict.Items.Insert(0, "Select District");
        }
        catch (Exception exception)
        {
            ExceptionHandler.WriteException(exception.Message);
        }
        finally
        {
            this.db.Close();
            dsDistrict.Clear();
            dsDistrict.Dispose();
        }
    }
    protected void PopulateDistrictsD()
    {
        DataSet dsDistrict = new DataSet();
        try
        {
            this.db.Open();
            if (rblOfficeTypeH.SelectedValue == "D")
                dsDistrict = this.db.ExecuteDataSet(CommandType.Text, "SELECT district_code,district_name FROM MASTER_DISTRICT ORDER BY district_name");//WHERE district_code!='2400' 
            else
                dsDistrict = this.db.ExecuteDataSet(CommandType.Text, "SELECT district_code,district_name FROM MASTER_DISTRICT WHERE district_code!='1800'  ORDER BY district_name");
            this.ddlDistrict.DataSource = dsDistrict;
            this.ddlDistrict.DataValueField = "district_code";
            this.ddlDistrict.DataTextField = "district_name";
            this.ddlDistrict.DataBind();
            if (rblOfficeTypeH.SelectedValue == "D")
                this.ddlDistrict.Items.Insert(0, "===ALL===");
            else
                this.ddlDistrict.Items.Insert(0, "Select District");
            if ((hfUserType.Value == "A" || hfUserType.Value == "H" || hfUserType.Value == "S"))
            {
                string strOfficeCode = hfOfficeCode.Value;
                string DistCode = strOfficeCode.Substring(0, 4);

                ddlDistrict.SelectedIndex = utl.ddlSelIndex(ddlDistrict, DistCode);
                //ddlDistrict.Enabled = false;
                trDistrict.Visible = true;
                PopulateBlock(DistCode);
                rowBlock.Visible = true;
            }
            else
            {
                ddlDistrict.SelectedIndex = utl.ddlSelIndex(ddlDistrict, dsDistrict.Tables[0].Rows[0]["district_code"].ToString());
                //ddlDistrict.Enabled = false;
            }


        }
        catch (Exception exception)
        {
            ExceptionHandler.WriteException(exception.Message);
        }
        finally
        {
            this.db.Close();
            dsDistrict.Clear();
            dsDistrict.Dispose();
        }
    }
    protected void ddlDistrict_Changed(object sender, EventArgs e)
    {
        //pnlProject.Visible = false;
        //if (ddlDistrict.SelectedIndex > 0)
        //    hfDistrictCode.Value = ddlDistrict.SelectedValue;

        //pnlSubScheme.Visible = false;
        if (ddlDistrict.SelectedIndex > 0)
        {
            hfDistrictCode.Value = ddlDistrict.SelectedValue;
            if (rblOfficeTypeH1.SelectedValue == "B" && ddlDistrict.SelectedIndex > 0)
            {
                PopulateBlock(ddlDistrict.SelectedValue);
                rowBlock.Visible = true;
            }
            else
            {
                ddlBlock.Items.Clear();
                rowBlock.Visible = false;
            }
        }
    }
    protected void PopulateBlock(string DistCode)
    {
        DataSet dataSet = new DataSet();
        try
        {
            this.db.Open();
            Regex regex = new Regex("^\\d{4}$");
            if (!regex.IsMatch(DistCode))
            {
                throw new ApplicationException("Invalid Characters!");
            }
            IDBManager dBManager = this.db;
            string[] strArrays = new string[] { "SELECT block_code,block_name FROM MASTER_BLOCK WHERE district_code='", DistCode, "' ORDER BY block_name" };
            dataSet = dBManager.ExecuteDataSet(CommandType.Text, string.Concat(strArrays));
            this.ddlBlock.DataSource = dataSet;
            this.ddlBlock.DataValueField = "block_code";
            this.ddlBlock.DataTextField = "block_name";
            this.ddlBlock.DataBind();
            //this.ddlBlock.Items.Insert(0, "Select Block");
            if (rblOfficeTypeH1.SelectedValue == "B")
                this.ddlBlock.Items.Insert(0, "===ALL===");
            else
                this.ddlBlock.Items.Insert(0, "Select Block");
        }
        catch (ApplicationException applicationException1)
        {
            ApplicationException applicationException = applicationException1;
            System.Web.UI.ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", string.Concat("alert('", applicationException.Message, "');"), true);
        }
        catch (Exception exception1)
        {
            Exception exception = exception1;
            System.Web.UI.ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            ExceptionHandler.WriteLog(ExceptionHandler.CreateErrorMessage(exception));
        }
        finally
        {
            this.db.Close();
            dataSet.Clear();
            dataSet.Dispose();
        }
    }
    protected void ddlDistrict_DataBound(object sender, EventArgs e)
    {
        foreach (ListItem myItem in ddlDistrict.Items)
        {
            if (myItem.Text == "State Office")
            {

                myItem.Attributes.Add("style", "color:#FF3300");
            }
        }
    }
    //#endregion

    //protected void GetBalanceAmt(string FnYear, string DeptCode, string SchemeCode, string OfficeCode, int SubSchemeCode, int HeadCode, string ProgramName)
    //{
    //    DataSet dsAAP_Amt = new DataSet();
    //    string sqlQry = string.Empty;
        
    //    try
    //    {

    //        db.Open();
    //        Regex regex = new Regex("^\\d{2}$");
    //        Regex regex1 = new Regex("^\\d{3}$");
    //        Regex regex2 = new Regex("^\\d{4}-\\d{2}$");
    //        if (!regex.IsMatch(DeptCode) || !regex1.IsMatch(SchemeCode) || !regex2.IsMatch(FnYear))
    //        {
    //            throw new ApplicationException("Invalid Characters!");
    //        }
    //        //sqlQry = "SELECT ISNULL(t1.AAP_Amt,0)AAP_Amt FROM(SELECT Dept_Code,Scheme_Code,F_Year,Office_Code,Office_Type ,SubScheme_Code,Head_Code,[Program_Name] ,ISNULL(SUM(AAP_Amt),0)AAP_Amt FROM dbo.SNA_AnnualActionPlan_ProgrammeWise_Dtls WHERE Dept_Code='" + DeptCode + "' AND Scheme_Code='" + SchemeCode + "' AND F_Year='" + FnYear + "' AND Office_Code='" + OfficeCode + "' AND  [Program_Name]='" + ProgramName + "' GROUP BY Dept_Code,Scheme_Code,F_Year,Office_Code,Office_Type,SubScheme_Code,Head_Code,[Program_Name])AS t1  ";

    //        sqlQry = "SELECT ISNULL(t1.Sanction,0)sanction FROM(SELECT Dept_Code, Scheme_Code, Fn_Year, Office_Code , ISNULL(SUM(Allotment_Amt),0)Sanction FROM dbo.SNA_Allotment WHERE Dept_Code = '" + DeptCode + "' AND Scheme_Code = '" + SchemeCode + "'  AND Fn_Year = '2022-23' AND Office_Code = '" + FnYear + "'  GROUP BY Dept_Code,Scheme_Code,Fn_Year,Office_Code)AS t1 ";


    //        dsAAP_Amt = this.db.ExecuteDataSet(CommandType.Text, sqlQry);
            
    //    }
    //    catch (ApplicationException applicationException1)
    //    {
    //        ApplicationException applicationException = applicationException1;
    //        System.Web.UI.ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", string.Concat("alert('", applicationException.Message, "');"), true);
    //    }
    //    catch (Exception exception1)
    //    {
    //        Exception exception = exception1;
    //        System.Web.UI.ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
    //        ExceptionHandler.WriteLog(ExceptionHandler.CreateErrorMessage(exception));
    //    }
    //    finally
    //    {
    //        this.db.Close();
    //        dsAAP_Amt.Clear();
    //        dsAAP_Amt.Dispose();
    //    }
    //}

    protected void btnCancel11_Click(object sender, System.EventArgs e)
    {
        ClearFields_OB();
    }
    protected void ClearFields1_OB()
    {
        ddlScheme.SelectedIndex = 0;
        rblOfficeTypeH.SelectedIndex = -1;
        panelAction.Visible = false;
    }
    protected void btnSubmit11_Click(object sender, System.EventArgs e)
    {

        if (ddlScheme.SelectedIndex == 0)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Please Select Scheme!');", true);
            ScriptManager.GetCurrent(Page).SetFocus(ddlScheme);
        }

        else if (rblOfficeTypeH1.SelectedIndex == -1)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Please Select Annual ActionPlan for!');", true);
            ScriptManager.GetCurrent(Page).SetFocus(rblOfficeTypeH);
        }
        else
        {

            string OfficeCode = "";
            string OfficeType = "";
            if (rblOfficeTypeH1.SelectedValue == "H")
            {
                OfficeType = "H";
                OfficeCode = hfOfficeCode.Value;
                GetHeadwise_OB_Dtls(ddlYear.SelectedValue, hfDeptCode.Value, ddlScheme.SelectedValue);


            }
            if (rblOfficeTypeH1.SelectedValue == "D")
            {
                DataSet dsScheme = new DataSet();
                OfficeType = "D";
                OfficeCode = ddlDistrict.SelectedValue + "00";
                GetHeadwise_OB_Dtls(ddlYear.SelectedValue, hfDeptCode.Value, ddlScheme.SelectedValue);

                dsScheme = db.ExecuteDataSet(CommandType.Text, "select Sum(isnull(Allotment_Amt,0)) 'amt' from SNA_Allotment where Office_Code='" + OfficeCode + "' and  Fn_Year='" + hfF_Year.Value + "'");
                if (dsScheme.Tables[0].Rows.Count > 0)
                {
                    string Avail_amt_state = dsScheme.Tables[0].Rows[0]["amt"].ToString();
                    if (Avail_amt_state != "")
                    {
                        lbl_totalAlt_Balance.Text = Avail_amt_state;
                    }
                    else
                    {
                        lbl_totalAlt_Balance.Text = "0.00000";
                    }

                }
                else
                {
                    lbl_totalAlt_Balance.Text = "0.00000";
                }

            }
            if (rblOfficeTypeH1.SelectedValue == "B")
            {
                DataSet dsScheme = new DataSet();
                OfficeType = "B";
                OfficeCode = ddlBlock.SelectedValue;
                GetHeadwise_OB_Dtls(ddlYear.SelectedValue, hfDeptCode.Value, ddlScheme.SelectedValue);
                dsScheme = db.ExecuteDataSet(CommandType.Text, "select Sum(isnull(Allotment_Amt,0)) 'amt' from SNA_Allotment where Office_Code='" + OfficeCode + "' and  Fn_Year='" + hfF_Year.Value + "'");
                if (dsScheme.Tables[0].Rows.Count > 0)
                {
                    string Avail_amt_state = dsScheme.Tables[0].Rows[0]["amt"].ToString();
                    if (Avail_amt_state != "")
                    {
                        lbl_totalAlt_Balance.Text = Avail_amt_state;
                    }
                    else
                    {
                        lbl_totalAlt_Balance.Text = "0.00000";
                    }

                }
                else
                {
                    lbl_totalAlt_Balance.Text = "0.00000";
                }


            }

        }
    }


    protected void GetHeadwise_OB_Dtls(string FnYear, string DeptCode, string SchemeCode)
    {
        string OfficeCode = "";
        string OfficeType = "";
        if (rblOfficeTypeH1.SelectedValue == "H")
        {
            OfficeType = "H";
            OfficeCode = hfOfficeCode.Value;

        }
        if (rblOfficeTypeH1.SelectedValue == "D")
        {
            OfficeType = "D";
            OfficeCode = ddlDistrict.SelectedValue + "00";

        }
        if (rblOfficeTypeH1.SelectedValue == "B")
        {
            OfficeType = "B";
            OfficeCode = ddlBlock.SelectedValue;
        }

        DataSet dsHeadwiseDtls = new DataSet();
        try
        {
            db.CreateInParameters(6);
            db.AddInParameters(0, "@action", "fill_allotment");
            db.AddInParameters(1, "@Dept_Code", hfDeptCode.Value);
            db.AddInParameters(2, "@Scheme_Code", SchemeCode);
            db.AddInParameters(3, "@Fn_Year", FnYear);
            db.AddInParameters(4, "@Office_Code", OfficeCode);
            db.AddInParameters(5, "@Office_Type", OfficeType);
            db.Open();

            dsHeadwiseDtls = db.ExecuteDataSet(CommandType.StoredProcedure, "USP_ALLOTMENT_GET_HEADWISE_PROGRAMMEWISE_DATA");
            db.Close();
            if (dsHeadwiseDtls != null && dsHeadwiseDtls.Tables.Count > 0 && dsHeadwiseDtls.Tables[0].Rows.Count > 0)
            {
                gvActionPlan.DataSource = dsHeadwiseDtls;
                gvActionPlan.DataBind();
                gvActionPlan.Columns[15].HeaderText = "Allot Amount <br/> for Qtr-" + ddlQuarterly.SelectedValue + "<br/> (In lakhs)";
                gvActionPlan.DataBind();
                panelAction1.Visible = true;

            }
            else
            {
                gvActionPlan.DataSource = dsHeadwiseDtls;
                gvActionPlan.DataBind();
                panelAction1.Visible = true;

            }

        }
        catch (ApplicationException exception)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
        }
        catch (Exception ex)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
            string message = ExceptionHandler.CreateErrorMessage(ex);
            ExceptionHandler.WriteLog(message);
            ExceptionHandler.WriteException(ex.Message);
        }
        finally
        {

            dsHeadwiseDtls.Clear();
            dsHeadwiseDtls.Dispose();
        }
    }
    public string Covert_To_DB_Date_Format_MMDDYYYY(string pstrDate)
    {

        if (pstrDate == "")
            return "";
        else
        {
            string[] Temp_Date = pstrDate.Split('/');
            pstrDate = Temp_Date[1] + "-" + Temp_Date[0] + "-" + Temp_Date[2];

            //CultureInfo provider = new CultureInfo("en-gb", true);
            //return Convert.ToDateTime(DateTime.Parse(pstrDate, provider)).ToString("dd/MM/yyyy");

            return pstrDate;
        }
    }
    protected void btnFinalSave_Click(object sender, System.EventArgs e)
    {
        FinalSaveData();
        lblMsg.Text = "";
    }
    protected void FinalSaveData()
    {

        
        if (ddlScheme.SelectedIndex == 0)
        {
            ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Please Select Scheme!');", true);
            ScriptManager.GetCurrent(Page).SetFocus(ddlScheme);
        }

        else
        {
            try
            {
                DateTime dt = new DateTime();
                string date = null;
                Regex regDept = new Regex(@"^\d{2}$");
                Regex regSch = new Regex(@"^\d{3}$");
                Regex regOff = new Regex(@"^\d{6}$");
                Regex regYear = new Regex(@"^\d{4}-\d{2}$");
                string dept = hfDeptCode.Value;
                string sch = ddlScheme.SelectedValue;
                string fyear = ddlYear.SelectedValue;
                string OfficeCode = hfOfficeCode.Value;
                string OfficeType = "";
                if (rblOfficeTypeH1.SelectedValue == "H")
                {
                    OfficeType = "H";
                    OfficeCode = hfOfficeCode.Value;

                }
                if (rblOfficeTypeH1.SelectedValue == "D")
                {
                    OfficeType = "D";

                    OfficeCode = ddlDistrict.SelectedValue + "00";

                }
                if (rblOfficeTypeH1.SelectedValue == "B")
                {
                    OfficeType = "B";
                    OfficeCode = ddlBlock.SelectedValue;
                }


                if (rd_alt.SelectedValue == "A")
                {
                    date = DateTime.Now.ToString();
                    Covert_To_DB_Date_Format_MMDDYYYY(date);
                    dt = Convert.ToDateTime(date);
                }

                if (regDept.IsMatch(dept) && regSch.IsMatch(sch) && regYear.IsMatch(fyear))
                {
                    DataSet dsAAP = new DataSet();
                    // CRTEATE DATA TABLE
                    DataTable dtAAP = new DataTable();
                    DataColumn colAAP;
                    decimal id;

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.String");
                    colAAP.ColumnName = "Dept_Code";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.String");
                    colAAP.ColumnName = "Scheme_Code";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.String");
                    colAAP.ColumnName = "F_Year";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.String");
                    colAAP.ColumnName = "Office_Code";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.String");
                    colAAP.ColumnName = "Office_Type";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.Int32");
                    colAAP.ColumnName = "SubScheme_Code";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.Int32");
                    colAAP.ColumnName = "Head_Code";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.String");
                    colAAP.ColumnName = "Program_Name";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.Decimal");
                    colAAP.ColumnName = "Allotment_Amt";
                    dtAAP.Columns.Add(colAAP);

                    colAAP = new DataColumn();
                    colAAP.DataType = Type.GetType("System.String");
                    colAAP.ColumnName = "Entry_By";

                    dtAAP.Columns.Add(colAAP);

                    foreach (GridViewRow gr in gvActionPlan.Rows)
                    {
                        //TextBox txtAAP_Amt = (TextBox)gvActionPlan.Rows[i].Cells[5].FindControl("txtBtxtAAP_Amtudget_Amt");
                        //if (txtBudget_Amt.Text != "0.00000")
                        //{

                        //}
                        Label lblSubSchemeCode = (Label)gr.FindControl("lblSubSchemeCode");
                        Label lblHeadCode = (Label)gr.FindControl("lblheadcode");
                        Label lblProgramName = (Label)gr.FindControl("lblProgramName");

                        TextBox txtAAP_Amt = (TextBox)gr.FindControl("txtAAP_Amt");
                        if (txtAAP_Amt.Text.Trim() != "" && txtAAP_Amt.Text.Trim() != "0.00000")
                        {
                            if (decimal.Parse(txtAAP_Amt.Text.Trim()) > 0)
                            {

                                DataRow dr = dtAAP.NewRow();
                                dr["Dept_Code"] = hfDeptCode.Value;
                                dr["Scheme_Code"] = ddlScheme.SelectedItem.Value;
                                dr["F_Year"] = ddlYear.SelectedItem.Value;
                                dr["Office_Code"] = OfficeCode;
                                dr["Office_Type"] = OfficeType;
                                dr["SubScheme_Code"] = lblSubSchemeCode.Text;
                                dr["Head_Code"] = lblHeadCode.Text;
                                dr["Program_Name"] = lblProgramName.Text;

                                if (Decimal.TryParse(txtAAP_Amt.Text.Trim(), out id))
                                {
                                    if (id < 0)
                                        throw new ApplicationException("Allotment Amount can not be less than 0!");
                                    else
                                        dr["Allotment_Amt"] = txtAAP_Amt.Text.Trim();
                                }
                                else
                                    throw new ApplicationException("AnnualActionPlan Amount!");

                                dr["Entry_By"] = hfUserID.Value;
                                dtAAP.Rows.Add(dr);
                                dtAAP.AcceptChanges();

                            }
                        }
                    }
                    StringBuilder sbSql = new StringBuilder();
                    StringWriter swSql = new StringWriter(sbSql);
                    string XmlFormat;
                    dsAAP.Merge(dtAAP, true, MissingSchemaAction.AddWithKey);
                    dsAAP.Tables[0].TableName = "AllotmentTable";
                    foreach (DataColumn col in dsAAP.Tables[0].Columns)
                    {
                        col.ColumnMapping = MappingType.Attribute;
                    }
                    dsAAP.WriteXml(swSql, XmlWriteMode.WriteSchema);
                    XmlFormat = sbSql.ToString();
                    db.Open();
                    db.CreateInParameters(8);
                    db.AddInParameters(0, "@Dept_Code", hfDeptCode.Value);
                    db.AddInParameters(1, "@Scheme_Code", ddlScheme.SelectedItem.Value);
                    db.AddInParameters(2, "@Office_Code", OfficeCode);
                    db.AddInParameters(3, "@Fn_Year", ddlYear.SelectedItem.Value);
                    db.AddInParameters(4, "@Qtr", ddlQuarterly.SelectedValue);
                    db.AddInParameters(5, "@Qtr_Date", date == null ? System.Convert.DBNull : dt);
                    db.AddInParameters(6, "@Entry_By", hfUserID.Value);
                    db.AddInParameters(7, "@XmlString", XmlFormat);
                    db.CreateOutParameters(1);
                    db.AddOutParameters(0, "@msg", 1, 100);
                    db.ExecuteNonQuery(CommandType.StoredProcedure, "SNA_Allotment_ProgrammeWise");

                    // MAINTAIN ACTIVITY LOG ON ACCESSING PAGE
                    //
                    string msg = db.outParameters[0].Value.ToString();
                    db.Close();
                    if (msg.ToString() == "Allotment Submited Successfully")
                    {

                        int activityid;
                        ActivityLog activity = new ActivityLog();
                        activity.UserID = hfUserID.Value;
                        activity.UserIP = Convert.ToString(HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
                        activity.ActivityType = "Action";
                        activity.Activity = "Opening Balance Entry";
                        activity.PageURL = System.Web.HttpContext.Current.Request.Url.ToString();
                        activity.Remark = db.outParameters[0].Value.ToString(); ;
                        activityid = ActivityLog.InsertActivityLog(activity);
                        ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + msg + "');", true);
                        ClearFields_OB();
                        panelAction.Visible = true;
                       // pnlGrid_OB.Visible = true;

                    }
                    else
                    {
                        ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + msg + "');", true);
                        panelAction.Visible = true;
                        //pnlGrid_OB.Visible = false;
                    }
                    GetHeadwise_OB_Dtls(ddlYear.SelectedValue, hfDeptCode.Value, ddlScheme.SelectedValue);
                }
                else
                    throw new ApplicationException("Invalid Characters!");
            }
            catch (ApplicationException exception)
            {
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('" + exception.Message + "');", true);
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
                string errorString = ExceptionHandler.CreateErrorMessage(ex);
                ExceptionHandler.WriteLog(errorString);
            }
            finally
            {

            }
        }
        //}
    }

    decimal Qtr4Allotment_Amt = 0;
    decimal Qtr3Allotment_Amt = 0;
    decimal Qtr2Allotment_Amt = 0;
    decimal Qtr1Allotment_Amt = 0;
    decimal AnnualActionPlanAmt = 0;
    decimal All4QuaterAmt = 0;
    decimal Exp_Amt = 0;
    decimal Balance_Amt = 0;
    decimal Total_OB_Sanction_Amt = 0;
    protected void gvActionPlan_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Qtr4Allotment_Amt += DataBinder.Eval(e.Row.DataItem, "Qtr4Allotment_Amt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "Qtr4Allotment_Amt").ToString());
            Qtr3Allotment_Amt += DataBinder.Eval(e.Row.DataItem, "Qtr3Allotment_Amt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "Qtr3Allotment_Amt").ToString());
            Qtr2Allotment_Amt += DataBinder.Eval(e.Row.DataItem, "Qtr2Allotment_Amt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "Qtr2Allotment_Amt").ToString());
            Qtr1Allotment_Amt += DataBinder.Eval(e.Row.DataItem, "Qtr1Allotment_Amt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "Qtr1Allotment_Amt").ToString());
            AnnualActionPlanAmt += DataBinder.Eval(e.Row.DataItem, "AnnualActionPlanAmt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "AnnualActionPlanAmt").ToString());
            All4QuaterAmt += DataBinder.Eval(e.Row.DataItem, "All4QuaterAmt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "All4QuaterAmt").ToString());
            Exp_Amt += DataBinder.Eval(e.Row.DataItem, "Exp_Amt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "Exp_Amt").ToString());
            Balance_Amt += DataBinder.Eval(e.Row.DataItem, "Balance_Amt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "Balance_Amt").ToString());
            Total_OB_Sanction_Amt += DataBinder.Eval(e.Row.DataItem, "Total_OB_Sanction_Amt").ToString() == "" ? 0 : Convert.ToDecimal(DataBinder.Eval(e.Row.DataItem, "Total_OB_Sanction_Amt").ToString());

        }
        if (e.Row.RowType == DataControlRowType.Footer)
        {
            Label lblfQtr4Allotment_Amt = (Label)e.Row.FindControl("lblfQtr4Allotment_Amt");
            Label lblfQtr3Allotment_Amt = (Label)e.Row.FindControl("lblfQtr3Allotment_Amt");
            Label lblfQtr2Allotment_Amt = (Label)e.Row.FindControl("lblfQtr2Allotment_Amt");
            Label lblfQtr1Allotment_Amt = (Label)e.Row.FindControl("lblfQtr1Allotment_Amt");

            Label lblAnnualActionPlanAmt = (Label)e.Row.FindControl("lblfAnnualActionPlanAmt");
            Label lblAll4QuaterAmt = (Label)e.Row.FindControl("lblfAll4QuaterAmt");
            Label lblExp_Amt = (Label)e.Row.FindControl("lblfExp_Amt");
            Label lblBalance_Amt = (Label)e.Row.FindControl("lblfBalance_Amt");
            Label lblfTotal_OB_Sanction_Amt = (Label)e.Row.FindControl("lblfTotal_OB_Sanction_Amt");

            lblfQtr4Allotment_Amt.Text = Qtr4Allotment_Amt.ToString();
            lblfQtr3Allotment_Amt.Text = Qtr3Allotment_Amt.ToString();
            lblfQtr2Allotment_Amt.Text = Qtr2Allotment_Amt.ToString();
            lblfQtr1Allotment_Amt.Text = Qtr1Allotment_Amt.ToString();

            lblAnnualActionPlanAmt.Text = AnnualActionPlanAmt.ToString();
            lblAll4QuaterAmt.Text = All4QuaterAmt.ToString();
            lblExp_Amt.Text = Exp_Amt.ToString();
            lblBalance_Amt.Text = Balance_Amt.ToString();
            lblfTotal_OB_Sanction_Amt.Text = Total_OB_Sanction_Amt.ToString();
        }
    }

    protected void gvActionPlan_Entered(object sender, EventArgs e)
    {
        GridViewRow row = (sender as TextBox).NamingContainer as GridViewRow;
        Label lblAnnualActionPlanAmt = (Label)row.FindControl("lblAnnualActionPlanAmt");
        Label lblTotal_OB_Sanction_Amt = (Label)row.FindControl("lblTotal_OB_Sanction_Amt");
        Label lblAll4QuaterAmt = (Label)row.FindControl("lblAll4QuaterAmt");
        TextBox txtAAP_Amt = (TextBox)row.FindControl("txtAAP_Amt");


        if (txtAAP_Amt.Text != "")
        {
            decimal tobeAllot = decimal.Parse(lblAnnualActionPlanAmt.Text) - decimal.Parse(lblAll4QuaterAmt.Text);
            decimal tobeOBSan = decimal.Parse(lblTotal_OB_Sanction_Amt.Text) - decimal.Parse(lblAll4QuaterAmt.Text);
            //if (decimal.Parse(txtAAP_Amt.Text) > tobeAllot)
            //{
            //    ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Allotment must not be grater than action plan  you can only allot " + tobeAllot.ToString() + " lakhs!');", true);
            //    txtAAP_Amt.Text = "0.00000";
            //}
            //else
            if (decimal.Parse(txtAAP_Amt.Text) > tobeOBSan)
            {
                ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Allotment must not be grater than Sanction Amount and Opening Balance you can only allot " + tobeOBSan.ToString() + " lakhs!');", true);
                txtAAP_Amt.Text = "0.00000";
            }
            else
            {
                calculateTotalAAP_Amt();
                int TotalRow = gvActionPlan.Rows.Count;
                int nextIndex = row.RowIndex + 1;
                if (nextIndex < TotalRow)
                {
                    TextBox txtbx = (TextBox)gvActionPlan.Rows[nextIndex].FindControl("txtAAP_Amt");
                    txtbx.Focus();
                }
            }
        }
    }
    void calculateTotalAAP_Amt()
    {

        if (rblOfficeTypeH.SelectedValue == "H")
            OfficeType_Chk = "H";
        if (rblOfficeTypeH.SelectedValue == "D")
            OfficeType_Chk = "D";
        if (rblOfficeTypeH.SelectedValue == "B")
            OfficeType_Chk = "B";
        //decimal Budget_Amt = ConvertText_To_Decimal(txtBudget_Amt.Text.Trim());
        //decimal Total_AAP_Amt = ConvertText_To_Decimal(lbl_Total_AAPAmt.Text.Trim());

        //decimal StateTotal = ConvertText_To_Decimal(lbl_StateOffice_AAPAmt.Text.Trim());
        //decimal DistrictTotal = ConvertText_To_Decimal(lbl_DistrictOffice_AAPAmt.Text.Trim());
        //decimal BlockTotal = ConvertText_To_Decimal(lbl_BlockOffice_AAPAmt.Text.Trim());
        Label lblfblank_Amt = (Label)this.gvActionPlan.FooterRow.FindControl("lblfblank_Amt");
        decimal SanctionAmt = new decimal(0);
        int i = 0;
        TextBox textBox = new TextBox();
        foreach (GridViewRow row in this.gvActionPlan.Rows)
        {
            textBox = (TextBox)row.FindControl("txtAAP_Amt");
            if (textBox.Text != "")
            {
                SanctionAmt = SanctionAmt + Convert.ToDecimal(textBox.Text);
            }
        }
        lblfblank_Amt.Text = SanctionAmt.ToString();

    }


    #endregion


    protected void rd_alt_SelectedIndexChanged(object sender, EventArgs e)
    {

    }

    //protected void ddlDist_SelectedIndexChanged(object sender, EventArgs e)
    //{
    //    if (ddlDistrict.SelectedIndex > 0)
    //    {
    //        if (rblOfficeTypeH1.SelectedValue == "B" || rblOfficeTypeH1.SelectedValue == "P")
    //        {
    //            this.PopulateBlock(this.ddlDistrict.SelectedValue);
    //        }
    //    }
    //}



    //protected void PopulateBlock(string DistCode)
    //{
    //    DataSet dataSet = new DataSet();
    //    try
    //    {
    //        try
    //        {
    //            this.db.Open();
    //            Regex regex = new Regex("^\\d{4}$");
    //            if (!regex.IsMatch(DistCode))
    //            {
    //                throw new ApplicationException("Invalid Characters!");
    //            }
    //            IDBManager dBManager = this.db;
    //            string[] strArrays = new string[] { "SELECT Office_Code,Office_Name FROM NHM_Office WHERE Left(Office_Code,4)='", DistCode, "' AND Right(Office_Code,2)<>'00' ORDER BY Office_Name" };
    //            dataSet = dBManager.ExecuteDataSet(CommandType.Text, string.Concat(strArrays));
    //            if (rblOfficeTypeH1.SelectedValue == "P")
    //            {
    //                this.ddlBlock.DataSource = dataSet;
    //                this.ddlBlock.DataValueField = "Office_Code";
    //                this.ddlBlock.DataTextField = "Office_Name";
    //                this.ddlBlock.DataBind();
    //                this.ddlBlock.Items.Insert(0, "Select Office");
    //            }
    //            else
    //            {
    //                this.ddlOffice.DataSource = dataSet;
    //                this.ddlOffice.DataValueField = "Office_Code";
    //                this.ddlOffice.DataTextField = "Office_Name";
    //                this.ddlOffice.DataBind();
    //                this.ddlOffice.Items.Insert(0, "Select Office");
    //            }

    //        }
    //        catch (ApplicationException applicationException1)
    //        {
    //            ApplicationException applicationException = applicationException1;
    //            System.Web.UI.ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", string.Concat("alert('", applicationException.Message, "');"), true);
    //        }
    //        catch (Exception exception1)
    //        {
    //            Exception exception = exception1;
    //            System.Web.UI.ScriptManager.RegisterClientScriptBlock(this.Page, this.Page.GetType(), "asyncPostBack", "alert('Your request could not be completed due to exception. Please intimate technical team for rectification!');", true);
    //            ExceptionHandler.WriteLog(ExceptionHandler.CreateErrorMessage(exception));
    //        }
    //    }
    //    finally
    //    {
    //        this.db.Close();
    //        dataSet.Clear();
    //        dataSet.Dispose();
    //    }
    //}
    protected void ddlBlock_SelectedIndexChanged(object sender, EventArgs e)
    {

    }

    protected void ddlOffice_SelectedIndexChanged(object sender, EventArgs e)
    {

    }

    protected void ddlDistrict_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ddlDistrict.SelectedIndex > 0)
        {
            if (rblOfficeTypeH1.SelectedValue == "B" || rblOfficeTypeH1.SelectedValue == "P")
            {
                //this.PopulateBlock(this.ddlDistrict.SelectedValue);
            }

        }
    }
}