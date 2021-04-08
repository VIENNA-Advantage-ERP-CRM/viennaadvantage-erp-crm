﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : MVAMPriceListVersion
 * Purpose        : 
 * Class Used     : 
 * Chronological    Development
 * Raghunandan     09-Jun-2009
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
//////using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    public class MVAMPriceListVersion : X_VAM_PriceListVersion
    {
        // Product Prices			
        private MVAMProductPrice[] _pp = null;
        // Price List				
        private MVAMPriceList _pl = null;

        /// <summary>
        /// Standard Cinstructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAM_PriceListVersion_ID">id</param>
        /// <param name="trxName">transaction</param>
        public MVAMPriceListVersion(Ctx ctx, int VAM_PriceListVersion_ID, Trx trxName)
            : base(ctx, VAM_PriceListVersion_ID, trxName)
        {
            if (VAM_PriceListVersion_ID == 0)
            {
                //	setName (null);	// @#Date@
                //	setVAM_PriceList_ID (0);
                //	setValidFrom (TimeUtil.getDay(null));	// @#Date@
                //	setVAM_DiscountCalculation_ID (0);
            }
        }

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">datarow</param>
        /// <param name="trxName">transaction</param>
        public MVAMPriceListVersion(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }

        /// <summary>
        /// Parent Constructor
        /// </summary>
        /// <param name="pl">parent</param>
        public MVAMPriceListVersion(MVAMPriceList pl)
            : this(pl.GetCtx(), 0, pl.Get_TrxName())
        {
            SetClientOrg(pl);
            SetVAM_PriceList_ID(pl.GetVAM_PriceList_ID());
        }

        /// <summary>
        /// Get Parent PriceList
        /// </summary>
        /// <returns>v</returns>
        public MVAMPriceList GetPriceList()
        {
            if (_pl == null && GetVAM_PriceList_ID() != 0)
            {
                _pl = MVAMPriceList.Get(GetCtx(), GetVAM_PriceList_ID(), null);
            }
            return _pl;
        }

        /// <summary>
        /// Get Product Price
        /// </summary>
        /// <param name="refresh">true if refresh</param>
        /// <returns>product price</returns>
        public MVAMProductPrice[] GetProductPrice(bool refresh)
        {
            if (_pp != null && !refresh)
                return _pp;
            _pp = GetProductPrice(null);
            return _pp;
        }

        /// <summary>
        /// Get Product Price
        /// </summary>
        /// <param name="whereClause">optional where clause</param>
        /// <returns>product price</returns>
        public MVAMProductPrice[] GetProductPrice(String whereClause)
        {
            List<MVAMProductPrice> list = new List<MVAMProductPrice>();
            String sql = "SELECT * FROM VAM_ProductPrice WHERE VAM_PriceListVersion_ID=" + GetVAM_PriceListVersion_ID();
            if (whereClause != null)
                sql += " " + whereClause;
            DataSet ds = null;
            try
            {
                ds = DataBase.DB.ExecuteDataset(sql, null, Get_TrxName());
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    DataRow dr = ds.Tables[0].Rows[i];
                    list.Add(new MVAMProductPrice(GetCtx(), dr, Get_TrxName()));
                }
                ds = null;
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            //
            MVAMProductPrice[] pp = new MVAMProductPrice[list.Count];
            pp = list.ToArray();
            return pp;
        }

        /// <summary>
        /// Set Name to Valid From Date.
        /// If valid from not set, use today
        /// </summary>
        public void SetName()
        {
            if (GetValidFrom() == null)
            {
                SetValidFrom(TimeUtil.GetDay(null));
            }
            if (GetName() == null)
            {
                //String name = DisplayType.getDateFormat(DisplayType.Date).format(getValidFrom());
                String name = GetValidFrom().ToString();
                SetName(name);
            }
        }

        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord">new</param>
        /// <returns>true</returns>
        protected override bool BeforeSave(bool newRecord)
        {
            SetName();
            return true;
        }
    }
}