﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVAMProductPricing
 * Purpose        : 
 * Class Used     : 
 * Chronological    Development
 * Raghunandan     04-Jun-2009
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
using VAdvantage.Print;
//////using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVAMProductPricing
    {
        #region Private variables
        private int _VAF_Client_ID;
        private int _VAF_Org_ID;
        private int _VAM_Product_ID;
        private int _VAB_BusinessPartner_ID;
        private Decimal _qty = Env.ONE;
        private Boolean _isSOTrx = true;
        //
        private int _VAM_PriceList_ID = 0;
        private int _VAM_PriceListVersion_ID = 0;
        private int _VAM_PFeature_SetInstance_ID = 0;
        private DateTime? _PriceDate;
        /** Precision -1 = no rounding		*/
        private int _precision = -1;


        private Boolean _calculated = false;
        private Boolean? _found = null;

        private Decimal _PriceList = Env.ZERO;
        private Decimal _PriceStd = Env.ZERO;
        private Decimal _PriceLimit = Env.ZERO;
        private int _VAB_Currency_ID = 0;
        private Boolean _enforcePriceLimit = false;
        private int _VAB_UOM_ID = 0;
        private int _VAM_ProductCategory_ID;
        private Boolean _discountSchema = false;
        private Boolean _isTaxIncluded = false;

        private Boolean? _userPricing = null;
        private UserPricingInterface _api = null;

        /**	Logger			*/
        //logger
        protected VLogger log = null;
        //protected CLogger log = CLogger.getCLogger(getClass());

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="VAF_Client_ID">product</param>
        /// <param name="VAF_Org_ID">partner</param>
        /// <param name="VAM_Product_ID"></param>
        /// <param name="VAB_BusinessPartner_ID"></param>
        /// <param name="Qty">quantity</param>
        /// <param name="isSOTrx">SO or PO</param>
        public MVAMProductPricing(int VAF_Client_ID, int VAF_Org_ID,
            int VAM_Product_ID, int VAB_BusinessPartner_ID,
            Decimal? qty, bool isSOTrx)
        {
            if (log == null)
            {
                log = VLogger.GetVLogger(this.GetType().FullName);
            }
            _VAF_Client_ID = VAF_Client_ID;
            _VAF_Org_ID = VAF_Org_ID;
            _VAM_Product_ID = VAM_Product_ID;
            _VAB_BusinessPartner_ID = VAB_BusinessPartner_ID;
            if (qty != null && Env.ZERO.CompareTo(qty) != 0)
                _qty = (Decimal)qty;
            _isSOTrx = isSOTrx;
        }

        /// <summary>
        /// Calculate Price
        /// </summary>
        /// <returns> true if calculated</returns>
        public bool CalculatePrice()
        {
            if (_VAM_Product_ID == 0 || (_found != null && !(Boolean)_found))	//	previously not found
                return false;
            //	Customer Pricing Engine
            if (!_calculated)
                _calculated = CalculateUser();
            //	Price List Version known
            if (!_calculated)
                _calculated = CalculatePLV();
            //	Price List known
            if (!_calculated)
                _calculated = CalculatePL();
            //	Base Price List used
            if (!_calculated)
                _calculated = CalculateBPL();
            //	Set UOM, Prod.Category
            if (!_calculated)
                SetBaseInfo();
            //	User based Discount
            if (_calculated)
                CalculateDiscount();
            SetPrecision();		//	from Price List
            _found = _calculated;
            return _calculated;
        }

        /// <summary>
        /// Calculate User Price
        /// </summary>
        /// <returns>true if calculated</returns>
        private bool CalculateUser()
        {
            if (_userPricing == null)
            {
                MVAFClientDetail client = MVAFClientDetail.Get(Env.GetContext(), _VAF_Client_ID);
                String userClass = client.GetPricingEngineClass();
                try
                {
                    // class<?> clazz = null;
                    Type clazz = null;
                    if (userClass != null)
                    {
                        //clazz = Class.forName(userClass);
                        clazz = Type.GetType(userClass);
                    }
                    if (clazz != null)
                    {
                        _api = (UserPricingInterface)Activator.CreateInstance(clazz);
                    }
                }
                catch (Exception ex)
                {
                    log.Warning("No User Pricing Engine (" + userClass + ") " + ex.ToString());
                    _userPricing = false;
                    return false;
                }
                _userPricing = _api != null;
            }
            if (!(Boolean)_userPricing)
                return false;

            UserPricingVO vo = null;
            if (_api != null)
            {
                try
                {
                    vo = _api.Price(_VAF_Org_ID, _isSOTrx, _VAM_PriceList_ID,
                        _VAB_BusinessPartner_ID, _VAM_Product_ID, _qty, _PriceDate);
                }
                catch (Exception ex)
                {
                    log.Warning("Error User Pricing - " + ex.ToString());
                    return false;
                }
            }

            if (vo != null && vo.IsValid())
            {
                _PriceList = vo.GetPriceList();
                _PriceStd = vo.GetPriceStd();
                _PriceLimit = vo.GetPriceLimit();
                _found = true;
                //	Optional
                _VAB_UOM_ID = vo.GetVAB_UOM_ID();
                _VAB_Currency_ID = vo.GetVAB_Currency_ID();
                _enforcePriceLimit = vo.IsEnforcePriceLimit();
                if (_VAB_UOM_ID == 0 || _VAB_Currency_ID == 0)
                    SetBaseInfo();
            }
            return false;
        }

        /// <summary>
        /// Calculate Price based on Price List Version
        /// </summary>
        /// <returns>true if calculated</returns>
        private bool CalculatePLV()
        {
            String sql = "";
            if (_VAM_Product_ID == 0 || _VAM_PriceListVersion_ID == 0)
                return false;
            // Check For Advance Pricing Module
            Tuple<String, String, String> mInfo = null;
            if (Env.HasModulePrefix("VAPRC_", out mInfo))
            {
                Tuple<String, String, String> mInfo1 = null;
                if (Env.HasModulePrefix("ED011_", out mInfo1))
                {
                    //vikas  mantis Issue ( 0000517)
                    string _sql = null;
                    _sql = "SELECT VAB_UOM_ID FROM VAM_Product WHERE  VAM_Product_ID=" + _VAM_Product_ID;
                    _VAB_UOM_ID = Util.GetValueOfInt(DB.ExecuteScalar(_sql));
                    //end
                    sql = "SELECT bomPriceStdUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceStd,"	//	1
                       + " boMVAMPriceListUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceList,"		//	2
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
                       + " AND pp.VAB_UOM_ID = " + _VAB_UOM_ID  //    #4
                       + " AND pp.IsActive='Y'";
                }
                else
                {
                    sql = "SELECT bomPriceStdAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceStd,"	//	1
                        + " boMVAMPriceListAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceList,"		//	2
                        + " bomPriceLimitAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceLimit,"	//	3
                        + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,"	//	4..7
                        + " pl.EnforcePriceLimit, pl.IsTaxIncluded "	// 8..9
                        + "FROM VAM_Product p"
                        + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                        + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                        + " INNER JOIN VAM_PriceList pl ON (pv.VAM_PriceList_ID=pl.VAM_PriceList_ID) "
                        + "WHERE pv.IsActive='Y'"
                        + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                        + " AND pv.VAM_PriceListVersion_ID=" + _VAM_PriceListVersion_ID	//	#2
                        + " AND pp.VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_ID;   //	#3
                }
            }
            else
            {
                sql = "SELECT bomPriceStd(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceStd,"	//	1
                    + " boMVAMPriceList(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceList,"		//	2
                    + " bomPriceLimit(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceLimit,"	//	3
                    + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,"	//	4..7
                    + " pl.EnforcePriceLimit, pl.IsTaxIncluded "	// 8..9
                    + "FROM VAM_Product p"
                    + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                    + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                    + " INNER JOIN VAM_PriceList pl ON (pv.VAM_PriceList_ID=pl.VAM_PriceList_ID) "
                    + "WHERE pv.IsActive='Y'"
                    + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                    + " AND pv.VAM_PriceListVersion_ID=" + _VAM_PriceListVersion_ID;	//	#2
            }
            _calculated = false;
            try
            {
                DataSet ds = ExecuteQuery.ExecuteDataset(sql, null);
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    DataRow dr = ds.Tables[0].Rows[i];
                    //	Prices
                    _PriceStd = Utility.Util.GetValueOfDecimal(dr[0]);//.getBigDecimal(1);
                    if (dr[0] == null)
                        _PriceStd = Env.ZERO;
                    _PriceList = Utility.Util.GetValueOfDecimal(dr[1]);//.getBigDecimal(2);
                    if (dr[1] == null)
                        _PriceList = Env.ZERO;
                    _PriceLimit = Utility.Util.GetValueOfDecimal(dr[2]);//.getBigDecimal(3);
                    if (dr[2] == null)
                        _PriceLimit = Env.ZERO;
                    //
                    _VAB_UOM_ID = Utility.Util.GetValueOfInt(dr[3].ToString());//.getInt(4);
                    _VAB_Currency_ID = Utility.Util.GetValueOfInt(dr[5].ToString());//.getInt(6);
                    _VAM_ProductCategory_ID = Utility.Util.GetValueOfInt(dr[6].ToString());//.getInt(7);
                    _enforcePriceLimit = "Y".Equals(dr[7].ToString());//.getString(8));
                    _isTaxIncluded = "Y".Equals(dr[8].ToString());//.getString(9));
                    //
                    log.Fine("VAM_PriceListVersion_ID=" + _VAM_PriceListVersion_ID + " - " + _PriceStd);
                    _calculated = true;
                }
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
                _calculated = false;
            }
            return _calculated;
        }

        /// <summary>
        /// Calculate Price based on Price List
        /// </summary>
        /// <returns>true if calculated</returns>
        private bool CalculatePL()
        {
            String sql = "";
            if (_VAM_Product_ID == 0)
                return false;

            //	Get Price List
            /**
            if (_VAM_PriceList_ID == 0)
            {
                String sql = "SELECT VAM_PriceList_ID, IsTaxIncluded "
                    + "FROM VAM_PriceList pl"
                    + " INNER JOIN VAM_Product p ON (pl.VAF_Client_ID=p.VAF_Client_ID) "
                    + "WHERE VAM_Product_ID=? "
                    + "ORDER BY IsDefault DESC";
                PreparedStatement pstmt = null;
                try
                {
                    pstmt = DataBase.prepareStatement(sql);
                    pstmt.setInt(1, _VAM_Product_ID);
                    ResultSet dr = pstmt.executeQuery();
                    if (dr.next())
                    {
                        _VAM_PriceList_ID = dr.getInt(1);
                        _isTaxIncluded = "Y".equals(dr.getString(2));
                    }
                    dr.close();
                    pstmt.close();
                    pstmt = null;
                }
                catch (Exception e)
                {
                    log.Log(Level.SEVERE, "calculatePL (PL)", e);
                }
                finally
                {
                    try
                    {
                        if (pstmt != null)
                            pstmt.close ();
                    }
                    catch (Exception e)
                    {}
                    pstmt = null;
                }
            }
            /** **/
            if (_VAM_PriceList_ID == 0)
            {
                log.Log(Level.SEVERE, "No PriceList");
                //Trace.printStack();//*******************************************
                return false;
            }

            //	Get Prices for Price List
            // Check For Advance Pricing Module
            Tuple<String, String, String> mInfo = null;
            if (Env.HasModulePrefix("VAPRC_", out mInfo))
            {
                Tuple<String, String, String> mInfo1 = null;
                if (Env.HasModulePrefix("ED011_", out mInfo1))
                {
                    sql = "SELECT bomPriceStdUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceStd,"	//	1
                       + " boMVAMPriceListUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceList,"		//	2
                       + " bomPriceLimitUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceLimit,"	//	3
                       + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,pl.EnforcePriceLimit "	// 4..8
                       + "FROM VAM_Product p"
                       + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                       + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                       + " INNER JOIN VAM_PriceList pl ON (pv.VAM_PriceList_ID=pl.VAM_PriceList_ID) "
                       + " WHERE pv.IsActive='Y'"
                       + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                       + " AND pv.VAM_PriceList_ID=" + _VAM_PriceList_ID			//	#2
                       + " AND pp.VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_ID   //	#3
                       + " AND pp.VAB_UOM_ID = " + _VAB_UOM_ID  //    #4
                       + " AND pp.IsActive='Y'"
                       + " ORDER BY pv.ValidFrom DESC";
                }
                else
                {
                    sql = "SELECT bomPriceStdAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceStd,"	//	1
                        + " boMVAMPriceListAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceList,"		//	2
                        + " bomPriceLimitAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceLimit,"	//	3
                        + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,pl.EnforcePriceLimit "	// 4..8
                        + "FROM VAM_Product p"
                        + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                        + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                        + " INNER JOIN VAM_PriceList pl ON (pv.VAM_PriceList_ID=pl.VAM_PriceList_ID) "
                        + " WHERE pv.IsActive='Y'"
                        + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                        + " AND pv.VAM_PriceList_ID=" + _VAM_PriceList_ID			//	#2
                        + " AND pp.VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_ID   //	#3
                        + " ORDER BY pv.ValidFrom DESC";
                }
            }
            else
            {
                sql = "SELECT bomPriceStd(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceStd,"	//	1
                    + " boMVAMPriceList(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceList,"		//	2
                    + " bomPriceLimit(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceLimit,"  	//	3
                    + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,pl.EnforcePriceLimit "	// 4..8
                    + "FROM VAM_Product p"
                    + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                    + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                    + " INNER JOIN VAM_PriceList pl ON (pv.VAM_PriceList_ID=pl.VAM_PriceList_ID) "
                    + "WHERE pv.IsActive='Y'"
                    + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                    + " AND pv.VAM_PriceList_ID=" + _VAM_PriceList_ID			//	#2
                    + " ORDER BY pv.ValidFrom DESC";
            }
            _calculated = false;
            if (_PriceDate == null)
                _PriceDate = DateTime.Now;
            IDataReader dr = null;
            try
            {
                dr = ExecuteQuery.ExecuteReader(sql, null);
                // ResultSet dr = pstmt.executeQuery();
                while (!_calculated && dr.Read())
                {
                    DateTime? plDate = Utility.Util.GetValueOfDateTime(dr[4]);
                    //	we have the price list
                    //	if order date is after or equal PriceList validFrom
                    if (plDate == null || !(_PriceDate < plDate))
                    {
                        //	Prices
                        _PriceStd = Utility.Util.GetValueOfDecimal(dr[0]);
                        // if (dr.wasNull())
                        if (dr[0] == null)
                            _PriceStd = Env.ZERO;
                        _PriceList = Utility.Util.GetValueOfDecimal(dr[1]);
                        // if (dr.wasNull())
                        if (dr[1] == null)
                            _PriceList = Env.ZERO;
                        _PriceLimit = Utility.Util.GetValueOfDecimal(dr[2]);
                        //if (dr.wasNull())
                        if (dr[2] == null)
                            _PriceLimit = Env.ZERO;
                        //
                        _VAB_UOM_ID = Utility.Util.GetValueOfInt(dr[3].ToString());
                        _VAB_Currency_ID = Utility.Util.GetValueOfInt(dr[5].ToString());
                        _VAM_ProductCategory_ID = Utility.Util.GetValueOfInt(dr[6].ToString());
                        _enforcePriceLimit = "Y".Equals(dr[7].ToString());
                        //
                        log.Fine("VAM_PriceList_ID=" + _VAM_PriceList_ID + "(" + plDate + ")" + " - " + _PriceStd);
                        _calculated = true;
                        break;
                    }
                }
                dr.Close();
            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
                _calculated = false;
            }
            if (!_calculated)
            {
                log.Finer("Not found (PL)");
            }
            return _calculated;
        }

        /// <summary>
        /// Calculate Price based on Base Price List
        /// </summary>
        /// <returns>true if calculated</returns>
        private bool CalculateBPL()
        {
            String sql = "";
            if (_VAM_Product_ID == 0 || _VAM_PriceList_ID == 0)
                return false;
            // Check For Advance Pricing Module
            Tuple<String, String, String> mInfo = null;
            if (Env.HasModulePrefix("VAPRC_", out mInfo))
            {
                Tuple<String, String, String> mInfo1 = null;
                if (Env.HasModulePrefix("ED011_", out mInfo1))
                {
                    sql = "SELECT bomPriceStdUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceStd,"	//	1
                        + " boMVAMPriceListUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceList,"		//	2
                        + " bomPriceLimitUOM(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID , pp.VAB_UOM_ID) AS PriceLimit,"	//	3
                        + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,"	//	4..7
                        + " pl.EnforcePriceLimit, pl.IsTaxIncluded "	// 8..9
                        + "FROM VAM_Product p"
                        + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                        + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                        + " INNER JOIN VAM_PriceList bpl ON (pv.VAM_PriceList_ID=bpl.VAM_PriceList_ID)"
                        + " INNER JOIN VAM_PriceList pl ON (bpl.VAM_PriceList_ID=pl.BasePriceList_ID) "
                        + "WHERE pv.IsActive='Y'"
                        + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                        + " AND pl.VAM_PriceList_ID= " + _VAM_PriceList_ID			//	#2
                        + " AND pp.VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_ID   //	#3
                        + " AND pp.VAB_UOM_ID = " + _VAB_UOM_ID  //    #4
                        + " AND pp.IsActive='Y'"
                        + "ORDER BY pv.ValidFrom DESC";
                }
                else
                {
                    sql = "SELECT bomPriceStdAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceStd,"	//	1
                        + " boMVAMPriceListAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceList,"		//	2
                        + " bomPriceLimitAttr(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID,pp.VAM_PFeature_SetInstance_ID) AS PriceLimit,"	//	3
                        + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,"	//	4..7
                        + " pl.EnforcePriceLimit, pl.IsTaxIncluded "	// 8..9
                        + "FROM VAM_Product p"
                        + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                        + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                        + " INNER JOIN VAM_PriceList bpl ON (pv.VAM_PriceList_ID=bpl.VAM_PriceList_ID)"
                        + " INNER JOIN VAM_PriceList pl ON (bpl.VAM_PriceList_ID=pl.BasePriceList_ID) "
                        + "WHERE pv.IsActive='Y'"
                        + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                        + " AND pl.VAM_PriceList_ID= " + _VAM_PriceList_ID			//	#2
                        + " AND pp.VAM_PFeature_SetInstance_ID = " + _VAM_PFeature_SetInstance_ID   //	#3
                        + "ORDER BY pv.ValidFrom DESC";
                }
            }
            else
            {

                sql = "SELECT bomPriceStd(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceStd,"	//	1
                    + " boMVAMPriceList(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceList,"		//	2
                    + " bomPriceLimit(p.VAM_Product_ID,pv.VAM_PriceListVersion_ID) AS PriceLimit,"	//	3
                    + " p.VAB_UOM_ID,pv.ValidFrom,pl.VAB_Currency_ID,p.VAM_ProductCategory_ID,"	//	4..7
                    + " pl.EnforcePriceLimit, pl.IsTaxIncluded "	// 8..9
                    + "FROM VAM_Product p"
                    + " INNER JOIN VAM_ProductPrice pp ON (p.VAM_Product_ID=pp.VAM_Product_ID)"
                    + " INNER JOIN  VAM_PriceListVersion pv ON (pp.VAM_PriceListVersion_ID=pv.VAM_PriceListVersion_ID)"
                    + " INNER JOIN VAM_PriceList bpl ON (pv.VAM_PriceList_ID=bpl.VAM_PriceList_ID)"
                    + " INNER JOIN VAM_PriceList pl ON (bpl.VAM_PriceList_ID=pl.BasePriceList_ID) "
                    + "WHERE pv.IsActive='Y'"
                    + " AND p.VAM_Product_ID=" + _VAM_Product_ID				//	#1
                    + " AND pl.VAM_PriceList_ID= " + _VAM_PriceList_ID			//	#2
                    + "ORDER BY pv.ValidFrom DESC";
            }
            _calculated = false;
            if (_PriceDate == null)
                _PriceDate = DateTime.Now;
            IDataReader dr = null;
            try
            {
                dr = ExecuteQuery.ExecuteReader(sql, null);
                while (!_calculated && dr.Read())
                {
                    DateTime? plDate = Utility.Util.GetValueOfDateTime(dr[4]);
                    //	we have the price list
                    //	if order date is after or equal PriceList validFrom
                    if (plDate == null || !(_PriceDate < plDate))
                    {
                        //	Prices
                        _PriceStd = Utility.Util.GetValueOfDecimal(dr[0]);
                        //if (dr.wasNull())
                        if (dr[0] == null)
                            _PriceStd = Env.ZERO;
                        _PriceList = Utility.Util.GetValueOfDecimal(dr[1]);
                        //if (dr.wasNull())
                        if (dr[1] == null)
                            _PriceList = Env.ZERO;
                        _PriceLimit = Utility.Util.GetValueOfDecimal(dr[2]);
                        //if (dr.wasNull())
                        if (dr[2] == null)
                            _PriceLimit = Env.ZERO;
                        //
                        _VAB_UOM_ID = Utility.Util.GetValueOfInt(dr[3].ToString());
                        _VAB_Currency_ID = Utility.Util.GetValueOfInt(dr[5].ToString());
                        _VAM_ProductCategory_ID = Utility.Util.GetValueOfInt(dr[6].ToString());
                        _enforcePriceLimit = "Y".Equals(dr[7].ToString());
                        _isTaxIncluded = "Y".Equals(dr[8].ToString());
                        //
                        log.Fine("VAM_PriceList_ID=" + _VAM_PriceList_ID + "(" + plDate + ")" + " - " + _PriceStd);
                        _calculated = true;
                        break;
                    }
                }
                dr.Close();
            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
                _calculated = false;
            }
            if (!_calculated)
            {
                log.Finer("Not found (BPL)");
            }
            return _calculated;
        }

        /// <summary>
        /// Set Base Info (UOM)
        /// </summary>
        private void SetBaseInfo()
        {
            if (_VAM_Product_ID == 0)
                return;
            //
            String sql = "SELECT VAB_UOM_ID, VAM_ProductCategory_ID FROM VAM_Product WHERE VAM_Product_ID=" + _VAM_Product_ID;
            IDataReader dr = null;
            try
            {
                dr = ExecuteQuery.ExecuteReader(sql, null);
                if (dr.Read())
                {
                    _VAB_UOM_ID = Utility.Util.GetValueOfInt(dr[0]);//.getInt(1);
                    _VAM_ProductCategory_ID = Utility.Util.GetValueOfInt(dr[1]);//.getInt(2);
                }
                dr.Close();
            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                }
                log.Log(Level.SEVERE, sql, e);
            }

        }

        /// <summary>
        /// Is Tax Included
        /// </summary>
        /// <returns>tax included</returns>
        public bool IsTaxIncluded()
        {
            return _isTaxIncluded;
        }

        /// <summary>
        /// Calculate (Business Partner) Discount
        /// </summary>
        private void CalculateDiscount()
        {
            try
            {
                _discountSchema = false;
                if (_VAB_BusinessPartner_ID == 0 || _VAM_Product_ID == 0)
                    return;

                int VAM_DiscountCalculation_ID = 0;
                Decimal? FlatDiscount = null;
                String sql = "SELECT COALESCE(p.VAM_DiscountCalculation_ID,g.VAM_DiscountCalculation_ID),"
                    + " COALESCE(p.PO_DiscountSchema_ID,g.PO_DiscountSchema_ID), p.FlatDiscount "
                    + "FROM VAB_BusinessPartner p"
                    + " INNER JOIN VAB_BPart_Category g ON (p.VAB_BPart_Category_ID=g.VAB_BPart_Category_ID) "
                    + "WHERE p.VAB_BusinessPartner_ID=" + _VAB_BusinessPartner_ID;
                DataTable dt = null;
                IDataReader idr = null;
                try
                {
                    idr = DataBase.DB.ExecuteReader(sql, null, null);
                    dt = new DataTable();
                    dt.Load(idr);
                    idr.Close();
                    //if (dr.Read())
                    foreach (DataRow dr in dt.Rows)
                    {
                        VAM_DiscountCalculation_ID = Utility.Util.GetValueOfInt(dr[_isSOTrx ? 0 : 1]);
                        if (dr[2] == DBNull.Value)
                        {
                            FlatDiscount = Env.ZERO;
                        }
                        else
                        {
                            FlatDiscount = Utility.Util.GetValueOfDecimal(dr[2]);//.getBigDecimal(3);
                        }
                        if (FlatDiscount == null)
                        {
                            FlatDiscount = Env.ZERO;
                        }
                    }

                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                    }
                    log.Log(Level.SEVERE, sql, e);
                }
                finally
                {
                    dt = null;
                    if (idr != null)
                    {
                        idr.Close();
                    }
                }
                //	No Discount Schema
                if (VAM_DiscountCalculation_ID == 0)
                    return;

                MVAMDiscountCalculation sd = MVAMDiscountCalculation.Get(Env.GetContext(), VAM_DiscountCalculation_ID);	//	not correct
                if (sd.Get_ID() == 0)
                    return;
                //
                _discountSchema = true;
                _PriceStd = sd.CalculatePrice(_qty, _PriceStd, _VAM_Product_ID, _VAM_ProductCategory_ID, (decimal)FlatDiscount);
            }
            catch (Exception ex)
            {
                // MessageBox.Show("MVAMProductPricing--CalculateDiscount");
                log.Severe(ex.ToString());
            }

        }

        /// <summary>
        /// Calculate Discount Percentage based on Standard/List Price
        /// </summary>
        /// <returns>Discount</returns>
        public Decimal GetDiscount()
        {
            Decimal Discount = Env.ZERO;
            if (_PriceList != 0)
            {
                Discount = new Decimal((Decimal.ToDouble(_PriceList) - Decimal.ToDouble(_PriceStd)) / Decimal.ToDouble(_PriceList) * 100.0);
            }
            if (Env.Scale(Discount) > 2)
                Discount = Decimal.Round(Discount, 2);//, MidpointRounding.AwayFromZero);
            return Discount;
        }

        /// <summary>
        /// Get Line Amt
        /// </summary>
        /// <param name="currencyPrecision">precision -1 = ignore</param>
        /// <returns>Standard Price * Qty</returns>
        public Decimal GetLineAmt(int currencyPrecision)
        {
            Decimal amt = Decimal.Multiply(GetPriceStd(), _qty);
            //	Currency Precision
            if (currencyPrecision >= 0 && Env.Scale(amt) > currencyPrecision)
            {
                amt = Decimal.Round(amt, currencyPrecision);//, MidpointRounding.AwayFromZero);
            }
            return amt;
        }

        /// <summary>
        /// Get Product ID
        /// </summary>
        /// <returns>id</returns>
        public int GetVAM_Product_ID()
        {
            return _VAM_Product_ID;
        }

        /// <summary>
        /// Get PriceList ID
        /// </summary>
        /// <returns>pl</returns>
        public int GetVAM_PriceList_ID()
        {
            return _VAM_PriceList_ID;
        }

        /// <summary>
        /// Set PriceList
        /// </summary>
        /// <param name="VAM_PriceList_ID">pl</param>
        public void SetVAM_PriceList_ID(int VAM_PriceList_ID)
        {
            _VAM_PriceList_ID = VAM_PriceList_ID;
            _calculated = false;
        }

        /// <summary>
        /// Get Attribute Set Instance
        /// </summary>
        /// <returns>plv</returns>
        public int GetVAM_PFeature_SetInstance_ID()
        {
            return _VAM_PFeature_SetInstance_ID;
        }

        /// <summary>
        /// Set Attribute Set Instance
        /// </summary>
        /// <param name="VAM_PFeature_SetInstance_ID">plv</param>
        public void SetVAM_PFeature_SetInstance_ID(int VAM_PFeature_SetInstance_ID)
        {
            _VAM_PFeature_SetInstance_ID = VAM_PFeature_SetInstance_ID;
            _calculated = false;
        }

        /// <summary>
        /// Get PriceList Version
        /// </summary>
        /// <returns>plv</returns>
        public int GetVAM_PriceListVersion_ID()
        {
            return _VAM_PriceListVersion_ID;
        }

        /// <summary>
        /// Set PriceList Version
        /// </summary>
        /// <param name="VAM_PriceListVersion_ID">plv</param>
        public void SetVAM_PriceListVersion_ID(int VAM_PriceListVersion_ID)
        {
            _VAM_PriceListVersion_ID = VAM_PriceListVersion_ID;
            _calculated = false;
        }

        /// <summary>
        /// Get Price Date
        /// </summary>
        /// <returns>date</returns>
        public DateTime? GetPriceDate()
        {
            return _PriceDate;
        }

        /// <summary>
        /// Set Price Date
        /// </summary>
        /// <param name="priceDate">date</param>
        public void SetPriceDate(DateTime? priceDate)
        {
            _PriceDate = (DateTime?)priceDate;
            _calculated = false;
        }

        /// <summary>
        /// Set Price Date
        /// </summary>
        /// <param name="priceTime">priceTime date</param>
        public void SetPriceDate(long priceTime)
        {
            SetPriceDate(Convert.ToDateTime(priceTime));
        }

        public void SetPriceDate1(DateTime? priceTime)
        {
            SetPriceDate(priceTime);
        }
        /// <summary>
        /// Set Price List Precision.
        /// </summary>
        private void SetPrecision()
        {
            if (_VAM_PriceList_ID != 0)
                _precision = MVAMPriceList.GetPricePrecision(Env.GetContext(), GetVAM_PriceList_ID());
        }

        /// <summary>
        /// Get Price List Precision
        /// </summary>
        /// <returns>precision - -1 = no rounding</returns>
        public int GetPrecision()
        {
            return _precision;
        }

        /// <summary>
        /// Round
        /// </summary>
        /// <param name="bd">number</param>
        /// <returns>rounded number</returns>
        private Decimal Round(Decimal bd)
        {
            if (_precision >= 0	//	-1 = no rounding
                && Env.Scale(bd) > _precision)
            {
                return Decimal.Round(bd, _precision, MidpointRounding.AwayFromZero);
            }
            return bd;
        }

        /// <summary>
        /// Get VAB_UOM_ID
        /// </summary>
        /// <returns>uom</returns>
        public int GetVAB_UOM_ID()
        {
            if (!_calculated)
                CalculatePrice();
            return _VAB_UOM_ID;
        }


        /// <summary>
        /// Set VAB_UOM_ID
        /// </summary>
        /// <returns></returns>
        public void SetVAB_UOM_ID(int VAB_UOM_ID)
        {
            _VAB_UOM_ID = VAB_UOM_ID;
            _calculated = false;
        }


        /// <summary>
        /// Get Price List
        /// </summary>
        /// <returns>list</returns>
        public Decimal GetPriceList()
        {
            if (!_calculated)
                CalculatePrice();
            return Round(_PriceList);
        }

        /// <summary>
        /// Get Price Std
        /// </summary>
        /// <returns>std</returns>
        public Decimal GetPriceStd()
        {
            if (!_calculated)
                CalculatePrice();
            return Round(_PriceStd);
        }

        /// <summary>
        /// Get Price Limit
        /// </summary>
        /// <returns>limit</returns>
        public Decimal GetPriceLimit()
        {
            if (!_calculated)
                CalculatePrice();
            return Round(_PriceLimit);
        }

        /// <summary>
        /// Get Price List Currency
        /// </summary>
        /// <returns>currency</returns>
        public int GetVAB_Currency_ID()
        {
            if (!_calculated)
                CalculatePrice();
            return _VAB_Currency_ID;
        }

        /// <summary>
        /// Is Price List enforded?
        /// </summary>
        /// <returns>enforce limit</returns>
        public bool IsEnforcePriceLimit()
        {
            if (!_calculated)
                CalculatePrice();
            return _enforcePriceLimit;
        }

        /// <summary>
        /// Is a DiscountSchema active?
        /// </summary>
        /// <returns>active Discount Schema</returns>
        public bool IsDiscountSchema()
        {
            return _discountSchema;
        }

        /// <summary>
        /// Is the Price Calculated (i.e. found)?
        /// </summary>
        /// <returns>calculated</returns>
        public bool IsCalculated()
        {
            return _calculated;
        }
    }
}