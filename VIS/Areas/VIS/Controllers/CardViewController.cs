﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using VAdvantage.Utility;
using VIS.Models;
namespace VIS.Controllers
{

    public class CardViewController : Controller
    {
        //
        // GET: /VIS/CardView/
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetCardView(int ad_Window_ID, int vaf_tab_ID)
        {
            Ctx ctx = Session["ctx"] as Ctx;
            CardViewModel objCardViewModel = new CardViewModel();
            List<CardViewPropeties> lstCardView = objCardViewModel.GetCardView(ad_Window_ID, vaf_tab_ID, ctx);
            List<RolePropeties> lstRole = objCardViewModel.GetAllRoles(ctx);
            List<List<RolePropeties>> lstCardViewRole = new List<List<RolePropeties>>();
            List<CardViewConditionPropeties> lstCVConditon = new List<CardViewConditionPropeties>();
            if (lstCardView != null)
            {
                for (int i = 0; i < lstCardView.Count; i++)
                {
                    lstCardViewRole.Add(objCardViewModel.GetCardViewRole(lstCardView[i].CardViewID, ctx));
                    lstCVConditon = objCardViewModel.GetCardViewCondition(lstCardView[i].CardViewID, ctx);
                }
            }
            ParameterPropeties objParamProperties = new ParameterPropeties()
            {
                lstCardViewData = lstCardView,
                lstRoleData = lstRole,
                lstCardViewRoleData = lstCardViewRole,
                lstCardViewConditonData = lstCVConditon

            };
            List<ParameterPropeties> lstParamProperties = new List<ParameterPropeties>();
            lstParamProperties.Add(objParamProperties);
            var jsonResult = Json(JsonConvert.SerializeObject(lstParamProperties), JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }


        public JsonResult GetCardViewColumns(int VAF_CardView_ID)
        {
            Ctx ctx = Session["ctx"] as Ctx;
            CardViewModel objCardViewModel = new CardViewModel();
            List<CardViewPropeties> lstCardView = objCardViewModel.GetCardViewColumns(VAF_CardView_ID, ctx);
            List<CardViewConditionPropeties> lstCVConditon = objCardViewModel.GetCardViewCondition(VAF_CardView_ID, ctx);
            ParameterPropeties objParamProperties = new ParameterPropeties()
            {
                lstCardViewData = lstCardView,
                lstCardViewConditonData = lstCVConditon

            };
            List<ParameterPropeties> lstParamProperties = new List<ParameterPropeties>();

            lstParamProperties.Add(objParamProperties);
            var jsonResult = Json(JsonConvert.SerializeObject(lstParamProperties), JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpPost]
        public JsonResult SaveCardViewColumns(List<CardViewPropeties> lstCardView, List<CardViewPropeties> lstCardViewColumns, List<RolePropeties> LstRoleID, List<CardViewConditionPropeties> lstCardViewCondition)
        {
            bool isNewRecord = lstCardView[0].isNewRecord;
            int id = 0;
            Ctx ctx = Session["ctx"] as Ctx;
            CardViewModel objCardViewModel = new CardViewModel();
            if (isNewRecord)
            {
                id = objCardViewModel.SaveCardViewRecord(lstCardView[0].CardViewName, lstCardView[0].AD_Window_ID, lstCardView[0].VAF_Tab_ID, lstCardView[0].UserID, lstCardView[0].VAF_GroupField_ID, ctx, 0, LstRoleID, lstCardViewCondition);
            }
            else
            {
                objCardViewModel.DeleteAllCardViewColumns(lstCardView[0].CardViewID, ctx);
                id = objCardViewModel.SaveCardViewRecord(lstCardView[0].CardViewName, lstCardView[0].AD_Window_ID, lstCardView[0].VAF_Tab_ID, lstCardView[0].UserID, lstCardView[0].VAF_GroupField_ID, ctx, lstCardView[0].CardViewID, LstRoleID, lstCardViewCondition);
            }

            if (lstCardView[0].IsDefault)
            {
                objCardViewModel.SetDefaultCardView(ctx, id, lstCardView[0].VAF_Tab_ID);
            }

            int sqNo = 0;
            if (lstCardViewColumns != null)
            {
                for (int i = 0; i < lstCardViewColumns.Count; i++)
                {
                    if (isNewRecord)
                    {
                        lstCardViewColumns[i].CardViewID = id;
                    }
                    sqNo = i * 10;
                    objCardViewModel.SaveCardViewColumns(lstCardViewColumns[i].CardViewID, lstCardViewColumns[i].VAF_Field_ID, sqNo, ctx);
                    id = lstCardViewColumns[i].CardViewID;
                }
            }
            var jsonResult = Json(JsonConvert.SerializeObject(id), JsonRequestBehavior.AllowGet); jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;


        }
        [HttpPost]
        public JsonResult DeleteCardViewRecord(int VAF_CardView_ID)
        {
            Ctx ctx = Session["ctx"] as Ctx;
            CardViewModel objCardViewModel = new CardViewModel();
            objCardViewModel.DeleteCardViewRecord(VAF_CardView_ID, ctx);
            var jsonResult = Json(JsonConvert.SerializeObject(""), JsonRequestBehavior.AllowGet); jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public JsonResult SetDefaultView(int VAF_Tab_ID, int cardView)
        {
            Ctx ctx = Session["ctx"] as Ctx;
            CardViewModel objCardViewModel = new CardViewModel();
            objCardViewModel.SetDefaultView(ctx, VAF_Tab_ID, cardView);
            var jsonResult = Json(JsonConvert.SerializeObject(""), JsonRequestBehavior.AllowGet);
            return jsonResult;
        }
    }
}