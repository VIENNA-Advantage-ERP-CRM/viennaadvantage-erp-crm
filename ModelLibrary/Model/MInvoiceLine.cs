﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MMatchPO
 * Purpose        : Used for invoice window's 2nd tab with VAB_InvoiceLine table
 * Class Used     : X_VAB_InvoiceLine
 * Chronological    Development
 * Raghunandan     08-Jun-2009
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
//using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVABInvoiceLine : X_VAB_InvoiceLine
    {
        //	Static Logger	
        private static VLogger _log = VLogger.GetVLogger(typeof(MVABInvoiceLine).FullName);
        private int _VAM_PriceList_ID = 0;
        private DateTime? _DateInvoiced = null;
        private int _VAB_BusinessPartner_ID = 0;
        private int _VAB_BPart_Location_ID = 0;
        private Boolean _IsSOTrx = true;
        private Boolean _priceSet = false;
        private MProduct _product = null;

        /**	Cached Name of the line		*/
        private String _name = null;
        /** Cached Precision			*/
        private int? _precision = null;
        /** Product Pricing				*/
        private MProductPricing _productPricing = null;
        /** Parent						*/
        private MVABInvoice _parent = null;
        private Decimal _PriceList = Env.ZERO;
        private Decimal _PriceStd = Env.ZERO;
        private Decimal _PriceLimit = Env.ZERO;
        private int VAM_PFeature_SetInstance_ID = 0;
        private int VAB_UOM_ID = 0;
        // Done by Bharat to check qty with MR
        private bool _checkMRQty = false;

        private bool resetAmtDim = false;
        private bool resetTotalAmtDim = false;
        /**
        * Get Invoice Line referencing InOut Line
        *	@param sLine shipment line
        *	@return (first) invoice line
        */
        public static MVABInvoiceLine GetOfInOutLine(MVAMInvInOutLine sLine)
        {
            if (sLine == null)
            {
                return null;
            }
            MVABInvoiceLine retValue = null;
            try
            {
                String sql = "SELECT * FROM VAB_InvoiceLine WHERE VAM_Inv_InOutLine_ID=" + sLine.GetVAM_Inv_InOutLine_ID();
                DataSet ds = new DataSet();
                try
                {
                    ds = DataBase.DB.ExecuteDataset(sql, null, sLine.Get_TrxName());
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        DataRow dr = ds.Tables[0].Rows[i];
                        retValue = new MVABInvoiceLine(sLine.GetCtx(), dr, sLine.Get_TrxName());
                        if (dr.HasErrors)
                        {
                            _log.Warning("More than one VAB_InvoiceLine of " + sLine);
                        }
                    }
                    ds = null;
                }
                catch (Exception e)
                {
                    _log.Log(Level.SEVERE, sql, e);
                }
                finally
                {
                    ds = null;
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--GetOfInOutLine");
            }
            return retValue;
        }

        /***
        * 	Invoice Line Constructor
        * 	@param ctx context
        * 	@param VAB_InvoiceLine_ID invoice line or 0
        * 	@param trxName transaction name
        */
        public MVABInvoiceLine(Ctx ctx, int VAB_InvoiceLine_ID, Trx trxName) :
            base(ctx, VAB_InvoiceLine_ID, trxName)
        {
            try
            {
                if (VAB_InvoiceLine_ID == 0)
                {
                    SetIsDescription(false);
                    SetIsPrinted(true);
                    SetLineNetAmt(Env.ZERO);
                    SetPriceEntered(Env.ZERO);
                    SetPriceActual(Env.ZERO);
                    SetPriceLimit(Env.ZERO);
                    SetPriceList(Env.ZERO);
                    SetVAM_PFeature_SetInstance_ID(0);
                    SetTaxAmt(Env.ZERO);
                    //
                    SetQtyEntered(Env.ZERO);
                    SetQtyInvoiced(Env.ZERO);
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--Invoice Line Constructor");
            }
        }

        /**
         * 	Parent Constructor
         * 	@param invoice parent
         */
        public MVABInvoiceLine(MVABInvoice invoice)
            : this(invoice.GetCtx(), 0, invoice.Get_TrxName())
        {
            try
            {

                if (invoice.Get_ID() == 0)
                    throw new ArgumentException("Header not saved");
                SetClientOrg(invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID());
                SetVAB_Invoice_ID(invoice.GetVAB_Invoice_ID());
                SetInvoice(invoice);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--Parent Constructor");
            }
        }


        /**
         *  Load Constructor
         *  @param ctx context
         *  @param rs result Set record
         *  @param trxName transaction
         */
        public MVABInvoiceLine(Ctx ctx, DataRow dr, Trx trxName) :
            base(ctx, dr, trxName)
        {

        }

        /**
         * 	Set Defaults from Order.
         * 	Called also from copy lines from invoice
         * 	Does not Set Parent !!
         * 	@param invoice invoice
         */
        public void SetInvoice(MVABInvoice invoice)
        {
            try
            {
                _parent = invoice;
                _VAM_PriceList_ID = invoice.GetVAM_PriceList_ID();
                _DateInvoiced = invoice.GetDateInvoiced();
                _VAB_BusinessPartner_ID = invoice.GetVAB_BusinessPartner_ID();
                _VAB_BPart_Location_ID = invoice.GetVAB_BPart_Location_ID();
                _IsSOTrx = invoice.IsSOTrx();
                _precision = invoice.GetPrecision();
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetInvoice");
            }
        }

        /**
         * 	Get Parent
         *	@return parent
         */
        public MVABInvoice GetParent()
        {
            try
            {
                if (_parent == null)
                {
                    _parent = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_TrxName());
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--GetParent");
            }
            return _parent;
        }

        /**
         * 	Set Client Org
         *	@param VAF_Client_ID client
         *	@param VAF_Org_ID org
         */
        public void SetClientOrg(int VAF_Client_ID, int VAF_Org_ID)
        {
            base.SetClientOrg(VAF_Client_ID, VAF_Org_ID);
        }

        /**
         * 	Set values from Order Line.
         * 	Does not Set quantity!
         *	@param oLine line
         */
        public void SetOrderLine(MVABOrderLine oLine)
        {
            try
            {
                SetVAB_OrderLine_ID(oLine.GetVAB_OrderLine_ID());
                //
                SetLine(oLine.GetLine());
                SetIsDescription(oLine.IsDescription());
                SetDescription(oLine.GetDescription());
                //
                SetVAB_Charge_ID(oLine.GetVAB_Charge_ID());
                //
                // Set Drop ship Checkbox - Added by Vivek
                SetIsDropShip(oLine.IsDropShip());
                SetVAM_Product_ID(oLine.GetVAM_Product_ID());
                SetVAM_PFeature_SetInstance_ID(oLine.GetVAM_PFeature_SetInstance_ID());
                SetVAS_Res_Assignment_ID(oLine.GetVAS_Res_Assignment_ID());
                SetVAB_UOM_ID(oLine.GetVAB_UOM_ID());
                //
                SetPriceEntered(oLine.GetPriceEntered());
                SetPriceActual(oLine.GetPriceActual());
                SetPriceLimit(oLine.GetPriceLimit());
                SetPriceList(oLine.GetPriceList());
                //
                SetVAB_TaxRate_ID(oLine.GetVAB_TaxRate_ID());
                SetLineNetAmt(oLine.GetLineNetAmt());
                //
                SetVAB_Project_ID(oLine.GetVAB_Project_ID());
                SetVAB_ProjectStage_ID(oLine.GetVAB_ProjectStage_ID());
                SetVAB_ProjectJob_ID(oLine.GetVAB_ProjectJob_ID());
                SetVAB_BillingCode_ID(oLine.GetVAB_BillingCode_ID());
                SetVAB_Promotion_ID(oLine.GetVAB_Promotion_ID());
                SetVAF_OrgTrx_ID(oLine.GetVAF_OrgTrx_ID());
                SetUser1_ID(oLine.GetUser1_ID());
                SetUser2_ID(oLine.GetUser2_ID());
                //
                SetRRAmt(oLine.GetRRAmt());
                SetRRStartDate(oLine.GetRRStartDate());
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetOrderLine");
            }
        }

        /**
         * 	Set values from Shipment Line.
         * 	Does not Set quantity!
         *	@param sLine ship line
         */
        public void SetShipLine(MVAMInvInOutLine sLine)
        {
            try
            {
                SetVAM_Inv_InOutLine_ID(sLine.GetVAM_Inv_InOutLine_ID());
                SetVAB_OrderLine_ID(sLine.GetVAB_OrderLine_ID());

                //
                SetLine(sLine.GetLine());
                SetIsDescription(sLine.IsDescription());
                SetDescription(sLine.GetDescription());
                //
                // Set Drop ship Checkbox - Added by Vivek
                SetIsDropShip(sLine.IsDropShip());
                SetVAM_Product_ID(sLine.GetVAM_Product_ID());
                SetVAB_UOM_ID(sLine.GetVAB_UOM_ID());
                SetVAM_PFeature_SetInstance_ID(sLine.GetVAM_PFeature_SetInstance_ID());
                //	SetVAS_Res_Assignment_ID(sLine.GetVAS_Res_Assignment_ID());
                SetVAB_Charge_ID(sLine.GetVAB_Charge_ID());
                int VAB_OrderLine_ID = sLine.GetVAB_OrderLine_ID();
                if (VAB_OrderLine_ID != 0)
                {
                    MVABOrderLine oLine = new MVABOrderLine(GetCtx(), VAB_OrderLine_ID, Get_TrxName());
                    MVABOrder ord = new MVABOrder(GetCtx(), oLine.GetVAB_Order_ID(), Get_TrxName());          //Added By Bharat
                    SetVAS_Res_Assignment_ID(oLine.GetVAS_Res_Assignment_ID());
                    VAM_PFeature_SetInstance_ID = sLine.GetVAM_PFeature_SetInstance_ID();               //Added By Bharat
                    VAB_UOM_ID = oLine.GetVAB_UOM_ID();
                    string docsubTypeSO = Util.GetValueOfString(DB.ExecuteScalar("SELECT DocSubTypeSO FROM VAB_DocTypes WHERE VAB_DocTypes_ID = " + ord.GetVAB_DocTypesTarget_ID()));
                    if (docsubTypeSO == "WR")
                    {
                        SetPriceEntered(oLine.GetPriceEntered());
                        SetPriceActual(oLine.GetPriceActual());
                        SetPriceLimit(oLine.GetPriceLimit());
                        SetPriceList(oLine.GetPriceList());
                    }
                    else
                    {
                        // Added By Bharat
                        // Changes Done For VAPRC Module To Set Price By Attribute Set Instance

                        //Changes done to resolve issue: For not getting price at invoice from Order line even  If Prices at Order line manually entered/changed by user. Now it will always pick price from Order line.
                        // Previously it was fetching the prices from pricelist

                        //Tuple<String, String, String> mInfo = null;
                        //if (Env.HasModulePrefix("VAPRC_", out mInfo) && ord.IsSOTrx() && !ord.IsReturnTrx())
                        //{
                        //    string qry = "SELECT max(VAM_PriceListVersion_ID) FROM VAM_PriceListVersion WHERE VAM_PriceList_ID=" + _VAM_PriceList_ID;
                        //    int VAM_PriceListVersion_ID = Util.GetValueOfInt(DB.ExecuteScalar(qry));
                        //    Tuple<String, String, String> mInfo1 = null;
                        //    if (Env.HasModulePrefix("ED011_", out mInfo1))
                        //    {
                        //        SetPriceForUOM(sLine.GetVAM_Product_ID(), VAM_PriceListVersion_ID, sLine.GetVAM_PFeature_SetInstance_ID(), VAB_UOM_ID);
                        //    }
                        //    else
                        //    {
                        //        SetPriceForAttribute(sLine.GetVAM_Product_ID(), VAM_PriceListVersion_ID, sLine.GetVAM_PFeature_SetInstance_ID());
                        //    }
                        //    SetPriceEntered(_PriceStd);
                        //    SetPriceActual(_PriceStd);
                        //    SetPriceLimit(_PriceLimit);
                        //    SetPriceList(_PriceList);
                        //}
                        //else
                        //{
                        SetPriceEntered(oLine.GetPriceEntered());
                        SetPriceActual(oLine.GetPriceActual());
                        SetPriceLimit(oLine.GetPriceLimit());
                        SetPriceList(oLine.GetPriceList());
                        //}
                    }
                    //
                    SetVAB_TaxRate_ID(oLine.GetVAB_TaxRate_ID());
                    SetLineNetAmt(oLine.GetLineNetAmt());
                    SetVAB_Project_ID(oLine.GetVAB_Project_ID());
                }
                else
                {
                    SetPrice();
                    SetTax();
                }
                //
                SetVAB_Project_ID(sLine.GetVAB_Project_ID());
                SetVAB_ProjectStage_ID(sLine.GetVAB_ProjectStage_ID());
                SetVAB_ProjectJob_ID(sLine.GetVAB_ProjectJob_ID());
                SetVAB_BillingCode_ID(sLine.GetVAB_BillingCode_ID());
                SetVAB_Promotion_ID(sLine.GetVAB_Promotion_ID());
                SetVAF_OrgTrx_ID(sLine.GetVAF_OrgTrx_ID());
                SetUser1_ID(sLine.GetUser1_ID());
                SetUser2_ID(sLine.GetUser2_ID());
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetShipLine");
            }
        }

        private void SetPriceForAttribute(int _VAM_Product_ID, int _VAM_PriceListVersion_ID, int _VAM_PFeature_SetInstance_ID)
        {
            string sql = "SELECT bomPriceStdAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceStd,"	//	1
                    + " bomPriceListAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceList,"		//	2
                    + " bomPriceLimitAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceLimit"	//	3
                    + " FROM VAM_Product p"
                    + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                    + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                    + " INNER JOIN VAM_PriceList pl ON (pv.VAM_PriceList_ID=pl.VAM_PriceList_ID) "
                    + "WHERE pv.IsActive='Y'"
                    + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                    + " AND pv.VAM_PriceListVersion_ID=" + _VAM_PriceListVersion_ID	//	#2
                    + " AND pp.VAM_PFeature_SetInstance_ID =" + _VAM_PFeature_SetInstance_ID;	                //	#3
            DataSet ds = ExecuteQuery.ExecuteDataset(sql, null);
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                DataRow dr = ds.Tables[0].Rows[i];
                //	Prices
                _PriceStd = Util.GetValueOfDecimal(dr[0]);//.getBigDecimal(1);
                if (dr[0] == null)
                    _PriceStd = Env.ZERO;
                _PriceList = Util.GetValueOfDecimal(dr[1]);//.getBigDecimal(2);
                if (dr[1] == null)
                    _PriceList = Env.ZERO;
                _PriceLimit = Util.GetValueOfDecimal(dr[2]);//.getBigDecimal(3);
                if (dr[2] == null)
                    _PriceLimit = Env.ZERO;
            }
        }

        private void SetPriceForUOM(int _VAM_Product_ID, int _VAM_PriceListVersion_ID, int _VAM_PFeature_SetInstance_ID, int UOM)
        {
            string sql = "SELECT bomPriceStdUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceStd,"	//	1
                          + " bomPriceListUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceList,"		//	2
                          + " bomPriceLimitUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceLimit,"	//	3
                          + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,"	//	4..7
                          + " pl.EnforcePriceLimit, pl.IsTaxIncluded "	// 8..9
                          + "FROM VAM_Product p"
                          + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                          + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                          + " INNER JOIN VAM_PriceList pl ON (pv.VAM_PriceList_ID=pl.VAM_PriceList_ID) "
                          + "WHERE pv.IsActive='Y'"
                          + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                          + " AND pv.VAM_PriceListVersion_ID=" + _VAM_PriceListVersion_ID	//	#2
                          + " AND pp.VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_ID  //	#3
                          + " AND pp.VAB_UOM_ID = " + VAB_UOM_ID  //    #4
                          + " AND pp.IsActive='Y'";
            DataSet ds = ExecuteQuery.ExecuteDataset(sql, null);
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                DataRow dr = ds.Tables[0].Rows[i];
                //	Prices
                _PriceStd = Util.GetValueOfDecimal(dr[0]);//.getBigDecimal(1);
                if (dr[0] == null)
                    _PriceStd = Env.ZERO;
                _PriceList = Util.GetValueOfDecimal(dr[1]);//.getBigDecimal(2);
                if (dr[1] == null)
                    _PriceList = Env.ZERO;
                _PriceLimit = Util.GetValueOfDecimal(dr[2]);//.getBigDecimal(3);
                if (dr[2] == null)
                    _PriceLimit = Env.ZERO;
            }
        }

        /**
         * 	Add to Description
         *	@param description text
         */
        public void AddDescription(String description)
        {
            try
            {
                String desc = GetDescription();
                if (desc == null)
                {
                    SetDescription(description);
                }
                else
                {
                    SetDescription(desc + " | " + description);
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--addDescription");
            }
        }

        /**
         * 	Set VAM_PFeature_SetInstance_ID
         *	@param VAM_PFeature_SetInstance_ID id
         */
        public void SetVAM_PFeature_SetInstance_ID(int VAM_PFeature_SetInstance_ID)
        {
            try
            {
                if (VAM_PFeature_SetInstance_ID == 0)		//	 0 is valid ID
                    Set_Value("VAM_PFeature_SetInstance_ID", 0);
                else
                    base.SetVAM_PFeature_SetInstance_ID(VAM_PFeature_SetInstance_ID);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetVAM_PFeature_SetInstance_ID");
            }
        }

        /***
         * 	Set Price for Product and PriceList.
         * 	Uses standard SO price list of not Set by invoice constructor
         */
        public void SetPrice()
        {
            try
            {
                if (GetVAM_Product_ID() == 0 || IsDescription())
                    return;
                if (_VAM_PriceList_ID == 0 || _VAB_BusinessPartner_ID == 0)
                    SetInvoice(GetParent());
                if (_VAM_PriceList_ID == 0 || _VAB_BusinessPartner_ID == 0)
                    throw new Exception("setPrice - PriceList unknown!");
                //throw new IllegalStateException("setPrice - PriceList unknown!");
                SetPrice(_VAM_PriceList_ID, _VAB_BusinessPartner_ID);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetPrice");
            }
        }

        /**
         * 	Set Price for Product and PriceList
         * 	@param VAM_PriceList_ID price list
         * 	@param VAB_BusinessPartner_ID business partner
         */
        public void SetPrice(int VAM_PriceList_ID, int VAB_BusinessPartner_ID)
        {
            try
            {
                if (GetVAM_Product_ID() == 0 || IsDescription())
                    return;
                //
                log.Fine("VAM_PriceList_ID=" + VAM_PriceList_ID);
                _productPricing = new MProductPricing(GetVAF_Client_ID(), GetVAF_Org_ID(),
                    GetVAM_Product_ID(), VAB_BusinessPartner_ID, GetQtyInvoiced(), _IsSOTrx);
                _productPricing.SetVAM_PriceList_ID(VAM_PriceList_ID);
                _productPricing.SetPriceDate(_DateInvoiced);
                _productPricing.SetVAM_PFeature_SetInstance_ID(VAM_PFeature_SetInstance_ID);
                //Amit 25-nov-2014
                if (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='ED011_'")) > 0)
                {
                    _productPricing.SetVAB_UOM_ID(GetVAB_UOM_ID());
                }
                ////Amit
                SetPriceActual(_productPricing.GetPriceStd());
                SetPriceList(_productPricing.GetPriceList());
                SetPriceLimit(_productPricing.GetPriceLimit());
                //
                if (Decimal.Compare(GetQtyEntered(), GetQtyInvoiced()) == 0)
                    SetPriceEntered(GetPriceActual());
                else
                    SetPriceEntered(Decimal.Multiply(GetPriceActual(), Decimal.Round(Decimal.Divide(GetQtyInvoiced(), GetQtyEntered()), 6)));

                //
                if (GetVAB_UOM_ID() == 0)
                    SetVAB_UOM_ID(_productPricing.GetVAB_UOM_ID());
                //
                _priceSet = true;
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetPrice");
            }
        }

        /**
         * 	Set Price Entered/Actual.
         * 	Use this Method if the Line UOM is the Product UOM 
         *	@param PriceActual price
         */
        public void SetPrice(Decimal priceActual)
        {
            try
            {
                SetPriceEntered(priceActual);
                SetPriceActual(priceActual);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetPrice");
            }
        }

        /**
         * 	Set Price Actual.
         * 	(actual price is not updateable)
         *	@param PriceActual actual price
         */
        public void SetPriceActual(Decimal? priceActual)
        {
            try
            {
                if (priceActual == null)
                    throw new ArgumentException("PriceActual is mandatory");
                Set_ValueNoCheck("PriceActual", priceActual);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetPriceActual");
            }
        }


        /**
         *	Set Tax - requires Warehouse
         *	@return true if found
         */
        public Boolean SetTax()
        {
            try
            {

                if (IsDescription())
                    return true;

                // Change to Set Tax ID based on the VAT Engine Module
                //if (_IsSOTrx)
                //{
                DataSet dsLoc = null;
                MVABInvoice inv = new MVABInvoice(Env.GetCtx(), Util.GetValueOfInt(Get_Value("VAB_Invoice_ID")), Get_TrxName());
                // Table ID Fixed for OrgInfo Table
                string taxrule = string.Empty;
                int _CountED002 = (Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX IN ('ED002_' , 'VATAX_' )")));

                string sql = "SELECT VATAX_TaxRule FROM VAF_OrgDetail WHERE VAF_Org_ID=" + inv.GetVAF_Org_ID() + " AND IsActive ='Y' AND VAF_Client_ID =" + GetCtx().GetVAF_Client_ID();
                if (_CountED002 > 0)
                {
                    taxrule = Util.GetValueOfString(DB.ExecuteScalar(sql, null, Get_TrxName()));
                }
                // if (taxrule == "T" && _IsSOTrx)
                if (taxrule == "T")
                {

                    //sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                    //               " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                    //int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    //if (taxType == 0)
                    //{
                    //    sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                    //    taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    //}
                    //MProduct prod = new MProduct(Env.GetCtx(), System.Convert.ToInt32(GetVAM_Product_ID()), null);
                    //sql = "SELECT VAB_TaxRate_ID FROM VATAX_TaxCatRate WHERE VAB_TaxCategory_ID = " + prod.GetVAB_TaxCategory_ID() + " AND IsActive ='Y' AND VATAX_TaxType_ID =" + taxType;
                    //int taxId = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, null));
                    //if (taxId > 0)
                    //{
                    //    SetVAB_TaxRate_ID(taxId);
                    //    return true;
                    //}
                    //return false;                        
                    sql = "SELECT Count(*) FROM VAF_Column WHERE ColumnName = 'VAB_TaxRate_ID' AND VAF_TableView_ID = (SELECT VAF_TableView_ID FROM VAF_TableView WHERE TableName = 'VAB_TaxCategory')";
                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql)) > 0)
                    {
                        int VAB_TaxRate_ID = 0, taxCategory = 0;
                        MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), inv.GetVAB_BusinessPartner_ID(), Get_TrxName());
                        if (bp.IsTaxExempt())
                        {
                            VAB_TaxRate_ID = GetExemptTax(GetCtx(), GetVAF_Org_ID());
                            SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                            return true;
                        }
                        if (GetVAM_Product_ID() > 0)
                        {
                            MProduct prod = new MProduct(Env.GetCtx(), GetVAM_Product_ID(), Get_TrxName());
                            taxCategory = Util.GetValueOfInt(prod.GetVAB_TaxCategory_ID());
                        }
                        if (GetVAB_Charge_ID() > 0)
                        {
                            MVABCharge chrg = new MVABCharge(Env.GetCtx(), GetVAB_Charge_ID(), Get_TrxName());
                            taxCategory = Util.GetValueOfInt(chrg.GetVAB_TaxCategory_ID());
                        }
                        if (taxCategory > 0)
                        {
                            MTaxCategory taxCat = new MTaxCategory(GetCtx(), taxCategory, Get_TrxName());
                            int Country_ID = 0, Region_ID = 0, orgCountry = 0, orgRegion = 0, taxRegion = 0;
                            string Postal = "", orgPostal = "";
                            sql = @"SELECT loc.VAB_Country_ID,loc.VAB_RegionState_ID,loc.Postal FROM VAB_Address loc INNER JOIN VAB_BPart_Location bpl ON loc.VAB_Address_ID = bpl.VAB_Address_ID 
                                    WHERE bpl.VAB_BPart_Location_ID =" + inv.GetVAB_BPart_Location_ID() + " AND bpl.IsActive = 'Y'";
                            dsLoc = DB.ExecuteDataset(sql, null, Get_TrxName());
                            if (dsLoc != null)
                            {
                                if (dsLoc.Tables[0].Rows.Count > 0)
                                {
                                    for (int j = 0; j < dsLoc.Tables[0].Rows.Count; j++)
                                    {
                                        Country_ID = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][0]);
                                        Region_ID = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][1]);
                                        Postal = Util.GetValueOfString(dsLoc.Tables[0].Rows[j][2]);
                                    }
                                }
                            }
                            dsLoc = null;
                            sql = @"SELECT loc.VAB_Country_ID,loc.VAB_RegionState_ID,loc.Postal FROM VAB_Address loc LEFT JOIN VAF_OrgDetail org ON loc.VAB_Address_ID = org.VAB_Address_ID WHERE org.VAF_Org_ID ="
                                    + inv.GetVAF_Org_ID() + " AND org.IsActive = 'Y'";
                            dsLoc = DB.ExecuteDataset(sql, null, Get_TrxName());
                            if (dsLoc != null)
                            {
                                if (dsLoc.Tables[0].Rows.Count > 0)
                                {
                                    for (int j = 0; j < dsLoc.Tables[0].Rows.Count; j++)
                                    {
                                        orgCountry = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][0]);
                                        orgRegion = Util.GetValueOfInt(dsLoc.Tables[0].Rows[j][1]);
                                        orgPostal = Util.GetValueOfString(dsLoc.Tables[0].Rows[j][2]);
                                    }
                                }
                            }

                            for (int i = 0; i < 3; i++)
                            {
                                string pref = taxCat.GetVATAX_Preference1();
                                if (i == 1)
                                {
                                    pref = taxCat.GetVATAX_Preference2();
                                }
                                else if (i == 2)
                                {
                                    pref = taxCat.GetVATAX_Preference3();
                                }
                                // if Tax Preference is Tax Class
                                if (pref == "T")
                                {
                                    sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                                                   " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                                    int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                                    if (taxType == 0)
                                    {
                                        sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                                        taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                                    }
                                    if (taxType > 0)
                                    {
                                        sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID  WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                                            " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxBase = 'T' AND tcr.VATAX_TaxType_ID =" + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                                        if (VAB_TaxRate_ID > 0)
                                        {
                                            SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                                            return true;
                                        }
                                    }
                                }
                                // if Tax Preference is Location
                                else if (pref == "L")
                                {
                                    VAB_TaxRate_ID = GetTaxFromLocation(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                    if (VAB_TaxRate_ID > 0)
                                    {
                                        SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                                        return true;
                                    }
                                }
                                // if Tax Preference is Tax Region
                                else if (pref == "R")
                                {
                                    if (Country_ID > 0)
                                    {
                                        dsLoc = null;
                                        sql = @"SELECT VATAX_TaxRegion_ID FROM VATAX_TaxCatRate  WHERE VAB_TaxCategory_ID = " + taxCategory +
                                            " AND VATAX_TaxBase = 'R' AND VATAX_DiffCountry = 'Y' AND IsActive = 'Y' AND VAB_Country_ID = " + Country_ID;
                                        dsLoc = DB.ExecuteDataset(sql, null, Get_TrxName());
                                        if (dsLoc != null)
                                        {
                                            if (dsLoc.Tables[0].Rows.Count > 0)
                                            {

                                            }
                                            else
                                            {
                                                VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                                if (VAB_TaxRate_ID > 0)
                                                {
                                                    SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                                                    return true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                            if (VAB_TaxRate_ID > 0)
                                            {
                                                SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                                                return true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        VAB_TaxRate_ID = GetTaxFromRegion(inv.IsSOTrx(), taxCategory, Country_ID, Region_ID, Postal);
                                        if (VAB_TaxRate_ID > 0)
                                        {
                                            SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                                            return true;
                                        }
                                    }
                                }

                                // if Tax Preference is Document Type
                                else if (pref == "D")
                                {
                                    sql = @"SELECT VATAX_TaxType_ID FROM VAB_DocTypes WHERE VAB_DocTypes_ID = " + inv.GetVAB_DocTypesTarget_ID();
                                    int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));

                                    if (taxType > 0)
                                    {
                                        sql = "SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID  WHERE tcr.VAB_TaxCategory_ID = " + taxCategory +
                                            " AND tcr.IsActive ='Y' AND tcr.VATAX_TaxBase = 'T' AND tcr.VATAX_TaxType_ID = " + taxType + " AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "')";
                                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                                        if (VAB_TaxRate_ID > 0)
                                        {
                                            SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                                            return true;
                                        }
                                    }
                                }
                            }
                            if (taxCat.GetVATAX_Preference1() == "R" || taxCat.GetVATAX_Preference2() == "R" || taxCat.GetVATAX_Preference3() == "R")
                            {
                                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxRegion tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.IsDefault = 'Y' AND tcr.IsActive = 'Y' 
                                    AND tx.SOPOType IN ('B','" + (inv.IsSOTrx() ? 'S' : 'P') + "') ORDER BY tcr.Updated";
                                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                                if (VAB_TaxRate_ID > 0)
                                {
                                    SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                                    return true;
                                }
                            }
                            sql = @"SELECT tcr.VAB_TaxRate_ID FROM VAB_TaxCategory tcr WHERE tcr.VAB_TaxCategory_ID =" + taxCategory + " AND tcr.IsActive = 'Y'";
                            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                            SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        sql = @"SELECT VATAX_TaxType_ID FROM VAB_BPart_Location WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() +
                                   " AND IsActive = 'Y'  AND VAB_BPart_Location_ID = " + inv.GetVAB_BPart_Location_ID();
                        int taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                        if (taxType == 0)
                        {
                            sql = @"SELECT VATAX_TaxType_ID FROM VAB_BusinessPartner WHERE VAB_BusinessPartner_ID =" + inv.GetVAB_BusinessPartner_ID() + " AND IsActive = 'Y'";
                            taxType = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                        }
                        MProduct prod = new MProduct(Env.GetCtx(), System.Convert.ToInt32(GetVAM_Product_ID()), Get_TrxName());
                        sql = "SELECT VAB_TaxRate_ID FROM VATAX_TaxCatRate WHERE VAB_TaxCategory_ID = " + prod.GetVAB_TaxCategory_ID() + " AND IsActive ='Y' AND VATAX_TaxType_ID =" + taxType;
                        int taxId = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                        if (taxId > 0)
                        {
                            SetVAB_TaxRate_ID(taxId);
                            return true;
                        }
                        return false;
                    }
                }
                else
                {
                    MVAFOrg org = MVAFOrg.Get(GetCtx(), GetVAF_Org_ID());
                    int VAM_Warehouse_ID = org.GetVAM_Warehouse_ID();
                    //
                    int VAB_TaxRate_ID = Tax.Get(GetCtx(), GetVAM_Product_ID(), GetVAB_Charge_ID(),
                        _DateInvoiced, _DateInvoiced,
                        GetVAF_Org_ID(), VAM_Warehouse_ID,
                        _VAB_BPart_Location_ID,		//	should be bill to
                        _VAB_BPart_Location_ID, _IsSOTrx);
                    if (VAB_TaxRate_ID == 0)
                    {
                        log.Log(Level.SEVERE, "No Tax found");
                        return false;
                    }
                    SetVAB_TaxRate_ID(VAB_TaxRate_ID);
                }
                //}
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetTax");
            }
            return true;
        }

        // Return Exempted Tax From the Organization
        private int GetExemptTax(Ctx ctx, int VAF_Org_ID)
        {
            int VAB_TaxRate_ID = 0;
            String sql = "SELECT t.VAB_TaxRate_ID "
                + "FROM VAB_TaxRate t"
                + " INNER JOIN VAF_Org o ON (t.VAF_Client_ID=o.VAF_Client_ID) "
                + "WHERE t.IsTaxExempt='Y' AND o.VAF_Org_ID= " + VAF_Org_ID
                + "ORDER BY t.Rate DESC";
            bool found = false;
            try
            {
                DataSet pstmt = ExecuteQuery.ExecuteDataset(sql, null);
                for (int i = 0; i < pstmt.Tables[0].Rows.Count; i++)
                {
                    DataRow dr = pstmt.Tables[0].Rows[i];
                    VAB_TaxRate_ID = Utility.Util.GetValueOfInt(dr[0]);
                    found = true;
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            log.Fine("TaxExempt=Y - VAB_TaxRate_ID=" + VAB_TaxRate_ID);
            if (VAB_TaxRate_ID == 0)
            {
                log.SaveError("TaxCriteriaNotFound", Msg.GetMsg(ctx, "TaxNoExemptFound")
                    + (found ? "" : " (Tax/Org=" + VAF_Org_ID + " not found)"));
            }
            return VAB_TaxRate_ID;
        }

        private int GetTaxFromLocation(bool isSoTrx, int taxCategory, int Country_ID, int Region_ID, string Postal)
        {
            string sql = "";
            int VAB_TaxRate_ID = 0;
            if (String.IsNullOrEmpty(Postal))
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                    " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID = " + Country_ID + " AND NVL(tcr.VAB_RegionState_ID,0) = " + Region_ID +
                    " AND tcr.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            else
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                    " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID = " + Country_ID + " AND NVL(tcr.VAB_RegionState_ID,0) = " + Region_ID +
                    " AND (CASE WHEN (tcr.vatax_ispostal = 'Y') THEN CASE WHEN tcr.postal <= '" + Postal + "' AND tcr.postal_to >= '" + Postal + "' THEN 1 ELSE 2" +
                    " END ELSE  CASE WHEN tcr.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
            if (VAB_TaxRate_ID > 0)
            {
                return VAB_TaxRate_ID;
            }
            else
            {
                if (String.IsNullOrEmpty(Postal))
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                        " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID = " + Country_ID + " AND tcr.VAB_RegionState_ID IS NULL AND tcr.Postal IS NULL AND tx.SOPOType IN ('B','"
                        + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                else
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                        " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID = " + Country_ID + " AND tcr.VAB_RegionState_ID IS NULL AND (CASE WHEN (tcr.vatax_ispostal = 'Y') THEN CASE WHEN tcr.postal <= '" + Postal +
                        "' AND tcr.postal_to >= '" + Postal + "' THEN 1 ELSE 2" + " END ELSE  CASE WHEN tcr.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','"
                        + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                if (VAB_TaxRate_ID > 0)
                {
                    return VAB_TaxRate_ID;
                }
                else
                {
                    if (!String.IsNullOrEmpty(Postal))
                    {
                        sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID =" + taxCategory +
                            " AND tcr.IsActive = 'Y' AND tcr.VATAX_TaxBase = 'L' AND tcr.VAB_Country_ID IS NULL " + " AND tcr.VAB_RegionState_ID IS NULL AND (CASE WHEN (tcr.vatax_ispostal = 'Y') THEN CASE WHEN tcr.postal <= '"
                            + Postal + "' AND tcr.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN tcr.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','"
                            + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                    }
                    if (VAB_TaxRate_ID > 0)
                    {
                        return VAB_TaxRate_ID;
                    }
                }
            }
            return VAB_TaxRate_ID;
        }

        private int GetTaxFromRegion(bool isSoTrx, int taxCategory, int Country_ID, int Region_ID, string Postal)
        {
            string sql = "";
            int VAB_TaxRate_ID = 0;
            if (String.IsNullOrEmpty(Postal))
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND NVL(trl.VAB_RegionState_ID,0) = " + Region_ID +
                " AND trl.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            else
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND NVL(trl.VAB_RegionState_ID,0) = " + Region_ID +
                " AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '" + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '"
                + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
            if (VAB_TaxRate_ID > 0)
            {
                return VAB_TaxRate_ID;
            }
            else
            {
                if (String.IsNullOrEmpty(Postal))
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                    + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID +
                    " AND trl.VAB_RegionState_ID IS NULL AND trl.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                else
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                    + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND trl.VAB_RegionState_ID IS NULL AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '"
                    + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','"
                    + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                if (VAB_TaxRate_ID > 0)
                {
                    return VAB_TaxRate_ID;
                }
                else
                {
                    if (!String.IsNullOrEmpty(Postal))
                    {
                        sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                        + taxCategory + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID IS NULL AND trl.VAB_RegionState_ID IS NULL AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '"
                        + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','"
                        + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));

                        if (VAB_TaxRate_ID > 0)
                        {
                            return VAB_TaxRate_ID;
                        }
                    }
                }
            }
            return VAB_TaxRate_ID;
        }

        private int GetTaxFromRegion(bool isSoTrx, int taxCategory, int Country_ID, int Region_ID, string Postal, int taxRegion, int toCountry)
        {
            string sql = "";
            int VAB_TaxRate_ID = 0;
            if (String.IsNullOrEmpty(Postal))
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID +
                " AND NVL(trl.VAB_RegionState_ID,0) = " + Region_ID + " AND trl.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            else
            {
                sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID +
                " AND NVL(trl.VAB_RegionState_ID,0) = " + Region_ID + " AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '" + Postal + "' AND trl.postal_to >= '" + Postal +
                "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
            }
            VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
            if (VAB_TaxRate_ID > 0)
            {
                return VAB_TaxRate_ID;
            }
            else
            {
                if (String.IsNullOrEmpty(Postal))
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                    + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID +
                    " AND trl.VAB_RegionState_ID IS NULL AND trl.Postal IS NULL AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                else
                {
                    sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                    + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID = " + Country_ID + " AND trl.VAB_RegionState_ID IS NULL AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '"
                    + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                }
                VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                if (VAB_TaxRate_ID > 0)
                {
                    return VAB_TaxRate_ID;
                }
                else
                {
                    if (!String.IsNullOrEmpty(Postal))
                    {
                        sql = @"SELECT tcr.VAB_TaxRate_ID FROM VATAX_TaxCatRate tcr LEFT JOIN VATAX_TaxRegionLine trl ON tcr.VATAX_TaxRegion_ID = trl.VATAX_TaxRegion_ID LEFT JOIN VAB_TaxRate tx ON tcr.VAB_TaxRate_ID = tx.VAB_TaxRate_ID WHERE tcr.VAB_TaxCategory_ID = "
                        + taxCategory + " AND tcr.VATAX_DiffCountry = 'Y' AND tcr.VAB_Country_ID = " + toCountry + " AND tcr.VATAX_TaxRegion_ID = " + taxRegion + " AND tcr.VATAX_TaxBase = 'R' AND tcr.IsActive = 'Y' AND trl.VAB_Country_ID IS NULL AND trl.VAB_RegionState_ID IS NULL AND (CASE WHEN (trl.vatax_ispostal = 'Y') THEN CASE WHEN trl.postal <= '"
                        + Postal + "' AND trl.postal_to >= '" + Postal + "' THEN 1 ELSE 2 END ELSE  CASE WHEN trl.postal = '" + Postal + "' THEN 1 ELSE 2 END END) = 1 AND tx.SOPOType IN ('B','" + (isSoTrx ? 'S' : 'P') + "') ORDER BY tx.SOPOType DESC";
                        VAB_TaxRate_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_TrxName()));
                        if (VAB_TaxRate_ID > 0)
                        {
                            return VAB_TaxRate_ID;
                        }
                    }
                }
            }
            return VAB_TaxRate_ID;
        }

        /**
         * 	Calculare Tax Amt.
         * 	Assumes Line Net is calculated
         */
        public void SetTaxAmt()
        {
            try
            {
                Decimal TaxAmt = Env.ZERO;
                if (GetVAB_TaxRate_ID() == 0)
                    return;
                //	SetLineNetAmt();
                MTax tax = MTax.Get(GetCtx(), GetVAB_TaxRate_ID());
                if (tax.IsDocumentLevel() && _IsSOTrx)		//	AR Inv Tax
                    return;
                //
                // if Surcharge Tax is selected on Tax, then calculate Tax accordingly
                if (Get_ColumnIndex("SurchargeAmt") > 0 && tax.GetSurcharge_Tax_ID() > 0)
                {
                    Decimal surchargeAmt = Env.ZERO;

                    // Calculate Surcharge Amount
                    TaxAmt = tax.CalculateSurcharge(GetLineNetAmt(), IsTaxIncluded(), GetPrecision(), out surchargeAmt);

                    if (IsTaxIncluded())
                        SetLineTotalAmt(GetLineNetAmt());
                    else
                        SetLineTotalAmt(Decimal.Add(Decimal.Add(GetLineNetAmt(), TaxAmt), surchargeAmt));
                    base.SetTaxAmt(TaxAmt);
                    SetSurchargeAmt(surchargeAmt);
                }
                else
                {
                    TaxAmt = tax.CalculateTax(GetLineNetAmt(), IsTaxIncluded(), GetPrecision());
                    if (IsTaxIncluded())
                        SetLineTotalAmt(GetLineNetAmt());
                    else
                        SetLineTotalAmt(Decimal.Add(GetLineNetAmt(), TaxAmt));
                    base.SetTaxAmt(TaxAmt);
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetTaxAmt");
            }
        }

        /**
         * 	Calculate Extended Amt.
         * 	May or may not include tax
         */
        public void SetLineNetAmt()
        {
            //try
            //{
            //	Calculations & Rounding 
            Decimal LineNetAmt = Decimal.Multiply(GetPriceActual(), GetQtyEntered());
            Decimal LNAmt = LineNetAmt;
            if (Env.IsModuleInstalled("ED007_"))
            {
                #region Set Discount Values
                MVABInvoice invoice = new MVABInvoice(GetCtx(), Util.GetValueOfInt(GetVAB_Invoice_ID()), null);
                MProduct product = new MProduct(GetCtx(), Util.GetValueOfInt(GetVAM_Product_ID()), null);
                MVABBusinessPartner bPartner = new MVABBusinessPartner(GetCtx(), invoice.GetVAB_BusinessPartner_ID(), null);
                MVAMDiscountCalculation discountSchema = new MVAMDiscountCalculation(GetCtx(), bPartner.GetVAM_DiscountCalculation_ID(), null);
                int precision = MVABCurrency.GetStdPrecision(GetCtx(), invoice.GetVAB_Currency_ID());
                String epl = GetCtx().GetContext("EnforcePriceLimit");
                bool enforce = invoice.IsSOTrx() && epl != null && epl.Equals("Y");
                decimal valueBasedDiscount = 0;
                #region set Value Based Discount Based on Discount Calculation
                if (bPartner.GetED007_DiscountCalculation() == "C3")
                {
                    SetED007_ValueBaseDiscount(0);
                }
                #endregion

                if (Util.GetValueOfInt(GetVAM_Product_ID()) > 0)
                {
                    if (bPartner.GetVAM_DiscountCalculation_ID() > 0 && ((Util.GetValueOfString(invoice.IsSOTrx()) == "True" && Util.GetValueOfString(invoice.IsReturnTrx()) == "False")
                                                               || (Util.GetValueOfString(invoice.IsSOTrx()) == "False" && Util.GetValueOfString(invoice.IsReturnTrx()) == "False")))
                    {
                        #region Combination
                        if (discountSchema.GetDiscountType() == "C" || discountSchema.GetDiscountType() == "T")
                        {
                            string sql = @"SELECT VAB_BusinessPartner_ID , VAB_BPart_Category_ID , VAM_ProductCategory_ID , VAM_Product_ID , ED007_DiscountPercentage1 , ED007_DiscountPercentage2 , ED007_DiscountPercentage3 ,
                                          ED007_DiscountPercentage4 , ED007_DiscountPercentage5 , ED007_ValueBasedDiscount FROM ED007_DiscountCombination WHERE VAM_DiscountCalculation_ID = " + bPartner.GetVAM_DiscountCalculation_ID() +
                                             " AND IsActive='Y' AND VAF_Client_ID =" + GetCtx().GetVAF_Client_ID();
                            DataSet dsDiscountCombination = new DataSet();
                            dsDiscountCombination = DB.ExecuteDataset(sql, null, null);
                            if (dsDiscountCombination != null)
                            {
                                if (dsDiscountCombination.Tables.Count > 0)
                                {
                                    if (dsDiscountCombination.Tables[0].Rows.Count > 0)
                                    {
                                        int i = 0, ProductValue = 0, BPValue = 0, BPAndProductValue = 0, BPGrpValue = 0, PCatValue = 0, PCatAndBpGrpValue = 0, noException = 0;
                                        decimal AmtProduct = 0, AmtBpartner = 0, AmtBpAndProduct = 0, AmtBpGroup = 0, AmtPCategory = 0, AmtPcatAndBpGrp = 0, AmtNoException = 0;

                                        for (i = 0; i < dsDiscountCombination.Tables[0].Rows.Count; i++)
                                        {
                                            #region Business Partner And Product
                                            if (Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAM_Product_ID"]) == product.GetVAM_Product_ID() &&
                                                Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAB_BusinessPartner_ID"]) == invoice.GetVAB_BusinessPartner_ID())
                                            {
                                                BPAndProductValue = i + 1;
                                                AmtBpAndProduct = LineNetAmt;

                                                #region set Value Based Discount Based on Discount Calculation
                                                if (bPartner.GetED007_DiscountCalculation() == "C1" || bPartner.GetED007_DiscountCalculation() == "C2")
                                                {
                                                    valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_ValueBasedDiscount"]);
                                                    SetED007_DiscountPerUnit(valueBasedDiscount);
                                                    //Amit 27-nov-2014
                                                    //mTab.SetValue("ED007_ValueBaseDiscount", Decimal.Round(Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value), precision));
                                                    SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(valueBasedDiscount, GetQtyEntered()), precision));
                                                    //Amit
                                                }
                                                #endregion

                                                #region Value Based Discount
                                                if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C2")
                                                {
                                                    //amit 27-nov-2014
                                                    //AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                    AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                                    //Amit
                                                }
                                                #endregion

                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"]) > 0)
                                                {
                                                    AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Divide(Decimal.Multiply(AmtBpAndProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"]) > 0)
                                                {
                                                    AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Divide(Decimal.Multiply(AmtBpAndProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"]) > 0)
                                                {
                                                    AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Divide(Decimal.Multiply(AmtBpAndProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"]) > 0)
                                                {
                                                    AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Divide(Decimal.Multiply(AmtBpAndProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"]) > 0)
                                                {
                                                    AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Divide(Decimal.Multiply(AmtBpAndProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"])), 100));
                                                }
                                                break;
                                            }
                                            #endregion
                                            #region  Product
                                            else if (Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAM_Product_ID"]) == product.GetVAM_Product_ID() &&
                                                     Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAB_BusinessPartner_ID"]) == 0)
                                            {
                                                ProductValue = i + 1;
                                                AmtProduct = LineNetAmt;
                                                #region set Value Based Discount Based on Discount Calculation
                                                if (bPartner.GetED007_DiscountCalculation() == "C1" || bPartner.GetED007_DiscountCalculation() == "C2")
                                                {
                                                    valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_ValueBasedDiscount"]);
                                                    SetED007_DiscountPerUnit(valueBasedDiscount);
                                                    //Amit 27-nov-2014
                                                    //mTab.SetValue("ED007_ValueBaseDiscount", Decimal.Round(Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value), precision));
                                                    SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(valueBasedDiscount, GetQtyEntered()), precision));
                                                    //Amit
                                                }
                                                #endregion

                                                #region Value Based Discount
                                                if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C2")
                                                {
                                                    //Amit 27-nov-2014
                                                    // AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                    AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                                    //Amit
                                                }
                                                #endregion

                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"]) > 0)
                                                {
                                                    AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Divide(Decimal.Multiply(AmtProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"]) > 0)
                                                {
                                                    AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Divide(Decimal.Multiply(AmtProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"]) > 0)
                                                {
                                                    AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Divide(Decimal.Multiply(AmtProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"]) > 0)
                                                {
                                                    AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Divide(Decimal.Multiply(AmtProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"]) > 0)
                                                {
                                                    AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Divide(Decimal.Multiply(AmtProduct, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"])), 100));
                                                }
                                            }
                                            #endregion
                                            #region Business Partner
                                            else if (Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAM_Product_ID"]) == 0 &&
                                                     Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAB_BusinessPartner_ID"]) == invoice.GetVAB_BusinessPartner_ID())
                                            {
                                                BPValue = i + 1;
                                                AmtBpartner = LineNetAmt;

                                                #region set Value Based Discount Based on Discount Calculation
                                                if (bPartner.GetED007_DiscountCalculation() == "C1" || bPartner.GetED007_DiscountCalculation() == "C2")
                                                {
                                                    valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_ValueBasedDiscount"]);
                                                    SetED007_DiscountPerUnit(valueBasedDiscount);
                                                    //Amit 27-nov-2014
                                                    //mTab.SetValue("ED007_ValueBaseDiscount", Decimal.Round(Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value), precision));
                                                    SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(valueBasedDiscount, GetQtyEntered()), precision));
                                                    //Amit
                                                }
                                                #endregion

                                                #region Value Based Discount
                                                if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C2")
                                                {
                                                    //Amit 27-nov-2014
                                                    //AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                    AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                                    //amit
                                                }
                                                #endregion

                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"]) > 0)
                                                {
                                                    AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Divide(Decimal.Multiply(AmtBpartner, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"]) > 0)
                                                {
                                                    AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Divide(Decimal.Multiply(AmtBpartner, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"]) > 0)
                                                {
                                                    AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Divide(Decimal.Multiply(AmtBpartner, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"]) > 0)
                                                {
                                                    AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Divide(Decimal.Multiply(AmtBpartner, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"]) > 0)
                                                {
                                                    AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Divide(Decimal.Multiply(AmtBpartner, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"])), 100));
                                                }
                                            }
                                            #endregion
                                            #region Business Partner Group And Product Category
                                            else if (Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAM_ProductCategory_ID"]) == product.GetVAM_ProductCategory_ID() &&
                                                    Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAB_BPart_Category_ID"]) == bPartner.GetVAB_BPart_Category_ID())
                                            {
                                                PCatAndBpGrpValue = i + 1;
                                                AmtPcatAndBpGrp = LineNetAmt;

                                                #region set Value Based Discount Based on Discount Calculation
                                                if (bPartner.GetED007_DiscountCalculation() == "C1" || bPartner.GetED007_DiscountCalculation() == "C2")
                                                {
                                                    valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_ValueBasedDiscount"]);
                                                    SetED007_DiscountPerUnit(valueBasedDiscount);
                                                    //Amit 27-nov-2014
                                                    //mTab.SetValue("ED007_ValueBaseDiscount", Decimal.Round(Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value), precision));
                                                    SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(valueBasedDiscount, GetQtyEntered()), precision));
                                                    //Amit
                                                }
                                                #endregion

                                                #region Value Based Discount
                                                if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C2")
                                                {
                                                    //Amit 27-nov-2014
                                                    //AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                    AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                                    //amit
                                                }
                                                #endregion

                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"]) > 0)
                                                {
                                                    AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Divide(Decimal.Multiply(AmtPcatAndBpGrp, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"]) > 0)
                                                {
                                                    AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Divide(Decimal.Multiply(AmtPcatAndBpGrp, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"]) > 0)
                                                {
                                                    AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Divide(Decimal.Multiply(AmtPcatAndBpGrp, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"]) > 0)
                                                {
                                                    AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Divide(Decimal.Multiply(AmtPcatAndBpGrp, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"]) > 0)
                                                {
                                                    AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Divide(Decimal.Multiply(AmtPcatAndBpGrp, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"])), 100));
                                                }
                                            }
                                            #endregion
                                            #region  Product Category
                                            else if (Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAM_ProductCategory_ID"]) == product.GetVAM_ProductCategory_ID() &&
                                                    Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAB_BPart_Category_ID"]) == 0)
                                            {
                                                PCatValue = i + 1;
                                                AmtPCategory = LineNetAmt;

                                                #region set Value Based Discount Based on Discount Calculation
                                                if (bPartner.GetED007_DiscountCalculation() == "C1" || bPartner.GetED007_DiscountCalculation() == "C2")
                                                {
                                                    valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_ValueBasedDiscount"]);
                                                    SetED007_DiscountPerUnit(valueBasedDiscount);
                                                    //Amit 27-nov-2014
                                                    //mTab.SetValue("ED007_ValueBaseDiscount", Decimal.Round(Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value), precision));
                                                    SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(valueBasedDiscount, GetQtyEntered()), precision));
                                                    //Amit
                                                }
                                                #endregion

                                                #region Value Based Discount
                                                if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C2")
                                                {
                                                    //Amit 27-nov-2014
                                                    //AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                    AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                                }
                                                #endregion

                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"]) > 0)
                                                {
                                                    AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Divide(Decimal.Multiply(AmtPCategory, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"]) > 0)
                                                {
                                                    AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Divide(Decimal.Multiply(AmtPCategory, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"]) > 0)
                                                {
                                                    AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Divide(Decimal.Multiply(AmtPCategory, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"]) > 0)
                                                {
                                                    AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Divide(Decimal.Multiply(AmtPCategory, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"]) > 0)
                                                {
                                                    AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Divide(Decimal.Multiply(AmtPCategory, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"])), 100));
                                                }
                                            }
                                            #endregion
                                            #region Business Partner Group
                                            else if (Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAM_ProductCategory_ID"]) == 0 &&
                                                   Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAB_BPart_Category_ID"]) == bPartner.GetVAB_BPart_Category_ID())
                                            {
                                                BPGrpValue = i + 1;
                                                AmtBpGroup = LineNetAmt;

                                                #region set Value Based Discount Based on Discount Calculation
                                                if (bPartner.GetED007_DiscountCalculation() == "C1" || bPartner.GetED007_DiscountCalculation() == "C2")
                                                {
                                                    valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_ValueBasedDiscount"]);
                                                    SetED007_DiscountPerUnit(valueBasedDiscount);
                                                    //Amit 27-nov-2014
                                                    //mTab.SetValue("ED007_ValueBaseDiscount", Decimal.Round(Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value), precision));
                                                    SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(valueBasedDiscount, GetQtyEntered()), precision));
                                                    //Amit
                                                }
                                                #endregion

                                                #region Value Based Discount
                                                if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C2")
                                                {
                                                    //Amit 27-nov-2014
                                                    //AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                    AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                                    //Amit
                                                }
                                                #endregion

                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"]) > 0)
                                                {
                                                    AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Divide(Decimal.Multiply(AmtBpGroup, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"]) > 0)
                                                {
                                                    AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Divide(Decimal.Multiply(AmtBpGroup, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"]) > 0)
                                                {
                                                    AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Divide(Decimal.Multiply(AmtBpGroup, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"]) > 0)
                                                {
                                                    AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Divide(Decimal.Multiply(AmtBpGroup, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"]) > 0)
                                                {
                                                    AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Divide(Decimal.Multiply(AmtBpGroup, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"])), 100));
                                                }
                                            }
                                            #endregion
                                            #region when no Exception
                                            if (Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAM_Product_ID"]) == 0 &&
                                                    Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAB_BusinessPartner_ID"]) == 0 &&
                                                    Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAM_ProductCategory_ID"]) == 0 &&
                                                    Util.GetValueOfInt(dsDiscountCombination.Tables[0].Rows[i]["VAB_BPart_Category_ID"]) == 0)
                                            {
                                                noException = i + 1;
                                                AmtNoException = LineNetAmt;

                                                #region set Value Based Discount Based on Discount Calculation
                                                if (bPartner.GetED007_DiscountCalculation() == "C1" || bPartner.GetED007_DiscountCalculation() == "C2")
                                                {
                                                    valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_ValueBasedDiscount"]);
                                                    SetED007_DiscountPerUnit(valueBasedDiscount);
                                                    //Amit 27-nov-2014
                                                    //mTab.SetValue("ED007_ValueBaseDiscount", Decimal.Round(Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value), precision));
                                                    SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(valueBasedDiscount, GetQtyEntered()), precision));
                                                    //Amit
                                                }
                                                #endregion

                                                #region Value Based Discount
                                                if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C2")
                                                {
                                                    //Amit 27-nov-2014
                                                    //AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                    AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                                    //Amit
                                                }
                                                #endregion

                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"]) > 0)
                                                {
                                                    AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Divide(Decimal.Multiply(AmtNoException, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage1"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"]) > 0)
                                                {
                                                    AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Divide(Decimal.Multiply(AmtNoException, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage2"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"]) > 0)
                                                {
                                                    AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Divide(Decimal.Multiply(AmtNoException, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage3"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"]) > 0)
                                                {
                                                    AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Divide(Decimal.Multiply(AmtNoException, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage4"])), 100));
                                                }
                                                if (Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"]) > 0)
                                                {
                                                    AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Divide(Decimal.Multiply(AmtNoException, Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[i]["ED007_DiscountPercentage5"])), 100));
                                                }
                                            }
                                            #endregion
                                        }


                                        #region Discount Percent
                                        SetED007_DiscountPercent(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()));
                                        #endregion

                                        #region Set value when record match for Business Partner And Product
                                        if (BPAndProductValue > 0)
                                        {
                                            SetED007_DiscountPercentage1(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPAndProductValue - 1]["ED007_DiscountPercentage1"]));
                                            SetED007_DiscountPercentage2(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPAndProductValue - 1]["ED007_DiscountPercentage2"]));
                                            SetED007_DiscountPercentage3(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPAndProductValue - 1]["ED007_DiscountPercentage3"]));
                                            SetED007_DiscountPercentage4(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPAndProductValue - 1]["ED007_DiscountPercentage4"]));
                                            SetED007_DiscountPercentage5(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPAndProductValue - 1]["ED007_DiscountPercentage5"]));
                                            AmtBpAndProduct = Decimal.Round(AmtBpAndProduct, precision);
                                            valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPAndProductValue - 1]["ED007_ValueBasedDiscount"]);
                                            SetED007_DiscountPerUnit(valueBasedDiscount);
                                            #region Break
                                            if (discountSchema.GetDiscountType() == "T")
                                            {
                                                AmtBpAndProduct = BreakCalculation(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                                                         AmtBpAndProduct, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                                            }
                                            #endregion
                                            #region Value Based Discount
                                            if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C1")
                                            {
                                                valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPAndProductValue - 1]["ED007_ValueBasedDiscount"]);
                                                SetED007_DiscountPerUnit(valueBasedDiscount);
                                                //AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                AmtBpAndProduct = Decimal.Subtract(AmtBpAndProduct, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                            }
                                            #endregion
                                            SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), AmtBpAndProduct), 100), precision));
                                            //SetLineNetAmt(Decimal.Subtract(AmtBpAndProduct, Util.GetValueOfDecimal(GetED007_DiscountAmount())));
                                            LNAmt = Decimal.Subtract(AmtBpAndProduct, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                                            if (Env.Scale(LNAmt) > GetPrecision())
                                            {
                                                LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                                            }
                                            SetLineNetAmt(LNAmt);

                                            //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                                            //      && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)

                                            if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                                 && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                                            {
                                                SetPriceActual(GetPriceLimit());
                                                SetPriceEntered(GetPriceLimit());

                                                if (GetPriceList() != 0)
                                                {
                                                    Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                                    if (Env.Scale(Discount) > 2)
                                                    {
                                                        Discount = Decimal.Round(Discount, 2);
                                                    }
                                                    SetED004_DiscntPrcnt(Discount);
                                                }
                                                //Amit 27-nov-2014
                                                //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                                                LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                                                //Amit
                                                if (Env.Scale(LineNetAmt) > precision)
                                                    LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                                                SetED007_DscuntlineAmt(LineNetAmt);
                                                SetLineNetAmt(LineNetAmt);
                                                SetED007_DiscountAmount(0);
                                                SetED007_DiscountPercentage1(0);
                                                SetED007_DiscountPercentage2(0);
                                                SetED007_DiscountPercentage3(0);
                                                SetED007_DiscountPercentage4(0);
                                                SetED007_DiscountPercentage5(0);
                                                SetED007_DiscountPercent(0);
                                            }
                                            else
                                            {
                                                // mTab.SetValue("LineNetAmt", AmtBpAndProduct);
                                                SetED007_DscuntlineAmt(AmtBpAndProduct);
                                                if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                                                {
                                                    //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), AmtBpAndProduct), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                                    SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), AmtBpAndProduct), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                                                }
                                            }
                                        }
                                        #endregion
                                        #region  Set value when record match for  Product
                                        else if (ProductValue > 0)
                                        {
                                            SetED007_DiscountPercentage1(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[ProductValue - 1]["ED007_DiscountPercentage1"]));
                                            SetED007_DiscountPercentage2(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[ProductValue - 1]["ED007_DiscountPercentage2"]));
                                            SetED007_DiscountPercentage3(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[ProductValue - 1]["ED007_DiscountPercentage3"]));
                                            SetED007_DiscountPercentage4(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[ProductValue - 1]["ED007_DiscountPercentage4"]));
                                            SetED007_DiscountPercentage5(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[ProductValue - 1]["ED007_DiscountPercentage5"]));
                                            AmtProduct = Decimal.Round(AmtProduct, precision);
                                            valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[ProductValue - 1]["ED007_ValueBasedDiscount"]);
                                            SetED007_DiscountPerUnit(valueBasedDiscount);
                                            #region Break
                                            if (discountSchema.GetDiscountType() == "T")
                                            {
                                                AmtProduct = BreakCalculation(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                                                          AmtProduct, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                                            }
                                            #endregion
                                            #region Value Based Discount
                                            if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C1")
                                            {
                                                valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[ProductValue - 1]["ED007_ValueBasedDiscount"]);
                                                SetED007_DiscountPerUnit(valueBasedDiscount);
                                                //AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                AmtProduct = Decimal.Subtract(AmtProduct, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                            }
                                            #endregion
                                            SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), AmtProduct), 100), precision));
                                            LNAmt = Decimal.Subtract(AmtProduct, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                                            if (Env.Scale(LNAmt) > GetPrecision())
                                            {
                                                LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                                            }
                                            SetLineNetAmt(LNAmt);

                                            //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                                            //      && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                                            if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                                 && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                                            {
                                                SetPriceActual(GetPriceLimit());
                                                SetPriceEntered(GetPriceLimit());

                                                if (GetPriceList() != 0)
                                                {
                                                    Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                                    if (Env.Scale(Discount) > 2)
                                                    {
                                                        Discount = Decimal.Round(Discount, 2);
                                                    }
                                                    SetED004_DiscntPrcnt(Discount);
                                                }
                                                //Amit 27-nov-2014
                                                //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                                                LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                                                //Amit
                                                if (Env.Scale(LineNetAmt) > precision)
                                                    LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                                                SetLineNetAmt(LineNetAmt);
                                                SetED007_DscuntlineAmt(LineNetAmt);
                                                SetED007_DiscountAmount(0);
                                                SetED007_DiscountPercentage1(0);
                                                SetED007_DiscountPercentage2(0);
                                                SetED007_DiscountPercentage3(0);
                                                SetED007_DiscountPercentage4(0);
                                                SetED007_DiscountPercentage5(0);
                                                SetED007_DiscountPercent(0);
                                            }
                                            else
                                            {
                                                // mTab.SetValue("LineNetAmt", AmtProduct);
                                                SetED007_DscuntlineAmt(AmtProduct);
                                                if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                                                {
                                                    //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), AmtProduct), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                                    SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), AmtProduct), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                                                }
                                            }
                                        }
                                        #endregion
                                        #region Set value when record match for Business Partner
                                        else if (BPValue > 0)
                                        {
                                            SetED007_DiscountPercentage1(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPValue - 1]["ED007_DiscountPercentage1"]));
                                            SetED007_DiscountPercentage2(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPValue - 1]["ED007_DiscountPercentage2"]));
                                            SetED007_DiscountPercentage3(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPValue - 1]["ED007_DiscountPercentage3"]));
                                            SetED007_DiscountPercentage4(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPValue - 1]["ED007_DiscountPercentage4"]));
                                            SetED007_DiscountPercentage5(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPValue - 1]["ED007_DiscountPercentage5"]));
                                            AmtBpartner = Decimal.Round(AmtBpartner, precision);
                                            valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPValue - 1]["ED007_ValueBasedDiscount"]);
                                            SetED007_DiscountPerUnit(valueBasedDiscount);
                                            #region Break
                                            if (discountSchema.GetDiscountType() == "T")
                                            {
                                                AmtBpartner = BreakCalculation(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                                                         AmtBpartner, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                                            }
                                            #endregion
                                            #region Value Based Discount
                                            if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C1")
                                            {
                                                valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPValue - 1]["ED007_ValueBasedDiscount"]);
                                                SetED007_DiscountPerUnit(valueBasedDiscount);
                                                //AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                AmtBpartner = Decimal.Subtract(AmtBpartner, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                            }
                                            #endregion
                                            SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), AmtBpartner), 100), precision));
                                            // Change LineNetAmt	
                                            LNAmt = Decimal.Subtract(AmtBpartner, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                                            if (Env.Scale(LNAmt) > GetPrecision())
                                            {
                                                LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                                            }
                                            SetLineNetAmt(LNAmt);
                                            //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                                            //      && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                                            if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                                 && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                                            {
                                                SetPriceActual(GetPriceLimit());
                                                SetPriceEntered(GetPriceLimit());
                                                if (GetPriceList() != 0)
                                                {
                                                    Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                                    if (Env.Scale(Discount) > 2)
                                                    {
                                                        Discount = Decimal.Round(Discount, 2);
                                                    }
                                                    SetED004_DiscntPrcnt(Discount);
                                                }
                                                //Amit 27-nov-2014
                                                //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                                                LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                                                //Amit
                                                if (Env.Scale(LineNetAmt) > precision)
                                                    LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                                                SetLineNetAmt(LineNetAmt);
                                                SetED007_DscuntlineAmt(LineNetAmt);
                                                SetED007_DiscountAmount(0);
                                                SetED007_DiscountPercentage1(0);
                                                SetED007_DiscountPercentage2(0);
                                                SetED007_DiscountPercentage3(0);
                                                SetED007_DiscountPercentage4(0);
                                                SetED007_DiscountPercentage5(0);
                                                SetED007_DiscountPercent(0);
                                            }
                                            else
                                            {
                                                // mTab.SetValue("LineNetAmt", AmtBpartner);
                                                SetED007_DscuntlineAmt(AmtBpartner);
                                                if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                                                {
                                                    //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), AmtBpartner), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                                    SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), AmtBpartner), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                                                }
                                            }
                                        }
                                        #endregion
                                        #region Set value when record match for Business Partner Group And Product Category
                                        else if (PCatAndBpGrpValue > 0)
                                        {
                                            SetED007_DiscountPercentage1(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatAndBpGrpValue - 1]["ED007_DiscountPercentage1"]));
                                            SetED007_DiscountPercentage2(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatAndBpGrpValue - 1]["ED007_DiscountPercentage2"]));
                                            SetED007_DiscountPercentage3(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatAndBpGrpValue - 1]["ED007_DiscountPercentage3"]));
                                            SetED007_DiscountPercentage4(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatAndBpGrpValue - 1]["ED007_DiscountPercentage4"]));
                                            SetED007_DiscountPercentage5(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatAndBpGrpValue - 1]["ED007_DiscountPercentage5"]));
                                            AmtPcatAndBpGrp = Decimal.Round(AmtPcatAndBpGrp, precision);
                                            valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatAndBpGrpValue - 1]["ED007_ValueBasedDiscount"]);
                                            SetED007_DiscountPerUnit(valueBasedDiscount);
                                            #region Break
                                            if (discountSchema.GetDiscountType() == "T")
                                            {
                                                AmtPcatAndBpGrp = BreakCalculation(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                                                         AmtPcatAndBpGrp, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                                            }
                                            #endregion
                                            #region Value Based Discount
                                            if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C1")
                                            {
                                                valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatAndBpGrpValue - 1]["ED007_ValueBasedDiscount"]);
                                                SetED007_DiscountPerUnit(valueBasedDiscount);
                                                //AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                AmtPcatAndBpGrp = Decimal.Subtract(AmtPcatAndBpGrp, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                            }
                                            #endregion
                                            SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), AmtPcatAndBpGrp), 100), precision));
                                            LNAmt = Decimal.Subtract(AmtPcatAndBpGrp, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                                            if (Env.Scale(LNAmt) > GetPrecision())
                                            {
                                                LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                                            }
                                            SetLineNetAmt(LNAmt);
                                            //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                                            //     && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                                            if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                                && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                                            {
                                                SetPriceActual(GetPriceLimit());
                                                SetPriceEntered(GetPriceLimit());

                                                if (GetPriceList() != 0)
                                                {
                                                    Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                                    if (Env.Scale(Discount) > 2)
                                                    {
                                                        Discount = Decimal.Round(Discount, 2);
                                                    }
                                                    SetED004_DiscntPrcnt(Discount);
                                                }
                                                //Amit 27-nov-2014
                                                //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                                                LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                                                //Amit
                                                if (Env.Scale(LineNetAmt) > precision)
                                                    LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                                                // log.Info("LineNetAmt=" + LineNetAmt);
                                                SetLineNetAmt(LineNetAmt);
                                                SetED007_DscuntlineAmt(LineNetAmt);
                                                SetED007_DiscountAmount(0);
                                                SetED007_DiscountPercentage1(0);
                                                SetED007_DiscountPercentage2(0);
                                                SetED007_DiscountPercentage3(0);
                                                SetED007_DiscountPercentage4(0);
                                                SetED007_DiscountPercentage5(0);
                                                SetED007_DiscountPercent(0);
                                            }
                                            else
                                            {
                                                // mTab.SetValue("LineNetAmt", AmtPcatAndBpGrp);
                                                SetED007_DscuntlineAmt(AmtPcatAndBpGrp);
                                                if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                                                {
                                                    //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), AmtPcatAndBpGrp), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                                    SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), AmtPcatAndBpGrp), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                                                }
                                            }
                                        }
                                        #endregion
                                        #region Set value when record match for  Product Category
                                        else if (PCatValue > 0)
                                        {
                                            SetED007_DiscountPercentage1(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatValue - 1]["ED007_DiscountPercentage1"]));
                                            SetED007_DiscountPercentage2(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatValue - 1]["ED007_DiscountPercentage2"]));
                                            SetED007_DiscountPercentage3(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatValue - 1]["ED007_DiscountPercentage3"]));
                                            SetED007_DiscountPercentage4(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatValue - 1]["ED007_DiscountPercentage4"]));
                                            SetED007_DiscountPercentage5(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatValue - 1]["ED007_DiscountPercentage5"]));
                                            AmtPCategory = Decimal.Round(AmtPCategory, precision);
                                            valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatValue - 1]["ED007_ValueBasedDiscount"]);
                                            SetED007_DiscountPerUnit(valueBasedDiscount);
                                            #region Break
                                            if (discountSchema.GetDiscountType() == "T")
                                            {
                                                AmtPCategory = BreakCalculation(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                                                         AmtPCategory, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                                            }
                                            #endregion
                                            #region Value Based Discount
                                            if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C1")
                                            {
                                                valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[PCatValue - 1]["ED007_ValueBasedDiscount"]);
                                                SetED007_DiscountPerUnit(valueBasedDiscount);
                                                //AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                AmtPCategory = Decimal.Subtract(AmtPCategory, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                            }
                                            #endregion
                                            SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), AmtPCategory), 100), precision));
                                            LNAmt = Decimal.Subtract(AmtPCategory, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                                            if (Env.Scale(LNAmt) > GetPrecision())
                                            {
                                                LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                                            }
                                            SetLineNetAmt(LNAmt);
                                            //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                                            //    && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                                            if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                             && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                                            {
                                                SetPriceActual(GetPriceLimit());
                                                SetPriceEntered(GetPriceLimit());

                                                if (GetPriceList() != 0)
                                                {
                                                    Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                                    if (Env.Scale(Discount) > 2)
                                                    {
                                                        Discount = Decimal.Round(Discount, 2);
                                                    }
                                                    SetED004_DiscntPrcnt(Discount);
                                                }
                                                //Amit 27-nov-2014
                                                //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                                                LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                                                //Amit
                                                if (Env.Scale(LineNetAmt) > precision)
                                                    LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                                                //  log.Info("LineNetAmt=" + LineNetAmt);
                                                SetLineNetAmt(LineNetAmt);
                                                SetED007_DscuntlineAmt(LineNetAmt);
                                                SetED007_DiscountAmount(0);
                                                SetED007_DiscountPercentage1(0);
                                                SetED007_DiscountPercentage2(0);
                                                SetED007_DiscountPercentage3(0);
                                                SetED007_DiscountPercentage4(0);
                                                SetED007_DiscountPercentage5(0);
                                                SetED007_DiscountPercent(0);
                                            }
                                            else
                                            {
                                                // mTab.SetValue("LineNetAmt", AmtPCategory);
                                                SetED007_DscuntlineAmt(AmtPCategory);
                                                if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                                                {
                                                    //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), AmtPCategory), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                                    SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), AmtPCategory), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                                                }
                                            }
                                        }
                                        #endregion
                                        #region Set value when record match for Business Partner Group
                                        else if (BPGrpValue > 0)
                                        {
                                            SetED007_DiscountPercentage1(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPGrpValue - 1]["ED007_DiscountPercentage1"]));
                                            SetED007_DiscountPercentage2(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPGrpValue - 1]["ED007_DiscountPercentage2"]));
                                            SetED007_DiscountPercentage3(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPGrpValue - 1]["ED007_DiscountPercentage3"]));
                                            SetED007_DiscountPercentage4(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPGrpValue - 1]["ED007_DiscountPercentage4"]));
                                            SetED007_DiscountPercentage5(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPGrpValue - 1]["ED007_DiscountPercentage5"]));
                                            AmtBpGroup = Decimal.Round(AmtBpGroup, precision);
                                            valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPGrpValue - 1]["ED007_ValueBasedDiscount"]);
                                            SetED007_DiscountPerUnit(valueBasedDiscount);
                                            #region Break
                                            if (discountSchema.GetDiscountType() == "T")
                                            {
                                                AmtBpGroup = BreakCalculation(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                                                                       AmtBpGroup, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                                            }
                                            #endregion
                                            #region Value Based Discount
                                            if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C1")
                                            {
                                                valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[BPGrpValue - 1]["ED007_ValueBasedDiscount"]);
                                                SetED007_DiscountPerUnit(valueBasedDiscount);
                                                //AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                AmtBpGroup = Decimal.Subtract(AmtBpGroup, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                            }
                                            #endregion
                                            SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), AmtBpGroup), 100), precision));
                                            LNAmt = Decimal.Subtract(AmtBpGroup, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                                            if (Env.Scale(LNAmt) > GetPrecision())
                                            {
                                                LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                                            }

                                            SetLineNetAmt(LNAmt);
                                            //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                                            // && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                                            if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                             && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                                            {
                                                SetPriceActual(GetPriceLimit());
                                                SetPriceEntered(GetPriceLimit());
                                                if (GetPriceList() != 0)
                                                {
                                                    Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                                    if (Env.Scale(Discount) > 2)
                                                    {
                                                        Discount = Decimal.Round(Discount, 2);
                                                    }
                                                    SetED004_DiscntPrcnt(Discount);
                                                }
                                                //Amit 27-nov-2014
                                                //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                                                LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                                                //Amit
                                                if (Env.Scale(LineNetAmt) > precision)
                                                    LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                                                //log.Info("LineNetAmt=" + LineNetAmt);
                                                SetLineNetAmt(LineNetAmt);
                                                SetED007_DscuntlineAmt(LineNetAmt);
                                                SetED007_DiscountAmount(0);
                                                SetED007_DiscountPercentage1(0);
                                                SetED007_DiscountPercentage2(0);
                                                SetED007_DiscountPercentage3(0);
                                                SetED007_DiscountPercentage4(0);
                                                SetED007_DiscountPercentage5(0);
                                                SetED007_DiscountPercent(0);
                                            }
                                            else
                                            {
                                                // mTab.SetValue("LineNetAmt", AmtBpGroup);
                                                SetED007_DscuntlineAmt(AmtBpGroup);
                                                if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                                                {
                                                    //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), AmtBpGroup), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                                    SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), AmtBpGroup), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                                                }
                                            }
                                        }
                                        #endregion
                                        #region Set Value when No Exception
                                        else if (noException > 0)
                                        {
                                            SetED007_DiscountPercentage1(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[noException - 1]["ED007_DiscountPercentage1"]));
                                            SetED007_DiscountPercentage2(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[noException - 1]["ED007_DiscountPercentage2"]));
                                            SetED007_DiscountPercentage3(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[noException - 1]["ED007_DiscountPercentage3"]));
                                            SetED007_DiscountPercentage4(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[noException - 1]["ED007_DiscountPercentage4"]));
                                            SetED007_DiscountPercentage5(Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[noException - 1]["ED007_DiscountPercentage5"]));
                                            AmtNoException = Decimal.Round(AmtNoException, precision);
                                            valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[noException - 1]["ED007_ValueBasedDiscount"]);
                                            SetED007_DiscountPerUnit(valueBasedDiscount);
                                            #region Break
                                            if (discountSchema.GetDiscountType() == "T")
                                            {
                                                AmtNoException = BreakCalculation(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                                                          AmtNoException, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                                            }
                                            #endregion
                                            #region Value Based Discount
                                            if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C1")
                                            {
                                                valueBasedDiscount = Util.GetValueOfDecimal(dsDiscountCombination.Tables[0].Rows[noException - 1]["ED007_ValueBasedDiscount"]);
                                                SetED007_DiscountPerUnit(valueBasedDiscount);
                                                //AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Multiply(valueBasedDiscount, QtyOrdered.Value));
                                                AmtNoException = Decimal.Subtract(AmtNoException, Decimal.Multiply(valueBasedDiscount, GetQtyEntered()));
                                            }
                                            #endregion
                                            SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), AmtNoException), 100), precision));
                                            // Change LineNetAmt	
                                            LNAmt = Decimal.Subtract(AmtNoException, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                                            if (Env.Scale(LNAmt) > GetPrecision())
                                            {
                                                LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                                            }

                                            SetLineNetAmt(LNAmt);
                                            //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                                            // && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                                            if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                          && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                                            {
                                                SetPriceActual(GetPriceLimit());
                                                SetPriceEntered(GetPriceLimit());

                                                if (GetPriceList() != 0)
                                                {
                                                    Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                                    if (Env.Scale(Discount) > 2)
                                                    {
                                                        Discount = Decimal.Round(Discount, 2);
                                                    }
                                                    SetED004_DiscntPrcnt(Discount);
                                                }
                                                //Amit 27-nov-2014
                                                //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                                                LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                                                //Amit
                                                if (Env.Scale(LineNetAmt) > precision)
                                                    LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                                                //log.Info("LineNetAmt=" + LineNetAmt);
                                                SetLineNetAmt(LineNetAmt);
                                                SetED007_DscuntlineAmt(LineNetAmt);
                                                SetED007_DiscountAmount(0);
                                                SetED007_DiscountPercentage1(0);
                                                SetED007_DiscountPercentage2(0);
                                                SetED007_DiscountPercentage3(0);
                                                SetED007_DiscountPercentage4(0);
                                                SetED007_DiscountPercentage5(0);
                                                SetED007_DiscountPercent(0);
                                            }
                                            else
                                            {
                                                // mTab.SetValue("LineNetAmt", AmtBpGroup);
                                                SetED007_DscuntlineAmt(AmtNoException);
                                                if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                                                {
                                                    //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), AmtNoException), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                                    SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), AmtNoException), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                                                }
                                            }
                                        }
                                        #endregion
                                        #region Set Value Based Discount
                                        SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(Util.GetValueOfDecimal(GetED007_DiscountPerUnit()), GetQtyEntered()), precision));
                                        #endregion
                                    }
                                }
                            }
                            dsDiscountCombination.Dispose();
                        }
                        else
                        {
                            #region

                            SetED007_DscuntlineAmt(LineNetAmt);
                            SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), LineNetAmt), 100), precision));
                            LNAmt = Decimal.Subtract(LineNetAmt, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                            if (Env.Scale(LNAmt) > GetPrecision())
                            {
                                LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                            }

                            SetLineNetAmt(LNAmt);
                            //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                            //        && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                            if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                    && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                            {
                                SetPriceActual(GetPriceLimit());
                                SetPriceEntered(GetPriceLimit());

                                if (GetPriceList() != 0)
                                {
                                    Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                    if (Env.Scale(Discount) > 2)
                                    {
                                        Discount = Decimal.Round(Discount, 2);
                                    }
                                    SetED004_DiscntPrcnt(Discount);
                                }
                                //Amit 27-nov-2014
                                //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                                LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                                //Amit
                                if (Env.Scale(LineNetAmt) > precision)
                                    LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                                // log.Info("LineNetAmt=" + LineNetAmt);
                                SetLineNetAmt(LineNetAmt);
                                SetED007_DscuntlineAmt(LineNetAmt);
                                SetED007_DiscountAmount(0);
                                SetED007_DiscountPercentage1(0);
                                SetED007_DiscountPercentage2(0);
                                SetED007_DiscountPercentage3(0);
                                SetED007_DiscountPercentage4(0);
                                SetED007_DiscountPercentage5(0);
                                SetED007_DiscountPercent(0);
                            }
                            else
                            {
                                if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                                {
                                    //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), LineNetAmt), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                    SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), LineNetAmt), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                                }
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else if ((Util.GetValueOfString(invoice.IsSOTrx()) == "True" && Util.GetValueOfString(invoice.IsReturnTrx()) == "False")
                        || (Util.GetValueOfString(invoice.IsSOTrx()) == "False" && Util.GetValueOfString(invoice.IsReturnTrx()) == "False"))
                    {
                        #region

                        SetED007_DscuntlineAmt(LineNetAmt);
                        SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), LineNetAmt), 100), precision));
                        SetLineNetAmt(Decimal.Subtract(LineNetAmt, Util.GetValueOfDecimal(GetED007_DiscountAmount())));

                        //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                        //        && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                        if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                                && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                        {
                            SetPriceActual(GetPriceLimit());
                            SetPriceEntered(GetPriceLimit());

                            if (GetPriceList() != 0)
                            {
                                Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                                if (Env.Scale(Discount) > 2)
                                {
                                    Discount = Decimal.Round(Discount, 2);
                                }
                                SetED004_DiscntPrcnt(Discount);
                            }
                            //Amit 27-nov-2014
                            //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                            LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                            //Amit
                            if (Env.Scale(LineNetAmt) > precision)
                                LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                            // log.Info("LineNetAmt=" + LineNetAmt);
                            SetLineNetAmt(LineNetAmt);
                            SetED007_DscuntlineAmt(LineNetAmt);
                            SetED007_DiscountAmount(0);
                            SetED007_DiscountPercentage1(0);
                            SetED007_DiscountPercentage2(0);
                            SetED007_DiscountPercentage3(0);
                            SetED007_DiscountPercentage4(0);
                            SetED007_DiscountPercentage5(0);
                            SetED007_DiscountPercent(0);
                        }
                        else
                        {
                            if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                            {
                                //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), LineNetAmt), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                                SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), LineNetAmt), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                            }
                        }
                        #endregion
                    }
                }
                else if ((Util.GetValueOfString(invoice.IsSOTrx()) == "True" && Util.GetValueOfString(invoice.IsReturnTrx()) == "False")
                      || (Util.GetValueOfString(invoice.IsSOTrx()) == "False" && Util.GetValueOfString(invoice.IsReturnTrx()) == "False"))
                {
                    #region

                    #region Set Payment Term Discount Percent
                    SetED007_DiscountPercent(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()));
                    #endregion

                    #region  #region set Value Based Discount Based on Discount Calculation
                    // mTab.SetValue("ED007_ValueBaseDiscount", Decimal.Round(Decimal.Multiply(Util.GetValueOfDecimal(mTab.GetValue("ED007_DiscountPerUnit")), QtyOrdered.Value), precision));
                    SetED007_ValueBaseDiscount(Decimal.Round(Decimal.Multiply(Util.GetValueOfDecimal(GetED007_DiscountPerUnit()), GetQtyEntered()), precision));
                    #endregion

                    Decimal ReLineNetAmount = 0;
                    //ReLineNetAmount = Decimal.Round(Decimal.Multiply(Util.GetValueOfDecimal(mTab.GetValue("PriceActual")), QtyOrdered.Value), precision);  // Total Line Net Amount
                    ReLineNetAmount = Decimal.Round(Decimal.Multiply(Util.GetValueOfDecimal(GetPriceEntered()), GetQtyEntered()), precision);  // Total Line Net Amount

                    #region Value Based Discount
                    if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C2")
                    {
                        ReLineNetAmount = Decimal.Subtract(ReLineNetAmount, Util.GetValueOfDecimal(GetED007_ValueBaseDiscount()));
                    }
                    #endregion

                    if (Util.GetValueOfDecimal(GetED007_DiscountPercentage1()) > 0)
                    {
                        ReLineNetAmount = Decimal.Subtract(ReLineNetAmount, Decimal.Divide(Decimal.Multiply(ReLineNetAmount, Util.GetValueOfDecimal(GetED007_DiscountPercentage1())), 100));
                    }
                    if (Util.GetValueOfDecimal(GetED007_DiscountPercentage2()) > 0)
                    {
                        ReLineNetAmount = Decimal.Subtract(ReLineNetAmount, Decimal.Divide(Decimal.Multiply(ReLineNetAmount, Util.GetValueOfDecimal(GetED007_DiscountPercentage2())), 100));
                    }
                    if (Util.GetValueOfDecimal(GetED007_DiscountPercentage3()) > 0)
                    {
                        ReLineNetAmount = Decimal.Subtract(ReLineNetAmount, Decimal.Divide(Decimal.Multiply(ReLineNetAmount, Util.GetValueOfDecimal(GetED007_DiscountPercentage3())), 100));
                    }
                    if (Util.GetValueOfDecimal(GetED007_DiscountPercentage4()) > 0)
                    {
                        ReLineNetAmount = Decimal.Subtract(ReLineNetAmount, Decimal.Divide(Decimal.Multiply(ReLineNetAmount, Util.GetValueOfDecimal(GetED007_DiscountPercentage4())), 100));
                    }
                    if (Util.GetValueOfDecimal(GetED007_DiscountPercentage5()) > 0)
                    {
                        ReLineNetAmount = Decimal.Subtract(ReLineNetAmount, Decimal.Divide(Decimal.Multiply(ReLineNetAmount, Util.GetValueOfDecimal(GetED007_DiscountPercentage5())), 100));
                    }
                    ReLineNetAmount = Decimal.Round(ReLineNetAmount, precision);
                    #region Break
                    if (discountSchema.GetDiscountType() == "T")
                    {
                        ReLineNetAmount = BreakCalculation(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                                                         ReLineNetAmount, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                    }
                    #endregion

                    #region Value Based Discount
                    if (Util.GetValueOfString(bPartner.GetED007_DiscountCalculation()) == "C1")
                    {
                        ReLineNetAmount = Decimal.Subtract(ReLineNetAmount, Util.GetValueOfDecimal(GetED007_ValueBaseDiscount()));
                    }
                    #endregion

                    SetED007_DiscountAmount(Decimal.Round(Decimal.Divide(Decimal.Multiply(Util.GetValueOfDecimal(invoice.GetED007_DiscountPercent()), ReLineNetAmount), 100), precision));
                    LNAmt = Decimal.Subtract(LineNetAmt, Util.GetValueOfDecimal(GetED007_DiscountAmount()));
                    if (Env.Scale(LNAmt) > GetPrecision())
                    {
                        LNAmt = Decimal.Round(LNAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                    }

                    SetLineNetAmt(LNAmt);
                    //if (enforce && Decimal.ToDouble(PriceLimit.Value) != 0.0
                    //            && Util.GetValueOfDecimal(mTab.GetValue("LineNetAmt")).CompareTo(Decimal.Round(Decimal.Multiply(PriceLimit.Value, QtyOrdered.Value), precision)) < 0)
                    if (enforce && Decimal.ToDouble(GetPriceLimit()) != 0.0
                              && Util.GetValueOfDecimal(GetLineNetAmt()).CompareTo(Decimal.Round(Decimal.Multiply(GetPriceLimit(), GetQtyEntered()), precision)) < 0)
                    {
                        SetPriceActual(GetPriceLimit());
                        SetPriceEntered(GetPriceLimit());

                        if (GetPriceList() != 0)
                        {
                            Decimal Discount = VAdvantage.Utility.Util.GetValueOfDecimal(((Decimal.ToDouble(GetPriceList()) - Decimal.ToDouble(GetPriceLimit())) / Decimal.ToDouble(GetPriceList()) * 100.0));
                            if (Env.Scale(Discount) > 2)
                            {
                                Discount = Decimal.Round(Discount, 2);
                            }
                            SetED004_DiscntPrcnt(Discount);
                        }
                        //Amit 27-nov-2014
                        //LineNetAmt = Decimal.Multiply(QtyOrdered.Value, PriceLimit.Value);
                        LineNetAmt = Decimal.Multiply(GetQtyEntered(), GetPriceLimit());
                        //Amit
                        if (Env.Scale(LineNetAmt) > precision)
                            LineNetAmt = Decimal.Round(LineNetAmt, precision);//, MidpointRounding.AwayFromZero);
                        // log.Info("LineNetAmt=" + LineNetAmt);
                        SetLineNetAmt(LineNetAmt);
                        SetED007_DscuntlineAmt(LineNetAmt);
                        SetED007_DiscountAmount(0);
                        SetED007_DiscountPercentage1(0);
                        SetED007_DiscountPercentage2(0);
                        SetED007_DiscountPercentage3(0);
                        SetED007_DiscountPercentage4(0);
                        SetED007_DiscountPercentage5(0);
                        SetED007_DiscountPercent(0);
                    }
                    else
                    {
                        // mTab.SetValue("LineNetAmt", ReLineNetAmount);
                        SetED007_DscuntlineAmt(ReLineNetAmount);
                        if (GetQtyInvoiced() > 0 && GetPriceList() > 0 && GetQtyEntered() > 0)
                        {
                            //mTab.SetValue("Discount", Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(PriceList.Value, QtyOrdered.Value), ReLineNetAmount), Decimal.Multiply(PriceList.Value, QtyOrdered.Value)), 100), precision));
                            SetED004_DiscntPrcnt(Decimal.Round(Decimal.Multiply(Decimal.Divide(Decimal.Subtract(Decimal.Multiply(GetPriceList(), GetQtyEntered()), ReLineNetAmount), Decimal.Multiply(GetPriceList(), GetQtyEntered())), 100), precision));
                        }
                    }

                    #endregion

                    if (Util.GetValueOfInt(GetVAM_Product_ID()) <= 0)
                    {
                        SetLineNetAmt(0);
                        SetED007_DscuntlineAmt(0);
                        SetED007_DiscountPercentage1(0);
                        SetED007_DiscountPercentage2(0);
                        SetED007_DiscountPercentage3(0);
                        SetED007_DiscountPercentage4(0);
                        SetED007_DiscountPercentage5(0);
                        SetED007_DiscountPercent(0);
                    }
                }
                #endregion
            }
            else if (Env.Scale(LineNetAmt) > GetPrecision())
            {
                LineNetAmt = Decimal.Round(LineNetAmt, GetPrecision(), MidpointRounding.AwayFromZero);
                base.SetLineNetAmt(LineNetAmt);
            }
            else
            {
                base.SetLineNetAmt(LineNetAmt);
            }
            //}
            //catch (Exception ex)
            //{
            //    // MessageBox.Show("MVABInvoiceLine--SetLineNetAmt");
            //}
        }

        private decimal BreakCalculation(int ProductId, int ClientId, decimal amount, int DiscountSchemaId, decimal FlatDiscount, decimal? QtyEntered)
        {
            StringBuilder query = new StringBuilder();
            decimal amountAfterBreak = amount;
            query.Append(@"SELECT DISTINCT VAM_ProductCategory_ID FROM VAM_Product WHERE IsActive='Y' AND VAM_Product_ID = " + ProductId);
            int productCategoryId = Util.GetValueOfInt(DB.ExecuteScalar(query.ToString(), null, null));
            bool isCalulate = false;

            #region Product Based
            query.Clear();
            query.Append(@"SELECT VAM_ProductCategory_ID , VAM_Product_ID , BreakValue , IsBPartnerFlatDiscount , BreakDiscount FROM VAM_BreakDiscount WHERE 
                                                                   VAM_DiscountCalculation_ID = " + DiscountSchemaId + " AND VAM_Product_ID = " + ProductId
                                                                       + " AND IsActive='Y'  AND VAF_Client_ID=" + ClientId + "Order BY BreakValue DESC");
            DataSet dsDiscountBreak = new DataSet();
            dsDiscountBreak = DB.ExecuteDataset(query.ToString(), null, null);
            if (dsDiscountBreak != null)
            {
                if (dsDiscountBreak.Tables.Count > 0)
                {
                    if (dsDiscountBreak.Tables[0].Rows.Count > 0)
                    {
                        int m = 0;
                        decimal discountBreakValue = 0;

                        for (m = 0; m < dsDiscountBreak.Tables[0].Rows.Count; m++)
                        {
                            if (QtyEntered.Value.CompareTo(Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakValue"])) < 0)
                            {
                                continue;
                            }
                            if (Util.GetValueOfString(dsDiscountBreak.Tables[0].Rows[0]["IsBPartnerFlatDiscount"]) == "N")
                            {
                                isCalulate = true;
                                discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakDiscount"])), 100));
                                break;
                            }
                            else
                            {
                                isCalulate = true;
                                discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, FlatDiscount), 100));
                                break;
                            }
                        }
                        if (isCalulate)
                        {
                            amountAfterBreak = discountBreakValue;
                            return amountAfterBreak;
                        }
                    }
                }
            }
            #endregion

            #region Product Category Based
            query.Clear();
            query.Append(@"SELECT VAM_ProductCategory_ID , VAM_Product_ID , BreakValue , IsBPartnerFlatDiscount , BreakDiscount FROM VAM_BreakDiscount WHERE 
                                                                   VAM_DiscountCalculation_ID = " + DiscountSchemaId + " AND VAM_ProductCategory_ID = " + productCategoryId
                                                                       + " AND IsActive='Y'  AND VAF_Client_ID=" + ClientId + "Order BY BreakValue DESC");
            dsDiscountBreak.Clear();
            dsDiscountBreak = DB.ExecuteDataset(query.ToString(), null, null);
            if (dsDiscountBreak != null)
            {
                if (dsDiscountBreak.Tables.Count > 0)
                {
                    if (dsDiscountBreak.Tables[0].Rows.Count > 0)
                    {
                        int m = 0;
                        decimal discountBreakValue = 0;

                        for (m = 0; m < dsDiscountBreak.Tables[0].Rows.Count; m++)
                        {
                            if (QtyEntered.Value.CompareTo(Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakValue"])) < 0)
                            {
                                continue;
                            }
                            if (Util.GetValueOfString(dsDiscountBreak.Tables[0].Rows[0]["IsBPartnerFlatDiscount"]) == "N")
                            {
                                isCalulate = true;
                                discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakDiscount"])), 100));
                                break;
                            }
                            else
                            {
                                isCalulate = true;
                                discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, FlatDiscount), 100));
                                break;
                            }
                        }
                        if (isCalulate)
                        {
                            amountAfterBreak = discountBreakValue;
                            return amountAfterBreak;
                        }
                    }
                }
            }
            #endregion

            #region Otherwise
            query.Clear();
            query.Append(@"SELECT VAM_ProductCategory_ID , VAM_Product_ID , BreakValue , IsBPartnerFlatDiscount , BreakDiscount FROM VAM_BreakDiscount WHERE 
                                                                   VAM_DiscountCalculation_ID = " + DiscountSchemaId + " AND VAM_ProductCategory_ID IS NULL AND VAM_Product_id IS NULL "
                                                                       + " AND IsActive='Y'  AND VAF_Client_ID=" + ClientId + "Order BY BreakValue DESC");
            dsDiscountBreak.Clear();
            dsDiscountBreak = DB.ExecuteDataset(query.ToString(), null, null);
            if (dsDiscountBreak != null)
            {
                if (dsDiscountBreak.Tables.Count > 0)
                {
                    if (dsDiscountBreak.Tables[0].Rows.Count > 0)
                    {
                        int m = 0;
                        decimal discountBreakValue = 0;

                        for (m = 0; m < dsDiscountBreak.Tables[0].Rows.Count; m++)
                        {
                            if (QtyEntered.Value.CompareTo(Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakValue"])) < 0)
                            {
                                continue;
                            }
                            if (Util.GetValueOfString(dsDiscountBreak.Tables[0].Rows[0]["IsBPartnerFlatDiscount"]) == "N")
                            {
                                isCalulate = true;
                                discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakDiscount"])), 100));
                                break;
                            }
                            else
                            {
                                isCalulate = true;
                                discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, FlatDiscount), 100));
                                break;
                            }
                        }
                        if (isCalulate)
                        {
                            amountAfterBreak = discountBreakValue;
                            return amountAfterBreak;
                        }
                    }
                }
            }
            #endregion

            return amountAfterBreak;
        }

        private decimal FlatDiscount(int ProductId, int ClientId, decimal amount, int DiscountSchemaId, decimal FlatDiscount, decimal? QtyEntered)
        {
            StringBuilder query = new StringBuilder();
            decimal amountAfterBreak = amount;
            query.Append(@"SELECT DISTINCT VAM_ProductCategory_ID FROM VAM_Product WHERE IsActive='Y' AND VAM_Product_ID = " + ProductId);
            int productCategoryId = Util.GetValueOfInt(DB.ExecuteScalar(query.ToString(), null, null));
            bool isCalulate = false;

            // Is flat Discount
            query.Clear();
            query.Append("SELECT  DiscountType  FROM VAM_DiscountCalculation WHERE "
                      + "VAM_DiscountCalculation_ID = " + DiscountSchemaId + " AND IsActive='Y'  AND VAF_Client_ID=" + ClientId);
            string discountType = Util.GetValueOfString(DB.ExecuteScalar(query.ToString()));

            if (discountType == "F")
            {
                isCalulate = true;
                decimal discountBreakValue = (amount - ((amount * FlatDiscount) / 100));
                amountAfterBreak = discountBreakValue;
                return amountAfterBreak;
            }
            else if (discountType == "B")
            {
                #region Product Based
                query.Clear();
                query.Append(@"SELECT VAM_ProductCategory_ID , VAM_Product_ID , BreakValue , IsBPartnerFlatDiscount , BreakDiscount FROM VAM_BreakDiscount WHERE 
                                                                   VAM_DiscountCalculation_ID = " + DiscountSchemaId + " AND VAM_Product_ID = " + ProductId
                                                                           + " AND IsActive='Y'  AND VAF_Client_ID=" + ClientId + "Order BY BreakValue DESC");
                DataSet dsDiscountBreak = new DataSet();
                dsDiscountBreak = DB.ExecuteDataset(query.ToString(), null, null);
                if (dsDiscountBreak != null)
                {
                    if (dsDiscountBreak.Tables.Count > 0)
                    {
                        if (dsDiscountBreak.Tables[0].Rows.Count > 0)
                        {
                            int m = 0;
                            decimal discountBreakValue = 0;

                            for (m = 0; m < dsDiscountBreak.Tables[0].Rows.Count; m++)
                            {
                                if (QtyEntered.Value.CompareTo(Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakValue"])) < 0)
                                {
                                    continue;
                                }
                                if (Util.GetValueOfString(dsDiscountBreak.Tables[0].Rows[0]["IsBPartnerFlatDiscount"]) == "N")
                                {
                                    isCalulate = true;
                                    discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakDiscount"])), 100));
                                    break;
                                }
                                else
                                {
                                    isCalulate = true;
                                    discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, FlatDiscount), 100));
                                    break;
                                }
                            }
                            if (isCalulate)
                            {
                                amountAfterBreak = discountBreakValue;
                                return amountAfterBreak;
                            }
                        }
                    }
                }
                #endregion

                #region Product Category Based
                query.Clear();
                query.Append(@"SELECT VAM_ProductCategory_ID , VAM_Product_ID , BreakValue , IsBPartnerFlatDiscount , BreakDiscount FROM VAM_BreakDiscount WHERE 
                                                                   VAM_DiscountCalculation_ID = " + DiscountSchemaId + " AND VAM_ProductCategory_ID = " + productCategoryId
                                                                           + " AND IsActive='Y'  AND VAF_Client_ID=" + ClientId + "Order BY BreakValue DESC");
                dsDiscountBreak.Clear();
                dsDiscountBreak = DB.ExecuteDataset(query.ToString(), null, null);
                if (dsDiscountBreak != null)
                {
                    if (dsDiscountBreak.Tables.Count > 0)
                    {
                        if (dsDiscountBreak.Tables[0].Rows.Count > 0)
                        {
                            int m = 0;
                            decimal discountBreakValue = 0;

                            for (m = 0; m < dsDiscountBreak.Tables[0].Rows.Count; m++)
                            {
                                if (QtyEntered.Value.CompareTo(Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakValue"])) < 0)
                                {
                                    continue;
                                }
                                if (Util.GetValueOfString(dsDiscountBreak.Tables[0].Rows[0]["IsBPartnerFlatDiscount"]) == "N")
                                {
                                    isCalulate = true;
                                    discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakDiscount"])), 100));
                                    break;
                                }
                                else
                                {
                                    isCalulate = true;
                                    discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, FlatDiscount), 100));
                                    break;
                                }
                            }
                            if (isCalulate)
                            {
                                amountAfterBreak = discountBreakValue;
                                return amountAfterBreak;
                            }
                        }
                    }
                }
                #endregion

                #region Otherwise
                query.Clear();
                query.Append(@"SELECT VAM_ProductCategory_ID , VAM_Product_ID , BreakValue , IsBPartnerFlatDiscount , BreakDiscount FROM VAM_BreakDiscount WHERE 
                                                                   VAM_DiscountCalculation_ID = " + DiscountSchemaId + " AND VAM_ProductCategory_ID IS NULL AND VAM_Product_id IS NULL "
                                                                           + " AND IsActive='Y'  AND VAF_Client_ID=" + ClientId + "Order BY BreakValue DESC");
                dsDiscountBreak.Clear();
                dsDiscountBreak = DB.ExecuteDataset(query.ToString(), null, null);
                if (dsDiscountBreak != null)
                {
                    if (dsDiscountBreak.Tables.Count > 0)
                    {
                        if (dsDiscountBreak.Tables[0].Rows.Count > 0)
                        {
                            int m = 0;
                            decimal discountBreakValue = 0;

                            for (m = 0; m < dsDiscountBreak.Tables[0].Rows.Count; m++)
                            {
                                if (QtyEntered.Value.CompareTo(Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakValue"])) < 0)
                                {
                                    continue;
                                }
                                if (Util.GetValueOfString(dsDiscountBreak.Tables[0].Rows[0]["IsBPartnerFlatDiscount"]) == "N")
                                {
                                    isCalulate = true;
                                    discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, Util.GetValueOfDecimal(dsDiscountBreak.Tables[0].Rows[m]["BreakDiscount"])), 100));
                                    break;
                                }
                                else
                                {
                                    isCalulate = true;
                                    discountBreakValue = Decimal.Subtract(amount, Decimal.Divide(Decimal.Multiply(amount, FlatDiscount), 100));
                                    break;
                                }
                            }
                            if (isCalulate)
                            {
                                amountAfterBreak = discountBreakValue;
                                return amountAfterBreak;
                            }
                        }
                    }
                }
                #endregion
            }

            return amountAfterBreak;
        }

        //Added By Manjot 09-july-2015

        public void SetPriceNew(int VAM_PriceList_ID, int VAB_BusinessPartner_ID)
        {
            try
            {
                if (GetVAM_Product_ID() == 0 || IsDescription())
                    return;
                //
                log.Fine("VAM_PriceList_ID=" + VAM_PriceList_ID);

                MProduct product = MProduct.Get(GetCtx(), GetVAM_Product_ID());
                DataSet ds = new DataSet();
                String sql = @"SELECT bomPriceStdUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceStd,
                                bomPriceListUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID)     AS PriceList,
                                bomPriceLimitUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID)    AS PriceLimit,
                                p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,pl.EnforcePriceLimit,pl.IsTaxIncluded
                                FROM VAM_Product p INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID) INNER JOIN VAM_PriceListVersion pv
                                ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID) INNER JOIN VAM_PriceList pl ON (pv.VAM_PriceList_ID           =pl.VAM_PriceList_ID)
                                WHERE pv.IsActive                ='Y'
                                AND p.VAM_Product_ID               = " + GetVAM_Product_ID()
                                + " AND pl.VAM_PriceList_ID            =" + VAM_PriceList_ID
                                + " AND pp.VAM_PFeature_SetInstance_ID = " + VAM_PFeature_SetInstance_ID
                                + "AND pp.VAB_UOM_ID                  = " + product.GetVAB_UOM_ID()
                                + " AND pp.IsActive                  ='Y' ORDER BY pv.ValidFrom DESC";
                ds = DB.ExecuteDataset(sql);
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                        {
                            SetPriceActual(Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["PriceStd"]));
                            SetPriceList(Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["PriceList"]));
                            SetPriceLimit(Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["PriceLimit"]));
                        }

                    }
                }
                if (Decimal.Compare(GetQtyEntered(), GetQtyInvoiced()) == 0)
                    SetPriceEntered(GetPriceActual());
                else
                    SetPriceEntered(Decimal.Multiply(GetPriceActual(), Decimal.Round(Decimal.Divide(GetQtyInvoiced(), GetQtyEntered()), 6)));

                //
                if (GetVAB_UOM_ID() == 0)
                    SetVAB_UOM_ID(product.GetVAB_UOM_ID());
                //
                _priceSet = true;
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetPrice");
            }
        }


        public void SetPrice(bool IsProcess)
        {
            try
            {
                if (GetVAM_Product_ID() == 0 || IsDescription())
                    return;
                if (_VAM_PriceList_ID == 0 || _VAB_BusinessPartner_ID == 0)
                    SetInvoice(GetParent());
                if (_VAM_PriceList_ID == 0 || _VAB_BusinessPartner_ID == 0)
                    throw new Exception("setPrice - PriceList unknown!");
                //throw new IllegalStateException("setPrice - PriceList unknown!");
                SetPriceNew(_VAM_PriceList_ID, _VAB_BusinessPartner_ID);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetPrice");
            }
        }

        //Manjot

        /**
         * 	Set Qty Invoiced/Entered.
         *	@param Qty Invoiced/Ordered
         */
        public void SetQty(int Qty)
        {
            try
            {
                SetQty(new Decimal(Qty));
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetQty");
            }
        }

        /**
         * 	Set Qty Invoiced
         *	@param Qty Invoiced/Entered
         */
        public void SetQty(Decimal Qty)
        {
            SetQtyEntered(Qty);
            SetQtyInvoiced(GetQtyEntered());
        }

        /**
         * 	Set Qty Entered - enforce entered UOM 
         *	@param QtyEntered
         */
        public void SetQtyEntered(Decimal? QtyEntered)
        {
            try
            {
                if (QtyEntered != null && GetVAB_UOM_ID() != 0)
                {
                    int precision = MUOM.GetPrecision(GetCtx(), GetVAB_UOM_ID());
                    QtyEntered = Decimal.Round((Decimal)QtyEntered, precision, MidpointRounding.AwayFromZero);
                }
                base.SetQtyEntered(Convert.ToDecimal(QtyEntered));
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetQtyEntered");
            }
        }

        /**
         * 	Set Qty Invoiced - enforce Product UOM 
         *	@param QtyInvoiced
         */
        public void SetQtyInvoiced(Decimal? QtyInvoiced)
        {
            try
            {
                MProduct product = GetProduct();
                if (QtyInvoiced != null && product != null)
                {
                    int precision = product.GetUOMPrecision();
                    QtyInvoiced = Decimal.Round((Decimal)QtyInvoiced, precision, MidpointRounding.AwayFromZero);
                }
                base.SetQtyInvoiced(Convert.ToDecimal(QtyInvoiced));
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetQtyInvoiced");
            }
        }

        /**
         * 	Set Product
         *	@param product product
         */
        public void SetProduct(MProduct product)
        {
            try
            {
                _product = product;
                if (_product != null)
                {
                    SetVAM_Product_ID(_product.GetVAM_Product_ID());
                    SetVAB_UOM_ID(_product.GetVAB_UOM_ID());
                }
                else
                {
                    SetVAM_Product_ID(0);
                    SetVAB_UOM_ID(0);
                }
                SetVAM_PFeature_SetInstance_ID(0);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetProduct");
            }
        }


        /**
         * 	Set VAM_Product_ID
         *	@param VAM_Product_ID product
         *	@param SetUOM Set UOM from product
         */
        public void SetVAM_Product_ID(int VAM_Product_ID, Boolean SetUOM)
        {
            try
            {
                if (SetUOM)
                    SetProduct(MProduct.Get(GetCtx(), VAM_Product_ID));
                else
                    base.SetVAM_Product_ID(VAM_Product_ID);
                SetVAM_PFeature_SetInstance_ID(0);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetVAM_Product_ID");
            }
        }

        /**
         * 	Set Product and UOM
         *	@param VAM_Product_ID product
         *	@param VAB_UOM_ID uom
         */
        public void SetVAM_Product_ID(int VAM_Product_ID, int VAB_UOM_ID)
        {
            try
            {
                base.SetVAM_Product_ID(VAM_Product_ID);
                base.SetVAB_UOM_ID(VAB_UOM_ID);
                SetVAM_PFeature_SetInstance_ID(0);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetVAM_Product_ID");
            }
        }

        /**
         * 	Get Product
         *	@return product or null
         */
        public MProduct GetProduct()
        {
            try
            {
                if (_product == null && GetVAM_Product_ID() != 0)
                {
                    _product = MProduct.Get(GetCtx(), GetVAM_Product_ID());
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--GetProduct");
            }
            return _product;
        }

        /// <summary>
        /// This function is used for costing calculation
        /// It gives consolidated product cost (taxable amt + tax amount + surcharge amt) based on setting
        /// </summary>
        /// <param name="invoiceline">Invoice Line reference</param>
        /// <returns>LineNetAmount of Product</returns>
        public Decimal GetProductLineCost(MVABInvoiceLine invoiceline)
        {
            if (invoiceline == null || invoiceline.Get_ID() <= 0)
            {
                return 0;
            }

            // Get Taxable amount from invoiceline
            Decimal amt = invoiceline.GetTaxBaseAmt();

            // create object of tax - for checking tax to be include in cost or not
            MTax tax = MTax.Get(invoiceline.GetCtx(), invoiceline.GetVAB_TaxRate_ID());
            if (tax.Get_ColumnIndex("IsIncludeInCost") >= 0)
            {
                // add Tax amount in product cost
                if (tax.IsIncludeInCost())
                {
                    amt += invoiceline.GetTaxAmt();
                }

                // add Surcharge amount in product cost
                if (tax.Get_ColumnIndex("Surcharge_Tax_ID") >= 0 && tax.GetSurcharge_Tax_ID() > 0)
                {
                    if (MTax.Get(invoiceline.GetCtx(), tax.GetSurcharge_Tax_ID()).IsIncludeInCost())
                    {
                        amt += invoiceline.GetSurchargeAmt();
                    }
                }
            }

            // if amount is ZERO, then calculate as usual with Line net amount
            if (amt == 0)
            {
                amt = invoiceline.GetLineNetAmt();
            }

            return amt;
        }

        /**
         * 	Get VAB_Project_ID
         *	@return project
         */
        public int GetVAB_Project_ID()
        {
            int ii = base.GetVAB_Project_ID();
            try
            {
                if (ii == 0)
                {
                    ii = GetParent().GetVAB_Project_ID();
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--GetVAB_Project_ID");
            }
            return ii;
        }

        /**
         * 	Get VAB_BillingCode_ID
         *	@return Activity
         */
        public int GetVAB_BillingCode_ID()
        {
            int ii = base.GetVAB_BillingCode_ID();
            try
            {
                if (ii == 0)
                {
                    ii = GetParent().GetVAB_BillingCode_ID();
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--GetVAB_BillingCode_ID");
            }
            return ii;
        }

        /**
         * 	Get VAB_Promotion_ID
         *	@return Campaign
         */
        public int GetVAB_Promotion_ID()
        {
            int ii = base.GetVAB_Promotion_ID();
            if (ii == 0)
                ii = GetParent().GetVAB_Promotion_ID();
            return ii;
        }	//	GetVAB_Promotion_ID

        /**
         * 	Get User2_ID
         *	@return User2
         */
        public int GetUser1_ID()
        {
            int ii = base.GetUser1_ID();
            if (ii == 0)
                ii = GetParent().GetUser1_ID();
            return ii;
        }	//	GetUser1_ID

        /**
         * 	Get User2_ID
         *	@return User2
         */
        public int GetUser2_ID()
        {
            int ii = base.GetUser2_ID();
            if (ii == 0)
                ii = GetParent().GetUser2_ID();
            return ii;
        }	//	GetUser2_ID

        /**
         * 	Get VAF_OrgTrx_ID
         *	@return trx org
         */
        public int GetVAF_OrgTrx_ID()
        {
            int ii = base.GetVAF_OrgTrx_ID();
            if (ii == 0)
                ii = GetParent().GetVAF_OrgTrx_ID();
            return ii;
        }	//	GetVAF_OrgTrx_ID

        /**
         * 	String Representation
         *	@return Info
         */
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder("MVABInvoiceLine[")
                .Append(Get_ID()).Append(",").Append(GetLine())
                .Append(",QtyInvoiced=").Append(GetQtyInvoiced())
                .Append(",LineNetAmt=").Append(GetLineNetAmt())
                .Append("]");
            return sb.ToString();
        }	//	toString

        /**
         * 	Get (Product/Charge) Name
         * 	@return name
         */
        public String GetName()
        {
            if (_name == null)
            {
                String sql = "SELECT COALESCE (p.Name, c.Name) "
                    + "FROM VAB_InvoiceLine il"
                    + " LEFT OUTER JOIN VAM_Product p ON (il.VAM_Product_ID=p.VAM_Product_ID)"
                    + " LEFT OUTER JOIN VAB_Charge C ON (il.VAB_Charge_ID=c.VAB_Charge_ID) "
                    + "WHERE VAB_InvoiceLine_ID=" + GetVAB_InvoiceLine_ID();
                IDataReader idr = null;
                try
                {
                    idr = DataBase.DB.ExecuteReader(sql, null, Get_TrxName());
                    if (idr.Read())
                    {
                        _name = idr[0].ToString();
                    }
                    idr.Close();
                    //pstmt.close();
                    //pstmt = null;
                    if (_name == null)
                        _name = "??";
                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                    }
                    log.Log(Level.SEVERE, "GetName", e);
                }

            }
            return _name;
        }

        /**
         * 	Set Temporary (cached) Name
         * 	@param tempName Cached Name
         */
        public void SetName(String tempName)
        {
            _name = tempName;
        }

        /**
         * 	Get Description Text.
         * 	For jsp access (vs. isDescription)
         *	@return description
         */
        public String GetDescriptionText()
        {
            return base.GetDescription();
        }	//	GetDescriptionText

        /**
         * 	Get Currency Precision
         *	@return precision
         */
        public int GetPrecision()
        {
            try
            {
                if (_precision != null)
                {
                    return Convert.ToInt32(_precision);
                }

                String sql = "SELECT c.StdPrecision "
                    + "FROM VAB_Currency c INNER JOIN VAB_Invoice x ON (x.VAB_Currency_ID=c.VAB_Currency_ID) "
                    + "WHERE x.VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                int i = Utility.Util.GetValueOfInt(DataBase.DB.ExecuteScalar(sql, null, Get_TrxName()));
                if (i < 0)
                {
                    log.Warning("Precision=" + i + " - Set to 2");
                    i = 2;
                }
                _precision = i;
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--GetPrecision()");
            }
            return (int)_precision;
        }

        /**
         *	Is Tax Included in Amount
         *	@return true if tax is included
         */
        public Boolean IsTaxIncluded()
        {
            // try
            // {
            if (_VAM_PriceList_ID == 0)
            {
                _VAM_PriceList_ID = DataBase.DB.GetSQLValue(Get_TrxName(),
                    "SELECT VAM_PriceList_ID FROM VAB_Invoice WHERE VAB_Invoice_ID=@param1",
                    GetVAB_Invoice_ID());
            }
            MPriceList pl = MPriceList.Get(GetCtx(), _VAM_PriceList_ID, Get_TrxName());
            // }
            // catch (Exception ex)
            //{
            //    // MessageBox.Show("MVABInvoiceLine--isTaxIncluded");
            //}
            return pl.IsTaxIncluded();
        }

        /// <summary>
        /// Create Lead/Request
        /// </summary>
        /// <param name="invoice"></param>
        public void CreateLeadRequest(MVABInvoice invoice)
        {
            try
            {
                if (GetProduct() == null || _product.GetVAR_Source_ID() == 0)
                    return;
                String summary = "Purchased: " + _product.GetName()
                    + " - " + GetQtyEntered() + " * " + GetPriceEntered();
                //
                MSource source = MSource.Get(GetCtx(), _product.GetVAR_Source_ID());
                //	Create Request
                if (MSource.SOURCECREATETYPE_Both.Equals(source.GetSourceCreateType())
                    || MSource.SOURCECREATETYPE_Request.Equals(source.GetSourceCreateType()))
                {
                    MRequest request = new MRequest(GetCtx(), 0, Get_TrxName());
                    request.SetClientOrg(this);
                    request.SetSummary(summary);
                    request.SetVAF_UserContact_ID(invoice.GetVAF_UserContact_ID());
                    request.SetVAB_BusinessPartner_ID(invoice.GetVAB_BusinessPartner_ID());
                    request.SetVAB_Invoice_ID(invoice.GetVAB_Invoice_ID());
                    request.SetVAB_Order_ID(invoice.GetVAB_Order_ID());
                    request.SetVAB_BillingCode_ID(invoice.GetVAB_BillingCode_ID());
                    request.SetVAB_Promotion_ID(invoice.GetVAB_Promotion_ID());
                    request.SetVAB_Project_ID(invoice.GetVAB_Project_ID());
                    //
                    request.SetVAM_Product_ID(GetVAM_Product_ID());
                    request.SetVAR_Source_ID(source.GetVAR_Source_ID());
                    request.Save();
                }
                //	Create Lead
                if (MSource.SOURCECREATETYPE_Both.Equals(source.GetSourceCreateType())
                    || MSource.SOURCECREATETYPE_Lead.Equals(source.GetSourceCreateType()))
                {
                    MVABLead lead = new MVABLead(GetCtx(), 0, Get_TrxName());
                    lead.SetClientOrg(this);
                    lead.SetDescription(summary);
                    lead.SetVAF_UserContact_ID(invoice.GetVAF_UserContact_ID());
                    lead.SetVAB_BPart_Location_ID(invoice.GetVAB_BPart_Location_ID());
                    lead.SetVAB_BusinessPartner_ID(invoice.GetVAB_BusinessPartner_ID());
                    lead.SetVAB_Promotion_ID(invoice.GetVAB_Promotion_ID());
                    lead.SetVAB_Project_ID(invoice.GetVAB_Project_ID());
                    //
                    MVABBPartLocation bpLoc = new MVABBPartLocation(GetCtx(), invoice.GetVAB_BPart_Location_ID(), null);
                    MVABAddress loc = bpLoc.GetLocation(false);
                    lead.SetAddress1(loc.GetAddress1());
                    lead.SetAddress2(loc.GetAddress2());
                    lead.SetCity(loc.GetCity());
                    lead.SetPostal(loc.GetPostal());
                    lead.SetPostal_Add(loc.GetPostal_Add());
                    lead.SetRegionName(loc.GetRegionName(false));
                    lead.SetVAB_RegionState_ID(loc.GetVAB_RegionState_ID());
                    lead.SetVAB_City_ID(loc.GetVAB_City_ID());
                    lead.SetVAB_Country_ID(loc.GetVAB_Country_ID());
                    //
                    lead.SetVAR_Source_ID(source.GetVAR_Source_ID());
                    lead.Save();
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--createLeadRequest");
            }
        }


        /**
         * 	Set Resource Assignment - Callout
         *	@param oldVAS_Res_Assignment_ID old value
         *	@param newVAS_Res_Assignment_ID new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout 
        public void SetVAS_Res_Assignment_ID(String oldVAS_Res_Assignment_ID,
            String newVAS_Res_Assignment_ID, int windowNo)
        {
            if (newVAS_Res_Assignment_ID == null || newVAS_Res_Assignment_ID.Length == 0)
                return;
            int VAS_Res_Assignment_ID = int.Parse(newVAS_Res_Assignment_ID);
            if (VAS_Res_Assignment_ID == 0)
                return;
            //
            base.SetVAS_Res_Assignment_ID(VAS_Res_Assignment_ID);

            int VAM_Product_ID = 0;
            String Name = null;
            String Description = null;
            Decimal? Qty = null;
            String sql = "SELECT p.VAM_Product_ID, ra.Name, ra.Description, ra.Qty "
                + "FROM VAS_Res_Assignment ra"
                + " INNER JOIN VAM_Product p ON (p.VAS_Resource_ID=ra.VAS_Resource_ID) "
                + "WHERE ra.VAS_Res_Assignment_ID= " + VAS_Res_Assignment_ID;
            IDataReader idr = null;
            try
            {
                //PreparedStatement pstmt = DataBase.prepareStatement(sql, null);
                //pstmt.SetInt(1, VAS_Res_Assignment_ID);
                //ResultSet rs = pstmt.executeQuery();
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                if (idr.Read())
                {
                    VAM_Product_ID = Utility.Util.GetValueOfInt(idr[0]);
                    Name = idr.GetString(1);
                    Description = idr.GetString(2);
                    Qty = Utility.Util.GetValueOfDecimal(idr[3]);
                }
                idr.Close();


            }
            catch (SqlException e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }


            log.Fine("VAS_Res_Assignment_ID=" + VAS_Res_Assignment_ID
                    + " - VAM_Product_ID=" + VAM_Product_ID);
            if (VAM_Product_ID != 0)
            {
                SetVAM_Product_ID(VAM_Product_ID);
                if (Description != null)
                    Name += " (" + Description + ")";
                if (!".".Equals(Name))
                    SetDescription(Name);
                if (Qty != null)
                    SetQtyInvoiced(Qty);
            }
        }


        /**************************************************************************
         * 	Before Save
         *	@param newRecord
         *	@return true if save
         */
        protected override bool BeforeSave(bool newRecord)
        {
            Decimal? QtyInvoiced, QtyEntered;
            try
            {
                log.Fine("New=" + newRecord);

                // JID_1624,JID_1625: If product or charge not selected, then show message "Please select Product or Charge".
                if (GetVAM_Product_ID() == 0 && GetVAB_Charge_ID() == 0)
                {
                    log.SaveError("VIS_NOProductOrCharge", "");
                    return false;
                }

                //	Charge
                if (GetVAB_Charge_ID() != 0)
                {
                    if (GetVAM_Product_ID() != 0)
                        SetVAM_Product_ID(0);
                }

                MVABInvoice inv = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_TrxName());



                // when invoice having advance payment term, and lines already are created with order reference then user are not allowed to create manual line
                // not to check this condition when record is in completed / closed / reversed / voided stage
                if (Env.IsModuleInstalled("VA009_") && !inv.IsProcessing() &&
                    !(inv.GetDocStatus() == "CO" || inv.GetDocStatus() == "CL" || inv.GetDocStatus() == "RE" || inv.GetDocStatus() == "VO"))
                {
                    bool isAdvance = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT SUM( CASE
                            WHEN VAB_Paymentterm.VA009_Advance!= COALESCE(VAB_PaymentSchedule.VA009_Advance,'N') THEN 1 ELSE 0 END) AS isAdvance
                        FROM VAB_Paymentterm LEFT JOIN VAB_PaymentSchedule ON VAB_Paymentterm.VAB_Paymentterm_ID = VAB_PaymentSchedule.VAB_Paymentterm_ID
                        WHERE VAB_Paymentterm.VAB_Paymentterm_ID = " + inv.GetVAB_PaymentTerm_ID(), null, Get_Trx())) > 0 ? true : false;

                    if (inv.GetVAB_Order_ID() > 0 && GetVAB_OrderLine_ID() == 0 && isAdvance)
                    {
                        log.SaveError("", Msg.GetMsg(GetCtx(), "VIS_CantSaveManualLine"));
                        return false;
                    }
                    else if (isAdvance && GetVAB_OrderLine_ID() > 0)
                    {
                        if (Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(VAB_InvoiceLine_ID) FROM VAB_InvoiceLine WHERE NVL(VAB_OrderLine_ID, 0) = 0
                        AND VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx())) > 0)
                        {
                            log.SaveError("", Msg.GetMsg(GetCtx(), "VIS_CantSaveManualLine"));
                            return false;
                        }
                    }
                }

                int primaryAcctSchemaCurrency = 0;
                // get current cost from product cost on new record and when product changed
                // currency conversion also required if order has different currency with base currency
                if (newRecord || (Is_ValueChanged("VAM_Product_ID")) || (Is_ValueChanged("VAM_PFeature_SetInstance_ID")))
                {

                    decimal currentcostprice = MVAMProductCost.GetproductCosts(GetVAF_Client_ID(), GetVAF_Org_ID(), GetVAM_Product_ID(), Util.GetValueOfInt(GetVAM_PFeature_SetInstance_ID()), Get_Trx());
                    primaryAcctSchemaCurrency = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_Currency_ID from VAB_AccountBook WHERE VAB_AccountBook_ID = 
                                            (SELECT VAB_AccountBook1_id FROM VAF_ClientDetail WHERE vaf_client_id = " + GetVAF_Client_ID() + ")", null, Get_Trx()));
                    if (inv.GetVAB_Currency_ID() != primaryAcctSchemaCurrency)
                    {
                        currentcostprice = MVABExchangeRate.Convert(GetCtx(), currentcostprice, primaryAcctSchemaCurrency, inv.GetVAB_Currency_ID(),
                                                                                    inv.GetDateAcct(), inv.GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                    }
                    if (inv.GetDescription() != null && inv.GetDescription().Contains("{->"))
                    {
                        // do not update current cost price during reversal, this time reverse doc contain same amount which are on original document
                    }
                    else
                    {
                        SetCurrentCostPrice(currentcostprice);
                    }
                }

                if (!inv.IsSOTrx())
                {
                    _IsSOTrx = inv.IsSOTrx();
                    //MProduct pro = new MProduct(GetCtx(), GetVAM_Product_ID(), null);
                    //String qryUom = "SELECT vdr.VAB_UOM_ID FROM VAM_Product p LEFT JOIN VAM_Product_PO vdr ON p.VAM_Product_ID= vdr.VAM_Product_ID WHERE p.VAM_Product_ID=" + GetVAM_Product_ID() + " AND vdr.VAB_BusinessPartner_ID = " + inv.GetVAB_BusinessPartner_ID();
                    //int uom = Util.GetValueOfInt(DB.ExecuteScalar(qryUom));
                    //if (pro.GetVAB_UOM_ID() != 0)
                    //{
                    //    if (pro.GetVAB_UOM_ID() != uom && uom != 0)
                    //    {
                    //        decimal? Res = Util.GetValueOfDecimal(DB.ExecuteScalar("SELECT trunc(multiplyrate,4) FROM VAB_UOM_Conversion WHERE VAB_UOM_ID = " + pro.GetVAB_UOM_ID() + " AND VAB_UOM_To_ID = " + uom + " AND VAM_Product_ID= " + GetVAM_Product_ID() + " AND IsActive='Y'"));
                    //        if (Res > 0)
                    //        {
                    //            SetQtyEntered(GetQtyEntered() * Res);
                    //            //OrdQty = MUOMConversion.ConvertProductTo(GetCtx(), _VAM_Product_ID, UOM, OrdQty);
                    //        }
                    //        else
                    //        {
                    //            decimal? res = Util.GetValueOfDecimal(DB.ExecuteScalar("SELECT trunc(multiplyrate,4) FROM VAB_UOM_Conversion WHERE VAB_UOM_ID = " + pro.GetVAB_UOM_ID() + " AND VAB_UOM_To_ID = " + uom + " AND IsActive='Y'"));
                    //            if (res > 0)
                    //            {
                    //                SetQtyEntered(GetQtyEntered() * res);
                    //                //OrdQty = MUOMConversion.Convert(GetCtx(), prdUOM, UOM, OrdQty);
                    //            }
                    //        }
                    //        SetVAB_UOM_ID(uom);
                    //    }
                    //    else
                    //    {
                    //        SetVAB_UOM_ID(pro.GetVAB_UOM_ID());
                    //    }
                    //}
                    QtyEntered = GetQtyEntered();
                    QtyInvoiced = GetQtyInvoiced();
                    int gp = MUOM.GetPrecision(GetCtx(), GetVAB_UOM_ID());
                    Decimal? QtyEntered1 = Decimal.Round(QtyEntered.Value, gp, MidpointRounding.AwayFromZero);
                    if (QtyEntered != QtyEntered1)
                    {
                        this.log.Fine("Corrected QtyEntered Scale UOM=" + GetVAB_UOM_ID()
                            + "; QtyEntered=" + QtyEntered + "->" + QtyEntered1);
                        QtyEntered = QtyEntered1;
                        SetQtyEntered(QtyEntered);
                    }
                    Decimal? pc = MUOMConversion.ConvertProductFrom(GetCtx(), GetVAM_Product_ID(), GetVAB_UOM_ID(), QtyEntered);
                    QtyInvoiced = pc;
                    bool conversion = false;
                    if (QtyInvoiced != null)
                    {
                        conversion = QtyEntered != QtyInvoiced;
                    }
                    if (QtyInvoiced == null)
                    {
                        conversion = false;
                        QtyInvoiced = 1;
                        SetQtyInvoiced(QtyInvoiced * QtyEntered1);
                    }
                    else
                    {
                        SetQtyInvoiced(QtyInvoiced);
                    }
                    // Added by Bharat on 06 July 2017 restrict to create invoice line for quantity greater than Received Quantity.

                    if (inv.GetDescription() != null && inv.GetDescription().Contains("{->"))
                    {
                        // Handled the case for Reversal
                    }
                    else
                    {
                        if (GetVAM_Inv_InOutLine_ID() > 0 && _checkMRQty)
                        {
                            MVAMInvInOutLine il = new MVAMInvInOutLine(GetCtx(), GetVAM_Inv_InOutLine_ID(), Get_TrxName());
                            decimal receivedQty = il.GetQtyEntered();
                            string invQry = @"SELECT SUM(COALESCE(li.QtyEntered,0)) as QtyEntered FROM VAM_Inv_InOutLine ml INNER JOIN VAB_InvoiceLine li 
                            ON li.VAM_Inv_InOutLine_ID = ml.VAM_Inv_InOutLine_ID INNER JOIN VAB_Invoice ci ON ci.VAB_Invoice_ID = li.VAB_Invoice_ID WHERE ci.DocStatus NOT IN ('VO', 'RE') 
                            AND ml.VAM_Inv_InOutLine_ID = " + GetVAM_Inv_InOutLine_ID() + " AND li.VAB_InvoiceLine_ID != " + Get_ID() + " GROUP BY ml.MovementQty, ml.QtyEntered";
                            decimal qtyInv = Util.GetValueOfDecimal(DB.ExecuteScalar(invQry, null, Get_TrxName()));
                            if (receivedQty < qtyInv + QtyEntered)
                            {
                                log.SaveError("", Msg.GetMsg(GetCtx(), "InvoiceQtyGreater"));
                                return false;
                            }
                        }
                    }

                    //Added by Bharat to set Discrepancy Amount
                    MVABDocTypes doc = new MVABDocTypes(GetCtx(), inv.GetVAB_DocTypesTarget_ID(), Get_TrxName());
                    if (!doc.IsReturnTrx())
                    {
                        //int table_ID = MVABInvoiceLine.Table_ID;
                        if (inv.Get_ColumnIndex("DiscrepancyAmt") >= 0)
                        {
                            decimal receivedQty = 0, invoicedQty = 0, qtyDiff = 0;
                            decimal invAmt = 0, ordAmt = 0, discrepancyAmt = 0;
                            invoicedQty = GetQtyEntered();
                            invAmt = GetPriceEntered();
                            //if (GetVAM_Inv_InOutLine_ID() > 0)
                            //{
                            //    MVAMInvInOutLine iol = new MVAMInvInOutLine(GetCtx(), GetVAM_Inv_InOutLine_ID(), Get_TrxName());
                            //    receivedQty = iol.GetQtyEntered();
                            //    qtyDiff = invoicedQty - receivedQty;
                            //    if (qtyDiff > 0)
                            //    {
                            //        discrepancyAmt = Decimal.Multiply(qtyDiff, invAmt);
                            //    }
                            //}
                            if (GetVAB_OrderLine_ID() > 0)
                            {
                                MVABOrderLine ol = new MVABOrderLine(GetCtx(), GetVAB_OrderLine_ID(), Get_TrxName());
                                ordAmt = ol.GetPriceEntered();
                                decimal diffAmt = Decimal.Subtract(invAmt, ordAmt);
                                if (diffAmt > 0)
                                {
                                    discrepancyAmt = Decimal.Add(discrepancyAmt, Decimal.Multiply(diffAmt, invoicedQty));
                                }
                            }
                            //if (GetVAM_Inv_InOutLine_ID() == 0 && GetVAB_OrderLine_ID() == 0)
                            //{
                            //    discrepancyAmt = Decimal.Add(discrepancyAmt, Decimal.Multiply(invAmt, invoicedQty));
                            //}
                            SetDiscrepancyAmt(discrepancyAmt);
                        }
                    }

                    // Set Converted Price                     
                    StringBuilder sql = new StringBuilder();
                    Tuple<String, String, String> iInfo = null;
                    if (Env.HasModulePrefix("ED011_", out iInfo))
                    {
                        //decimal convertedprice = 0;
                        //MVABInvoice invoice1 = new MVABInvoice(GetCtx(), Util.GetValueOfInt(GetVAB_Invoice_ID()), null);
                        //MBPartner bPartner = new MBPartner(GetCtx(), invoice1.GetVAB_BusinessPartner_ID(), null);

                        //string qry = "SELECT VAM_PriceListVersion_ID FROM VAM_PriceListVersion WHERE IsActive = 'Y' AND VAM_PriceList_id = " + inv.GetVAM_PriceList_ID() + @" AND VALIDFROM <= sysdate order by validfrom desc";
                        //int _Version_ID = Util.GetValueOfInt(DB.ExecuteScalar(qry));
                        //sql.Append(@"SELECT PriceList , PriceStd , PriceLimit FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + GetVAM_Product_ID()
                        //                     + " AND VAM_PriceListVersion_ID = " + _Version_ID
                        //                     + " AND VAM_PFeature_SetInstance_ID = " + GetVAM_PFeature_SetInstance_ID() + " AND VAB_UOM_ID=" + GetVAB_UOM_ID());

                        //DataSet ds = new DataSet();
                        //try
                        //{
                        //    ds = DB.ExecuteDataset(sql.ToString(), null, null);
                        //    if (ds.Tables.Count > 0)
                        //    {
                        //        if (ds.Tables[0].Rows.Count > 0)
                        //        {
                        //            convertedprice = FlatDiscount(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                        //                     Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceStd"]),
                        //                     bPartner.GetVAM_DiscountCalculation_ID(),
                        //                     Util.GetValueOfDecimal(bPartner.GetFlatDiscount()),
                        //                     GetQtyEntered());
                        //            SetPriceList(Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceList"]));
                        //            SetPriceLimit(Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceLimit"]));
                        //            SetPriceActual(convertedprice);
                        //            SetPriceEntered(convertedprice);
                        //        }
                        //        else
                        //        {
                        //            ds.Dispose();
                        //            sql.Clear();
                        //            sql.Append(@"SELECT PriceList , PriceStd , PriceLimit FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + GetVAM_Product_ID()
                        //                         + " AND VAM_PriceListVersion_ID = " + _Version_ID
                        //                         + " AND VAM_PFeature_SetInstance_ID = 0 AND VAB_UOM_ID=" + GetVAB_UOM_ID());
                        //            ds = DB.ExecuteDataset(sql.ToString(), null, null);
                        //            if (ds.Tables.Count > 0)
                        //            {
                        //                if (ds.Tables[0].Rows.Count > 0)
                        //                {
                        //                    convertedprice = FlatDiscount(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                        //                     Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceStd"]),
                        //                     bPartner.GetVAM_DiscountCalculation_ID(),
                        //                     Util.GetValueOfDecimal(bPartner.GetFlatDiscount()),
                        //                     GetQtyEntered());
                        //                    SetPriceList(Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceList"]));
                        //                    SetPriceLimit(Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["PriceLimit"]));
                        //                    SetPriceActual(convertedprice);
                        //                    SetPriceEntered(convertedprice);
                        //                }
                        //                else
                        //                {
                        //                    decimal? PriceActual = Util.GetValueOfDecimal(GetPriceEntered());
                        //                    decimal? PriceEntered = (Decimal?)MUOMConversion.ConvertProductFrom(GetCtx(), GetVAM_Product_ID(),
                        //                        GetVAB_UOM_ID(), PriceActual.Value);
                        //                    if (PriceEntered == null)
                        //                        PriceEntered = PriceActual;

                        //                    MProduct prod = new MProduct(Env.GetCtx(), Util.GetValueOfInt(GetVAM_Product_ID()), null);
                        //                    sql.Clear();
                        //                    sql.Append(@"SELECT PriceList FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + Util.GetValueOfInt(GetVAM_Product_ID())
                        //                           + " AND VAM_PriceListVersion_ID = " + _Version_ID
                        //                           + " AND VAM_PFeature_SetInstance_ID = " + GetVAM_PFeature_SetInstance_ID() + " AND VAB_UOM_ID=" + prod.GetVAB_UOM_ID());
                        //                    decimal pricelist = Util.GetValueOfDecimal(DB.ExecuteScalar(sql.ToString(), null, null));
                        //                    if (pricelist == 0)
                        //                    {
                        //                        sql.Clear();
                        //                        sql.Append(@"SELECT PriceList FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + Util.GetValueOfInt(GetVAM_Product_ID())
                        //                           + " AND VAM_PriceListVersion_ID = " + _Version_ID
                        //                           + " AND VAM_PFeature_SetInstance_ID = 0 AND VAB_UOM_ID=" + prod.GetVAB_UOM_ID());
                        //                        pricelist = Util.GetValueOfDecimal(DB.ExecuteScalar(sql.ToString(), null, null));
                        //                    }
                        //                    sql.Clear();
                        //                    sql.Append(@"SELECT PriceStd FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + Util.GetValueOfInt(GetVAM_Product_ID())
                        //                        + " AND VAM_PriceListVersion_ID = " + _Version_ID
                        //                        + " AND VAM_PFeature_SetInstance_ID = " + GetVAM_PFeature_SetInstance_ID() + " AND VAB_UOM_ID=" + prod.GetVAB_UOM_ID());
                        //                    decimal pricestd = Util.GetValueOfDecimal(DB.ExecuteScalar(sql.ToString(), null, null));
                        //                    if (pricestd == 0)
                        //                    {
                        //                        sql.Clear();
                        //                        sql.Append(@"SELECT PriceStd FROM VAM_ProductPrice WHERE Isactive='Y' AND VAM_Product_ID = " + Util.GetValueOfInt(GetVAM_Product_ID())
                        //                        + " AND VAM_PriceListVersion_ID = " + _Version_ID
                        //                        + " AND VAM_PFeature_SetInstance_ID = 0 AND VAB_UOM_ID=" + prod.GetVAB_UOM_ID());
                        //                        pricestd = Util.GetValueOfDecimal(DB.ExecuteScalar(sql.ToString(), null, null));
                        //                    }
                        //                    pricestd = FlatDiscount(Util.GetValueOfInt(GetVAM_Product_ID()), GetCtx().GetVAF_Client_ID(),
                        //                                     pricestd, bPartner.GetVAM_DiscountCalculation_ID(), Util.GetValueOfDecimal(bPartner.GetFlatDiscount()), GetQtyEntered());
                        //                    sql.Clear();
                        //                    sql.Append(@"SELECT con.DivideRate FROM VAB_UOM_Conversion con INNER JOIN VAB_UOM uom ON con.VAB_UOM_ID = uom.VAB_UOM_ID WHERE con.IsActive = 'Y' AND con.VAM_Product_ID = " + Util.GetValueOfInt(GetVAM_Product_ID()) +
                        //                           " AND con.VAB_UOM_ID = " + prod.GetVAB_UOM_ID() + " AND con.VAB_UOM_To_ID = " + GetVAB_UOM_ID());
                        //                    decimal rate = Util.GetValueOfDecimal(DB.ExecuteScalar(sql.ToString(), null, null));
                        //                    if (rate == 0)
                        //                    {
                        //                        sql.Clear();
                        //                        sql.Append(@"SELECT con.DivideRate FROM VAB_UOM_Conversion con INNER JOIN VAB_UOM uom ON con.VAB_UOM_ID = uom.VAB_UOM_ID WHERE con.IsActive = 'Y'" +
                        //                          " AND con.VAB_UOM_ID = " + prod.GetVAB_UOM_ID() + " AND con.VAB_UOM_To_ID = " + GetVAB_UOM_ID());

                        //                        rate = Util.GetValueOfDecimal(DB.ExecuteScalar(sql.ToString(), null, null));
                        //                    }
                        //                    if (rate > 0)
                        //                    {
                        //                        SetPriceList(Decimal.Multiply(pricelist, rate));
                        //                        SetPriceActual(Decimal.Multiply(pricestd, rate));
                        //                        SetPriceEntered(Decimal.Multiply(pricestd, rate));
                        //                    }
                        //                    else
                        //                    {
                        //                        SetPriceList(pricelist);
                        //                        SetPriceActual(pricestd);
                        //                        SetPriceEntered(pricestd);
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //    ds.Dispose();
                        //}
                        //catch
                        //{

                        //}
                        //finally
                        //{
                        //    ds.Dispose();
                        //}
                    }
                    else	//	Set Product Price
                    {
                        if (!_priceSet
                            && Env.ZERO.CompareTo(GetPriceActual()) == 0
                            && Env.ZERO.CompareTo(GetPriceList()) == 0)
                            SetPrice();
                    }
                }
                else	//	Set Product Price
                {
                    if (!_priceSet
                        && Env.ZERO.CompareTo(GetPriceActual()) == 0
                        && Env.ZERO.CompareTo(GetPriceList()) == 0)
                    {
                        if (Env.IsModuleInstalled("VAPOS_"))
                        {
                            string sqlOLRef = "SELECT Ref_OrderLine_ID FROM VAB_OrderLine WHERE VAB_OrderLine_ID = " + GetVAB_OrderLine_ID();
                            if (Util.GetValueOfInt(DB.ExecuteQuery(sqlOLRef)) <= 0)
                            {
                                SetPrice();
                            }
                        }
                        else
                            SetPrice();
                    }
                }

                //	Set Tax

                if (GetVAB_TaxRate_ID() == 0)
                    SetTax();

                if (GetVAB_TaxRate_ID() > 0)
                {
                    SetVAB_TaxRate_ID(GetVAB_TaxRate_ID());
                }
                else
                {
                    SetVAB_TaxRate_ID(GetCtx().GetContextAsInt("VAB_TaxRate_ID"));
                }

                //	Get Line No
                if (GetLine() == 0)
                {
                    String sql = "SELECT COALESCE(MAX(Line),0)+10 FROM VAB_InvoiceLine WHERE VAB_Invoice_ID=@param1";
                    int ii = DataBase.DB.GetSQLValue(Get_TrxName(), sql, GetVAB_Invoice_ID());
                    SetLine(ii);
                }
                //	UOM
                if (GetVAB_UOM_ID() == 0)
                {
                    int VAB_UOM_ID = MUOM.GetDefault_UOM_ID(GetCtx());
                    if (VAB_UOM_ID > 0)
                        SetVAB_UOM_ID(VAB_UOM_ID);
                }
                //	Qty Precision
                if (newRecord || Is_ValueChanged("QtyEntered"))
                    SetQtyEntered(GetQtyEntered());
                if (newRecord || Is_ValueChanged("QtyInvoiced"))
                    SetQtyInvoiced(GetQtyInvoiced());

                //JID_1744 PriceList Precision should as per Currency Precision
                if (newRecord || Is_ValueChanged("PriceList"))
                    SetPriceList(Decimal.Round(GetPriceList(), GetPrecision(), MidpointRounding.AwayFromZero));

                //	Calculations & Rounding
                SetLineNetAmt();
                if (((Decimal)GetTaxAmt()).CompareTo(Env.ZERO) == 0 || (Get_ColumnIndex("SurchargeAmt") > 0 && GetSurchargeAmt().CompareTo(Env.ZERO) == 0))
                    SetTaxAmt();

                // set Tax Amount in base currency
                if (Get_ColumnIndex("TaxBaseCurrencyAmt") >= 0)
                {
                    decimal taxAmt = 0;
                    primaryAcctSchemaCurrency = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_Currency_ID FROM VAB_AccountBook WHERE VAB_AccountBook_ID = 
                                            (SELECT VAB_AccountBook1_id FROM VAF_ClientDetail WHERE vaf_client_id = " + GetVAF_Client_ID() + ")", null, Get_Trx()));
                    if (inv.GetVAB_Currency_ID() != primaryAcctSchemaCurrency)
                    {
                        taxAmt = MVABExchangeRate.Convert(GetCtx(), GetTaxAmt(), primaryAcctSchemaCurrency, inv.GetVAB_Currency_ID(),
                                                                                   inv.GetDateAcct(), inv.GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                    }
                    else
                    {
                        taxAmt = GetTaxAmt();
                    }
                    SetTaxBaseCurrencyAmt(taxAmt);
                }

                // set Taxable Amount -- (Line Total-Tax Amount)
                if (Get_ColumnIndex("TaxBaseAmt") >= 0)
                {
                    if (Get_ColumnIndex("SurchargeAmt") > 0)
                    {
                        SetTaxBaseAmt(Decimal.Subtract(Decimal.Subtract(GetLineTotalAmt(), GetTaxAmt()), GetSurchargeAmt()));
                    }
                    else
                    {
                        SetTaxBaseAmt(Decimal.Subtract(GetLineTotalAmt(), GetTaxAmt()));
                    }
                }


                // Change by mohit Asked by ravikant 21/03/2016
                //if (!_IsSOTrx)
                //{
                if (newRecord)
                {

                    if (((Decimal)GetPriceEntered()).CompareTo(Env.ZERO) != 0)
                    {
                        SetBasePrice(GetPriceEntered());
                    }
                }
                else
                {
                    if (Is_ValueChanged("VAM_Product_ID"))
                    {
                        if (((Decimal)GetPriceEntered()).CompareTo(Env.ZERO) != 0)
                        {
                            SetBasePrice(GetPriceEntered());
                        }
                    }
                }

                // Calculate Withholding Tax
                if (newRecord || !inv.IsProcessing())
                {
                    if (!CalculateWithholding(inv.GetVAB_BusinessPartner_ID(), inv.GetVAB_BPart_Location_ID(), inv.IsSOTrx()))
                    {
                        log.SaveError("Error", Msg.GetMsg(GetCtx(), "WrongWithholdingTax"));
                        return false;
                    }
                }

                // Reset Amount Dimension if Line Amount is different
                if (!newRecord && Is_ValueChanged("LineNetAmt"))
                {
                    if (Util.GetValueOfInt(Get_Value("AmtDimLineNetAmt")) > 0)
                    {
                        string qry = "SELECT Amount FROM VAB_DimAmt WHERE VAB_DimAmt_ID=" + Util.GetValueOfInt(Get_Value("AmtDimLineNetAmt"));
                        decimal amtdimAmt = Util.GetValueOfDecimal(DB.ExecuteScalar(qry, null, Get_TrxName()));

                        if (amtdimAmt != GetLineNetAmt())
                        {
                            Set_Value("AmtDimLineNetAmt", null);
                        }
                    }
                    resetAmtDim = true;
                }

                // Reset Amount Dimension if Line Total Amount is different
                if (!newRecord && Is_ValueChanged("LineTotalAmt"))
                {
                    if (Util.GetValueOfInt(Get_Value("AmtDimLineTotalAmt")) > 0)
                    {
                        string qry = "SELECT Amount FROM VAB_DimAmt WHERE VAB_DimAmt_ID=" + Util.GetValueOfInt(Get_Value("AmtDimLineTotalAmt"));
                        decimal amtdimAmt = Util.GetValueOfDecimal(DB.ExecuteScalar(qry, null, Get_TrxName()));

                        if (amtdimAmt != GetLineTotalAmt())
                        {
                            Set_Value("AmtDimLineTotalAmt", null);
                        }
                    }
                    resetTotalAmtDim = true;
                }

                //}
                // End CHange

            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--BeforeSave");
            }
            return true;
        }


        /**
         * 	After Save
         *	@param newRecord new
         *	@param success success
         *	@return saved
         */
        protected override bool AfterSave(bool newRecord, bool success)
        {
            try
            {
                if (!success || IsProcessed())
                    return success;

                // Reset Amount Dimension on header after save of new record
                if (newRecord && GetLineNetAmt() != 0)
                {
                    resetAmtDim = true;
                    resetTotalAmtDim = true;
                }

                if (!newRecord && Is_ValueChanged("VAB_TaxRate_ID"))
                {
                    //	Recalculate Tax for old Tax
                    MVABInvoiceTax tax = MVABInvoiceTax.Get(this, GetPrecision(),
                        true, Get_TrxName());	//	old Tax
                    if (tax != null)
                    {
                        if (!tax.CalculateTaxFromLines())
                            return false;
                        if (!tax.Save(Get_TrxName()))
                            return true;
                    }

                    // if Surcharge Tax is selected then calculate Tax for this Surcharge Tax.
                    if (Get_ColumnIndex("SurchargeAmt") > 0)
                    {
                        tax = MVABInvoiceTax.GetSurcharge(this, GetPrecision(), true, Get_TrxName());  //	old Tax
                        if (tax != null)
                        {
                            if (!tax.CalculateSurchargeFromLines())
                                return false;
                            if (!tax.Save(Get_TrxName()))
                                return false;
                        }
                    }
                }

                //Added by Bharat to set Discrepancy Amount
                MVABInvoice inv = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_TrxName());
                MVABDocTypes doc = new MVABDocTypes(GetCtx(), inv.GetVAB_DocTypesTarget_ID(), Get_TrxName());
                if (!inv.IsSOTrx() && !doc.IsReturnTrx())
                {
                    if (inv.Get_ColumnIndex("DiscrepancyAmt") >= 0)
                    {
                        String sql = "SELECT SUM(NVL(DiscrepancyAmt,0))"
                                + " FROM VAB_InvoiceLine WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                        decimal desAmt = Util.GetValueOfDecimal(DB.ExecuteScalar(sql));
                        if (desAmt > 0)
                        {
                            sql = "UPDATE VAB_Invoice o"
                                + " SET IsInDispute = 'Y', DiscrepancyAmt ="
                                    + desAmt + " WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                        }
                        else
                        {
                            sql = "UPDATE VAB_Invoice o"
                                + " SET IsInDispute = 'N', DiscrepancyAmt ="
                                    + desAmt + " WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                        }
                        int no = DB.ExecuteQuery(sql, null, Get_Trx());
                        log.Fine("Lines -> #" + no);
                    }
                }

                // when purchase order having setting as "Hold payment" then need to set "Hold payment" on Invoice header also.
                // calling - when IsHoldPayment column exist, Purchase side record, not in processing (like prepareit, completeit..) , not a hold payment Invoice
                if (inv.Get_ColumnIndex("IsHoldPayment") > 0 && !inv.IsSOTrx() && !inv.IsProcessing() && !inv.IsHoldPayment())
                {
                    if (!UpdateHoldPayment())
                    {
                        log.SaveWarning("", Msg.GetMsg(GetCtx(), "VIS_HoldPaymentNotUpdated")); ;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--AfterSave");
            }
            return UpdateHeaderTax();
        }

        /**
         * 	After Delete
         *	@param success success
         *	@return deleted
         */
        protected override bool AfterDelete(bool success)
        {
            if (!success)
                return success;
            //Added by Bharat to set Discrepancy Amount
            MVABInvoice inv = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_TrxName());
            MVABDocTypes doc = new MVABDocTypes(GetCtx(), inv.GetVAB_DocTypesTarget_ID(), Get_TrxName());
            if (!inv.IsSOTrx() && !doc.IsReturnTrx())
            {
                if (inv.Get_ColumnIndex("DiscrepancyAmt") >= 0)
                {
                    String sql = "SELECT SUM(NVL(DiscrepancyAmt,0))"
                            + " FROM VAB_InvoiceLine WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                    decimal desAmt = Util.GetValueOfDecimal(DB.ExecuteScalar(sql));
                    if (desAmt > 0)
                    {
                        sql = "UPDATE VAB_Invoice o"
                            + " SET IsInDispute = 'Y', DiscrepancyAmt ="
                                + desAmt + " WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                    }
                    else
                    {
                        sql = "UPDATE VAB_Invoice o"
                            + " SET IsInDispute = 'N', DiscrepancyAmt ="
                                + desAmt + " WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
                    }
                    int no = DB.ExecuteQuery(sql, null, Get_Trx());
                    log.Fine("Lines -> #" + no);
                }
            }

            // Reset Amount Dimension on header after delete of non zero line
            if (GetLineNetAmt() != 0)
            {
                resetAmtDim = true;
                resetTotalAmtDim = true;
            }
            return UpdateHeaderTax();
        }

        /**
         *	Update Tax & Header
         *	@return true if header updated with tax
         */
        private bool UpdateHeaderTax()
        {
            MVABInvoice invoice = null;
            try
            {
                //	Recalculate Tax for this Tax

                MVABInvoiceTax tax = MVABInvoiceTax.Get(this, GetPrecision(),
                    false, Get_TrxName());	//	current Tax
                if (tax != null)
                {
                    if (!tax.CalculateTaxFromLines())
                        return false;
                    if (!tax.Save(Get_TrxName()))
                        return false;
                }

                MTax taxRate = tax.GetTax();
                if (taxRate.IsSummary())
                {
                    invoice = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_TrxName());
                    if (!CalculateChildTax(invoice, tax, taxRate, Get_TrxName()))
                    {
                        return false;
                    }
                }

                // if Surcharge Tax is selected then calculate Tax for this Surcharge Tax.
                else if (Get_ColumnIndex("SurchargeAmt") > 0 && taxRate.Get_ColumnIndex("Surcharge_Tax_ID") > 0 && taxRate.GetSurcharge_Tax_ID() > 0)
                {
                    tax = MVABInvoiceTax.GetSurcharge(this, GetPrecision(), false, Get_TrxName());  //	current Tax
                    if (!tax.CalculateSurchargeFromLines())
                        return false;
                    if (!tax.Save(Get_TrxName()))
                        return false;
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--UpdateHeaderTax()");
            }

            //	Update Invoice Header
            String sql = "UPDATE VAB_Invoice i"
                + " SET TotalLines="
                    + "(SELECT COALESCE(SUM(LineNetAmt),0) FROM VAB_InvoiceLine il WHERE i.VAB_Invoice_ID=il.VAB_Invoice_ID) "
                    + (resetAmtDim ? ", AmtDimSubTotal = null " : "")       // reset Amount Dimension if Sub Total Amount is different
                    + (resetTotalAmtDim ? ", AmtDimGrandTotal = null " : "")     // reset Amount Dimension if Grand Total Amount is different
                    + (Get_ColumnIndex("WithholdingAmt") > 0 ? ", WithholdingAmt = ((SELECT COALESCE(SUM(WithholdingAmt),0) FROM VAB_InvoiceLine il WHERE i.VAB_Invoice_ID=il.VAB_Invoice_ID))" : "")
                + "WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
            int no = DataBase.DB.ExecuteQuery(sql, null, Get_TrxName());
            if (no != 1)
            {
                log.Warning("(1) #" + no);
            }

            if (IsTaxIncluded())
                sql = "UPDATE VAB_Invoice i "
                    + "SET GrandTotal=TotalLines "
                    + (Get_ColumnIndex("WithholdingAmt") > 0 ? " , GrandTotalAfterWithholding = (TotalLines - NVL(WithholdingAmt, 0) - NVL(BackupWithholdingAmount, 0)) " : "")
                    + "WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
            else
                sql = "UPDATE VAB_Invoice i "
                    + "SET GrandTotal=TotalLines+"
                        + "(SELECT COALESCE(SUM(TaxAmt),0) FROM VAB_Tax_Invoice it WHERE i.VAB_Invoice_ID=it.VAB_Invoice_ID) "
                        + (Get_ColumnIndex("WithholdingAmt") > 0 ? " , GrandTotalAfterWithholding = (TotalLines + (SELECT COALESCE(SUM(TaxAmt),0) FROM VAB_Tax_Invoice it WHERE i.VAB_Invoice_ID=it.VAB_Invoice_ID) - NVL(WithholdingAmt, 0) - NVL(BackupWithholdingAmount, 0))" : "")
                        + "WHERE VAB_Invoice_ID=" + GetVAB_Invoice_ID();
            no = DataBase.DB.ExecuteQuery(sql, null, Get_TrxName());
            if (no != 1)
            {
                log.Warning("(2) #" + no);
            }
            else
            {
                // calculate withholdng on header 
                if (invoice == null)
                {
                    invoice = new MVABInvoice(GetCtx(), GetVAB_Invoice_ID(), Get_TrxName());
                }
                if (invoice.GetVAB_Withholding_ID() > 0)
                {
                    if (!invoice.SetWithholdingAmount(invoice))
                    {
                        log.SaveWarning("Warning", Msg.GetMsg(GetCtx(), "WrongBackupWithholding"));
                    }
                    else
                    {
                        invoice.Save();
                    }
                }
            }
            _parent = null;

            return no == 1;
        }

        /// <summary>
        /// Update Invoice as Hold Payment or not based on order selected on respective Invoice
        /// </summary>
        /// <returns>True, In case when record updated successfully or when no record found as Hold payment Order</returns>
        private bool UpdateHoldPayment()
        {
            String sql = @"SELECT DISTINCT COALESCE(SUM(
                          CASE WHEN IsHoldPayment!= 'N' THEN 1 ELSE 0 END) , 0) AS IsHoldPayment
                        FROM VAB_InvoiceLine INNER JOIN VAB_OrderLine ON VAB_InvoiceLine.VAB_OrderLine_ID=VAB_OrderLine.VAB_OrderLine_ID
                        INNER JOIN VAB_Order ON VAB_OrderLine.VAB_Order_ID = VAB_Order.VAB_Order_ID
                        WHERE VAB_InvoiceLine.VAB_Invoice_ID =  " + GetVAB_Invoice_ID();
            int no = DataBase.DB.GetSQLValue(Get_Trx(), sql, null);
            if (no > 0)
            {
                no = DB.ExecuteQuery("UPDATE VAB_Invoice SET IsHoldPayment = 'Y' WHERE VAB_Invoice_ID = " + GetVAB_Invoice_ID(), null, Get_Trx());
                log.Fine("Hold Payment Updated as TRUE -> #" + no);
                if (no <= 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// This function is used to calculate withholding cost
        /// </summary>
        /// <param name="VAB_BusinessPartner_ID">Buisness partner refrence</param>
        /// <param name="VAB_BPart_Location_ID">business partner location</param>
        /// <param name="issotrx">transaction type</param>
        /// <returns>success</returns>
        private bool CalculateWithholding(int VAB_BusinessPartner_ID, int VAB_BPart_Location_ID, bool issotrx)
        {
            Decimal withholdingAmt = 0.0M;
            String sql = @"SELECT VAB_BusinessPartner.IsApplicableonAPInvoice, VAB_BusinessPartner.IsApplicableonAPPayment, VAB_BusinessPartner.IsApplicableonARInvoice,
                            VAB_BusinessPartner.IsApplicableonARReceipt,  
                            VAB_Address.VAB_Country_ID , VAB_Address.VAB_RegionState_ID";
            if (GetVAM_Product_ID() > 0)
            {
                sql += " , (SELECT VAB_WithholdingCategory_ID FROM VAM_Product WHERE VAM_Product_ID = " + GetVAM_Product_ID() + ") AS VAB_WithholdingCategory_ID ";
            }
            else
            {
                sql += " , (SELECT VAB_WithholdingCategory_ID FROM VAB_Charge WHERE VAB_Charge_ID = " + GetVAB_Charge_ID() + ") AS VAB_WithholdingCategory_ID ";
            }
            sql += @" FROM VAB_BusinessPartner INNER JOIN VAB_BPart_Location ON 
                     VAB_BusinessPartner.VAB_BusinessPartner_ID = VAB_BPart_Location.VAB_BusinessPartner_ID 
                     INNER JOIN VAB_Address ON VAB_BPart_Location.VAB_Address_ID = VAB_Address.VAB_Address_ID  WHERE 
                     VAB_BusinessPartner.VAB_BusinessPartner_ID = " + VAB_BusinessPartner_ID + @" AND VAB_BPart_Location.VAB_BPart_Location_ID = " + VAB_BPart_Location_ID;
            DataSet ds = DB.ExecuteDataset(sql, null, null);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                // check Withholding applicable on vendor/customer
                if ((!issotrx && Util.GetValueOfString(ds.Tables[0].Rows[0]["IsApplicableonAPInvoice"]).Equals("Y")) ||
                    (issotrx && Util.GetValueOfString(ds.Tables[0].Rows[0]["IsApplicableonARInvoice"]).Equals("Y")))
                {
                    sql = "SELECT  VAB_Withholding_ID , InvCalculation, InvPercentage FROM VAB_Withholding " +
                          " WHERE IsActive = 'Y' AND VAB_WithholdingCategory_ID = " + Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_WithholdingCategory_ID"]) +
                          " AND TransactionType = '" + (issotrx ? X_VAB_Withholding.TRANSACTIONTYPE_Sale : X_VAB_Withholding.TRANSACTIONTYPE_Purchase) + "' " +
                          " AND IsApplicableonInv='Y' AND VAF_Client_ID = " + GetVAF_Client_ID() +
                          " AND VAF_Org_ID IN (0 , " + GetVAF_Org_ID() + ")";
                    if (Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_RegionState_ID"]) > 0)
                    {
                        sql += " AND NVL(VAB_RegionState_ID, 0) IN (0 ,  " + Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_RegionState_ID"]) + ")";
                    }
                    else
                    {
                        sql += " AND NVL(VAB_RegionState_ID, 0) IN (0) ";
                    }
                    if (Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Country_ID"]) > 0)
                    {
                        sql += " AND NVL(VAB_Country_ID , 0) IN (0 ,  " + Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Country_ID"]) + ")";
                    }
                    if (GetVAB_Withholding_ID() > 0)
                    {
                        sql += " AND VAB_Withholding_ID = " + GetVAB_Withholding_ID();
                    }
                    sql += " ORDER BY InvCalculation ASC , NVL(VAB_RegionState_ID , 0) DESC , NVL(VAB_Country_ID , 0) DESC"; // priority to LineNetAmt, Region, Country
                    ds = DB.ExecuteDataset(sql, null, null);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        // get amount on which we have to derive withholding tax amount
                        if (Util.GetValueOfString(ds.Tables[0].Rows[0]["InvCalculation"]).Equals(X_VAB_Withholding.INVCALCULATION_SubTotal))
                        {
                            // get lineNetAmount
                            withholdingAmt = GetLineNetAmt();
                        }
                        else if (Util.GetValueOfString(ds.Tables[0].Rows[0]["InvCalculation"]).Equals(X_VAB_Withholding.INVCALCULATION_TaxAmount))
                        {
                            // get tax amount from Invoice tax
                            withholdingAmt = GetTaxAmt();
                        }

                        _log.Info("Invoice withholding detail, Invoice ID = " + GetVAB_Invoice_ID() + " , Amount on distribute = " + withholdingAmt +
                         " , Invoice Withhold Percentage " + Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["InvPercentage"]));

                        // derive formula
                        withholdingAmt = Decimal.Divide(
                                         Decimal.Multiply(withholdingAmt, Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["InvPercentage"]))
                                         , 100);

                        SetWithholdingAmt(Decimal.Round(withholdingAmt, GetPrecision()));
                        SetVAB_Withholding_ID(Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAB_Withholding_ID"]));
                    }
                    else
                    {
                        // when exact data not found 
                        SetWithholdingAmt(0);
                        // when withholdinf define by user manual or already set
                        if (GetVAB_Withholding_ID() > 0)
                        {
                            //SetVAB_Withholding_ID(0);
                            return false;
                        }
                    }
                }
                else
                {
                    // when withholding not applicable on Business Partner
                    SetWithholdingAmt(0);
                    // when withholdinf define by user manual or already set, but not applicable on invoice
                    if (GetVAB_Withholding_ID() > 0)
                    {
                        //SetVAB_Withholding_ID(0);
                        return false;
                    }
                }
            }
            return true;
        }

        // Create or Update Child Tax
        private bool CalculateChildTax(MVABInvoice invoice, MVABInvoiceTax iTax, MTax tax, Trx trxName)
        {
            MTax[] cTaxes = tax.GetChildTaxes(false);	//	Multiple taxes
            for (int j = 0; j < cTaxes.Length; j++)
            {
                MVABInvoiceTax newITax = null;
                MTax cTax = cTaxes[j];
                Decimal taxAmt = cTax.CalculateTax(iTax.GetTaxBaseAmt(), false, GetPrecision());

                // check child tax record is avialable or not 
                // if not then create new record
                String sql = "SELECT * FROM VAB_Tax_Invoice WHERE VAB_Invoice_ID=" + invoice.GetVAB_Invoice_ID() + " AND VAB_TaxRate_ID=" + cTax.GetVAB_TaxRate_ID();
                try
                {
                    DataSet ds = DataBase.DB.ExecuteDataset(sql, null, trxName);
                    if (ds.Tables.Count > 0)
                    {
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            newITax = new MVABInvoiceTax(GetCtx(), dr, trxName);
                        }
                    }
                }
                catch (Exception e)
                {
                    _log.Log(Level.SEVERE, sql, e);
                }

                if (newITax != null)
                {
                    newITax.Set_TrxName(trxName);
                }

                // Create New
                if (newITax == null)
                {
                    newITax = new MVABInvoiceTax(GetCtx(), 0, Get_TrxName());
                    newITax.SetClientOrg(this);
                    newITax.SetVAB_Invoice_ID(GetVAB_Invoice_ID());
                    newITax.SetVAB_TaxRate_ID(cTax.GetVAB_TaxRate_ID());
                }

                newITax.SetPrecision(GetPrecision());
                newITax.SetIsTaxIncluded(IsTaxIncluded());
                newITax.SetTaxBaseAmt(iTax.GetTaxBaseAmt());
                newITax.SetTaxAmt(taxAmt);
                //Set Tax Amount (Base Currency) on Invoice Tax Window 
                if (newITax.Get_ColumnIndex("TaxBaseCurrencyAmt") > 0)
                {
                    decimal? baseTaxAmt = taxAmt;
                    int primaryAcctSchemaCurrency = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT VAB_Currency_ID FROM VAB_AccountBook WHERE VAB_AccountBook_ID = 
                                            (SELECT VAB_AccountBook1_id FROM VAF_ClientDetail WHERE vaf_client_id = " + GetVAF_Client_ID() + ")", null, Get_Trx()));
                    if (invoice.GetVAB_Currency_ID() != primaryAcctSchemaCurrency)
                    {
                        baseTaxAmt = MVABExchangeRate.Convert(GetCtx(), taxAmt, primaryAcctSchemaCurrency, invoice.GetVAB_Currency_ID(),
                                                                                   invoice.GetDateAcct(), invoice.GetVAB_CurrencyType_ID(), GetVAF_Client_ID(), GetVAF_Org_ID());
                    }
                    newITax.Set_Value("TaxBaseCurrencyAmt", baseTaxAmt);
                }
                if (!newITax.Save(Get_TrxName()))
                    return false;
            }
            // Delete Summary Level Tax Line
            if (!iTax.Delete(true, Get_TrxName()))
                return false;

            return true;
        }


        /// <summary>
        /// Allocate Landed Costs
        /// </summary>
        /// <returns>String, error message or ""</returns>
        public String AllocateLandedCosts()
        {
            StringBuilder qry = new StringBuilder();
            DataSet ds = null;
            try
            {
                if (IsProcessed())
                {
                    return "Processed";
                }

                MLandedCost[] lcs = MLandedCost.GetLandedCosts(this);
                if (lcs.Length == 0)
                {
                    return "";
                }

                //int nos = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(*) FROM VAB_LCost WHERE  LandedCostDistribution = 'C' AND VAB_InvoiceLine_ID = " + GetVAB_InvoiceLine_ID(), null, Get_Trx()));
                //if (nos > 0)
                //{
                //    return "";
                //}

                String sql = "DELETE FROM VAB_LCostDistribution WHERE VAB_InvoiceLine_ID="
                    + GetVAB_InvoiceLine_ID();
                int no = DataBase.DB.ExecuteQuery(sql, null, Get_TrxName());
                if (no != 0)
                {
                    log.Info("Deleted #" + no);
                }
                int inserted = 0;
                ValueNamePair pp = null;
                int hasMovement = lcs[0].Get_ColumnIndex("VAM_InventoryTransfer_ID");

                //	*** Single Criteria ***
                if (lcs.Length == 1)
                {
                    MLandedCost lc = lcs[0];
                    #region Landed Cost Distrinution based on Import Value (Invoice)

                    if (lc.GetLandedCostDistribution() == MLandedCost.LANDEDCOSTDISTRIBUTION_ImportValue)
                    {
                        // All Invoice Lines
                        if (lc.GetRef_Invoice_ID() != 0)
                        {
                            //	Create List
                            List<MVABInvoiceLine> list = new List<MVABInvoiceLine>();
                            MVABInvoice inv = new MVABInvoice(GetCtx(), lc.GetRef_Invoice_ID(), Get_TrxName());
                            MVABInvoiceLine[] lines = inv.GetLines();

                            Decimal total = Env.ZERO;
                            //MVABInvoiceLine line = null;
                            decimal mrPrice = Env.ZERO;
                            List<DataRow> dr = new List<DataRow>();

                            // now in landed cost distribution, consider "tax amt" and "surcharge amt" based on setting applicable on tax rate
                            qry.Append(@"SELECT il.VAM_Product_ID, il.VAM_PFeature_SetInstance_ID, sum(mi.Qty) as Qty, ");
                            //SUM(mi.Qty * il.PriceActual) AS LineNetAmt , 
                            qry.Append(@" SUM(mi.Qty *
                                                CASE
                                                WHEN NVL(C_SurChargeTax.IsIncludeInCost , 'N') = 'Y'
                                                AND NVL(VAB_TaxRate.IsIncludeInCost , 'N')           = 'Y'
                                                THEN ROUND((il.taxbaseamt + il.taxamt + il.surchargeamt) / il.qtyinvoiced , 4)
                                                WHEN NVL(C_SurChargeTax.IsIncludeInCost , 'N') = 'N'
                                                AND NVL(VAB_TaxRate.IsIncludeInCost , 'N')           = 'Y'
                                                THEN ROUND((il.taxbaseamt + il.taxamt) / il.qtyinvoiced , 4)
                                                WHEN NVL(C_SurChargeTax.IsIncludeInCost , 'N') = 'Y'
                                                AND NVL(VAB_TaxRate.IsIncludeInCost , 'N')           = 'N'
                                                THEN ROUND((il.taxbaseamt + il.surchargeamt) / il.qtyinvoiced, 4)
                                                ELSE ROUND(il.taxbaseamt  / il.qtyinvoiced, 4)
                                              END) AS LineNetAmt , io.VAM_Warehouse_ID
                            FROM VAB_InvoiceLine il INNER JOIN VAM_MatchInvoice mi ON Mi.VAB_InvoiceLine_ID = Il.VAB_InvoiceLine_ID INNER JOIN VAM_Inv_InOutLine iol ON iol.VAM_Inv_InOutLine_ID = mi.VAM_Inv_InOutLine_ID
                            INNER JOIN VAM_Inv_InOut io ON io.VAM_Inv_InOut_ID = iol.VAM_Inv_InOut_ID INNER JOIN VAM_Warehouse wh ON wh.VAM_Warehouse_ID = io.VAM_Warehouse_ID 
                            INNER JOIN VAB_TaxRate VAB_TaxRate ON VAB_TaxRate.VAB_TaxRate_ID = il.VAB_TaxRate_ID 
                            LEFT JOIN VAB_TaxRate C_SurChargeTax ON VAB_TaxRate.Surcharge_Tax_ID = C_SurChargeTax.VAB_TaxRate_ID 
                            WHERE il.VAB_Invoice_ID = " + lc.GetRef_Invoice_ID());

                            //	Single Invoice Line
                            if (lc.GetRef_InvoiceLine_ID() != 0)
                            {
                                qry.Append(" AND il.VAB_InvoiceLine_ID = " + lc.GetRef_InvoiceLine_ID());
                            }

                            if (lc.GetVAM_Product_ID() > 0)
                            {
                                qry.Append(" AND il.VAM_Product_ID = " + lc.GetVAM_Product_ID());
                            }

                            if (lc.Get_ColumnIndex("VAM_PFeature_SetInstance_ID") > 0 && lc.GetVAM_PFeature_SetInstance_ID() > 0)
                            {
                                qry.Append(" AND il.VAM_PFeature_SetInstance_ID = " + lc.GetVAM_PFeature_SetInstance_ID());
                            }

                            qry.Append(@" GROUP BY il.VAM_Product_ID, il.VAM_PFeature_SetInstance_ID, io.VAM_Warehouse_ID 
                                        ,  il.taxbaseamt , il.taxamt , il.surchargeamt , C_SurChargeTax.IsIncludeInCost , VAB_TaxRate.IsIncludeInCost, il.qtyinvoiced");

                            ds = DB.ExecuteDataset(qry.ToString(), null, Get_TrxName());

                            if (ds != null && ds.Tables[0].Rows.Count > 0)
                            {
                                //	Calculate total & base
                                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                {
                                    total = Decimal.Add(total, Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["LineNetAmt"]));
                                    dr.Add(ds.Tables[0].Rows[i]);
                                }

                                // if No Matching Lines (with Product)
                                if (dr.Count == 0)
                                {
                                    ds.Dispose();
                                    return Msg.GetMsg(GetCtx(), "NoMatchProduct") + "@VAB_Invoice_ID@";
                                }

                                // if Total of Base values is 0
                                if (Env.Signum(total) == 0)
                                {
                                    ds.Dispose();
                                    return Msg.GetMsg(GetCtx(), "TotalBaseZero") + lc.GetLandedCostDistribution();
                                }
                                ds.Dispose();

                                //	Create Allocations
                                MVABInvoiceLine iol = null;
                                MLandedCostAllocation lca = null;
                                Decimal base1 = 0;
                                double result = 0;

                                for (int i = 0; i < dr.Count; i++)
                                {
                                    mrPrice = Util.GetValueOfDecimal(dr[i]["LineNetAmt"]);

                                    //iol = (MVABInvoiceLine)list[i];
                                    lca = new MLandedCostAllocation(this,
                                        lc.GetVAM_ProductCostElement_ID());
                                    lca.SetVAM_Product_ID(Util.GetValueOfInt(dr[i]["VAM_Product_ID"]));
                                    lca.SetVAM_PFeature_SetInstance_ID(Util.GetValueOfInt(dr[i]["VAM_PFeature_SetInstance_ID"]));
                                    base1 = Util.GetValueOfDecimal(dr[i]["Qty"]);
                                    lca.SetBase(base1);
                                    if (Env.Signum(mrPrice) != 0)
                                    {
                                        //result = Decimal.ToDouble(Decimal.Multiply(GetLineNetAmt(), mrPrice));
                                        result = Decimal.ToDouble(Decimal.Multiply(GetProductLineCost(this), mrPrice));
                                        result /= Decimal.ToDouble(total);
                                        lca.SetAmt(result, GetPrecision());
                                    }
                                    lca.SetQty(Util.GetValueOfDecimal(dr[i]["Qty"]));

                                    // Set Landed Cost Id and Warehouse ID on Landed Cost Allocation
                                    if (lca.Get_ColumnIndex("VAB_LCost_ID") > 0)
                                    {
                                        lca.SetVAB_LCost_ID(lc.Get_ID());
                                    }
                                    if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                                    {
                                        lca.SetVAM_Warehouse_ID(Util.GetValueOfInt(dr[i]["VAM_Warehouse_ID"]));
                                    }

                                    if (!lca.Save())
                                    {
                                        pp = VLogger.RetrieveError();
                                        if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                                        {
                                            return pp.GetName();
                                        }
                                        else
                                        {
                                            return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                                        }
                                    }
                                    inserted++;
                                }

                                log.Info("Inserted " + inserted);
                                AllocateLandedCostRounding();
                                return "";
                            }
                            else
                            {
                                // if No Matching Lines (with Product)
                                return Msg.GetMsg(GetCtx(), "NoMatchProduct") + "@VAB_Invoice_ID@";
                            }
                        }

                        //	Single Product
                        else if (lc.GetVAM_Product_ID() != 0)
                        {
                            // create landed cost allocation
                            MLandedCostAllocation lca = new MLandedCostAllocation(this, lc.GetVAM_ProductCostElement_ID());
                            lca.SetVAM_Product_ID(lc.GetVAM_Product_ID());	//	No ASI
                            //lca.SetAmt(GetLineNetAmt());
                            lca.SetAmt(GetProductLineCost(this));

                            // System distributes and allocates the Landed Cost of individual Product or variant, based on the quantity and amount defined for the Charge in the same Invoice Line.
                            lca.SetQty(GetQtyEntered());
                            lca.SetBase(GetQtyEntered());

                            if (lc.Get_ColumnIndex("VAM_PFeature_SetInstance_ID") > 0)
                            {
                                lca.SetVAM_PFeature_SetInstance_ID(lc.GetVAM_PFeature_SetInstance_ID());
                            }
                            if (!lca.Save())
                            {
                                pp = VLogger.RetrieveError();
                                if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                                {
                                    return pp.GetName();
                                }
                                else
                                {
                                    return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                                }
                            }
                            return "";
                        }
                        else
                        {
                            return Msg.GetMsg(GetCtx(), "NoReference") + lc;
                        }
                    }
                    #endregion

                    #region Landed cost allocation based on Receipt and Movement
                    else
                    {
                        //	Single Receipt Line
                        if (lc.GetVAM_Inv_InOutLine_ID() != 0)
                        {
                            MVAMInvInOut io = new MVAMInvInOut(GetCtx(), lc.GetVAM_Inv_InOut_ID(), Get_TrxName());
                            MVAMInvInOutLine iol = new MVAMInvInOutLine(GetCtx(), lc.GetVAM_Inv_InOutLine_ID(), Get_TrxName());

                            // if line is description only or without Product then it is invalid
                            if (iol.IsDescription() || iol.GetVAM_Product_ID() == 0)
                            {
                                return Msg.GetMsg(GetCtx(), "InvalidReceipt") + iol;
                            }

                            // create landed cost allocation
                            MLandedCostAllocation lca = new MLandedCostAllocation(this, lc.GetVAM_ProductCostElement_ID());
                            lca.SetVAM_Product_ID(iol.GetVAM_Product_ID());
                            lca.SetVAM_PFeature_SetInstance_ID(iol.GetVAM_PFeature_SetInstance_ID());
                            //lca.SetAmt(GetLineNetAmt());
                            lca.SetAmt(GetProductLineCost(this));
                            lca.SetBase(iol.GetBase(lc.GetLandedCostDistribution()));            // Get Base value based on Landed cost distribution
                            lca.SetQty(iol.GetMovementQty());

                            // Set Landed Cost Id and Warehouse ID on Landed Cost Allocation
                            if (lca.Get_ColumnIndex("VAB_LCost_ID") > 0)
                            {
                                lca.SetVAB_LCost_ID(lc.Get_ID());
                            }
                            if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                            {
                                lca.SetVAM_Warehouse_ID(io.GetVAM_Warehouse_ID());
                            }
                            if (lca.Get_ColumnIndex("VAM_Inv_InOutLine_ID") > 0)
                            {
                                lca.SetVAM_Inv_InOutLine_ID(iol.GetVAM_Inv_InOutLine_ID());
                            }
                            // get difference of (expected - actual) landed cost allocation amount if have
                            Decimal diffrenceAmt = 0;
                            if (iol.GetVAB_OrderLine_ID() > 0)
                            {
                                int VAB_ExpectedCost_ID = GetExpectedLandedCostId(lc, iol.GetVAB_OrderLine_ID());
                                if (VAB_ExpectedCost_ID > 0)
                                {
                                    diffrenceAmt = GetLandedCostDifferenceAmt(lc, iol.GetVAM_Inv_InOutLine_ID(), iol.GetMovementQty(), lca.GetAmt(), VAB_ExpectedCost_ID, GetPrecision());
                                    lca.SetIsExpectedCostCalculated(true);
                                }
                            }
                            if (lca.Get_ColumnIndex("DifferenceAmt") > 0)
                            {
                                lca.SetDifferenceAmt(Decimal.Round(diffrenceAmt, GetPrecision()));
                            }
                            if (!lca.Save())
                            {
                                pp = VLogger.RetrieveError();
                                if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                                {
                                    return pp.GetName();
                                }
                                else
                                {
                                    return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                                }
                            }
                            return "";
                        }

                        // All Receipt Lines
                        else if (lc.GetVAM_Inv_InOut_ID() != 0)
                        {
                            //	Create List
                            List<MVAMInvInOutLine> list = new List<MVAMInvInOutLine>();
                            MVAMInvInOut ship = new MVAMInvInOut(GetCtx(), lc.GetVAM_Inv_InOut_ID(), Get_TrxName());
                            MVAMInvInOutLine[] lines = ship.GetLines();
                            Decimal total = Env.ZERO;

                            for (int i = 0; i < lines.Length; i++)
                            {
                                // if line is description only or without Product then skip the line
                                if (lines[i].IsDescription() || lines[i].GetVAM_Product_ID() == 0)
                                    continue;

                                //System consider the combination of Product & Attribute Set Instance for updating Landed Cost on Current cost of the Product.
                                if (lc.Get_ColumnIndex("VAM_PFeature_SetInstance_ID") > 0 && lc.GetVAM_PFeature_SetInstance_ID() > 0)
                                {
                                    if (lc.GetVAM_Product_ID() == 0
                                        || (lc.GetVAM_Product_ID() == lines[i].GetVAM_Product_ID() && lc.GetVAM_PFeature_SetInstance_ID() == lines[i].GetVAM_PFeature_SetInstance_ID()))
                                    {
                                        list.Add(lines[i]);
                                        total = Decimal.Add(total, lines[i].GetBase(lc.GetLandedCostDistribution()));         //	Calculate total 
                                    }
                                }
                                else
                                {
                                    if (lc.GetVAM_Product_ID() == 0
                                        || lc.GetVAM_Product_ID() == lines[i].GetVAM_Product_ID())
                                    {
                                        list.Add(lines[i]);
                                        total = Decimal.Add(total, lines[i].GetBase(lc.GetLandedCostDistribution()));           //	Calculate total
                                    }
                                }
                            }

                            // if No Matching Lines (with Product)
                            if (list.Count == 0)
                            {
                                return Msg.GetMsg(GetCtx(), "NoMatchProduct") + "@VAM_Inv_InOut@";
                            }

                            //	Calculate total & base

                            //for (int i = 0; i < list.Count; i++)
                            //{
                            //    MVAMInvInOutLine iol = (MVAMInvInOutLine)list[i];
                            //    total = Decimal.Add(total, iol.GetBase(lc.GetLandedCostDistribution()));
                            //}

                            // if Total of Base values is 0
                            if (Env.Signum(total) == 0)
                            {
                                return Msg.GetMsg(GetCtx(), "TotalBaseZero") + lc.GetLandedCostDistribution();
                            }

                            //	Create Allocations
                            MVAMInvInOutLine iol = null;
                            MLandedCostAllocation lca = null;
                            Decimal base1 = 0;
                            double result = 0;
                            Decimal diffrenceAmt = 0;
                            int VAB_ExpectedCost_ID = 0;

                            for (int i = 0; i < list.Count; i++)
                            {
                                iol = (MVAMInvInOutLine)list[i];
                                lca = new MLandedCostAllocation(this,
                                    lc.GetVAM_ProductCostElement_ID());
                                lca.SetVAM_Product_ID(iol.GetVAM_Product_ID());
                                lca.SetVAM_PFeature_SetInstance_ID(iol.GetVAM_PFeature_SetInstance_ID());
                                base1 = iol.GetBase(lc.GetLandedCostDistribution());            // Get Base value based on Landed cost distribution
                                lca.SetBase(base1);
                                if (Env.Signum(base1) != 0)
                                {
                                    //result = Decimal.ToDouble(Decimal.Multiply(GetLineNetAmt(), base1));
                                    result = Decimal.ToDouble(Decimal.Multiply(GetProductLineCost(this), base1));
                                    result /= Decimal.ToDouble(total);
                                    lca.SetAmt(result, GetPrecision());
                                }
                                lca.SetQty(iol.GetMovementQty());

                                // Set Landed Cost Id and Warehouse ID on Landed Cost Allocation
                                if (lca.Get_ColumnIndex("VAB_LCost_ID") > 0)
                                {
                                    lca.SetVAB_LCost_ID(lc.Get_ID());
                                }
                                if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                                {
                                    lca.SetVAM_Warehouse_ID(ship.GetVAM_Warehouse_ID());
                                }
                                if (lca.Get_ColumnIndex("VAM_Inv_InOutLine_ID") > 0)
                                {
                                    lca.SetVAM_Inv_InOutLine_ID(iol.GetVAM_Inv_InOutLine_ID());
                                }
                                // get difference of (expected - actual) landed cost allocation amount if have
                                if (iol.GetVAB_OrderLine_ID() > 0)
                                {
                                    VAB_ExpectedCost_ID = GetExpectedLandedCostId(lc, iol.GetVAB_OrderLine_ID());
                                    if (VAB_ExpectedCost_ID > 0)
                                    {
                                        diffrenceAmt = GetLandedCostDifferenceAmt(lc, iol.GetVAM_Inv_InOutLine_ID(), iol.GetMovementQty(), lca.GetAmt(), VAB_ExpectedCost_ID, GetPrecision());
                                        lca.SetIsExpectedCostCalculated(true);
                                    }
                                }
                                if (lca.Get_ColumnIndex("DifferenceAmt") > 0)
                                {
                                    lca.SetDifferenceAmt(Decimal.Round(diffrenceAmt, GetPrecision()));
                                }
                                if (!lca.Save())
                                {
                                    pp = VLogger.RetrieveError();
                                    if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                                    {
                                        return pp.GetName();
                                    }
                                    else
                                    {
                                        return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                                    }
                                }
                                inserted++;
                            }
                            log.Info("Inserted " + inserted);
                            AllocateLandedCostRounding();
                            return "";
                        }

                        //	Single Movement Line
                        else if (hasMovement > 0 && lc.GetVAM_InvTrf_Line_ID() != 0)
                        {
                            MMovement mov = new MMovement(GetCtx(), lc.GetVAM_InventoryTransfer_ID(), Get_TrxName());
                            MMovementLine iol = new MMovementLine(GetCtx(), lc.GetVAM_InvTrf_Line_ID(), Get_TrxName());
                            MLocator loc = new MLocator(GetCtx(), iol.GetVAM_LocatorTo_ID(), Get_TrxName());

                            // if line is without Product then it is invalid
                            if (iol.GetVAM_Product_ID() == 0)
                            {
                                return Msg.GetMsg(GetCtx(), "InvalidMovement") + iol;
                            }

                            // create landed cost allocation
                            MLandedCostAllocation lca = new MLandedCostAllocation(this, lc.GetVAM_ProductCostElement_ID());
                            lca.SetVAM_Product_ID(iol.GetVAM_Product_ID());
                            lca.SetVAM_PFeature_SetInstance_ID(iol.GetVAM_PFeature_SetInstance_ID());
                            //lca.SetAmt(GetLineNetAmt());
                            lca.SetAmt(GetProductLineCost(this));
                            lca.SetBase(iol.GetBase(lc.GetLandedCostDistribution()));            // Get Base value based on Landed cost distribution
                            lca.SetQty(iol.GetMovementQty());

                            // Set Landed Cost Id and Warehouse ID on Landed Cost Allocation
                            if (lca.Get_ColumnIndex("VAB_LCost_ID") > 0)
                            {
                                lca.SetVAB_LCost_ID(lc.Get_ID());
                            }
                            if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                            {
                                lca.SetVAM_Warehouse_ID(mov.GetVAM_Warehouse_ID() > 0 ? mov.GetVAM_Warehouse_ID() : loc.GetVAM_Warehouse_ID());
                            }

                            if (!lca.Save())
                            {
                                pp = VLogger.RetrieveError();
                                if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                                {
                                    return pp.GetName();
                                }
                                else
                                {
                                    return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                                }
                            }
                            return "";
                        }

                        // All movement Lines
                        else if (hasMovement > 0 && lc.GetVAM_InventoryTransfer_ID() != 0)
                        {
                            //	Create List
                            List<MMovementLine> list = new List<MMovementLine>();
                            MMovement mov = new MMovement(GetCtx(), lc.GetVAM_InventoryTransfer_ID(), Get_TrxName());
                            MMovementLine[] lines = mov.GetLines(true);
                            MLocator loc = null;
                            Decimal total = Env.ZERO;

                            for (int i = 0; i < lines.Length; i++)
                            {
                                // if line is description only or without Product then skip it.
                                if (lines[i].GetVAM_Product_ID() == 0)
                                    continue;

                                //System consider the combination of Product & Attribute Set Instance for updating Landed Cost on Current cost of the Product.
                                if (lc.Get_ColumnIndex("VAM_PFeature_SetInstance_ID") > 0 && lc.GetVAM_PFeature_SetInstance_ID() > 0)
                                {
                                    if (lc.GetVAM_Product_ID() == 0
                                        || (lc.GetVAM_Product_ID() == lines[i].GetVAM_Product_ID() && lc.GetVAM_PFeature_SetInstance_ID() == lines[i].GetVAM_PFeature_SetInstance_ID()))
                                    {
                                        list.Add(lines[i]);
                                        total = Decimal.Add(total, lines[i].GetBase(lc.GetLandedCostDistribution()));         //	Calculate total 
                                    }
                                }
                                else
                                {
                                    if (lc.GetVAM_Product_ID() == 0
                                        || lc.GetVAM_Product_ID() == lines[i].GetVAM_Product_ID())
                                    {
                                        list.Add(lines[i]);
                                        total = Decimal.Add(total, lines[i].GetBase(lc.GetLandedCostDistribution()));               //	Calculate total
                                    }
                                }
                            }

                            // if No Matching Lines (with Product)
                            if (list.Count == 0)
                            {
                                return Msg.GetMsg(GetCtx(), "NoMatchProduct") + "@VAM_InventoryTransfer_ID@";
                            }

                            //	Calculate total & base
                            //Decimal total = Env.ZERO;
                            //for (int i = 0; i < list.Count; i++)
                            //{
                            //    MMovementLine iol = (MMovementLine)list[i];
                            //    total = Decimal.Add(total, iol.GetBase(lc.GetLandedCostDistribution()));
                            //}

                            // if Total of Base values is 0
                            if (Env.Signum(total) == 0)
                            {
                                return Msg.GetMsg(GetCtx(), "TotalBaseZero") + lc.GetLandedCostDistribution();
                            }

                            //	Create Allocations
                            MMovementLine iol = null;
                            MLandedCostAllocation lca = null;
                            Decimal base1 = 0;
                            double result = 0;

                            for (int i = 0; i < list.Count; i++)
                            {
                                iol = (MMovementLine)list[i];
                                loc = new MLocator(GetCtx(), iol.GetVAM_LocatorTo_ID(), Get_TrxName());
                                lca = new MLandedCostAllocation(this,
                                    lc.GetVAM_ProductCostElement_ID());
                                lca.SetVAM_Product_ID(iol.GetVAM_Product_ID());
                                lca.SetVAM_PFeature_SetInstance_ID(iol.GetVAM_PFeature_SetInstance_ID());
                                base1 = iol.GetBase(lc.GetLandedCostDistribution());                // Get Base value based on Landed cost distribution
                                lca.SetBase(base1);
                                if (Env.Signum(base1) != 0)
                                {
                                    //result = Decimal.ToDouble(Decimal.Multiply(GetLineNetAmt(), base1));
                                    result = Decimal.ToDouble(Decimal.Multiply(GetProductLineCost(this), base1));
                                    result /= Decimal.ToDouble(total);
                                    lca.SetAmt(result, GetPrecision());
                                }
                                lca.SetQty(iol.GetMovementQty());

                                // Set Landed Cost Id and Warehouse ID on Landed Cost Allocation
                                if (lca.Get_ColumnIndex("VAB_LCost_ID") > 0)
                                {
                                    lca.SetVAB_LCost_ID(lc.Get_ID());
                                }
                                if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                                {
                                    lca.SetVAM_Warehouse_ID(mov.GetVAM_Warehouse_ID() > 0 ? mov.GetVAM_Warehouse_ID() : loc.GetVAM_Warehouse_ID());
                                }

                                if (!lca.Save())
                                {
                                    pp = VLogger.RetrieveError();
                                    if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                                    {
                                        return pp.GetName();
                                    }
                                    else
                                    {
                                        return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                                    }
                                }
                                inserted++;
                            }
                            log.Info("Inserted " + inserted);
                            AllocateLandedCostRounding();
                            return "";
                        }
                        //	Single Product
                        else if (lc.GetVAM_Product_ID() != 0)
                        {
                            // Craete landed cost allocation
                            MLandedCostAllocation lca = new MLandedCostAllocation(this, lc.GetVAM_ProductCostElement_ID());
                            lca.SetVAM_Product_ID(lc.GetVAM_Product_ID());	//	No ASI
                            //lca.SetAmt(GetLineNetAmt());
                            lca.SetAmt(GetProductLineCost(this));

                            // System distributes and allocates the Landed Cost of individual Product or variant, based on the quantity and amount defined for the Charge in the same Invoice Line.
                            lca.SetQty(GetQtyEntered());
                            lca.SetBase(GetQtyEntered());

                            if (lc.Get_ColumnIndex("VAM_PFeature_SetInstance_ID") > 0)
                            {
                                lca.SetVAM_PFeature_SetInstance_ID(lc.GetVAM_PFeature_SetInstance_ID());
                            }

                            if (!lca.Save())
                            {
                                pp = VLogger.RetrieveError();
                                if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                                {
                                    return pp.GetName();
                                }
                                else
                                {
                                    return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                                }
                            }
                            return "";
                        }
                        else
                        {
                            return Msg.GetMsg(GetCtx(), "NoReference") + lc;
                        }
                    }
                    #endregion
                }

                //	*** Multiple Criteria ***
                String LandedCostDistribution = lcs[0].GetLandedCostDistribution();
                int VAM_ProductCostElement_ID = lcs[0].GetVAM_ProductCostElement_ID();
                int VAM_Inv_InOut_ID = lcs[0].GetVAM_Inv_InOut_ID();
                int VAM_InventoryTransfer_ID = hasMovement > 0 ? lcs[0].GetVAM_InventoryTransfer_ID() : 0;
                for (int i = 0; i < lcs.Length; i++)
                {
                    MLandedCost lc = lcs[i];

                    // Multiple Landed Cost Rules must have consistent Landed Cost Distribution
                    if (!LandedCostDistribution.Equals(lc.GetLandedCostDistribution()))
                    {
                        return Msg.GetMsg(GetCtx(), "LandedCostDistribution");
                    }

                    // Multiple Landed Cost Rules cannot directly allocate to a Product
                    if (LandedCostDistribution.Equals(MLandedCost.LANDEDCOSTDISTRIBUTION_ImportValue))
                    {
                        if (lc.GetVAM_Product_ID() != 0 && lc.GetRef_Invoice_ID() == 0 && lc.GetRef_InvoiceLine_ID() == 0)
                        {
                            return Msg.GetMsg(GetCtx(), "MultiLandedCostProduct");
                        }
                    }
                    if (lc.Get_ColumnIndex("VAM_InventoryTransfer_ID") > 0)
                    {
                        if (lc.GetVAM_Product_ID() != 0 && lc.GetVAM_Inv_InOut_ID() == 0 && lc.GetVAM_Inv_InOutLine_ID() == 0
                            && lc.GetVAM_InventoryTransfer_ID() == 0 && lc.GetVAM_InvTrf_Line_ID() == 0)
                        {
                            return Msg.GetMsg(GetCtx(), "MultiLandedCostProduct");
                        }
                    }

                    else if (lc.GetVAM_Product_ID() != 0 && lc.GetVAM_Inv_InOut_ID() == 0 && lc.GetVAM_Inv_InOutLine_ID() == 0)
                    {
                        return Msg.GetMsg(GetCtx(), "MultiLandedCostProduct");
                    }

                    // Multiple Landed Cost Rules cannot have different Cost Elements
                    if (VAM_ProductCostElement_ID != lc.GetVAM_ProductCostElement_ID())
                    {
                        return Msg.GetMsg(GetCtx(), "LandedCostElement");
                    }

                    // Multiple Landed Cost Rules must have consistent Reference like (Receipt, Movement)
                    if (hasMovement > 0 && !LandedCostDistribution.Equals(MLandedCost.LANDEDCOSTDISTRIBUTION_ImportValue))
                    {
                        if (VAM_Inv_InOut_ID > 0 && lc.GetVAM_Inv_InOut_ID() == 0)
                        {
                            return Msg.GetMsg(GetCtx(), "LandedCostReference");
                        }

                        if (VAM_InventoryTransfer_ID > 0 && lc.GetVAM_InventoryTransfer_ID() == 0)
                        {
                            return Msg.GetMsg(GetCtx(), "LandedCostReference");
                        }
                    }
                }

                //	Create List
                #region if Landed cost Distribution is - Import Value
                if (LandedCostDistribution == MLandedCost.LANDEDCOSTDISTRIBUTION_ImportValue)
                {
                    List<MVABInvoiceLine> list1 = new List<MVABInvoiceLine>();
                    //MVABInvoice inv = null;
                    //MVABInvoiceLine[] lines = null;
                    //MVABInvoiceLine iol = null;
                    Decimal total1 = Env.ZERO;
                    decimal mrPrice = Env.ZERO;
                    List<DataRow> dr = new List<DataRow>();

                    for (int ii = 0; ii < lcs.Length; ii++)
                    {
                        MLandedCost lc = lcs[ii];

                        qry.Clear();
                        qry.Append(@"SELECT il.VAM_Product_ID, il.VAM_PFeature_SetInstance_ID, sum(mi.Qty) as Qty, ");
                        //SUM(mi.Qty * il.PriceActual) AS LineNetAmt,
                        qry.Append(@" SUM(mi.Qty *
                                      CASE
                                        WHEN NVL(C_SurChargeTax.IsIncludeInCost , 'N') = 'Y'
                                        AND NVL(VAB_TaxRate.IsIncludeInCost , 'N')           = 'Y'
                                        THEN ROUND((il.taxbaseamt + il.taxamt + il.surchargeamt) / il.qtyinvoiced , 4)
                                        WHEN NVL(C_SurChargeTax.IsIncludeInCost , 'N') = 'N'
                                        AND NVL(VAB_TaxRate.IsIncludeInCost , 'N')           = 'Y'
                                        THEN ROUND((il.taxbaseamt + il.taxamt) / il.qtyinvoiced , 4)
                                        WHEN NVL(C_SurChargeTax.IsIncludeInCost , 'N') = 'Y'
                                        AND NVL(VAB_TaxRate.IsIncludeInCost , 'N')           = 'N'
                                        THEN ROUND((il.taxbaseamt + il.surchargeamt) / il.qtyinvoiced, 4)
                                        ELSE ROUND(il.taxbaseamt  / il.qtyinvoiced, 4)
                                      END) AS LineNetAmt , io.VAM_Warehouse_ID
                            FROM VAB_InvoiceLine il INNER JOIN VAM_MatchInvoice mi ON Mi.VAB_InvoiceLine_ID = Il.VAB_InvoiceLine_ID INNER JOIN VAM_Inv_InOutLine iol ON iol.VAM_Inv_InOutLine_ID = mi.VAM_Inv_InOutLine_ID
                            INNER JOIN VAM_Inv_InOut io ON io.VAM_Inv_InOut_ID = iol.VAM_Inv_InOut_ID INNER JOIN VAM_Warehouse wh ON wh.VAM_Warehouse_ID = io.VAM_Warehouse_ID 
                            INNER JOIN VAB_TaxRate VAB_TaxRate ON VAB_TaxRate.VAB_TaxRate_ID = il.VAB_TaxRate_ID
                            LEFT JOIN VAB_TaxRate C_SurChargeTax ON VAB_TaxRate.Surcharge_Tax_ID = C_SurChargeTax.VAB_TaxRate_ID 
                            WHERE il.VAB_Invoice_ID = " + lc.GetRef_Invoice_ID());

                        //	Single Invoice Line
                        if (lc.GetRef_InvoiceLine_ID() != 0)
                        {
                            qry.Append(" AND il.VAB_InvoiceLine_ID = " + lc.GetRef_InvoiceLine_ID());
                        }

                        if (lc.GetVAM_Product_ID() > 0)
                        {
                            qry.Append(" AND il.VAM_Product_ID = " + lc.GetVAM_Product_ID());
                        }

                        if (lc.Get_ColumnIndex("VAM_PFeature_SetInstance_ID") > 0 && lc.GetVAM_PFeature_SetInstance_ID() > 0)
                        {
                            qry.Append(" AND il.VAM_PFeature_SetInstance_ID = " + lc.GetVAM_PFeature_SetInstance_ID());
                        }

                        qry.Append(" GROUP BY il.VAM_Product_ID, il.VAM_PFeature_SetInstance_ID, io.VAM_Warehouse_ID ," +
                            "  il.taxbaseamt , il.taxamt , il.surchargeamt , C_SurChargeTax.IsIncludeInCost , VAB_TaxRate.IsIncludeInCost, il.qtyinvoiced ");

                        ds = DB.ExecuteDataset(qry.ToString(), null, Get_TrxName());

                        if (ds != null && ds.Tables[0].Rows.Count > 0)
                        {
                            //total1 = Env.ZERO;
                            //	Calculate total & base
                            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                            {
                                total1 = Decimal.Add(total1, Util.GetValueOfDecimal(ds.Tables[0].Rows[i]["LineNetAmt"]));
                                dr.Add(ds.Tables[0].Rows[i]);
                            }
                            ds.Dispose();
                        }
                    }

                    // if No Matching Lines (with Product)
                    if (dr.Count == 0)
                    {
                        return Msg.GetMsg(GetCtx(), "NoMatchProduct") + "@VAB_Invoice_ID@";
                    }

                    // if Total of Base values is 0
                    if (Env.Signum(total1) == 0)
                    {
                        return Msg.GetMsg(GetCtx(), "TotalBaseZero") + LandedCostDistribution;
                    }

                    //	Create Allocations
                    //MVABInvoiceLine inl = null;
                    MLandedCostAllocation lca = null;
                    Decimal base1 = 0;
                    double result = 0;

                    for (int i = 0; i < dr.Count; i++)
                    {
                        mrPrice = Util.GetValueOfDecimal(dr[i]["LineNetAmt"]);

                        //inl = (MVABInvoiceLine)list1[i];
                        lca = new MLandedCostAllocation(this, lcs[0].GetVAM_ProductCostElement_ID());
                        lca.SetVAM_Product_ID(Util.GetValueOfInt(dr[i]["VAM_Product_ID"]));
                        lca.SetVAM_PFeature_SetInstance_ID(Util.GetValueOfInt(dr[i]["VAM_PFeature_SetInstance_ID"]));
                        base1 = Util.GetValueOfDecimal(dr[i]["Qty"]);
                        lca.SetBase(base1);
                        if (Env.Signum(mrPrice) != 0)
                        {
                            //result = Decimal.ToDouble(Decimal.Multiply(GetLineNetAmt(), mrPrice));
                            result = Decimal.ToDouble(Decimal.Multiply(GetProductLineCost(this), mrPrice));
                            result /= Decimal.ToDouble(total1);
                            lca.SetAmt(result, GetPrecision());
                        }
                        lca.SetQty(Util.GetValueOfDecimal(dr[i]["Qty"]));

                        // Set Landed Cost Id and Warehouse ID on Landed Cost Allocation
                        if (lca.Get_ColumnIndex("VAB_LCost_ID") > 0)
                        {
                            lca.SetVAB_LCost_ID(lcs[0].Get_ID());
                        }
                        if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                        {
                            lca.SetVAM_Warehouse_ID(Util.GetValueOfInt(dr[i]["VAM_Warehouse_ID"]));
                        }

                        if (!lca.Save())
                        {
                            pp = VLogger.RetrieveError();
                            if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                            {
                                return pp.GetName();
                            }
                            else
                            {
                                return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                            }
                        }
                        inserted++;
                    }
                }
                #endregion

                #region   if Landed cost is distributed based on Receipt .
                else if (VAM_Inv_InOut_ID > 0)
                {
                    List<MVAMInvInOutLine> list1 = new List<MVAMInvInOutLine>();
                    MVAMInvInOut ship = null;
                    MVAMInvInOutLine[] lines = null;
                    MVAMInvInOutLine iol = null;
                    Decimal total1 = Env.ZERO;

                    for (int ii = 0; ii < lcs.Length; ii++)
                    {
                        MLandedCost lc = lcs[ii];
                        if (lc.GetVAM_Inv_InOut_ID() != 0 && lc.GetVAM_Inv_InOutLine_ID() == 0)		//	entire receipt
                        {
                            ship = new MVAMInvInOut(GetCtx(), lc.GetVAM_Inv_InOut_ID(), Get_TrxName());
                            lines = ship.GetLines();
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].IsDescription()		//	decription or no product then skip the line
                                    || lines[i].GetVAM_Product_ID() == 0)
                                    continue;

                                //System consider the combination of Product & Attribute Set Instance for updating Landed Cost on Current cost of the Product.
                                if (lc.Get_ColumnIndex("VAM_PFeature_SetInstance_ID") > 0 && lc.GetVAM_PFeature_SetInstance_ID() > 0)
                                {
                                    if (lc.GetVAM_Product_ID() == 0
                                        || (lc.GetVAM_Product_ID() == lines[i].GetVAM_Product_ID() && lc.GetVAM_PFeature_SetInstance_ID() == lines[i].GetVAM_PFeature_SetInstance_ID()))
                                    {
                                        list1.Add(lines[i]);
                                        total1 = Decimal.Add(total1, lines[i].GetBase(lc.GetLandedCostDistribution()));         //	Calculate total 
                                    }
                                }
                                else
                                {
                                    if (lc.GetVAM_Product_ID() == 0		//	no restriction or product match
                                        || lc.GetVAM_Product_ID() == lines[i].GetVAM_Product_ID())
                                    {
                                        list1.Add(lines[i]);
                                        total1 = Decimal.Add(total1, lines[i].GetBase(LandedCostDistribution));             //	Calculate total & base
                                    }
                                }
                            }
                        }
                        else if (lc.GetVAM_Inv_InOutLine_ID() != 0)	//	receipt line
                        {
                            iol = new MVAMInvInOutLine(GetCtx(), lc.GetVAM_Inv_InOutLine_ID(), Get_TrxName());

                            // if line is description only or without Product then skip it.
                            if (!iol.IsDescription() && iol.GetVAM_Product_ID() != 0)
                            {
                                list1.Add(iol);
                                total1 = Decimal.Add(total1, iol.GetBase(LandedCostDistribution));                      //	Calculate total & base
                            }
                        }
                    }

                    // if No Matching Lines (with Product)
                    if (list1.Count == 0)
                    {
                        return Msg.GetMsg(GetCtx(), "NoMatchProduct") + "@VAM_Inv_InOut_ID@";
                    }

                    //	Calculate total & base

                    //for (int i = 0; i < list1.Count; i++)
                    //{
                    //    MVAMInvInOutLine iol = (MVAMInvInOutLine)list1[i];
                    //    total1 = Decimal.Add(total1, iol.GetBase(LandedCostDistribution));
                    //}

                    // if Total of Base values is 0
                    if (Env.Signum(total1) == 0)
                    {
                        return Msg.GetMsg(GetCtx(), "TotalBaseZero") + LandedCostDistribution;
                    }

                    //	Create Allocations
                    MVAMInvInOut ino = null;
                    MVAMInvInOutLine inl = null;
                    MLandedCostAllocation lca = null;
                    Decimal base1 = 0;
                    double result = 0;

                    for (int i = 0; i < list1.Count; i++)
                    {
                        inl = (MVAMInvInOutLine)list1[i];
                        ino = new MVAMInvInOut(GetCtx(), inl.GetVAM_Inv_InOut_ID(), Get_TrxName());
                        lca = new MLandedCostAllocation(this, lcs[0].GetVAM_ProductCostElement_ID());
                        lca.SetVAM_Product_ID(inl.GetVAM_Product_ID());
                        lca.SetVAM_PFeature_SetInstance_ID(inl.GetVAM_PFeature_SetInstance_ID());
                        base1 = inl.GetBase(LandedCostDistribution);                   // Get Base value for Cost Distribution
                        lca.SetBase(base1);
                        if (Env.Signum(base1) != 0)
                        {
                            //result = Decimal.ToDouble(Decimal.Multiply(GetLineNetAmt(), base1));
                            result = Decimal.ToDouble(Decimal.Multiply(GetProductLineCost(this), base1));
                            result /= Decimal.ToDouble(total1);
                            lca.SetAmt(result, GetPrecision());
                        }
                        lca.SetQty(inl.GetMovementQty());

                        // Set Landed Cost Id and Warehouse ID on Landed Cost Allocation
                        if (lca.Get_ColumnIndex("VAB_LCost_ID") > 0)
                        {
                            lca.SetVAB_LCost_ID(lcs[i].Get_ID());
                        }
                        if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                        {
                            lca.SetVAM_Warehouse_ID(ino.GetVAM_Warehouse_ID());
                        }

                        if (lca.Get_ColumnIndex("VAM_Inv_InOutLine_ID") > 0)
                        {
                            lca.SetVAM_Inv_InOutLine_ID(inl.GetVAM_Inv_InOutLine_ID());
                        }
                        // get difference of (expected - actual) landed cost allocation amount if have
                        Decimal diffrenceAmt = 0;
                        if (inl.GetVAB_OrderLine_ID() > 0)
                        {
                            int VAB_ExpectedCost_ID = GetExpectedLandedCostId(lcs[0], inl.GetVAB_OrderLine_ID());
                            if (VAB_ExpectedCost_ID > 0)
                            {
                                diffrenceAmt = GetLandedCostDifferenceAmt(lcs[0], inl.GetVAM_Inv_InOutLine_ID(), inl.GetMovementQty(), lca.GetAmt(), VAB_ExpectedCost_ID, GetPrecision());
                                lca.SetIsExpectedCostCalculated(true);
                            }
                        }
                        if (lca.Get_ColumnIndex("DifferenceAmt") > 0)
                        {
                            lca.SetDifferenceAmt(Decimal.Round(diffrenceAmt, GetPrecision()));
                        }

                        if (!lca.Save())
                        {
                            pp = VLogger.RetrieveError();
                            if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                            {
                                return pp.GetName();
                            }
                            else
                            {
                                return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                            }
                        }
                        inserted++;
                    }
                }
                #endregion

                #region  if Landed cost is distributed based on Movement .
                else if (VAM_InventoryTransfer_ID > 0)
                {
                    List<MMovementLine> list1 = new List<MMovementLine>();
                    MMovement mov = null;
                    MMovementLine[] lines = null;
                    MMovementLine iol = null;
                    Decimal total1 = Env.ZERO;

                    for (int ii = 0; ii < lcs.Length; ii++)
                    {
                        MLandedCost lc = lcs[ii];
                        if (lc.GetVAM_InventoryTransfer_ID() != 0 && lc.GetVAM_InvTrf_Line_ID() == 0)		//	entire receipt
                        {
                            mov = new MMovement(GetCtx(), lc.GetVAM_InventoryTransfer_ID(), Get_TrxName());
                            lines = mov.GetLines(true);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                // if line is without Product then skip the line
                                if (lines[i].GetVAM_Product_ID() == 0)
                                    continue;

                                //System consider the combination of Product & Attribute Set Instance for updating Landed Cost on Current cost of the Product.
                                if (lc.Get_ColumnIndex("VAM_PFeature_SetInstance_ID") > 0 && lc.GetVAM_PFeature_SetInstance_ID() > 0)
                                {
                                    if (lc.GetVAM_Product_ID() == 0
                                        || (lc.GetVAM_Product_ID() == lines[i].GetVAM_Product_ID() && lc.GetVAM_PFeature_SetInstance_ID() == lines[i].GetVAM_PFeature_SetInstance_ID()))
                                    {
                                        list1.Add(lines[i]);
                                        total1 = Decimal.Add(total1, lines[i].GetBase(lc.GetLandedCostDistribution()));         //	Calculate total 
                                    }
                                }
                                else
                                {
                                    if (lc.GetVAM_Product_ID() == 0		//	no restriction or product match
                                        || lc.GetVAM_Product_ID() == lines[i].GetVAM_Product_ID())
                                    {
                                        list1.Add(lines[i]);
                                        total1 = Decimal.Add(total1, lines[i].GetBase(LandedCostDistribution));         //	Calculate total & base
                                    }
                                }
                            }
                        }
                        else if (lc.GetVAM_InvTrf_Line_ID() != 0)	//	receipt line
                        {
                            iol = new MMovementLine(GetCtx(), lc.GetVAM_InvTrf_Line_ID(), Get_TrxName());

                            // if line is without Product then skip the line
                            if (iol.GetVAM_Product_ID() != 0)
                            {
                                list1.Add(iol);
                                total1 = Decimal.Add(total1, iol.GetBase(LandedCostDistribution));                  //	Calculate total & base
                            }
                        }
                    }

                    // if No Matching Lines (with Product)
                    if (list1.Count == 0)
                    {
                        return Msg.GetMsg(GetCtx(), "NoMatchProduct") + "@VAM_InventoryTransfer_ID@";
                    }

                    //	Calculate total & base

                    //for (int i = 0; i < list1.Count; i++)
                    //{
                    //    MMovementLine iol = (MMovementLine)list1[i];
                    //    total1 = Decimal.Add(total1, iol.GetBase(LandedCostDistribution));
                    //}

                    // if Total of Base values is 0
                    if (Env.Signum(total1) == 0)
                    {
                        return Msg.GetMsg(GetCtx(), "TotalBaseZero") + LandedCostDistribution;
                    }

                    //	Create Allocations
                    MMovementLine iml = null;
                    MLocator loc = null;
                    MLandedCostAllocation lca = null;
                    Decimal base1 = 0;
                    double result = 0;

                    for (int i = 0; i < list1.Count; i++)
                    {
                        iml = (MMovementLine)list1[i];
                        mov = new MMovement(GetCtx(), iml.GetVAM_InventoryTransfer_ID(), Get_TrxName());
                        loc = new MLocator(GetCtx(), iml.GetVAM_LocatorTo_ID(), Get_TrxName());
                        lca = new MLandedCostAllocation(this, lcs[0].GetVAM_ProductCostElement_ID());
                        lca.SetVAM_Product_ID(iml.GetVAM_Product_ID());
                        lca.SetVAM_PFeature_SetInstance_ID(iml.GetVAM_PFeature_SetInstance_ID());
                        base1 = iml.GetBase(LandedCostDistribution);                            // Get Base value for Cost Distribution
                        lca.SetBase(base1);

                        // Set Landed Cost Id and Warehouse ID on Landed Cost Allocation
                        if (lca.Get_ColumnIndex("VAB_LCost_ID") > 0)
                        {
                            lca.SetVAB_LCost_ID(lcs[i].Get_ID());
                        }
                        if (lca.Get_ColumnIndex("VAM_Warehouse_ID") > 0)
                        {
                            lca.SetVAM_Warehouse_ID(mov.GetVAM_Warehouse_ID() > 0 ? mov.GetVAM_Warehouse_ID() : loc.GetVAM_Warehouse_ID());
                        }

                        if (Env.Signum(base1) != 0)
                        {
                            //result = Decimal.ToDouble(Decimal.Multiply(GetLineNetAmt(), base1));
                            result = Decimal.ToDouble(Decimal.Multiply(GetProductLineCost(this), base1));
                            result /= Decimal.ToDouble(total1);
                            lca.SetAmt(result, GetPrecision());
                        }
                        lca.SetQty(iml.GetMovementQty());
                        if (!lca.Save())
                        {
                            pp = VLogger.RetrieveError();
                            if (pp != null && !String.IsNullOrEmpty(pp.GetName()))
                            {
                                return pp.GetName();
                            }
                            else
                            {
                                return Msg.GetMsg(GetCtx(), "LandedCostAllocNotSaved");
                            }
                        }
                        inserted++;
                    }
                }
                #endregion
                log.Info("Inserted " + inserted);
                AllocateLandedCostRounding();
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--AllocateLandedCosts");
            }
            return "";
        }

        /// <summary>
        /// This function is used to get difference value between expecetd landed cost and actual landed cost invoice
        /// </summary>
        /// <param name="lc">landed cost</param>
        /// <param name="VAM_Inv_InOutLine_ID">receipt line</param>
        /// <param name="Quantity">receipt movement qty</param>
        /// <param name="ActualLandeCost">actual landed cost value</param>
        /// <param name="VAB_ExpectedCost_ID">expecetde landed cost</param>
        /// <param name="precision">standard precison</param>
        /// <returns>diffrence value</returns>
        private Decimal GetLandedCostDifferenceAmt(MLandedCost lc, int VAM_Inv_InOutLine_ID, Decimal Quantity, Decimal ActualLandeCost, int VAB_ExpectedCost_ID, int precision)
        {
            Decimal differenceAmt = 0.0M;
            // get expected freight amount of each (round upto 15 in query only) 
            String sql = @"Select CASE When VAB_ExpectedCostDis.Qty = 0 THEN 0
                            ELSE  ROUND(VAB_ExpectedCostDis.Amt / VAB_ExpectedCostDis.Qty , 15) 
                            END AS Amt , CASE WHEN VAM_Product.IsCostAdjustmentOnLost='Y' THEN VAB_ExpectedCostDis.Qty ELSE 0 END AS OrderlineQty 
                        From VAM_Inv_InOutLine Inner Join VAB_ExpectedCostDis On VAM_Inv_InOutLine.VAB_Orderline_Id = VAB_ExpectedCostDis.VAB_Orderline_Id
                        INNER JOIN VAB_ExpectedCost ON VAB_ExpectedCost.VAB_ExpectedCost_Id = VAB_ExpectedCostDis.VAB_ExpectedCost_Id
                        INNER JOIN VAM_Product ON VAM_Product.VAM_Product_ID = VAM_Inv_InOutLine.VAM_Product_ID 
                        WHERE VAM_Inv_InOutLine.VAM_Inv_InOutLine_ID = " + VAM_Inv_InOutLine_ID + @"  AND VAB_ExpectedCost.VAB_ExpectedCost_ID=" + VAB_ExpectedCost_ID;
            //differenceAmt = Util.GetValueOfDecimal(DB.ExecuteScalar(sql, null, Get_Trx()));
            DataSet ds = DB.ExecuteDataset(sql, null, Get_Trx());
            // order qty
            Decimal orderedQty = 0;
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                differenceAmt = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["Amt"]);
                orderedQty = Util.GetValueOfDecimal(ds.Tables[0].Rows[0]["OrderlineQty"]);
            }

            // multiply with movement qty of MR
            differenceAmt = Decimal.Multiply(differenceAmt, (orderedQty > 0 ? orderedQty : Quantity));

            // diffrence between actual landed cost - expected received cost
            if (ActualLandeCost > 0)  // during invoice completion
            {
                differenceAmt = Decimal.Subtract(ActualLandeCost, differenceAmt);
            }
            else // during invoice reversal
            {
                differenceAmt = Decimal.Add(ActualLandeCost, differenceAmt);
            }
            return differenceAmt;
        }

        /// <summary>
        /// Get expected landed cost id, when expected landed cost distribution is defined on purchase order
        /// </summary>
        /// <param name="lc">landedc cost reference</param>
        /// <param name="VAB_OrderLine_ID">order line reference</param>
        /// <returns>VAB_ExpectedCost_ID</returns>
        private int GetExpectedLandedCostId(MLandedCost lc, int VAB_OrderLine_ID)
        {
            int VAB_ExpectedCost_ID = 0;
            String sql = @"Select Distinct VAB_ExpectedCost.VAB_ExpectedCost_ID From VAB_ExpectedCost 
                            INNER JOIN VAB_ExpectedCostDis ON VAB_ExpectedCost.VAB_ExpectedCost_Id = VAB_ExpectedCostDis.VAB_ExpectedCost_Id
                            WHERE VAB_ExpectedCost.VAM_ProductCostElement_Id = " + lc.GetVAM_ProductCostElement_ID() + @"
                            AND VAB_ExpectedCostDis.VAB_OrderLine_ID = " + VAB_OrderLine_ID;
            // commeneted afetr discussion with ashish - not consider "cost distribution type" during re-distribution
            /*And VAB_ExpectedCost.Landedcostdistribution = '" + lc.GetLandedCostDistribution() + @"'*/
            VAB_ExpectedCost_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql, null, Get_Trx()));
            return VAB_ExpectedCost_ID;
        }

        /// <summary>
        /// Allocate Landed Cost - Enforce Rounding
        /// </summary>
        private void AllocateLandedCostRounding()
        {
            try
            {
                MLandedCostAllocation[] allocations = MLandedCostAllocation.GetOfInvoiceLine(
                    GetCtx(), GetVAB_InvoiceLine_ID(), Get_TrxName());
                MLandedCostAllocation largestAmtAllocation = null;
                Decimal allocationAmt = Env.ZERO;
                for (int i = 0; i < allocations.Length; i++)
                {
                    MLandedCostAllocation allocation = allocations[i];
                    if (largestAmtAllocation == null
                        || allocation.GetAmt().CompareTo(largestAmtAllocation.GetAmt()) > 0)
                        largestAmtAllocation = allocation;
                    allocationAmt = Decimal.Add(allocationAmt, allocation.GetAmt());
                }
                //Decimal difference = Decimal.Subtract(GetLineNetAmt(), allocationAmt);
                Decimal difference = Decimal.Subtract(GetProductLineCost(this), allocationAmt);
                if (Env.Signum(difference) != 0)
                {
                    largestAmtAllocation.SetAmt(Decimal.Add(largestAmtAllocation.GetAmt(), difference));
                    largestAmtAllocation.Save();
                    log.Config("Difference=" + difference
                        + ", VAB_LCostDistribution_ID=" + largestAmtAllocation.GetVAB_LCostDistribution_ID()
                        + ", Amt" + largestAmtAllocation.GetAmt());
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--AllocateLandedCostRounding");
            }
        }

        /**
         *	Invoice Line - Quantity.
         *		- called from VAB_UOM_ID, QtyEntered, QtyInvoiced
         *		- enforces qty UOM relationship
         *	@param ctx context
         *	@param WindowNo window no
         *	@param mTab tab
         *	@param mField field
         *	@param value value
         *	@return null or error message
         */
        private bool SetQty(int WindowNo, String columnName)
        {
            try
            {
                int VAM_Product_ID = GetVAM_Product_ID();
                //	log.log(Level.WARNING,"qty - init - VAM_Product_ID=" + VAM_Product_ID);
                Decimal QtyInvoiced;
                Decimal QtyEntered, PriceActual, PriceEntered;

                //	No Product
                if (VAM_Product_ID == 0)
                {
                    QtyEntered = GetQtyEntered();
                    SetQtyInvoiced(QtyEntered);
                }
                //	UOM Changed - convert from Entered -> Product
                else if (columnName.Equals("VAB_UOM_ID"))
                {
                    int VAB_UOM_To_ID = GetVAB_UOM_ID();
                    QtyEntered = GetQtyEntered();
                    Decimal QtyEntered1 = Decimal.Round((Decimal)QtyEntered,
                        MUOM.GetPrecision(GetCtx(), VAB_UOM_To_ID)
                        , MidpointRounding.AwayFromZero);
                    if (QtyEntered.CompareTo(QtyEntered1) != 0)
                    {
                        log.Fine("Corrected QtyEntered Scale UOM=" + VAB_UOM_To_ID
                            + "; QtyEntered=" + QtyEntered + "->" + QtyEntered1);
                        QtyEntered = QtyEntered1;
                        SetQtyEntered(QtyEntered);
                    }
                    QtyInvoiced = (Decimal)MUOMConversion.ConvertProductFrom(GetCtx(), VAM_Product_ID,
                        VAB_UOM_To_ID, QtyEntered);
                    if (QtyInvoiced == null)
                    {
                        QtyInvoiced = QtyEntered;
                    }
                    bool conversion = QtyEntered.CompareTo(QtyInvoiced) != 0;
                    PriceActual = GetPriceActual();
                    PriceEntered = (Decimal)MUOMConversion.ConvertProductFrom(GetCtx(), VAM_Product_ID,
                        VAB_UOM_To_ID, PriceActual);
                    if (PriceEntered == null)
                    {
                        PriceEntered = PriceActual;
                    }
                    log.Fine("qty - UOM=" + VAB_UOM_To_ID
                        + ", QtyEntered/PriceActual=" + QtyEntered + "/" + PriceActual
                        + " -> " + conversion
                        + " QtyInvoiced/PriceEntered=" + QtyInvoiced + "/" + PriceEntered);
                    SetContext(WindowNo, "UOMConversion", conversion ? "Y" : "N");
                    SetQtyInvoiced(QtyInvoiced);
                    SetPriceEntered(PriceEntered);
                }
                //	QtyEntered changed - calculate QtyInvoiced
                else if (columnName.Equals("QtyEntered"))
                {
                    int VAB_UOM_To_ID = GetVAB_UOM_ID();
                    QtyEntered = GetQtyEntered();
                    QtyEntered = Decimal.Round(QtyEntered, MUOM.GetPrecision(GetCtx(), VAB_UOM_To_ID), MidpointRounding.AwayFromZero);
                    Decimal QtyEntered1 = QtyEntered;
                    if (QtyEntered.CompareTo(QtyEntered1) != 0)
                    {
                        log.Fine("Corrected QtyEntered Scale UOM=" + VAB_UOM_To_ID
                            + "; QtyEntered=" + QtyEntered + "->" + QtyEntered1);
                        QtyEntered = QtyEntered1;
                        SetQtyEntered(QtyEntered);
                    }
                    QtyInvoiced = (Decimal)MUOMConversion.ConvertProductFrom(GetCtx(), VAM_Product_ID,
                        VAB_UOM_To_ID, QtyEntered);
                    if (QtyInvoiced == null)
                        QtyInvoiced = QtyEntered;
                    bool conversion = QtyEntered.CompareTo(QtyInvoiced) != 0;
                    log.Fine("qty - UOM=" + VAB_UOM_To_ID
                        + ", QtyEntered=" + QtyEntered
                        + " -> " + conversion
                        + " QtyInvoiced=" + QtyInvoiced);
                    SetContext(WindowNo, "UOMConversion", conversion ? "Y" : "N");
                    SetQtyInvoiced(QtyInvoiced);
                }
                //	QtyInvoiced changed - calculate QtyEntered (should not happen)
                else if (columnName.Equals("QtyInvoiced"))
                {
                    int VAB_UOM_To_ID = GetVAB_UOM_ID();
                    QtyInvoiced = GetQtyInvoiced();
                    int precision = MProduct.Get(GetCtx(), VAM_Product_ID).GetUOMPrecision();
                    Decimal QtyInvoiced1 = Decimal.Round(QtyInvoiced, precision, MidpointRounding.AwayFromZero);
                    if (QtyInvoiced.CompareTo(QtyInvoiced1) != 0)
                    {
                        log.Fine("Corrected QtyInvoiced Scale "
                            + QtyInvoiced + "->" + QtyInvoiced1);
                        QtyInvoiced = QtyInvoiced1;
                        SetQtyInvoiced(QtyInvoiced);
                    }
                    QtyEntered = (Decimal)MUOMConversion.ConvertProductTo(GetCtx(), VAM_Product_ID,
                        VAB_UOM_To_ID, QtyInvoiced);
                    if (QtyEntered == null)
                        QtyEntered = QtyInvoiced;
                    bool conversion = QtyInvoiced.CompareTo(QtyEntered) != 0;
                    log.Fine("qty - UOM=" + VAB_UOM_To_ID
                        + ", QtyInvoiced=" + QtyInvoiced
                        + " -> " + conversion
                        + " QtyEntered=" + QtyEntered);
                    SetContext(WindowNo, "UOMConversion", conversion ? "Y" : "N");
                    SetQtyEntered(QtyEntered);
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetQty");
            }
            return true;
        }


        /**
         *	Invoice - Amount.
         *		- called from QtyInvoiced, PriceActual
         *		- calculates LineNetAmt
         *	@param ctx context
         *	@param WindowNo window no
         *	@param mTab tab
         *	@param mField field
         *	@param value value
         *	@return null or error message
         */
        private bool SetAmt(int WindowNo, String columnName)
        {
            try
            {

                //	log.log(Level.WARNING,"amt - init");
                int VAB_UOM_To_ID = GetVAB_UOM_ID();
                int VAM_Product_ID = GetVAM_Product_ID();
                int VAM_PriceList_ID = GetCtx().GetContextAsInt(WindowNo, "VAM_PriceList_ID");
                int StdPrecision = MPriceList.GetPricePrecision(GetCtx(), VAM_PriceList_ID);
                Decimal PriceActual, PriceEntered, PriceLimit, PriceList, Discount;
                Decimal? QtyEntered, QtyInvoiced;
                //	Get values
                QtyEntered = GetQtyEntered();
                QtyInvoiced = GetQtyInvoiced();
                log.Fine("QtyEntered=" + QtyEntered + ", Invoiced=" + QtyInvoiced + ", UOM=" + VAB_UOM_To_ID);
                //
                PriceEntered = GetPriceEntered();
                PriceActual = GetPriceActual();
                //	Discount = (Decimal)mTab.GetValue("Discount");
                PriceLimit = GetPriceLimit();
                PriceList = GetPriceList();
                log.Fine("PriceList=" + PriceList + ", Limit=" + PriceLimit + ", Precision=" + StdPrecision);
                log.Fine("PriceEntered=" + PriceEntered + ", Actual=" + PriceActual);// + ", Discount=" + Discount);

                //	Qty changed - recalc price
                if ((columnName.Equals("QtyInvoiced")
                    || columnName.Equals("QtyEntered")
                    || columnName.Equals("VAM_Product_ID"))
                    && !"N".Equals(GetCtx().GetContext(WindowNo, "DiscountSchema")))
                {
                    int VAB_BusinessPartner_ID = GetCtx().GetContextAsInt(WindowNo, "VAB_BusinessPartner_ID");
                    if (columnName.Equals("QtyEntered"))
                        QtyInvoiced = MUOMConversion.ConvertProductTo(GetCtx(), VAM_Product_ID,
                            VAB_UOM_To_ID, QtyEntered);
                    if (QtyInvoiced == null)
                        QtyInvoiced = QtyEntered;
                    bool IsSOTrx = GetCtx().IsSOTrx(WindowNo);
                    MProductPricing pp = new MProductPricing(GetVAF_Client_ID(), GetVAF_Org_ID(),
                            VAM_Product_ID, VAB_BusinessPartner_ID, QtyInvoiced, IsSOTrx);
                    pp.SetVAM_PriceList_ID(VAM_PriceList_ID);
                    int VAM_PriceListVersion_ID = GetCtx().GetContextAsInt(WindowNo, "VAM_PriceListVersion_ID");
                    pp.SetVAM_PriceListVersion_ID(VAM_PriceListVersion_ID);
                    DateTime date = CommonFunctions.CovertMilliToDate(GetCtx().GetContextAsTime(WindowNo, "DateInvoiced"));

                    pp.SetPriceDate(date);
                    //
                    PriceEntered = (Decimal)MUOMConversion.ConvertProductFrom(GetCtx(), VAM_Product_ID,
                        VAB_UOM_To_ID, pp.GetPriceStd());
                    if (PriceEntered == null)
                        PriceEntered = pp.GetPriceStd();
                    //
                    log.Fine("amt - QtyChanged -> PriceActual=" + pp.GetPriceStd()
                        + ", PriceEntered=" + PriceEntered + ", Discount=" + pp.GetDiscount());
                    PriceActual = pp.GetPriceStd();
                    SetPriceActual(PriceActual);
                    //	mTab.SetValue("Discount", pp.GetDiscount());
                    SetPriceEntered(PriceEntered);
                    SetContext(WindowNo, "DiscountSchema", pp.IsDiscountSchema() ? "Y" : "N");
                }
                else if (columnName.Equals("PriceActual"))
                {
                    PriceActual = GetPriceActual();
                    PriceEntered = (Decimal)MUOMConversion.ConvertProductFrom(GetCtx(), VAM_Product_ID,
                        VAB_UOM_To_ID, (Decimal)PriceActual);
                    if (PriceEntered == null)
                        PriceEntered = PriceActual;
                    //
                    log.Fine("amt - PriceActual=" + PriceActual
                        + " -> PriceEntered=" + PriceEntered);
                    SetPriceEntered(PriceEntered);
                }
                else if (columnName.Equals("PriceEntered"))
                {
                    PriceEntered = GetPriceEntered();
                    PriceActual = (Decimal)MUOMConversion.ConvertProductTo(GetCtx(), VAM_Product_ID,
                        VAB_UOM_To_ID, PriceEntered);
                    if (PriceActual == null)
                        PriceActual = PriceEntered;
                    //
                    log.Fine("amt - PriceEntered=" + PriceEntered
                        + " -> PriceActual=" + PriceActual);
                    SetPriceActual(PriceActual);
                }

                /**  Discount entered - Calculate Actual/Entered
                if (columnName.equals("Discount"))
                {
                    PriceActual = new Decimal ((100.0 - Discount.doubleValue()) / 100.0 * PriceList.doubleValue());
                    if (PriceActual.scale() > StdPrecision)
                        PriceActual = PriceActual.SetScale(StdPrecision, Decimal.ROUND_HALF_UP);
                    PriceEntered = MUOMConversion.convertProductFrom (ctx, VAM_Product_ID, 
                        VAB_UOM_To_ID, PriceActual);
                    if (PriceEntered == null)
                        PriceEntered = PriceActual;
                    mTab.SetValue("PriceActual", PriceActual);
                    mTab.SetValue("PriceEntered", PriceEntered);
                }
                //	calculate Discount
                else
                {
                    if (PriceList.intValue() == 0)
                        Discount = Env.ZERO;
                    else
                        Discount = new Decimal ((PriceList.doubleValue() - PriceActual.doubleValue()) / PriceList.doubleValue() * 100.0);
                    if (Discount.scale() > 2)
                        Discount = Discount.SetScale(2, Decimal.ROUND_HALF_UP);
                    mTab.SetValue("Discount", Discount);
                }
                log.Fine("amt = PriceEntered=" + PriceEntered + ", Actual" + PriceActual + ", Discount=" + Discount);
                /* */

                //	Check PriceLimit
                String epl = GetCtx().GetContext(WindowNo, "EnforcePriceLimit");
                bool enforce = GetCtx().IsSOTrx(WindowNo) && epl != null && epl.Equals("Y");
                if (enforce && MVAFRole.GetDefault(GetCtx()).IsOverwritePriceLimit())
                    enforce = false;
                //	Check Price Limit?
                if (enforce && Decimal.ToDouble((Decimal)PriceLimit) != 0.0
                  && PriceActual.CompareTo(PriceLimit) < 0)
                {
                    PriceActual = PriceLimit;
                    PriceEntered = (Decimal)MUOMConversion.ConvertProductFrom(GetCtx(), VAM_Product_ID,
                        VAB_UOM_To_ID, (Decimal)PriceLimit);
                    if (PriceEntered == 0)
                        PriceEntered = PriceLimit;
                    log.Fine("amt =(under) PriceEntered=" + PriceEntered + ", Actual" + PriceLimit);
                    SetPriceActual(PriceLimit);
                    SetPriceEntered(PriceEntered);
                    //addError(Msg.GetMsg(GetCtx(), "UnderLimitPrice"));
                    //	Repeat Discount calc
                    if (Decimal.ToInt32(PriceList) != 0)
                    {
                        Discount = new Decimal((Decimal.ToDouble(PriceList) - Decimal.ToDouble(PriceActual)) / Decimal.ToDouble(PriceList) * 100.0);
                        if (Env.Scale(Discount) > 2)
                            Discount = Decimal.Round(Discount, 2, MidpointRounding.AwayFromZero);
                        //	mTab.SetValue ("Discount", Discount);
                    }
                }

                //	Line Net Amt
                Decimal LineNetAmt = Decimal.Multiply((Decimal)QtyInvoiced, PriceActual);
                if (Env.Scale(LineNetAmt) > StdPrecision)
                    LineNetAmt = Decimal.Round(LineNetAmt, StdPrecision, MidpointRounding.AwayFromZero);
                log.Info("amt = LineNetAmt=" + LineNetAmt);
                SetLineNetAmt(LineNetAmt);

                //	Calculate Tax Amount for PO
                bool isSOTrx = GetCtx().IsSOTrx(WindowNo);
                if (!isSOTrx)
                {
                    Decimal TaxAmt = Env.ZERO;
                    if (columnName.Equals("TaxAmt"))
                    {
                        TaxAmt = (Decimal)GetTaxAmt();
                    }
                    else
                    {
                        int taxID = GetVAB_TaxRate_ID();
                        if (taxID != null)
                        {
                            int VAB_TaxRate_ID = taxID;
                            MTax tax = new MTax(GetCtx(), VAB_TaxRate_ID, null);
                            TaxAmt = tax.CalculateTax(LineNetAmt, IsTaxIncluded(), StdPrecision);
                            SetTaxAmt(TaxAmt);
                        }
                    }
                    //	Add it up
                    SetLineTotalAmt(Decimal.Add(LineNetAmt, TaxAmt));
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetAmt");
            }
            return true;
        }


        /**
         * 	Set UOM - Callout
         *	@param oldVAB_UOM_ID old value
         *	@param newVAB_UOM_ID new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetVAB_UOM_ID(String oldVAB_UOM_ID,
            String newVAB_UOM_ID, int windowNo)
        {
            if (newVAB_UOM_ID == null || newVAB_UOM_ID.Length == 0)
                return;
            int VAB_UOM_ID = int.Parse(newVAB_UOM_ID);
            if (VAB_UOM_ID == 0)
                return;
            //
            base.SetVAB_UOM_ID(VAB_UOM_ID);
            SetQty(windowNo, "VAB_UOM_ID");
            SetAmt(windowNo, "VAB_UOM_ID");
        }


        /**
         * 	Set QtyEntered - Callout
         *	@param oldQtyEntered old value
         *	@param newQtyEntered new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout 
        public void SetQtyEntered(String oldQtyEntered,
            String newQtyEntered, int windowNo)
        {
            if (newQtyEntered == null || newQtyEntered.Length == 0)
                return;
            Decimal QtyEntered = Convert.ToDecimal(newQtyEntered);
            base.SetQtyEntered(QtyEntered);
            SetQty(windowNo, "QtyEntered");
            SetAmt(windowNo, "QtyEntered");
        }

        /**
         * 	Set QtyOrdered - Callout
         *	@param oldQtyInvoiced old value
         *	@param newQtyInvoiced new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout 
        public void SetQtyInvoiced(String oldQtyInvoiced,
            String newQtyInvoiced, int windowNo)
        {
            if (newQtyInvoiced == null || newQtyInvoiced.Length == 0)
                return;
            Decimal qtyInvoiced = Convert.ToDecimal(newQtyInvoiced);
            base.SetQtyInvoiced(qtyInvoiced);
            SetQty(windowNo, "QtyInvoiced");
            SetAmt(windowNo, "QtyInvoiced");
        }



        /**
         * 	Set VAB_TaxRate_ID - Callout
         *	@param oldVAB_TaxRate_ID old value
         *	@param newVAB_TaxRate_ID new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout 
        public void SetVAB_TaxRate_ID(String oldVAB_TaxRate_ID,
            String newVAB_TaxRate_ID, int windowNo)
        {
            if (newVAB_TaxRate_ID == null || newVAB_TaxRate_ID.Length == 0)
                return;
            Decimal VAB_TaxRate_ID = Convert.ToDecimal(newVAB_TaxRate_ID);
            base.SetTaxAmt(VAB_TaxRate_ID);
            SetAmt(windowNo, "VAB_TaxRate_ID");
        }


        /**
         * 	Set PriceActual - Callout
         *	@param oldPriceActual old value
         *	@param newPriceActual new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetPriceActual(String oldPriceActual,
            String newPriceActual, int windowNo)
        {
            if (newPriceActual == null || newPriceActual.Length == 0)
                return;
            Decimal PriceActual = Convert.ToDecimal(newPriceActual);
            base.SetPriceActual(PriceActual);
            SetAmt(windowNo, "PriceActual");
        }

        /**
         * 	Set PriceEntered - Callout
         *	@param oldPriceEntered old value
         *	@param newPriceEntered new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetPriceEntered(String oldPriceEntered,
            String newPriceEntered, int windowNo)
        {
            if (newPriceEntered == null || newPriceEntered.Length == 0)
                return;
            Decimal PriceEntered = Convert.ToDecimal(newPriceEntered);
            base.SetPriceEntered(PriceEntered);
            SetAmt(windowNo, "PriceEntered");
        }	//	SetPriceEntered


        /**
         * 	Set TaxAmt - Callout
         *	@param oldTaxAmt old value
         *	@param newTaxAmt new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetTaxAmt(String oldTaxAmt,
            String newTaxAmt, int windowNo)
        {
            if (newTaxAmt == null || newTaxAmt.Length == 0)
                return;
            Decimal taxAmt = Convert.ToDecimal(newTaxAmt);
            base.SetTaxAmt(taxAmt);
            SetAmt(windowNo, "TaxAmt");
        }


        /***
         *	Invoice Line - Product.
         *		- reSet VAB_Charge_ID / VAM_PFeature_SetInstance_ID
         *		- PriceList, PriceStd, PriceLimit, VAB_Currency_ID, EnforcePriceLimit
         *		- UOM
         *	Calls Tax
         *	@param oldVAM_Product_ID old value
         *	@param newVAM_Product_ID new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout 
        public void SetVAM_Product_ID(String oldVAM_Product_ID,
            String newVAM_Product_ID, int WindowNo)
        {
            if (newVAM_Product_ID == null || newVAM_Product_ID.Length == 0)
                return;
            int VAM_Product_ID = int.Parse(newVAM_Product_ID);
            if (VAM_Product_ID == 0)
                return;

            SetVAB_Charge_ID(0);

            //	Set Attribute
            if (GetCtx().GetContextAsInt(Env.WINDOW_INFO, Env.TAB_INFO, "VAM_Product_ID") == VAM_Product_ID
                && GetCtx().GetContextAsInt(Env.WINDOW_INFO,
                Env.TAB_INFO, "VAM_PFeature_SetInstance_ID") != 0)
                SetVAM_PFeature_SetInstance_ID((GetCtx().GetContextAsInt(Env.WINDOW_INFO,
                    Env.TAB_INFO, "VAM_PFeature_SetInstance_ID")));
            else
                SetVAM_PFeature_SetInstance_ID(-1);

            /*****	Price Calculation see also qty	****/
            bool IsSOTrx = GetCtx().IsSOTrx(WindowNo);
            int VAB_BusinessPartner_ID = GetCtx().GetContextAsInt(WindowNo, "VAB_BusinessPartner_ID");
            Decimal Qty = GetQtyInvoiced();
            MProductPricing pp = new MProductPricing(GetVAF_Client_ID(), GetVAF_Org_ID(),
                    VAM_Product_ID, VAB_BusinessPartner_ID, Qty, IsSOTrx);
            //
            int VAM_PriceList_ID = GetCtx().GetContextAsInt(WindowNo, "VAM_PriceList_ID");
            pp.SetVAM_PriceList_ID(VAM_PriceList_ID);
            int VAM_PriceListVersion_ID = GetCtx().GetContextAsInt(WindowNo, "VAM_PriceListVersion_ID");
            pp.SetVAM_PriceListVersion_ID(VAM_PriceListVersion_ID);
            long time = GetCtx().GetContextAsTime(WindowNo, "DateInvoiced");
            pp.SetPriceDate(time);
            //		
            SetPriceList(pp.GetPriceList());
            SetPriceLimit(pp.GetPriceLimit());
            SetPriceActual(pp.GetPriceStd());
            SetPriceEntered(pp.GetPriceStd());
            SetContext(WindowNo, "VAB_Currency_ID", pp.GetVAB_Currency_ID().ToString());
            //	mTab.SetValue("Discount", pp.GetDiscount());
            SetVAB_UOM_ID(pp.GetVAB_UOM_ID());
            SetContext(WindowNo, "EnforcePriceLimit", pp.IsEnforcePriceLimit() ? "Y" : "N");
            SetContext(WindowNo, "DiscountSchema", pp.IsDiscountSchema() ? "Y" : "N");
            //
            SetTax(WindowNo, "VAM_Product_ID");

            return;
        }

        /**
         * 	Set Charge - Callout
         *	@param oldVAB_Charge_ID old value
         *	@param newVAB_Charge_ID new value
         *	@param windowNo window
         *	@throws Exception
         */
        //@UICallout
        public void SetVAB_Charge_ID(String oldVAB_Charge_ID,
            String newVAB_Charge_ID, int WindowNo)
        {
            if (newVAB_Charge_ID == null || newVAB_Charge_ID.Length == 0)
                return;
            int VAB_Charge_ID = int.Parse(newVAB_Charge_ID);
            if (VAB_Charge_ID == 0)
                return;

            //	No Product defined
            if (GetVAM_Product_ID() != 0)
            {
                SetVAB_Charge_ID(0);

                //addError( Msg.GetMsg( GetCtx(), "ChargeExclusively" ) );
            }
            SetVAM_PFeature_SetInstance_ID(-1);
            SetVAS_Res_Assignment_ID(0);
            SetVAB_UOM_ID(100);	//	EA

            SetContext(WindowNo, "DiscountSchema", "N");
            String sql = "SELECT ChargeAmt FROM VAB_Charge WHERE VAB_Charge_ID=" + VAB_Charge_ID;

            IDataReader idr = null;
            try
            {
                //PreparedStatement pstmt = DataBase.prepareStatement(sql, null);
                //pstmt.SetInt(1, VAB_Charge_ID);
                //ResultSet rs = pstmt.executeQuery();
                idr = DataBase.DB.ExecuteReader(sql, null, null);

                if (idr.Read())
                {
                    SetPriceEntered(idr.GetDecimal(0));
                    SetPriceActual(idr.GetDecimal(0));
                    SetPriceLimit(Env.ZERO);
                    SetPriceList(Env.ZERO);
                    SetContext(WindowNo, "Discount", Env.ZERO.ToString());
                }

                idr.Close();
            }
            catch (SqlException e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Log(Level.SEVERE, sql + e);
                //addError( e.GetLocalizedMessage() );
            }

            //
            SetTax(WindowNo, "VAB_Charge_ID");
        }



        /**
         *	Invoice Line - Tax.
         *		- basis: Product, Charge, BPartner Location
         *		- Sets VAB_TaxRate_ID
         *  Calles Amount
         *	@param ctx context
         *	@param WindowNo window no
         *	@param mTab tab
         *	@param mField field
         *	@param value value
         *	@return null or error message
         */
        private bool SetTax(int WindowNo, String columnName)
        {
            try
            {
                //	Check Product
                int VAM_Product_ID = GetVAM_Product_ID();
                int VAB_Charge_ID = GetVAB_Charge_ID();
                log.Fine("Product=" + VAM_Product_ID + ", VAB_Charge_ID=" + VAB_Charge_ID);
                if (VAM_Product_ID == 0 && VAB_Charge_ID == 0)
                    return SetAmt(WindowNo, columnName);

                //	Check Partner Location
                int shipVAB_BPart_Location_ID = GetCtx().GetContextAsInt(WindowNo, "VAB_BPart_Location_ID");
                if (shipVAB_BPart_Location_ID == 0)
                    return SetAmt(WindowNo, columnName);
                log.Fine("Ship BP_Location=" + shipVAB_BPart_Location_ID);
                int billVAB_BPart_Location_ID = shipVAB_BPart_Location_ID;
                log.Fine("Bill BP_Location=" + billVAB_BPart_Location_ID);

                //	Dates
                DateTime billDate = CommonFunctions.CovertMilliToDate(GetCtx().GetContextAsTime(WindowNo, "DateInvoiced"));
                log.Fine("Bill Date=" + billDate);
                DateTime shipDate = billDate;
                log.Fine("Ship Date=" + shipDate);

                int VAF_Org_ID = GetVAF_Org_ID();
                log.Fine("Org=" + VAF_Org_ID);

                int VAM_Warehouse_ID = GetCtx().GetContextAsInt("#VAM_Warehouse_ID");
                log.Fine("Warehouse=" + VAM_Warehouse_ID);

                //
                int VAB_TaxRate_ID = Tax.Get(GetCtx(), VAM_Product_ID, VAB_Charge_ID, billDate, shipDate,
                    VAF_Org_ID, VAM_Warehouse_ID, billVAB_BPart_Location_ID, shipVAB_BPart_Location_ID,
                    GetCtx().IsSOTrx(WindowNo));
                log.Info("Tax ID=" + VAB_TaxRate_ID);
                //
                if (VAB_TaxRate_ID == 0)
                {
                    //ValueNamePair pp = CLogger.retrieveError();
                    //if (pp != null)
                    //    addError(pp.GetValue());
                    //else
                    //    addError( Msg.GetMsg( GetCtx(), "Tax Error" ) );
                }
                else
                    SetVAB_TaxRate_ID(VAB_TaxRate_ID);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVABInvoiceLine--SetTax");
            }
            return SetAmt(WindowNo, columnName);
        }

    }
}
