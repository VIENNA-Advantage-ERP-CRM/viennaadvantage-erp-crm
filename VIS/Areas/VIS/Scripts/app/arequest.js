﻿; (function (VIS, $) {

    var baseUrl = VIS.Application.contextUrl;
    var dataSetUrl = baseUrl + "JsonData/JDataSetWithCode";

    var executeReader = function (sql, param, callback) {
        var async = callback ? true : false;

        var dataIn = { sql: sql, page: 1, pageSize: 0 };
        if (param) {
            dataIn.param = param;
        }
        var dr = null;
        getDataSetJString(dataIn, async, function (jString) {
            dr = new VIS.DB.DataReader().toJson(jString);
            if (callback) {
                callback(dr);
            }
        });
        return dr;
    };

    //DataSet String
    function getDataSetJString(data, async, callback) {
        var result = null;
       // data.sql = VIS.secureEngine.encrypt(data.sql);
        $.ajax({
            url: dataSetUrl,
            type: "POST",
            datatype: "json",
            contentType: "application/json; charset=utf-8",
            async: async,
            data: JSON.stringify(data)
        }).done(function (json) {
            result = json;
            if (callback) {
                callback(json);
            }
            //return result;
        });
        return result;
    };


    function ARequest(invoker, VAF_TableView_ID, Record_ID, VAB_BusinessPartner_ID, iBusy, container) {
        var VAF_Screen_ID = 232;
        var m_where = '';
        var window = null;
        var tab = null;

        this.getRequests = function (item) {

            var sql = "VIS_72";
            var dr = executeReader(sql, null);
            while (dr.read()) {
                if (parseInt(dr.getString(0)) == 0) {
                    VIS.ADialog.error('VIS_NotSupported');
                    return;
                }
            }
            m_where = "(VAF_TableView_ID=" + VAF_TableView_ID + " AND Record_ID=" + Record_ID + ")";

            if (VAF_TableView_ID == 114) {// MUser.Table_ID){
                m_where += " OR VAF_UserContact_ID=" + Record_ID + " OR SalesRep_ID=" + Record_ID;
            }
            else if (VAF_TableView_ID == 291) {//MBPartner.Table_ID){
                m_where += " OR VAB_BusinessPartner_ID=" + Record_ID;
            }
            else if (VAF_TableView_ID == 259) {// MOrder.Table_ID){
                m_where += " OR VAB_Order_ID=" + Record_ID;
            }
            else if (VAF_TableView_ID == 318) {//MVABInvoice.Table_ID){
                m_where += " OR VAB_Invoice_ID=" + Record_ID;
            }
            else if (VAF_TableView_ID == 335) {// MPayment.Table_ID){
                m_where += " OR VAB_Payment_ID=" + Record_ID;
            }
            else if (VAF_TableView_ID == 208) {//MProduct.Table_ID){
                m_where += " OR VAM_Product_ID=" + Record_ID;
            }
            else if (VAF_TableView_ID == 203) {//MProject.Table_ID){
                m_where += " OR VAB_Project_ID=" + Record_ID;
            }
            else if (VAF_TableView_ID == 539) {// MAsset.Table_ID){
                m_where += " OR VAA_Asset_ID=" + Record_ID;
            }
            //sql = "SELECT Processed, COUNT(*) "
            //    + "FROM VAR_Request WHERE " + m_where
            //    + " GROUP BY Processed "
            //    + "ORDER BY Processed DESC";

            //dr = executeReader(sql, null);


           var dr=null;
              $.ajax({
                type: 'Get',
                async: false,
                url: VIS.Application.contextUrl + "Form/GetProcessedRequest",
                data: { VAF_TableView_ID: VAF_TableView_ID, Record_ID: Record_ID },
                success: function (data) {
                    dr = new VIS.DB.DataReader().toJson(data);
                },
            });

            var inactiveCount = 0;
            var activeCount = 0;

            while (dr.read()) {
                if ("Y" == dr.getString(0))
                    inactiveCount = dr.getString(1);
                else
                    activeCount += dr.getString(1);
            }

            var $root = $("<div>");
            var ul = $('<ul class=vis-apanel-rb-ul>');
            $root.append(ul);
            var li = $("<li data-id='RequestCreate'>");
            li.append(VIS.Msg.getMsg("RequestNew"));
            li.on("click", function (e) {
                createNewRequest(e);
            });
            ul.append(li);
            if (activeCount > 0) {
                li = $("<li data-id='RequestActive'>");
                li.append(VIS.Msg.getMsg("RequestActive"));
                li.on("click", function (e) {
                    activeRequest(e);
                });
                ul.append(li);
            }
            if (inactiveCount > 0) {
                li = $("<li data-id='RequestActive'>");
                li.append(VIS.Msg.getMsg("RequestAll"));
                li.on("click", function (e) {
                    allRequest(e);
                });
                ul.append(li);
            }
            container.w2overlay($root.clone(true), { css: { height: '200px' } });

        };


        var createNewRequest = function (e) {

            e.stopImmediatePropagation();
            //var vm=new VIS.viewManager();
            window = VIS.viewManager.startWindow(VAF_Screen_ID, null);
            window.onLoad = function () {
                var gc = window.cPanel.curGC;

                gc.onRowInserting = function () {
                    window.cPanel.cmd_new(false);
                };

                gc.onRowInserted = function () {
                    tab = window.cPanel.curTab;
                    tab.setValue("VAF_TableView_ID", VAF_TableView_ID);
                    tab.setValue("Record_ID", Record_ID);

                    if (VAB_BusinessPartner_ID != null && VAB_BusinessPartner_ID > 0)
                    {
                        tab.setValue("VAB_BusinessPartner_ID", VAB_BusinessPartner_ID);
                    }

                    if (VAF_TableView_ID == 291)// MBPartner.Table_ID)
                        tab.setValue("VAB_BusinessPartner_ID", Record_ID);
                    else if (VAF_TableView_ID == 114)//MUser.Table_ID)
                        tab.setValue("VAF_UserContact_ID", Record_ID);
                        //
                    else if (VAF_TableView_ID == 203)// MProject.Table_ID)
                        tab.setValue("VAB_Project_ID", Record_ID);
                    else if (VAF_TableView_ID == 539)// MAsset.Table_ID)
                        tab.setValue("VAA_Asset_ID", Record_ID);

                    else if (VAF_TableView_ID == 259)
                        tab.setValue("VAB_Order_ID", Record_ID);
                    else if (VAF_TableView_ID == 318)//MVABInvoice.Table_ID)
                        tab.setValue("VAB_Invoice_ID", Record_ID);
                        //
                    else if (VAF_TableView_ID == 208)//MProduct.Table_ID)
                        tab.setValue("VAM_Product_ID", Record_ID);
                    else if (VAF_TableView_ID == 335)//MPayment.Table_ID)
                        tab.setValue("VAB_Payment_ID", Record_ID);
                        //
                    else if (VAF_TableView_ID == 319)// MVAMInvInOut.Table_ID)
                        tab.setValue("VAM_Inv_InOut_ID", Record_ID);
                };


               // window.cPanel.cmd_new(false);
                //tab = window.cPanel.curTab;


                
            };

            var overlay = $('#w2ui-overlay');
            overlay.hide();
            overlay = null;
            //window = null;
            //tab = null;
        };

        var activeRequest = function (e) {
            e.stopImmediatePropagation();
            var zoomQuery = new VIS.Query();
            zoomQuery.addRestriction("(" + m_where + ") AND Processed='N'");
            VIS.viewManager.startWindow(VAF_Screen_ID, zoomQuery);
            var overlay = $('#w2ui-overlay');
            overlay.hide();
            overlay = null;
        };

        var allRequest = function (e) {

            e.stopImmediatePropagation();
            var zoomQuery = new VIS.Query();
            zoomQuery.addRestriction(m_where);
            VIS.viewManager.startWindow(VAF_Screen_ID, zoomQuery);
            var overlay = $('#w2ui-overlay');
            overlay.hide();
            overlay = null;
        };

        

    };
    VIS.ARequest = ARequest;
})(VIS, jQuery);