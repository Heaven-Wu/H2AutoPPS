using System.Data;
using System.Web.Http;
using System;
using H2AutoPPS.Models;
using H2AutoPPS.Services;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace H2AutoPPS.Controllers
{
    [RoutePrefix("Out")]
    public class OutController : ApiController
    {
        public string strSql;
        public DataTable dt = new DataTable();
        public static string MAC = "84-A9-3E-63-F6-4D";

        [HttpGet, Route("CheckSNandReturnLabelInfor")]
        public IHttpActionResult CheckSNandReturnLabelInfor(string SNList)
        {
            WebResult result = new WebResult();
            try
            {
                if (string.IsNullOrEmpty(SNList))
                {
                    result.Msg = "请传入SN";
                    return Json(result);
                }

                Dictionary<string, object> dic = new Dictionary<string, object>();

                string[] strSNList = SNList.Split(',');
                string strSN = strSNList[0].ToUpper().TrimStart('S');
                if (strSNList[0].Length != 22 && strSNList[0].Length != 15 && strSNList[0].Length != 13 && strSNList[0].Length != 10 && strSNList[0].Length != 18 && strSNList[0].Length != 20)
                {
                    result.Msg = "此SN: " + strSN + "的格式错误，请check";
                    return Json(result);
                }

                strSql = "select Cust_PN_2 from QMSdb.dbo.QMS_SNInfo where right(Serial_Number,12) ='" + strSN + "'";
                dt = clsGlobalVar.GetDataTable(strSql);
                if (dt.Rows.Count > 0)
                {
                    string strclimat = dt.Rows[0][0].ToString().ToUpper();
                    if (strclimat.Trim().Substring(0, 1) == "Z" && strclimat.IndexOf('/') == -1)
                    {
                        strSql = "select distinct soldto,shpmrk from PAL_QSMC_USA.dbo.BingdingSNandShpmrk where SN='" + strSN + "'";
                        dt = clsGlobalVar.GetDataTable(strSql);
                        if (dt.Rows.Count <= 0)
                        {
                            result.Msg = "CTO的机器必须先进行绑板操作!!";
                            return Json(result);
                        }
                    }
                }

                dic.Add("SN", strSN);
                #region//1.从唛头与SN绑定的表中判断扫描的SN是不是EDI的出货
                strSql = "select distinct soldto,shpmrk from PAL_QSMC_USA.dbo.BingdingSNandShpmrk where SN='" + strSN + "'";
                dt = clsGlobalVar.GetDataTable(strSql);

                if (dt.Rows.Count > 0)
                {
                    string strsoldto = ((string)dt.Rows[0][0]).ToUpper();
                    string strShpmrk = (string)dt.Rows[0][1];
                    string strLoadid = strShpmrk.Substring(0, 10);
                    string strPid = strShpmrk.Substring(11, 2);
                    //首先判断扫描的SNList中是不是所有的SN都属于这个板
                    for (int i = 0; i < strSNList.Length; i++)
                    {
                        strSql = "select distinct soldto,shpmrk from PAL_QSMC_USA.dbo.BingdingSNandShpmrk where shpmrk='" + strShpmrk + "' and SN='" + strSNList[i].ToUpper().TrimStart('S') + "'";
                        dt = clsGlobalVar.GetDataTable(strSql);
                        if (dt.Rows.Count <= 0)
                        {
                            result.Msg = "此SN: " + strSNList[i] + "不属于该唛头，请check";
                            return Json(result);
                        }
                    }
                    dic.Add("PLTMAK", strShpmrk);
                    string strZPLLabel = EDIReturnLableZPL(dic, strsoldto, strLoadid, strPid);
                    if (strZPLLabel != "_OK_")
                    {
                        result.Msg = strZPLLabel;
                    }
                    else
                    {
                        string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_EDIShip','" + strShpmrk + "',GETDATE(),'" + strSN + "')";
                        clsGlobalVar.ExecSQL(sqlLog);
                        result.Status = 200;
                        result.Msg = "label信息返回成功";
                        result.Data = dic;
                    }
                    return Json(result);
                }
                #endregion
                #region//2.通过SN找是不是我们系统的出货
                else
                {
                    strSql = "select loadid,j.refnm1,c.SOLDTO from [PAL_QSMC_USA].dbo.cast(nolock) c inner join [PAL_QSMC_USA].dbo.joit(nolock) j on c.CASENO=j.CASENO and j.SERNUM='" + strSN + "' union select loadid,j.refnm1,c.SOLDTO from [PAL_QSMC_EMEA].dbo.cast(nolock) c inner join [PAL_QSMC_EMEA].dbo.joit(nolock) j on c.CASENO=j.CASENO and j.SERNUM='" + strSN + "' union select loadid,j.refnm1,c.SOLDTO from [PAL_QSMC_ASIA].dbo.cast(nolock) c inner join [PAL_QSMC_ASIA].dbo.joit(nolock) j on c.CASENO=j.CASENO and j.SERNUM='" + strSN + "'";
                    dt = clsGlobalVar.GetDataTable(strSql);
                    if (dt.Rows.Count > 0)
                    {
                        string strLoadid = dt.Rows[0][0].ToString();
                        string strPid = dt.Rows[0][1].ToString().Substring(1, 2);
                        string strsoldtoOld = dt.Rows[0][2].ToString();
                        string strsoldto = getSoldto(strsoldtoOld);
                        //首先判断扫描的SNList中是不是所有的SN都属于这个Loadid
                        for (int i = 0; i < strSNList.Length; i++)
                        {
                            strSql = "select * from " + strsoldto + ".dbo.cast c inner join " + strsoldto + ".dbo.joit j on c.CASENO=j.CASENO where LOADID='" + strLoadid + "' AND  right(j.refnm1, 2) = '" + strPid + "' and SERNUM = '" + strSNList[i].ToUpper().TrimStart('S') + "'";
                            dt = clsGlobalVar.GetDataTable(strSql);
                            if (dt.Rows.Count <= 0)
                            {
                                result.Msg = "此SN: " + strSNList[i] + "不属于该Loadid或该板，请check";
                                return Json(result);
                            }
                        }
                        string strShpmrk = strLoadid + "-" + strPid;
                        dic.Add("PLTMAK", strShpmrk);
                        string strZPLLabel = EDIReturnLableZPL(dic, strsoldtoOld, strLoadid, strPid);
                        if (strZPLLabel != "_OK_")
                        {
                            result.Msg = strZPLLabel;
                        }
                        else
                        {
                            string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_EDIShip','" + strShpmrk + "',GETDATE(),'" + strSN + "')";
                            clsGlobalVar.ExecSQL(sqlLog);
                            result.Status = 200;
                            result.Msg = "label信息返回成功";
                            result.Data = dic;
                        }
                        return Json(result);
                    }
                    #endregion
                    #region//3.nonedi出货的sn确认
                    else
                    {
                        strSql = string.Format(@"select PalletID from SDS_NONEDI.dbo.Shipment_Unit where SerialNo = '{0}' or QMS_BoxID = '{0}'", strSN);
                        string strPalletID = "";
                        strPalletID = clsGlobalVar.GetFieldValue(strSql);
                        if (strPalletID != null && strPalletID != "")
                        {
                            for (int i = 0; i < strSNList.Length; i++)
                            {
                                strSql = string.Format(@"select PalletID from SDS_NONEDI.dbo.Shipment_Unit where (SerialNo = '{0}' or QMS_BoxID = '{0}') and PalletID = '{1}'", strSNList[i].ToUpper().TrimStart('S'), strPalletID);
                                dt = clsGlobalVar.GetDataTable(strSql);
                                if (dt.Rows.Count <= 0)
                                {
                                    result.Msg = "此SN: " + strSNList[i] + "和SN：" + strSN + "不属于同一个板，请check";
                                    return Json(result);
                                }
                            }
                            strSql = string.Format(@"select ShipmentNo + '-' + Shipmark from SDS_NONEDI.dbo.Shipment_Package where PalletID = '{0}'", strPalletID);
                            string strShpmrk = clsGlobalVar.GetFieldValue(strSql);
                            dic.Add("PLTMAK", strShpmrk);
                            string strZPLLabel = NONEDIReturnLableZPL(dic, strSN, "SCAN");
                            if (strZPLLabel != "_OK_")
                            {
                                result.Msg = strZPLLabel;
                            }
                            else
                            {
                                string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_NONEDIShip','" + strShpmrk + "',GETDATE(),'" + strSN + "')";
                                clsGlobalVar.ExecSQL(sqlLog);
                                result.Status = 200;
                                result.Msg = "label信息返回成功";
                                result.Data = dic;
                            }
                            return Json(result);
                        }
                        #endregion
                        #region //4.EDI&NonEDI都没找到出货信息,属于提前称重,则只打印防火标签和OverPack
                        else
                        {
                            if (strSNList[0].ToUpper().StartsWith("S") == true && strSNList[0].Length == 13)
                            {
                                string strFlag = ChkSNandPallet(strSN, strSNList);
                                if (strFlag == "N")
                                {
                                    result.Msg = "此次扫描的SN没有提前绑板或者不属于同一板，请check";
                                    return Json(result);
                                }
                                else if (strFlag == "Error")
                                {
                                    result.Msg = "传入的SN获取不到QMS信息，请check";
                                    return Json(result);
                                }
                                else
                                {
                                    strSql = "select * from QMSdb.dbo.QMS_SNInfo where right(Serial_Number,12) ='" + strSN + "' and Cust_PN_2 like '%-%'";
                                    dt = clsGlobalVar.GetDataTable(strSql);
                                    if (dt.Rows.Count > 0)//Non-EDI系统作业-半机,EDI系统作业-半机 not print FireLabel OverPack
                                    {
                                        string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_NONEDIPREWE','NOT PRINT FL&OP',GETDATE(),'" + strSN + "')";
                                        clsGlobalVar.ExecSQL(sqlLog);
                                        dic.Add("PLTMAK", "OK");
                                        dic.Add("PPqty", "2");
                                        dic.Add("PPTYPE", "21");
                                        EDIShpmrk EDI = new EDIShpmrk("N");
                                        NonEDIShpmrk NonEDI = new NonEDIShpmrk("N");
                                        GS1 GS1 = new GS1("N");
                                        GS1MIX GS1MIX = new GS1MIX("N");
                                        PalletID PalletID = new PalletID("N");
                                        FireLabel FL = new FireLabel("N");
                                        OverPack OP = new OverPack("N");
                                        dic.Add("EDISHPMRK", EDI);
                                        dic.Add("NONEDISHPMRK", NonEDI);
                                        dic.Add("GS1", GS1);
                                        dic.Add("GS1MIX", GS1MIX);
                                        dic.Add("PalletID", PalletID);
                                        dic.Add("FireLabel", FL);
                                        dic.Add("OverPack", OP);
                                        result.Status = 200;
                                        result.Msg = "label信息返回成功";
                                        result.Data = dic;
                                        return Json(result);
                                    }
                                    else//不属于半机的则需要打印FireLabel OverPack
                                    {
                                        string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_EDIPREWEIGHT','PRINT FireLabel&OverPack',GETDATE(),'" + strSN + "')";
                                        clsGlobalVar.ExecSQL(sqlLog);
                                        string strPPqty = "2";
                                        string strPPtype = "21";
                                        strSql = "select Model_Name from QMSdb.dbo.QMS_SNInfo where Serial_Number='S" + strSN + "'";
                                        string strPro = clsGlobalVar.GetFieldValue(strSql);

                                        strSql = "select top 1 TYPE from PAL_QSMC_USA.dbo.Plant_Model where model='" + strPro + "' ";
                                        string strMod = clsGlobalVar.GetFieldValue(strSql).ToString().ToUpper();
                                        if (strMod == "PB")
                                        {
                                            strSql = "select top 1 PPqty,PPtype from PAL_QSMC_USA.dbo.[AutoPPSTapeQty] where product = '" + strPro + "' and type='prePPS'";
                                            DataTable dt = clsGlobalVar.GetDataTable(strSql);
                                            if (dt.Rows.Count > 0)
                                            {
                                                strPPqty = dt.Rows[0][0].ToString();
                                                strPPtype = dt.Rows[0][1].ToString();
                                            }
                                        }
                                        dic.Add("PLTMAK", "OK");
                                        dic.Add("PPqty", strPPqty);
                                        dic.Add("PPTYPE", strPPtype);
                                        GS1 GS1 = new GS1("N");
                                        GS1MIX GS1MIX = new GS1MIX("N");
                                        PalletID PalletID = new PalletID("N");
                                        FireLabel FL = new FireLabel("Y");
                                        OverPack OP = new OverPack("Y");
                                        EDIShpmrk EDI = new EDIShpmrk("N");
                                        NonEDIShpmrk NonEDI = new NonEDIShpmrk("N");
                                        dic.Add("EDISHPMRK", EDI);
                                        dic.Add("NONEDISHPMRK", NonEDI);
                                        dic.Add("GS1", GS1);
                                        dic.Add("GS1MIX", GS1MIX);
                                        dic.Add("PalletID", PalletID);
                                        dic.Add("FireLabel", FL);
                                        dic.Add("OverPack", OP);
                                        result.Status = 200;
                                        result.Msg = "label信息返回成功";
                                        result.Data = dic;
                                        return Json(result);
                                    }
                                }
                            }
                            else//tea SN not print FireLabel OverPack
                            {
                                string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_TEAPREWEIGHT','NOT PRINT FireLabel&OverPack',GETDATE(),'" + strSN + "')";
                                clsGlobalVar.ExecSQL(sqlLog);

                                GS1 GS1 = new GS1("N");
                                GS1MIX GS1MIX = new GS1MIX("N");
                                string strDate = String.Format("{0:yyyy-MM-dd HH:mm}", DateTime.Now);
                                PalletID palletID = new PalletID();

                                string strCustomer = "PAL";
                                string strType = "S";
                                int index = 5;
                                if (strSN.Trim().Length == 10 && strSN.ToUpper().StartsWith("3S") == true)
                                {
                                    strType = "B";
                                    index = 2;
                                }
                                if ((strSN.Trim().Length == 22 && strSN.ToUpper().StartsWith("B") == true) || (strSN.Trim().Length == 15 && strSN.ToUpper().StartsWith("19") == true))
                                {
                                    strCustomer = "TEA";
                                    strType = "B";
                                    index = 2;
                                }
                                sqlLog = string.Format(@"exec [SDS_NONEDI].DBO.[usp_GetQMSData_WeighingInAdvance_H2AT] '{0}', '{1}','{2}'", strCustomer, strSN, strType);
                                DataTable dt = clsGlobalVar.GetDataTable(sqlLog);

                                if (dt.Rows.Count != 0 && dt.Rows[0]["Description"].ToString() == "")
                                {
                                    int flag = 0;
                                    foreach (string strsn in strSNList)
                                    {
                                        flag = 0;
                                        foreach (DataRow drb in dt.Rows)
                                        {
                                            if (strsn == drb[index].ToString())
                                            {
                                                flag = 1;
                                                break;
                                            }
                                        }
                                        if (flag == 0)
                                        {
                                            result.Msg = "这些SN不在同一个板，请check";
                                            return Json(result);
                                        }
                                    }
                                    if (strCustomer == "TEA")
                                    {
                                        palletID.labelName = "PalletID.txt";
                                        palletID.print = "Y";
                                        palletID.PALLET_ID = dt.Rows[0]["Pallet_ID"].ToString();
                                        palletID.QPN = dt.Rows[0]["Quanta_PN"].ToString();
                                        palletID.CPN = dt.Rows[0]["Cust_PN"].ToString();
                                        palletID.LOCATION = dt.Rows[0]["Pallet_Type"].ToString();
                                        palletID.QTY = dt.Rows[0]["Pallet_Qty"].ToString();
                                        palletID.DATE = strDate;
                                    }
                                    else
                                    {
                                        palletID.labelName = "PalletID.txt";
                                        palletID.print = "N";
                                    }

                                }
                                else
                                {
                                    if (dt.Rows.Count == 0)
                                    {
                                        result.Msg = "传入的SN获取不到QMS信息，请check";
                                        return Json(result);
                                    }
                                    else
                                    {
                                        result.Msg = "QMS信息有异常:" + dt.Rows[0]["Description"].ToString() + "，请check";
                                        return Json(result);
                                    }
                                }

                                FireLabel FL = new FireLabel("N");
                                OverPack OP = new OverPack("N");
                                EDIShpmrk EDI = new EDIShpmrk("N");
                                NonEDIShpmrk NonEDI = new NonEDIShpmrk("N");
                                dic.Add("PLTMAK", "OK");
                                dic.Add("PPqty", "2");
                                dic.Add("PPTYPE", "21");
                                dic.Add("EDISHPMRK", EDI);
                                dic.Add("NONEDISHPMRK", NonEDI);
                                dic.Add("GS1", GS1);
                                dic.Add("GS1MIX", GS1MIX);
                                dic.Add("PalletID", palletID);
                                dic.Add("FireLabel", FL);
                                dic.Add("OverPack", OP);
                                result.Status = 200;
                                result.Msg = "label信息返回成功";
                                result.Data = dic;
                                return Json(result);
                            }
                        }
                        #endregion
                    }
                }
            }
            catch (Exception e)
            {
                string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_Exception','" + e.Message + e.StackTrace + "',GETDATE(),'')";
                clsGlobalVar.ExecSQL(sqlLog);
                return Json(e.Message);

            }
        }
        
        //枚举类型
        public enum enStatus
        {
            success = 0,
            warning = 1,
            error = 2
        }
        public static void Log(enStatus enStatus, string Text)
        {
            try
            {
                string LogFileUrl = System.AppDomain.CurrentDomain.BaseDirectory + "LogFile";
                if (!Directory.Exists(LogFileUrl))
                    Directory.CreateDirectory(LogFileUrl);
                string LogAddress = System.AppDomain.CurrentDomain.BaseDirectory + "LogFile" + '\\' +
                DateTime.Now.Year + '-' +
                DateTime.Now.Month + '-' +
                DateTime.Now.Day + ".log";
                string dateTime = DateTime.Now.ToString("hh:mm:ss");
                StreamWriter sw = new StreamWriter(LogAddress, true);
                if (enStatus == enStatus.error)
                {
                    sw.WriteLine(dateTime + ":Error:" + Text);
                }
                if (enStatus == enStatus.success)
                {
                    sw.WriteLine(dateTime + ":Success:" + Text);
                }
                if (enStatus == enStatus.warning)
                {
                    sw.WriteLine(dateTime + ":Warning:" + Text);
                }
                sw.WriteLine();
                sw.Close();
            }
            catch { }
        }

        [HttpGet, Route("GenWeightLabel")]
        public IHttpActionResult GenWeightLabel(string strSernum, string strWeight)
        {
            WebResult result = new WebResult();
            try
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                string strsql = "";
                bool blResult = false;

                if (!Regex.IsMatch(strWeight, @"^(?:[1-9][0-9]*\.[0-9]+|0\.(?!0+$)[0-9]+|[1-9]+\d*)$"))
                {
                    result.Msg = "error,weight format is not correct";
                    return Json(result);
                }

                if (string.IsNullOrEmpty(strSernum))
                {
                    result.Msg = "请传入SN";
                    return Json(result);
                }
                else
                {
                    strSernum = strSernum.ToUpper().TrimStart('S');
                    #region check sn 是否是EDI出货的SN
                    strsql = "select t.loadid,t.PALLID,sum(t.S_TOTALWET)+min(t.PLTWET) as EvaluatedWeight,sum(t.MATQTY),t.soldto,CONVERT(VARCHAR(100),GETDATE(),110) AS DATE ,CONVERT(VARCHAR(100),GETDATE(),108) AS TIME,locnum,climod from [PAL_QSMC_USA].dbo.TMPPALSHPMRK(nolock) T inner join [PAL_QSMC_USA].dbo.cast(nolock) c on t.LOADID=c.LOADID inner join [PAL_QSMC_USA].dbo.joit(nolock) j on c.CASENO=j.CASENO and t.pallid=j.REFNM1 where j.SERNUM='" + strSernum.ToUpper().Trim() + "' group by t.loadid,t.PALLID,t.SOLDTO,locnum,climod union select t.loadid,t.PALLID,sum(t.S_TOTALWET)+min(t.PLTWET) as EvaluatedWeight,sum(t.MATQTY),t.soldto,CONVERT(VARCHAR(100),GETDATE(),110) AS DATE ,CONVERT(VARCHAR(100),GETDATE(),108) AS TIME,locnum,climod from  [PAL_QSMC_EMEA].dbo.TMPPALSHPMRK(nolock) T inner join [PAL_QSMC_EMEA].dbo.cast(nolock) c on t.LOADID=c.LOADID inner join [PAL_QSMC_EMEA].dbo.joit(nolock) j on c.CASENO=j.CASENO and t.pallid=j.REFNM1 where j.SERNUM='" + strSernum.ToUpper().Trim() + "' group by t.loadid,t.PALLID,t.SOLDTO,locnum,climod   union select t.loadid,t.PALLID,sum(t.S_TOTALWET)+min(t.PLTWET) as EvaluatedWeight,sum(t.MATQTY),t.soldto,CONVERT(VARCHAR(100),GETDATE(),110) AS DATE ,CONVERT(VARCHAR(100),GETDATE(),108) AS TIME,locnum,climod from [PAL_QSMC_ASIA].dbo.TMPPALSHPMRK(nolock) T inner join [PAL_QSMC_ASIA].dbo.cast(nolock) c on t.LOADID=c.LOADID inner join [PAL_QSMC_ASIA].dbo.joit(nolock) j on c.CASENO=j.CASENO and t.pallid=j.REFNM1 where j.SERNUM='" + strSernum.ToUpper().Trim() + "' group by t.loadid,t.PALLID,t.SOLDTO,locnum,climod union select t.loadid,t.PALLID,sum(t.S_TOTALWET)+min(t.PLTWET) as EvaluatedWeight,sum(t.MATQTY),t.soldto,CONVERT(VARCHAR(100), GETDATE(), 110) AS DATE, CONVERT(VARCHAR(100), GETDATE(), 108) AS TIME, locnum, climod from  [PAL_QSMC_USA].dbo.TMPPALSHPMRK(nolock) T inner join[PAL_QSMC_USA].dbo.BingdingSNandShpmrk(nolock) c on t.LOADID = left(c.SHPMRK, 10) and right(t.PALLID, 2)= right(c.SHPMRK, 2) where c.SN = '" + strSernum.ToUpper().Trim() + "' group by t.loadid,t.PALLID,t.SOLDTO,locnum,climod union select t.loadid,t.PALLID,sum(t.S_TOTALWET)+min(t.PLTWET) as EvaluatedWeight,sum(t.MATQTY),t.soldto,CONVERT(VARCHAR(100), GETDATE(), 110) AS DATE, CONVERT(VARCHAR(100), GETDATE(), 108) AS TIME, locnum, climod from [PAL_QSMC_EMEA].dbo.TMPPALSHPMRK(nolock) T inner join[PAL_QSMC_USA].dbo.BingdingSNandShpmrk(nolock) c on t.LOADID = left(c.SHPMRK, 10) and right(t.PALLID, 2)= right(c.SHPMRK, 2) where c.SN = '" + strSernum.ToUpper().Trim() + "' group by t.loadid,t.PALLID,t.SOLDTO,locnum,climod union select t.loadid,t.PALLID,sum(t.S_TOTALWET)+min(t.PLTWET) as EvaluatedWeight,sum(t.MATQTY),t.soldto,CONVERT(VARCHAR(100), GETDATE(), 110) AS DATE, CONVERT(VARCHAR(100), GETDATE(), 108) AS TIME, locnum, climod from [PAL_QSMC_ASIA].dbo.TMPPALSHPMRK(nolock) T inner join [PAL_QSMC_USA].dbo.BingdingSNandShpmrk(nolock) c on t.LOADID = left(c.SHPMRK, 10) and right(t.PALLID, 2)= right(c.SHPMRK, 2) where c.SN = '" + strSernum.ToUpper().Trim() + "' group by t.loadid,t.PALLID,t.SOLDTO,locnum,climod ";
                    dt = clsGlobalVar.GetDataTable(strsql);
                    #endregion

                    if (dt.Rows.Count > 0)
                    {
                        #region 如果是EDI出货的SN,则要进行EDI出货称重逻辑，传南门地磅，打印称重标签
                        string strLoadid = dt.Rows[0][0].ToString();
                        //string strPid = dt.Rows[0][1].ToString();
                        string strEvaluatedWeight = dt.Rows[0][2].ToString();
                        string strqty = dt.Rows[0][3].ToString();
                        string strsoldtoOld = dt.Rows[0][4].ToString();
                        string strsoldto = getSoldto(strsoldtoOld);
                        string strDATE = dt.Rows[0][5].ToString();
                        string strTIME = dt.Rows[0][6].ToString();
                        string strLoc = dt.Rows[0][7].ToString();
                        string strClimod = dt.Rows[0][8].ToString();
                        string strErrMsg = "";
                        string strPlid = "";
                        strSql = "select '0'+right(SHPMRK,2) from [PAL_QSMC_USA].dbo.BingdingSNandShpmrk where SN = '" + strSernum + "'";
                        dt = clsGlobalVar.GetDataTable(strSql);
                        if (dt.Rows.Count > 0)
                        {
                            strPlid = dt.Rows[0][0].ToString();
                        }
                        else
                        {
                            strsql = "select REFNM1 from " + strsoldto + ".DBO.joit where SERNUM='" + strSernum + "' ";
                            dt = clsGlobalVar.GetDataTable(strsql);
                            if (dt.Rows.Count > 0)
                            {
                                strPlid = dt.Rows[0][0].ToString();
                            }
                            else
                            {
                                result.Msg = "not Find pallid";
                                return Json(result);
                            }
                        }

                        string strShpmrk = strLoadid + strPlid.Substring(1, 2);
                        CheckWeight(strLoadid, strPlid, strsoldto, strWeight, strEvaluatedWeight, strSernum, ref strErrMsg);
                        if (strErrMsg != "")
                        {
                            result.Msg = strErrMsg;
                            return Json(result);
                        }
                        else
                        {
                            EDIWeight EW = new EDIWeight("Y", strWeight, strqty, strLoadid, strPlid, strLoc, strDATE, strTIME);
                            NonEDIWeight NEW = new NonEDIWeight("N");
                            //dic.Add("SN", strSernum);
                            //dic.Add("PLTMAK", strShpmrk);
                            dic.Add("EDIWEIGHT", EW);
                            dic.Add("NONEDIWEIGHT", NEW);
                            string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_EDIShiPWeight','" + strShpmrk + "',GETDATE(),'" + strSernum + "')";
                            clsGlobalVar.ExecSQL(sqlLog);
                            result.Status = 200;
                            result.Msg = "称重label信息返回成功";
                            result.Data = dic;
                            return Json(result);
                        }
                        #endregion
                    }
                    else
                    {
                        strsql = "select * from SDS_NONEDI.dbo.Shipment_Unit where SerialNo = '" + strSernum + "' or QMS_BoxID = '" + strSernum + "'";
                        dt = clsGlobalVar.GetDataTable(strsql);
                        if (dt.Rows.Count > 0)
                        {
                            #region 如果是NONEDI出货的SN,则要进行NONEDI出货称重逻辑，传南门地磅，打印称重标签
                            string strSQL = "";
                            string strCustomerID = "";
                            string strShipmentNo = "";
                            string strShipmark = "";
                            string strPalletID = "";
                            string strTXTPalletID = "";
                            string strSysWet = "";
                            string strDate = String.Format("{0:yyyy-MM-dd HH:mm}", DateTime.Now);
                            DataTable dtLabel = new DataTable();

                            strSQL = string.Format(@"select SU.CustomerID,SU.REFNM2 as ShipmentNo,SP.Shipmark,SP.PalletID from SDS_NONEDI.dbo.Shipment_Unit SU 
                                        inner join SDS_NONEDI.dbo.Shipment_Package SP on SP.ShipmentNo = SU.REFNM2 and SP.PalletID = SU.PalletID 
                                        where SU.SerialNo =  '{0}' or SU.QMS_BoxID = '{0}'", strSernum);
                            DataTable dt = new DataTable();
                            dt = clsGlobalVar.GetDataTable(strSQL);
                            strCustomerID = dt.Rows[0][0].ToString();
                            strShipmentNo = dt.Rows[0][1].ToString();
                            strShipmark = dt.Rows[0][2].ToString();
                            strPalletID = dt.Rows[0][3].ToString();
                            strTXTPalletID = strShipmentNo + "-" + strShipmark;

                            #region 吴跟雷:取消 PAL客户别在自动线称重时Check Barcode
                            //if (CheckBarcodeOK(strCustomerID, strShipmentNo, strShipmark))
                            //{
                            //    result.Msg = "2D Barcode 没有检查完成，请先检查label上的数据";
                            //    return Json(result);
                            //}
                            #endregion
                            strSQL = string.Format(@"select distinct H.CustomerID,H.ShipmentNo,'' as ActualWet,  
                                H.ShipmentNo+'-'+P.Shipmark as Shipmark,H.ShipmentNo+'-'+P.Shipmark as Bar,P.PalletQty,
                                isnull(P.GrossWeight,0) as GrossWeight ,GETDATE() as PrintTime,'' as RSLOC 
                                from SDS_NONEDI.dbo.Shipment_Header H 
                                inner join SDS_NONEDI.dbo.Shipment_Package P on H.CustomerID=P.CustomerID and H.ShipmentNo=P.ShipmentNo  
                                inner join SDS_NONEDI.dbo.Shipment_Unit U on P.CustomerID=U.CustomerID and P.PalletID=U.PalletID  
                                where H.CustomerID='{0}' and   H.ShipmentNo = '{1}' and P.Shipmark ='{2}'", strCustomerID, strShipmentNo, strShipmark);

                            dtLabel = clsGlobalVar.GetDataTable(strSQL);
                            if (dtLabel.Rows.Count == 0)
                            {
                                result.Msg = "No Data";
                                return Json(result);
                            }
                            else
                            {
                                strSysWet = dtLabel.Rows[0]["GrossWeight"].ToString();
                            }

                            string strFlag = clsGlobalVar.GetFieldValue(string.Format(@"SELECT  PalletizationFlag FROM SDS_NONEDI.dbo.Shipment_Package WHERE CustomerID='{0}' AND ShipmentNo = '{1}' and Shipmark ='{2}'", strCustomerID, strShipmentNo, strShipmark));

                            double dbSysWeight = double.Parse(strSysWet);
                            double dbWeight = double.Parse(strWeight);

                            double dbRate = (dbSysWeight - dbWeight) / dbWeight;

                            if ((dbRate <= 0.03 && dbRate >= -0.03) || strFlag == "NSM")
                            {
                                //
                                string strRSLOC = Get_RSLoc(strCustomerID, strTXTPalletID);
                                //
                                strSQL = string.Format(@"insert into SDS_NONEDI.dbo.Packing_Weight 
                                                    select CustomerID,ShipmentNo,Shipmark,GrossWeight,'{0}','0' as status,'' as ErroMessage,'H2ATPPS' as userid,
                                                    CONVERT(varchar(20),getdate(),120),'','','','' from SDS_NONEDI.dbo.Shipment_Package P 
                                                    where P.PalletID ='{1}' ", strWeight, strPalletID);
                                clsGlobalVar.ExecSQL(strSQL);

                                strSQL = string.Format(@"update SDS_NONEDI.dbo.Shipment_Package set ActualWeight='{0}' where CustomerID='{1}' and  PalletID='{2}'", strWeight, strCustomerID, strPalletID);
                                clsGlobalVar.ExecSQL(strSQL);

                                strSQL = string.Format(@"EXEC SDS_NONEDI.dbo.usp_Insert_WMS '{0}','{1}','H2ATPPS'", strCustomerID, strTXTPalletID);
                                clsGlobalVar.ExecSQL(strSQL);

                                string strpalletID = clsGlobalVar.GetFieldValue("select PalletSeqnum from SDS_NONEDI.dbo.Shipment_Package where ShipmentNo+'-'+Shipmark ='" + strTXTPalletID + "'");

                                strSQL = "select * from [WMS_WEIGHT].DBO.WeightInfo where LOADID ='" + strShipmentNo + "' and PALLETID ='" + strpalletID + "'";
                                DataTable dtem = clsGlobalVar.GetDataTable(strSQL);

                                if (dtem.Rows.Count > 0)
                                {
                                    strSQL = "delete from [WMS_WEIGHT].DBO.WeightInfo where LOADID ='" + strShipmentNo + "' and PALLETID ='" + strpalletID + "'";
                                    clsGlobalVar.ExecSQL(strSQL);
                                }
                                strSQL = "INSERT INTO [WMS_WEIGHT].DBO.WeightInfo (LOADID,PALLETID,InDate,InTime,MAC,WeightTime) " +
                                           "  VALUES('" + strShipmentNo + "','" + strpalletID + "',CONVERT(varchar(8),getdate(),112), " +
                                           "  REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),'" + MAC + "',getdate() ) ";
                                clsGlobalVar.ExecSQL(strSQL);

                                EDIWeight EW2 = new EDIWeight();
                                NonEDIWeight NEW2 = new NonEDIWeight();
                                if (strCustomerID == "PAL")
                                {
                                    NEW2.print = "N";
                                    EW2.labelName = "EDI_Weight.txt";
                                    EW2.print = "Y";
                                    EW2.GW = strWeight;
                                    EW2.LOADID = strShipmentNo;
                                    EW2.PALLID = strTXTPalletID;
                                    EW2.QTY = dtLabel.Rows[0]["PalletQty"].ToString();
                                    EW2.TIME = dtLabel.Rows[0]["PrintTime"].ToString();
                                    EW2.LOCATION = strRSLOC;
                                }
                                else
                                {
                                    NEW2.labelName = "NONEDIWeight.txt";
                                    EW2.print = "N";
                                    NEW2.print = "Y";
                                    NEW2.ACTUALWET = strWeight;
                                    NEW2.SHIPMARK = strTXTPalletID;
                                    NEW2.PRINTTIME = dtLabel.Rows[0]["PrintTime"].ToString();
                                    NEW2.RSLOC = strRSLOC;
                                }
                                dic.Add("SN", strSernum);
                                dic.Add("PLTMAK", strTXTPalletID);
                                dic.Add("EDIWEIGHT", EW2);
                                dic.Add("NONEDIWEIGHT", NEW2);
                                string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_EDIShiPWeight','" + strTXTPalletID + "',GETDATE(),'" + strSernum + "')";
                                clsGlobalVar.ExecSQL(sqlLog);
                                result.Status = 200;
                                result.Msg = "称重label信息返回成功";
                                result.Data = dic;
                                return Json(result);
                            }
                            else
                            {
                                result.Msg = "ERROR! Weight Exceed 3%";
                                return Json(result);
                            }
                            #endregion
                        }
                        else
                        {
                            //如果不是出货的sn则需要判断是NONEDI的提前称重还是EDI的提前称重

                            if (strSernum.Length == 12)
                            {
                                #region//PAL PRE WEIGHT SN

                                #region check sn 是否入储，如果未入储，则进行入储
                                strsql = "select * from [PAL_QSMC_USA].dbo.Location_Pallet_Information where SerailNo='" + strSernum.ToUpper().Trim() + "'";
                                dt = clsGlobalVar.GetDataTable(strsql);
                                //未入储，则进行入储
                                if (dt.Rows.Count <= 0)
                                {
                                    strsql = "select Pallet_ID from qmsdb.dbo.QMS_SNInfo where Serial_Number='S" + strSernum.ToUpper().Trim() + "' ";
                                    //strsql = "EXEC [172.26.40.15].[PAL_COMPSN_P80].[dbo].[SP_WH_Allot] 'S" + strSernum.ToUpper().Trim() + "','M'";
                                    dt = clsGlobalVar.GetDataTable(strsql);
                                    if (dt.Rows.Count > 0)
                                    {
                                        string strQMSPalletID = dt.Rows[0][0].ToString();
                                        //如果产线该板未入储，则进行入储
                                        strsql = "exec usp_Location_QMS_Get_LocationNo '" + strQMSPalletID + "',''";
                                        clsGlobalVar.ExecSQL(strsql);
                                    }
                                    else
                                    {
                                        result.Msg = "该SN:"+ strSernum.ToUpper().Trim() + "没传送到QMS_SNInfo，请联系QMS";
                                        return Json(result);
                                    }
                                }
                                #endregion

                                string strPalletid = "";
                                strsql = "select pallid,plant from PAL_QSMC_USA.PAL.pllb where SERNUM ='S" + strSernum.ToUpper().Trim() + "'";
                                dt = clsGlobalVar.GetDataTable(strsql);
                                if (dt.Rows.Count > 0)
                                {
                                    strPalletid = dt.Rows[0][0].ToString();
                                    blResult = SaveEdiData_NoLoadid(strPalletid, strSernum, strWeight.Trim());
                                }
                                else
                                {
                                    strsql = "EXEC [172.26.40.6].PAL_COMPSN_P80.dbo.[usp_SDS_Reprint_2D] 'S" + strSernum.ToUpper().Trim() + "'";
                                    dt = clsGlobalVar.GetDataTable(strsql);
                                    if (dt.Rows.Count > 0)
                                    {
                                        strPalletid = dt.Rows[0][0].ToString();
                                        blResult = SaveNonEdiData_NoSi(strPalletid, strSernum, strWeight.Trim());
                                    }
                                    else
                                    {
                                        blResult = false;
                                    }
                                }

                                if (blResult)
                                {
                                    strsql = " SELECT pallid,CLIMAT,PALLETQTY, Location, ACTWT ,CONVERT(VARCHAR(100),GETDATE(),110) AS DATE ,CONVERT(VARCHAR(100),GETDATE(),108) AS TIME FROM [PAL_QSMC_USA].[DBO].WGHT_NOSI WHERE PALLID='" + strPalletid + "' ";
                                    dt = clsGlobalVar.GetDataTable(strsql);

                                    EDIWeight EW = new EDIWeight("Y", dt.Rows[0][4].ToString(), dt.Rows[0][2].ToString(), dt.Rows[0][1].ToString(), dt.Rows[0][0].ToString(), dt.Rows[0][3].ToString(), dt.Rows[0][5].ToString(), dt.Rows[0][6].ToString());
                                    NonEDIWeight NEW = new NonEDIWeight("N");
                                    //dic.Add("SN", strSernum);
                                    //dic.Add("PLTMAK", dt.Rows[0][0].ToString());
                                    dic.Add("EDIWEIGHT", EW);
                                    dic.Add("NONEDIWEIGHT", NEW);
                                    string sqlLog = "INSERT  [PAL_QSMC_USA].DBO.LOGG  VALUES ('SDS','H2AutoPPS_EDIPerWeight','" + strPalletid + "',GETDATE(),'" + strSernum + "')";
                                    clsGlobalVar.ExecSQL(sqlLog);
                                    result.Status = 200;
                                    result.Msg = "称重label信息返回成功";
                                    result.Data = dic;
                                    return Json(result);
                                }
                                else
                                {
                                    result.Msg = "error,请先检查下此板有没有入储！入储后仍有错误，请联系MIS!";
                                    return Json(result);
                                }
                                #endregion
                            }
                            else
                            {
                                #region //TEA PRE WEIGHT SN
                                string strCustomer = "PAL";
                                string strType = "S";
                                if (strSernum.Trim().Length == 10 && strSernum.ToUpper().StartsWith("3S") == true)
                                {
                                    strType = "B";
                                }
                                if ((strSernum.Trim().Length == 22 && strSernum.ToUpper().StartsWith("B") == true) || (strSernum.Trim().Length == 15 && strSernum.ToUpper().StartsWith("19") == true))
                                {
                                    strCustomer = "TEA";
                                    strType = "B";
                                }
                                string strRSLOC = "";
                                //获取QMS信息
                                strsql = string.Format(@"exec [SDS_NONEDI].DBO.[usp_GetQMSData_WeighingInAdvance_H2AT] '{0}', '{1}','{2}'", strCustomer, strSernum, strType);
                                DataTable dt = clsGlobalVar.GetDataTable(strsql);
                                //删除之前的提前称重记录
                                strsql = string.Format(@"select PalletID from [SDS_NONEDI].[DBO].[PalletInfor] where SerialNo = '{0}' or Box_ID = '{0}'", strSernum);
                                string strPalletID = clsGlobalVar.GetFieldValue(strsql).ToString();
                                if (strPalletID != "")
                                {
                                    strsql = string.Format(@"delete from [SDS_NONEDI].[DBO].[PalletInfor] where PalletID = '{0}'", strPalletID);
                                }
                                clsGlobalVar.ExecSQL(strsql);
                                //插入新的称重记录
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {

                                    strsql = string.Format(@"insert into [SDS_NONEDI].[DBO].[PalletInfor](CutomerID,PalletID,SerialNo,QCI_SN,Box_ID,Cust_PN,Quanta_PN,SSCC,Model_Name,Region,QTY,
                                              CurrentUser,REFNM2,Upddate)  values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}')",
                                                   strCustomer, dt.Rows[i]["Pallet_ID"].ToString(), dt.Rows[i]["Serial_Number"].ToString(), dt.Rows[i]["QCI_SN"].ToString(),
                                                   dt.Rows[i]["Box_ID"].ToString(), dt.Rows[i]["Cust_PN"].ToString(), dt.Rows[i]["Quanta_PN"].ToString(), dt.Rows[i]["SSCC"].ToString(),
                                                   dt.Rows[i]["Model_Name"].ToString(), dt.Rows[i]["Region"].ToString(), dt.Rows[i]["QTY"].ToString(), "H2ATPerWeight", dt.Rows[i]["Batch"].ToString(), String.Format("{0:yyyy-MM-dd HH:mm}", DateTime.Now));
                                    clsGlobalVar.ExecSQL(strsql);
                                }
                                strsql = string.Format(@"select PalletID from [SDS_NONEDI].[DBO].[PalletInfor] where Box_ID = '{0}'", strSernum);
                                strPalletID = clsGlobalVar.GetFieldValue(strsql).ToString();

                                strsql = string.Format(@"update [SDS_NONEDI].[DBO].[PalletInfor] set AcWeight='{0}' where PalletID='{1}'", strWeight.Trim(), strPalletID);
                                clsGlobalVar.ExecSQL(strsql);

                                strsql = string.Format(@"select * from [SDS_NONEDI].[DBO].[PalletInfor] where PalletID = '{0}'", strPalletID);
                                DataTable dtLabel = clsGlobalVar.GetDataTable(strsql);

                                if (strCustomer == "TEA")
                                {
                                    string Str_sql = string.Format("exec [SDS_NONEDI].[DBO].[usp_Location_datatransmission_QMS] '{0}','{1}','{2}'", strCustomer, strPalletID, "F");
                                    dt = clsGlobalVar.GetDataTable(Str_sql);
                                    strRSLOC = dt.Rows[0]["LocationPalletNo"].ToString();
                                }

                                if (strCustomer == "PAL")
                                {
                                    #region PAL入储，暂时不需要启用，因为在上自动线之前就已经入储了
                                    //if (!Process(strSernum, "", strPalletID))
                                    //{
                                    //    result.Msg = "入储失败,请联系MIS~";
                                    //    return Json(result);
                                    //}
                                    #endregion
                                    string Str_sql = string.Format("select top 1 location from[PAL_QSMC_USA].dbo.Location_Pallet_Information where box_id = '{0}' or SerailNo='{0}'", strSernum);
                                    strRSLOC = clsGlobalVar.GetFieldValue(Str_sql);

                                }
                                string strSQL = "select * from [WMS_WEIGHT].DBO.WeightInfo where LOADID ='' and PALLETID ='" + strPalletID + "'";
                                DataTable dtem = clsGlobalVar.GetDataTable(strSQL);

                                if (dtem.Rows.Count > 0)
                                {
                                    strSQL = "delete from [WMS_WEIGHT].DBO.WeightInfo where LOADID ='' and PALLETID ='" + strPalletID + "'";
                                    clsGlobalVar.ExecSQL(strSQL);
                                }
                                strSQL = "INSERT INTO [WMS_WEIGHT].DBO.WeightInfo (LOADID,PALLETID,InDate,InTime,MAC,WeightTime) " +
                                           "  VALUES('','" + strPalletID + "',CONVERT(varchar(8),getdate(),112), " +
                                           "  REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),'" + MAC + "',getdate() ) ";
                                clsGlobalVar.ExecSQL(strSQL);

                                EDIWeight EW2 = new EDIWeight();
                                NonEDIWeight NEW2 = new NonEDIWeight();
                                if (strCustomer == "TEA")
                                {
                                    NEW2.labelName = "NONEDIWeight.txt";
                                    EW2.print = "N";
                                    NEW2.print = "Y";
                                    NEW2.ACTUALWET = strWeight;
                                    NEW2.SHIPMARK = "4A" + strPalletID;
                                    NEW2.PRINTTIME = dtLabel.Rows[0]["Upddate"].ToString();
                                    NEW2.RSLOC = strRSLOC;
                                }
                                else
                                {
                                    NEW2.print = "N";
                                    EW2.labelName = "EDI_Weight.txt";
                                    EW2.print = "Y";
                                    EW2.GW = strWeight;
                                    EW2.PALLID = strPalletID;
                                    EW2.QTY = dtLabel.Rows.Count.ToString();
                                    EW2.TIME = dtLabel.Rows[0]["Upddate"].ToString();
                                    EW2.LOCATION = strRSLOC;
                                }
                                dic.Add("SN", strSernum);
                                dic.Add("PLTMAK", strPalletID);
                                dic.Add("EDIWEIGHT", EW2);
                                dic.Add("NONEDIWEIGHT", NEW2);
                                result.Status = 200;
                                result.Msg = "称重label信息返回成功";
                                result.Data = dic;
                                #endregion
                            }
                        }
                    }
                }
                return Json(result);
            }
            catch (Exception e)
            {
                string sqlLog = "INSERT [PAL_QSMC_USA].DBO.LOGG VALUES ('SDS','H2AutoPPS_Exception','" + e.Message + e.StackTrace.ToString()
 + "',GETDATE(),'')";
                clsGlobalVar.ExecSQL(sqlLog);
                return Json(e.Message);
            }
        }

        #region 以下4个方法为称重后PAL入储，暂时不需要启用，因为在上自动线之前就已经入储了

        public bool Process(string strSN, string strPlant, string strPalletID)
        {
            string strRegion = "";
            string strModel = "";
            string strClimat = "";


            int Qty = 0;
            bool bolResult = false;
            DataTable dtQMS = new DataTable();

            string strSQL = "Exec [172.26.40.6].[PAL_COMPSN_P80].[dbo].[SP_WH_Allot]  '" + strSN + "','N' ";
            dtQMS = clsGlobalVar.GetDataTable(strSQL);
            strModel = dtQMS.Rows[0]["Model_Name"].ToString();
            strRegion = dtQMS.Rows[0]["Region"].ToString();
            strClimat = dtQMS.Rows[0]["Cust_PN"].ToString();
            Qty = dtQMS.Rows.Count;
            if (!ChecktheCheckinCondition(strModel, Qty))
            {
                //MessageBox.Show("请联系Duty维护Model Base!");
                return bolResult;
            }
            if (GetLocationandcheckin(strPlant, strRegion, strModel, strClimat, Qty, "F", "H2ATPreWeight", dtQMS, strPalletID))
            {
                bolResult = true;
            }
            return bolResult;
        }
        public bool ChecktheCheckinCondition(string strModel, int iQty)
        {
            bool bFullPallet = true;
            string strSQL = "DECLARE @RESULT AS CHAR(1) ";
            strSQL += "EXEC [PAL_QSMC_USA].[DBO].[usp_Location_Check_FullPallet]  '" + strModel + "'," + iQty + ",@RESULT OUTPUT ";
            strSQL += "SELECT @RESULT ";
            string strResult = clsGlobalVar.GetFieldValue(strSQL);
            if (strResult == "1" || strResult == "2")   //not maintain Location_CTRL(Full Pallet base)
            {
                bFullPallet = false;
            }
            return bFullPallet;
        }
        public bool GetLocationandcheckin(string strPlant, string strRegion, string strModel, string Climat, int Qty, string strPLTTYP, string strUserID, DataTable dtQMS, string strQMSPalletID)
        {
            string strCLIMAT = "";
            string strQCIMAT = "";
            string strSerailNo = "";
            string RAMNO = "";
            #region get Location Auto 系统自动分储位

            string strSQL = "EXEC [PAL_QSMC_USA].[DBO].usp_Get_LocationPallet '','" + strPlant + "','" + strRegion + "','" + strModel + "','" + Climat + "','" + Qty + "','" + strPLTTYP + "'";

            DataTable dtLocation = clsGlobalVar.GetDataTable(strSQL);
            if (dtLocation.Rows.Count == 0 || dtLocation.Rows.Count < Int16.Parse(dtLocation.Rows[0]["PLTQTY"].ToString()))
            {
                return false;
            }
            else
            {
                // add jaye 20101111  检查所分储位是否被Lock
                if (clsGlobalVar.GetFieldValue("SELECT LocationNo FROM Location WHERE Status IN ('2','3') AND LocationNo='" + dtLocation.Rows[0]["LOCATIONNO"].ToString() + "'") != "")
                {
                    //Update_Location_Status();
                    //MessageBox.Show("储位：" + dtLocation.Rows[0]["LOCATIONNO"].ToString() + "被锁，LOCK IN !");
                    return false;
                }

                #endregion
                #region one pallet
                if (dtLocation.Rows.Count == 1)
                {
                    for (int i = 0; i < dtQMS.Rows.Count; i++)
                    {
                        strCLIMAT = dtQMS.Rows[i]["Cust_PN"].ToString();

                        strQCIMAT = dtQMS.Rows[i]["Quanta_PN"].ToString();
                        strSerailNo = dtQMS.Rows[i]["Serial_Number"].ToString();
                        RAMNO = dtQMS.Rows[i]["PO"].ToString();
                        //入储，让SN信息塞到储位系统table
                        CheckInProcess(strPlant, strModel, dtLocation.Rows[0]["LOCATIONNO"].ToString(), dtLocation.Rows[0]["LOCATIONPALLETNO"].ToString(), strQMSPalletID, strCLIMAT, strQCIMAT, strSerailNo, strUserID, strPLTTYP, RAMNO);
                    }
                }
                #endregion
                return true;
            }
        }
        public bool CheckInProcess(string strPlant, string strModel, string strLocation, string strLocationPalletID, string strQMSPAlletID, string strCLIMAT, string strQCIMAT, string strSerialNo, string strUserID, string strPLTTYP, string RAMNO)
        {
            string strSQL = "INSERT INTO [PAL_QSMC_USA].[dbo].[Location_Pallet_Information] " +
                            "([MODEL],[Location] " +
                            " ,[Location_PalletID] " +
                            " ,[QMS_PalletID] " +
                            " ,[SerailNo]" +
                            ",[CLIMAT]" +
                            ",[QCIMAT]" +
                            ",[StockInDate]" +
                            ",[StockInTime]" +
                            ",[Status]" +
                            ",[UserID],[REFNM3],[REFNM4],[REFNM5])" +
                            " VALUES" +
                            "('" + strModel + "','" + strLocation + "','" + strLocationPalletID + "','" + strQMSPAlletID + "','" + strSerialNo + "','" + strCLIMAT +
                            "','" + strQCIMAT + "',convert(char(8),getdate(),112),REPLACE(convert(char(8),getdate(),108),':',''),'0','" + strUserID + "','" + strPLTTYP + "','" + strPlant + "','" + RAMNO + "')";

            clsGlobalVar.ExecSQL(strSQL);
            return true;
        }
        #endregion

        //check传送过来的SN是不是属于同一个整板
        //1.没有进行PDA绑板的板号直接提示
        //2.都绑板了，分别扫描的SN是不是同一板上面的SN

        private string ChkSNandPallet(string strSN, string[] strSNList)
        {
            string strFlag = "Y";
            strSql = "select distinct pallid from PAL_QSMC_USA.PAL.pllb where SERNUM ='S" + strSN + "'";
            string strPid = clsGlobalVar.GetFieldValue(strSql);
            if (strPid.Length > 0)
            {
                for (int i = 0; i < strSNList.Length; i++)
                {
                    strSql = "select distinct PalletID from [PAL_QSMC_USA].DBO.PalletInfor where  PalletID='" + strPid + "' and 'S'+SerialNo = '" + strSNList[i].ToString() + "' and refnm1 = '1'  ";
                    dt = clsGlobalVar.GetDataTable(strSql);
                    if (dt.Rows.Count <= 0)
                    {
                        strFlag = "N";
                    }
                }
            }
            else
            {
                strSql = "select pallet_ID from QMSdb.dbo.QMS_SNInfo where right(Serial_Number,12) ='" + strSN + "' ";
                string strQMSPid = clsGlobalVar.GetFieldValue(strSql);
                if (strQMSPid.Length > 0)
                {
                    for (int i = 0; i < strSNList.Length; i++)
                    {
                        strSql = "select distinct PalletID from [PAL_QSMC_USA].DBO.PalletInfor where  PalletID='" + strQMSPid + "' and 'S'+SerialNo = '" + strSNList[i].ToString() + "' and refnm1 = '1'  ";
                        dt = clsGlobalVar.GetDataTable(strSql);
                        if (dt.Rows.Count <= 0)
                        {
                            strFlag = "N";
                        }
                    }
                }
                else
                {
                    strFlag = "Error";
                }
            }
            return strFlag;
        }
        private string Get_RSLoc(string strCustomerID, string strShipMark)
        {
            string strSQL = "";
            string strReSult = "";
            if (strCustomerID == "PAL")
            {
                string strShipmentNo = strShipMark.Substring(0, 10).ToString();
                string strMark = strShipMark.Substring(11, strShipMark.Length - 11).ToString();
                strSQL = "exec PAL_QSMC_USA.[dbo].[usp_Location_GetLocationForReadyLoadid_NONEDI] '" + strShipmentNo + "','" + strMark + "'";
                strReSult = clsGlobalVar.GetFieldValue(strSQL);
            }
            else
            {
                strSQL = "EXEC [SDS_NONEDI].[dbo].[usp_Location_GetRS_byshipmark] '" + strCustomerID + "','" + strShipMark + "'";

                DataTable dt = clsGlobalVar.GetDataTable(strSQL);
                if (dt.Rows[0][0].ToString() == "Y")
                {
                    strReSult = dt.Rows[0]["Message"].ToString();
                }
            }
            #region 作业区出储
            strSQL = "EXEC [SDS_NONEDI].[dbo].[usp_Location_AutoStockout_Operation] '" + strCustomerID + "','" + strShipMark + "'";
            clsGlobalVar.ExecSQL(strSQL);
            #endregion
            return strReSult;
        }

        #region 吴跟雷:取消 PAL客户别在自动线称重时Check Barcode
        //private bool CheckBarcodeOK(string strCustomerID, string strShipmentno, string strShipmark)
        //{
        //    bool Resault = false;
        //    string strSQL = "";
        //    strSQL = " declare @result as varchar(5)  "; ;
        //    strSQL += " exec SDS_NONEDI.dbo.[usp_Wet_CheckPackageStatus] '" + strCustomerID + "','" + strShipmentno + "','" + strShipmark + "',@result output ";
        //    strSQL += "select @result";
        //    string strResault = clsGlobalVar.GetFieldValue(strSQL);
        //    if (strResault == "Error")
        //    {
        //        Resault = true;
        //    }
        //    return Resault;
        //}
        #endregion
        public static void CheckWeight(string Loadid, string Palletid, string strSoldto, string strActualWeight, string EvaluatedWeight, string Sernum, ref string strErrMsg)
        {
            try
            {
                double dEvPalletWeight;
                double dActWeight = 0;
                double dBlance;
                Palletid = Palletid.Substring(1, Palletid.Length - 1);
                if (strActualWeight != "")
                {
                    dActWeight = double.Parse(strActualWeight);
                }
                else
                {
                    strErrMsg = "Actual Weight is 0!!";
                    return;
                }
                dEvPalletWeight = double.Parse(EvaluatedWeight);
                double dDifferent = Math.Abs(dActWeight - dEvPalletWeight);
                dBlance = double.Parse((dEvPalletWeight * 0.03).ToString());

                if (dDifferent > dBlance)
                {
                    #region 重量超限直接不允许后续操作
                    #region  // Insert Error Data to WMS ErrPalletinfo table
                    //strErrMsg = "Actual Weight is different from EvaluateWeight!!";
                    //C1/C2/H1C对应的厂区都是H131
                    /*string PLANT = "H241";

                    string strSQLinsert = "INSERT INTO [WMS_WEIGHT].DBO.ERRPALLETINFO(LOADID,PALLID,SHIPMARK,A_WEIGHT,AVE_WEIGHT,INDATE,INTIME)VALUES "
                      + " ('" + Loadid + "','" + Palletid + "','" + Sernum + "'," + dActWeight + "," + dEvPalletWeight + ",CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':','')) ";
                    clsGlobalVar.ExecSQL(strSQLinsert);
                    strSQLinsert = " INSERT INTO WMS_WEIGHT.[dbo].[errWeightReport] (SHIPID, PALLID, PLANT,BU, E_WEIGHT, A_WEIGHT) VALUES('" + Loadid + "','" + Palletid + "','" + PLANT + "','PAL'," + dEvPalletWeight + "," + dActWeight + ")";

                    clsGlobalVar.ExecSQL(strSQLinsert);
                    string strsql = " SELECT * FROM [WMS_WEIGHT].DBO.WeightInfo WHERE LOADID = '" + Loadid + "'AND PALLETID = '" + Palletid + "'";
                    DataTable dtFalg = clsGlobalVar.GetDataTable(strsql);
                    if (dtFalg.Rows.Count > 0)
                    {
                        strSQLinsert = " UPDATE [WMS_WEIGHT].DBO.WeightInfo SET InDate=CONVERT(CHAR(8),GETDATE(),112),InTime=REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),MAC = '" + MAC + "' WHERE LOADID = '" + Loadid + "'AND PALLETID = '" + Palletid + "'";
                    }
                    else
                    {
                        strSQLinsert = " INSERT INTO [WMS_WEIGHT].DBO.WeightInfo (LOADID,PALLETID,InDate,InTime,MAC)VALUES('" + Loadid + "','" + Palletid + "',CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),'" + MAC + "')";
                    }
                    clsGlobalVar.ExecSQL(strSQLinsert);

                    strSQLinsert = "INSERT PAL_QSMC_ASIA.dbo.WEIGHT_LOG ([TABLNM],[LOADID],[PALLID],[SERNUM],[A_WEIGHT],[AVE_WEIGHT],[InDate],[InTime]) VALUES('ERRPALLETINFO','" + Loadid + "','" + Palletid + "','" + Sernum + "','" + strActualWeight + "','" + EvaluatedWeight + "',CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''))";
                    clsGlobalVar.ExecSQL(strSQLinsert);
                    #endregion
                    #region add by cherry for get location from print shipmark

                    string strLocation = " select PLANT,Location_PalletID FROM PAL_QSMC_USA.dbo.Location_shpmrk WHERE LOADID='" + Loadid + "' AND PALLETID='" + Palletid.PadLeft(3, '0') + "'";
                    DataTable dt = clsGlobalVar.GetDataTable(strLocation);
                    if (dt.Rows.Count > 0)
                    {
                        strLocation = "UPDATE [PAL_QSMC_USA].[DBO].Location_PalletStatus SET REFNM1='" + Loadid.ToUpper() + "',REFNM2='" + Palletid.PadLeft(3, '0') + "' WHERE LocationPalletNo='" + dt.Rows[0]["Location_PalletID"].ToString() + "' AND PLANT='" + dt.Rows[0]["PLANT"].ToString() + "'";
                        strLocation += "UPDATE [PAL_QSMC_USA].[DBO].Location_shpmrk SET   STATUS='0',LOADID='',PALLETID='' WHERE LOADID='" + Loadid + "' AND PALLETID='" + Palletid.PadLeft(3, '0') + "'";
                        //add by heaven 20181120 for H1C pallet QTY
                        clsGlobalVar.ExecSQL(strLocation);
                        string strLocationId = dt.Rows[0]["Location_PalletID"].ToString();
                        //EDIPalletPN(Loadid, Palletid, strLocationId, strSoldto);
                    }
                    else
                    {
                        strLocation = "EXEC " + strSoldto + ".DBO.[usp_Location_GetLocationForReadyLoadid_New] 'C1','" + Loadid + "','" + Palletid + "',''";
                        clsGlobalVar.ExecSQL(strLocation);
                        //add by heaven 20181120 for H1C pallet QTY
                        string strLocationId = DateTime.Now.ToString("yyyymmddhhmmss");
                        //EDIPalletPN(Loadid, Palletid, strLocationId, strSoldto);
                    }

                    #endregion

                    strErrMsg = InsertAuditWeight(Loadid, Palletid, strActualWeight, strSoldto);
                    */
                    #endregion
                    #endregion
                    #region  // Insert Error Data to WMS ErrPalletinfo table
                    string PLANT = "H241";

                    string strSQLinsert = "INSERT INTO [WMS_WEIGHT].DBO.ERRPALLETINFO(LOADID,PALLID,SHIPMARK,A_WEIGHT,AVE_WEIGHT,INDATE,INTIME)VALUES "
                      + " ('" + Loadid + "','" + Palletid + "','" + Sernum + "'," + dActWeight + "," + dEvPalletWeight + ",CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':','')) ";
                    clsGlobalVar.ExecSQL(strSQLinsert);
                    strSQLinsert = " INSERT INTO WMS_WEIGHT.[dbo].[errWeightReport] (SHIPID, PALLID, PLANT,BU, E_WEIGHT, A_WEIGHT) VALUES('" + Loadid + "','" + Palletid + "','" + PLANT + "','PAL'," + dEvPalletWeight + "," + dActWeight + ")";

                    clsGlobalVar.ExecSQL(strSQLinsert);
                    #endregion
                    strErrMsg = "ERROR! 重量超过3%，请check!";
                    return;
                }
                else
                {
                    #region  // Save Data into WMS Palletinfo table
                    string strSQLselect = "SELECT * FROM [WMS_WEIGHT].DBO.PALLETINFO WHERE LOADID ='" + Loadid + "' AND PALLID ='" + Palletid + "'";
                    DataTable dtWMSweiht = clsGlobalVar.GetDataTable(strSQLselect);
                    if (dtWMSweiht.Rows.Count != 0)
                    {
                        string strSQLupdateWMS = " UPDATE [WMS_WEIGHT].DBO.PALLETINFO SET AVE_WEIGHT =" + dEvPalletWeight + ",A_WEIGHT = " + dActWeight + ",SHIPMARK='" + Sernum + "',InDate=CONVERT(CHAR(8),GETDATE(),112) ,InTime=REPLACE(CONVERT(CHAR(8),GETDATE(),108),':','') WHERE LOADID = '" + Loadid + "'AND PALLID = '" + Palletid + "' ";
                        strSQLupdateWMS += " UPDATE [WMS_WEIGHT].DBO.WeightInfo SET InDate=CONVERT(CHAR(8),GETDATE(),112),InTime=REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),MAC = '" + MAC + "' WHERE LOADID = '" + Loadid + "'AND PALLETID = '" + Palletid + "'";
                        clsGlobalVar.ExecSQL(strSQLupdateWMS);
                    }
                    else
                    {
                        string strSQLinsertWMS = "INSERT INTO [WMS_WEIGHT].DBO.PALLETINFO (LOADID,PALLID,SHIPMARK,A_WEIGHT,AVE_WEIGHT,InDate,InTime)VALUES('" + Loadid + "','" + Palletid + "','" + Sernum + "'," + dActWeight + "," + dEvPalletWeight + ",CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':','')) ";
                        strSQLinsertWMS += "INSERT INTO [WMS_WEIGHT].DBO.WeightInfo (LOADID,PALLETID,InDate,InTime,MAC)VALUES('" + Loadid + "','" + Palletid + "',CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),'" + MAC + "')";
                        clsGlobalVar.ExecSQL(strSQLinsertWMS);
                    }

                    strSQLselect = "INSERT PAL_QSMC_ASIA.dbo.WEIGHT_LOG ([TABLNM],[LOADID],[PALLID],[SERNUM],[A_WEIGHT],[AVE_WEIGHT],[InDate],[InTime]) VALUES('PALLETINFO','" + Loadid + "','" + Palletid + "','" + Sernum + "','" + strActualWeight + "','" + EvaluatedWeight + "',CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''))";
                    clsGlobalVar.ExecSQL(strSQLselect);

                    #endregion
                    strErrMsg = "";
                    #region add by cherry for get location from print shipmark
                    string strLocation = " select PLANT,Location_PalletID FROM PAL_QSMC_USA.dbo.Location_shpmrk WHERE LOADID='" + Loadid + "' AND PALLETID='" + Palletid.PadLeft(3, '0') + "'";
                    DataTable dt = clsGlobalVar.GetDataTable(strLocation);
                    if (dt.Rows.Count > 0)
                    {
                        strLocation = "UPDATE [PAL_QSMC_USA].[DBO].Location_PalletStatus SET REFNM1='" + Loadid + "',REFNM2='" + Palletid.PadLeft(3, '0') + "' WHERE LocationPalletNo='" + dt.Rows[0]["Location_PalletID"].ToString() + "' AND PLANT='" + dt.Rows[0]["PLANT"].ToString() + "'";
                        strLocation += "UPDATE [PAL_QSMC_USA].[DBO].Location_shpmrk SET   STATUS='0',LOADID='',PALLETID='' WHERE LOADID='" + Loadid + "' AND PALLETID='" + Palletid.PadLeft(3, '0') + "'";
                        clsGlobalVar.ExecSQL(strLocation);
                        //add by heaven 20181120 for H1C pallet QTY
                        string strLocationId = dt.Rows[0]["Location_PalletID"].ToString();
                        //EDIPalletPN(Loadid, Palletid, strLocationId, strSoldto);

                    }
                    else
                    {
                        strLocation = "EXEC " + strSoldto + ".DBO.[usp_Location_GetLocationForReadyLoadid_New] 'H2B','" + Loadid + "','" + Palletid + "',''";
                        clsGlobalVar.ExecSQL(strLocation);
                        //add by heaven 20181120 for H1C pallet QTY
                        string strLocationId = DateTime.Now.ToString("yyyymmddhhmmss");
                        //EDIPalletPN(Loadid, Palletid, strLocationId, strSoldto);
                    }
                    #endregion


                    if (2 < dActWeight && dActWeight < 1000)
                    {
                        strErrMsg = InsertAuditWeight(Loadid, Palletid, strActualWeight, strSoldto);
                    }

                }
                if (2 < dActWeight && dActWeight < 1000)
                {
                    float TOTALWET = float.Parse(strActualWeight);
                    string strSQL = "UPDATE " + strSoldto + ".DBO.TMPPALSHPMRK SET A_TOTALWET='" + TOTALWET + "' WHERE LOADID='" + Loadid + "' AND PALLID= RIGHT('0" + Palletid + "',3)";
                    clsGlobalVar.ExecSQL(strSQL);

                    strSQL = "INSERT PAL_QSMC_ASIA.dbo.WEIGHT_LOG ([TABLNM],[LOADID],[PALLID],[SERNUM],[A_WEIGHT],[AVE_WEIGHT],[InDate],[InTime]) VALUES('TMPPALSHPMRK','" + Loadid + "','" + Palletid + "','" + Sernum + "','" + strActualWeight + "','" + EvaluatedWeight + "',CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''))";
                    clsGlobalVar.ExecSQL(strSQL);
                }

                #region Check data add Jaye

                string strCheck = " SELECT * FROM WMS_WEIGHT.dbo.PAL_WEIGHT where Loadid='" + Loadid + "'AND PalletID='" + Palletid + "' ";
                DataTable dtCheck = clsGlobalVar.GetDataTable(strCheck);
                if (dtCheck.Rows.Count == 0)
                {
                    strErrMsg = InsertAuditWeight(Loadid, Palletid, strActualWeight, strSoldto);
                }
                #endregion

            }
            catch (Exception ex)
            {
                strErrMsg = ex.Message;
            }
        }
        public static string InsertAuditWeight(string Loadid, string Palletid, string strWeight, string strSOLDTO)
        {
            try
            {
                string res = "";
                string strSQL = "";
                if (Loadid.Substring(0, 2) == "20")
                    strSQL = " DECLARE @Result AS CHAR(1)  EXEC [PAL_QSMC_USA].DBO.[usp_WEIGHT_FOR_AUDIT] '','" + Loadid + "','" + Palletid + "','" + strWeight + "',@Result OUTPUT   SELECT @Result ";
                else
                    strSQL = " DECLARE @Result AS CHAR(1)  EXEC [PAL_QSMC_USA].DBO.[usp_WEIGHT_FOR_AUDIT] '" + strSOLDTO + "','" + Loadid + "','" + Palletid + "','" + strWeight + "',@Result OUTPUT   SELECT @Result  ";
                string strResult = clsGlobalVar.GetFieldValue(strSQL);
                if (strResult == "0")
                {
                    return res = "塞南门地磅数据有问题，请重新称重！";
                }
                else
                {
                    strSQL = "INSERT PAL_QSMC_ASIA.dbo.WEIGHT_LOG ([TABLNM],[LOADID],[PALLID],[SERNUM],[A_WEIGHT],[AVE_WEIGHT],[InDate],[InTime]) VALUES('PalletWeight','" + Loadid + "','" + Palletid + "','','" + strWeight + "','',CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''))";
                    clsGlobalVar.ExecSQL(strSQL);
                    return res;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static bool SaveNonEdiData_NoSi(string PalletID, string Sernum, string Weight)
        {
            try
            {
                bool bolReturn = false;
                double ActWeight_Non_NoSi = 0;
                string strLocation = "";
                string strLocation_PalletID = "";
                string strCLIMAT = "";
                string strQTY = "";
                string strQMS_PalletID = "";
                string strSQL = "";
                string strSQLFilter = "";
                string strSQLinsert = "";

                if (Weight != "")
                {
                    ActWeight_Non_NoSi = double.Parse(Weight);
                }


                strSQL = "EXEC [PAL_QSMC_USA].[DBO].[usp_GetTMP_WGHT_NOSI] '" + PalletID + "'";
                DataTable dt_ = clsGlobalVar.GetDataTable(strSQL);
                if (dt_.Rows.Count > 0)
                {
                    strLocation = dt_.Rows[0]["Location"].ToString();
                    strLocation_PalletID = dt_.Rows[0]["Location_PalletID"].ToString();
                    strCLIMAT = dt_.Rows[0]["APPLE_PN"].ToString();
                    strQTY = dt_.Rows[0]["QTY"].ToString();
                    //heaven update
                    strQMS_PalletID = dt_.Rows[0]["Pallet_ID"].ToString();
                }
                else
                {
                    // MessageBox.Show("There is no data!!");
                    return false;

                }


                #region  // Insert Data to WGHT_NOSI table


                strSQL = "SELECT * FROM [PAL_QSMC_USA].[DBO].WGHT_NOSI WHERE PALLID='" + PalletID + "' ";
                DataTable dt = clsGlobalVar.GetDataTable(strSQL + strSQLFilter);

                if (dt.Rows.Count > 0)
                {
                    strSQL = "DELETE FROM [PAL_QSMC_USA].[DBO].WGHT_NOSI WHERE PALLID='" + PalletID + "' ";
                    clsGlobalVar.ExecSQL(strSQL + strSQLFilter);
                }

                strSQL = "INSERT INTO [PAL_QSMC_USA].[DBO].WGHT_NOSI ( SHIPMENT, PALLID,Plant,CLIMAT,PALLETQTY, Location, Location_PalletID, ACTWT, MAC, WEIGHTDATE, WEIGHTTIME) VALUES('','" + PalletID + "','C1','" + strCLIMAT + "','" + strQTY + "','" + strLocation + "','" + strLocation_PalletID + "','" + ActWeight_Non_NoSi + "','" + MAC + "',CONVERT(VARCHAR(100),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''))";

                clsGlobalVar.ExecSQL(strSQL);

                DataTable dtFalg = clsGlobalVar.GetDataTable(" SELECT * FROM [WMS_WEIGHT].DBO.WeightInfo WITH (NOLOCK) WHERE PALLETID = '" + PalletID + "'");
                if (dtFalg.Rows.Count > 0)
                {
                    strSQLinsert += " UPDATE [WMS_WEIGHT].DBO.WeightInfo SET InDate=CONVERT(CHAR(8),GETDATE(),112),InTime=REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),MAC = '" + MAC + "' WHERE  PALLETID = '" + PalletID + "'";
                }
                else
                {
                    strSQLinsert += " INSERT INTO [WMS_WEIGHT].DBO.WeightInfo (LOADID,PALLETID,InDate,InTime,MAC)VALUES('PAL','" + PalletID + "',CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),'" + MAC + "')";
                }

                clsGlobalVar.ExecSQL(strSQLinsert);
                bolReturn = true;
                #endregion

                return bolReturn;
            }
            catch (Exception e)
            {
                // MessageBox.Show(e.Message);
                return false;
            }
        }
        public static bool SaveEdiData_NoLoadid(string PalletID, string Sernum, string Weight)
        {
            try
            {
                bool bolReturn = false;
                double ActWeight_EDI_NoLoadid = 0;
                string strLocation = "";
                string strLocation_PalletID = "";
                string strCLIMAT = "";
                string strQTY = "";
                //string strPalletID = "";
                string strSQL = "";
                string strSQLFilter = "";
                string strSQLinsert = "";

                if (Weight != "")
                {
                    ActWeight_EDI_NoLoadid = double.Parse(Weight);
                }

                strSQL = "select distinct B.PALLID,B.CLIMAT AS Cust_PN,B.PALQTY AS Pallet_Qty,L.Location,l.Location_PalletID,l.QMS_PalletID From [PAL_QSMC_USA].PAL.PLLB B INNER JOIN [PAL_QSMC_USA].[DBO].Location_Pallet_Information L ON B.SERNUM='S'+L.SerailNo WHERE B.PALLID='" + PalletID + "' and L.Status='0' ";

                DataTable dt_ = clsGlobalVar.GetDataTable(strSQL);
                if (dt_.Rows.Count == 1)  //在产线（F4,F6,F7）做的拼板
                {
                    //strPALLID = dt_.Rows[0]["PALLID"].ToString();
                    strLocation = dt_.Rows[0]["Location"].ToString();
                    strLocation_PalletID = dt_.Rows[0]["Location_PalletID"].ToString();
                    strCLIMAT = dt_.Rows[0]["Cust_PN"].ToString();
                    strQTY = dt_.Rows[0]["Pallet_Qty"].ToString();

                }
                else if (dt_.Rows.Count > 1)//在外仓（H1,H2）做的拼板
                {
                    string strSQLPallet = "select distinct QMS_PalletID FROM Location_Pallet_Information WHERE SerailNo ='" + Sernum + "'";
                    DataTable dt2 = clsGlobalVar.GetDataTable(strSQL);

                    strSQL = "EXEC [PAL_QSMC_USA].[DBO].[usp_GetTMP_WGHT_NOLoadid] '" + dt2.Rows[0]["QMS_PalletID"] + "'";
                    DataTable dt3 = clsGlobalVar.GetDataTable(strSQL);
                    if (dt3.Rows.Count > 0)
                    {
                        strLocation = dt3.Rows[0]["Location"].ToString();
                        strLocation_PalletID = dt3.Rows[0]["Location_PalletID"].ToString();
                        strCLIMAT = dt3.Rows[0]["CLIMAT"].ToString();
                        strQTY = dt3.Rows[0]["Pallet_qty"].ToString();

                    }
                    else
                    {
                        //MessageBox.Show("There is no data!!请确保以下两个条件成立：（1）" + PalletID + "在储位系统中已获得储位，（2）QMS接口：[172.26.40.15].PAL_COMPSN_P80.dbo.[MIS_Return_PackData]" + PalletID + ",'P'有数据,若无,请联系QMS同仁。");
                        return false;

                    }

                }
                else
                {
                    return false;
                }


                #region  // Insert Data to WGHT_NOSI table

                strSQL = "SELECT * FROM [PAL_QSMC_USA].[DBO].WGHT_NOSI WHERE PALLID='" + PalletID + "' ";
                DataTable dt = clsGlobalVar.GetDataTable(strSQL + strSQLFilter);

                if (dt.Rows.Count > 0)
                {
                    strSQL = "DELETE FROM [PAL_QSMC_USA].[DBO].WGHT_NOSI WHERE PALLID='" + PalletID + "' ";
                    clsGlobalVar.ExecSQL(strSQL + strSQLFilter);
                }
                strSQL = "INSERT INTO [PAL_QSMC_USA].[DBO].WGHT_NOSI ( SHIPMENT, PALLID,Plant,CLIMAT,PALLETQTY, Location, Location_PalletID, ACTWT, MAC, WEIGHTDATE, WEIGHTTIME) VALUES('','" + PalletID + "','C1','" + strCLIMAT + "','" + strQTY + "','" + strLocation + "','" + strLocation_PalletID + "','" + ActWeight_EDI_NoLoadid + "','" + MAC + "',CONVERT(VARCHAR(100),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''))";

                clsGlobalVar.ExecSQL(strSQL);

                DataTable dtFalg = clsGlobalVar.GetDataTable(" SELECT * FROM [WMS_WEIGHT].DBO.WeightInfo WITH (NOLOCK) WHERE PALLETID = '" + PalletID + "'");
                if (dtFalg.Rows.Count > 0)
                {
                    strSQLinsert += " UPDATE [WMS_WEIGHT].DBO.WeightInfo SET InDate=CONVERT(CHAR(8),GETDATE(),112),InTime=REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),MAC = '" + MAC + "' WHERE  PALLETID = '" + PalletID + "'";
                }
                else
                {
                    strSQLinsert += " INSERT INTO [WMS_WEIGHT].DBO.WeightInfo (LOADID,PALLETID,InDate,InTime,MAC)VALUES('PAL','" + PalletID + "',CONVERT(CHAR(8),GETDATE(),112),REPLACE(CONVERT(CHAR(8),GETDATE(),108),':',''),'" + MAC + "')";
                }
                clsGlobalVar.ExecSQL(strSQLinsert);
                bolReturn = true;
                #endregion

                return bolReturn;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public static string getSoldto(string strsoldtoOld)
        {

            if (strsoldtoOld == "QIT-PALU")
            {
                return "PAL_QSMC_USA";
            }
            else if (strsoldtoOld == "QIT-PALE")
            {
                return "PAL_QSMC_EMEA";
            }
            else
            {
                return "PAL_QSMC_ASIA";
            }
        }
        private string EDIReturnLableZPL(Dictionary<string, object> dic, string strsoldto, string strLoadid, string strPid)
        {
            string strAND = "";
            string strShpdat = DateTime.Now.ToString("yyyyMMdd");
            DataTable dt = new DataTable();
            string strRegion = getSoldto(strsoldto);
            strSql = "select top 1 climod from  " + strRegion + ".dbo.TMPPALSHPMRK(NOLOCK) where LOADID='" + strLoadid + "' AND  right(PALLID, 2) = '" + strPid + "'";
            string strmod = clsGlobalVar.GetFieldValue(strSql);


            #region 是否加尾板的逻辑，自动线暂时不用考虑，可以不加这段逻辑   
            //string strIsWb = string.Format(" SELECT [PAL_QSMC_USA].[dbo].[Fuc_IsLatamHighRiskCountries]('{0}' )", strLoadid);

            //if (txtFlag.Text.Trim().ToUpper().EndsWith("$") || strIsWb.ToUpper().Equals("Y"))//|| (ibPB && ibBULK) ////modify by wepy 1226
            //{
            //    strFlag = "$";
            //}
            #endregion

            #region label data
            strAND = " AND  right(T.PALLID,2)='" + strPid + "'";
            string strSQL = "SELECT T.LOADID, CAST(COUNT(DISTINCT(T.SHPMRK)) AS VARCHAR(3)) AS PALQTY  INTO #PALQTY FROM " + strRegion + ".dbo.TMPPALSHPMRK(NOLOCK) T INNER JOIN (SELECT DISTINCT LOADID FROM " + strRegion + ".dbo.tmpPPSJOIT(NOLOCK)WHERE (SSCC18='" + strLoadid + "' OR LOADID='" + strLoadid + "' ))LIST ON LIST.LOADID=T.LOADID GROUP BY T.LOADID ; SELECT B.LOADID, B.PALLID, B.CTOBTR  INTO #CTO FROM ( SELECT A.LOADID, A.PALLID, A.CTOBTR, ROW_NUMBER()OVER(PARTITION BY A.LOADID, A.PALLID ORDER BY A.CTOBTR DESC) AS SEQ  FROM (  SELECT DISTINCT LOADID, PALLID, CASE WHEN LEFT(CLIMAT,1)='Z' AND CLIMAT NOT LIKE '%/%' THEN 'CTO' WHEN LEFT(CLIMAT,3)='625' THEN 'CTO' ELSE  'BTR' END AS CTOBTR  FROM " + strRegion + ".dbo.tmpPPSJOIT(NOLOCK)  WHERE (SSCC18='" + strLoadid + "' OR LOADID='" + strLoadid + "' ) )A  )B  WHERE B.SEQ=1 ;  SELECT T.LOADID, T.PALLID, SUM(T.MATQTY) AS MATQTY INTO #MATQTY FROM " + strRegion + ".dbo.TMPPALSHPMRK(NOLOCK) T  INNER JOIN (  SELECT DISTINCT LOADID, PALLID  FROM " + strRegion + ".dbo.tmpPPSJOIT(NOLOCK)  WHERE LOADID='" + strLoadid + "' OR SSCC18='" + strLoadid + "' )P ON P.LOADID=T.LOADID AND P.PALLID=T.PALLID  GROUP BY T.LOADID, T.PALLID ; SELECT * INTO #GS1 FROM (SELECT DISTINCT LOADID, PALLID, REFNM3, SSCC18 AS SSCCNO, '' AS CLIMAT, '' AS GTINNO, 0 AS CUNQTY, '' AS MODNUM  FROM " + strRegion + ".dbo.tmpPPSJOIT(NOLOCK)  WHERE REFNM3='2.Mix' AND (LOADID='" + strLoadid + "' OR SSCC18='" + strLoadid + "') UNION  SELECT DISTINCT P.LOADID, P.PALLID, P.REFNM3, P.SSCC18 AS SSCCNO, P.CLIMAT, CASE WHEN ISNULL(S.UPCCOD,'')='' THEN '' ELSE RIGHT(REPLICATE('0',14)+CAST(S.UPCCOD AS VARCHAR),14) END AS GTINNO, QTY.CUNQTY, S.MODNUM FROM " + strRegion + ".dbo.tmpPPSJOIT(NOLOCK) P  INNER JOIN (  SELECT LOADID, PALLID, SSCC18 AS SSCCNO, COUNT(DISTINCT TEMPSN) AS CUNQTY  FROM " + strRegion + ".dbo.tmpPPSJOIT(NOLOCK)  WHERE REFNM3='1.Basic' AND (LOADID='" + strLoadid + "' OR SSCC18='" + strLoadid + "')  GROUP BY LOADID, PALLID, SSCC18  )QTY ON QTY.LOADID=P.LOADID AND QTY.PALLID=P.PALLID AND QTY.SSCCNO=P.SSCC18  INNER JOIN " + strRegion + ".dbo.SC14 S ON S.CLIMAT=P.CLIMAT  WHERE P.REFNM3='1.Basic' AND (P.LOADID='" + strLoadid + "' OR P.SSCC18='" + strLoadid + "')  UNION  SELECT DISTINCT LOADID, PALLID, '' AS REFNM3, '' AS SSCCNO, '' AS CLIMAT, '' AS GTINNO, 0 AS CUNQTY, '' AS MODNUM  FROM " + strRegion + ".dbo.tmpPPSJOIT(NOLOCK)  WHERE REFNM3 NOT IN ('1.Basic','2.Mix') AND (LOADID='" + strLoadid + "' OR SSCC18='" + strLoadid + "')) A ;SELECT T.PALLID,  L.LOADID+'-'+CASE WHEN LEN(CAST(T.PALLID AS INT))>=3 THEN T.PALLID ELSE RIGHT(T.PALLID,2) END AS LOADID,  MIN(L.LOADQT) AS TTLQTY, T.SHPMRK, ISNULL (S.SHPMRK,'') AS SHIPTO,  CAST(CAST(T.PALLID AS INT) AS VARCHAR(3))+'-'+#PALQTY.PALQTY AS PLTQTY, #MATQTY.MATQTY AS PALLETQTY, L.SCACID, L.TRKMAS, ISNULL(#GS1.SSCCNO,'') AS SSCCNO, ISNULL(#GS1.CLIMAT,'') AS CLIMAT, ISNULL(#GS1.CUNQTY,0) AS CUNQTY, #GS1.REFNM3, ISNULL(#GS1.MODNUM,'') AS MODNUM,  J.SHPMK1 +'/'+P.PalletCode AS SHPMK1, J.SHPMK2, #CTO.CTOBTR, CASE WHEN #CTO.CTOBTR='CTO' AND #GS1.GTINNO='00000000000000' THEN '' ELSE ISNULL(#GS1.GTINNO,'') END AS GTINNO ,T.LOCNUM  FROM " + strRegion + ".dbo.LOAT(NOLOCK) L   INNER JOIN " + strRegion + ".dbo.TMPPALSHPMRK(NOLOCK) T ON L.LOADID=T.LOADID INNER JOIN " + strRegion + ".dbo.VBAK(NOLOCK) K ON K.ORDNUM=T.ORDNUM  INNER JOIN " + strRegion + ".dbo.tmpPPSJOIT(NOLOCK) J ON J.LOADID=T.LOADID AND J.PALLID=T.PALLID   INNER JOIN " + strRegion + ".dbo.PKRL(NOLOCK) P ON T.CLIMOD=P.OVPTYP AND T.PALVOL=P.LENGTH   AND P.PALWET=T.PLTWET AND T.SHPMOD=P.SHPMOD INNER JOIN #CTO ON #CTO.LOADID=T.LOADID AND #CTO.PALLID=T.PALLID  INNER JOIN #PALQTY ON #PALQTY.LOADID=T.LOADID  INNER JOIN #MATQTY ON #MATQTY.LOADID=T.LOADID AND #MATQTY.PALLID=T.PALLID  INNER JOIN #GS1 ON #GS1.LOADID=T.LOADID AND #GS1.PALLID=T.PALLID  LEFT JOIN [PAL_QSMC_USA].[DBO].SHPMRK_POE(NOLOCK) S ON S.POE=CASE WHEN L.SHPCOO IN ('UPS', 'UPSW', 'FEDU','ZZ') THEN L.SHPCOO+K.SHPCOO ELSE L.SHPCOO END  WHERE (L.LOADID='" + strLoadid + "' OR J.SSCC18='" + strLoadid + "')  " + strAND + "GROUP BY L.LOADID, L.SCACID, T.PALLID, L.TRKMAS, T.SHPMRK, S.SHPMRK, #GS1.SSCCNO, #GS1.CLIMAT, #GS1.GTINNO, #GS1.CUNQTY, #GS1.REFNM3, #PALQTY.PALQTY, #GS1.MODNUM, #MATQTY.MATQTY, J.SHPMK1, J.SHPMK2, #CTO.CTOBTR ,T.LOCNUM ,P.PalletCode ";
            #endregion

            dt = clsGlobalVar.GetDataTable(strSQL);
            if (dt.Rows.Count != 0)
            {
                if (dt.Rows[0]["SHIPTO"].ToString() == "")
                {
                    return strLoadid + " Have no POE";
                }
            }
            else
            {
                return strLoadid + " Have no Data";
            }

            //strLabel:EDI Ship-Mark, strGS1output:GS1
            string strpallet = dt.Rows[0]["PLTQTY"].ToString();
            string strsumQty = dt.Rows[0]["TTLQTY"].ToString();
            string strDate = String.Format("{0:yyyy-MM-dd HH:mm}", DateTime.Now);
            string strQty = dt.Rows[0]["PALLETQTY"].ToString();
            string strPOE = dt.Rows[0]["SHIPTO"].ToString();
            string strSPNO = dt.Rows[0]["SHPMRK"].ToString();
            string strloadidNO = dt.Rows[0]["LOADID"].ToString();
            string strpModel = dt.Rows[0]["PALLID"].ToString();
            string strGS1 = dt.Rows[0]["SHPMK1"].ToString();
            string strSSCC = dt.Rows[0]["SHPMK2"].ToString();
            string strLocation = dt.Rows[0]["LOCNUM"].ToString();

            //出货返回PP带条数和打包方式
            string strPPqty = "";
            string strPPtype = "";
            DataTable dtPP = ReturnPPQty("SHIP", strmod, strQty);
            if (dtPP.Rows.Count <= 0)
            {
                strPPqty = "2";
                strPPtype = "21";
            }
            else
            {
                strPPqty = dtPP.Rows[0][0].ToString();
                strPPtype = dtPP.Rows[0][1].ToString();
            }
            dic.Add("PPqty", strPPqty);
            dic.Add("PPTYPE", strPPtype);
            EDIShpmrk ediShpmrk = new EDIShpmrk("Y", strpallet, strsumQty, strDate, strQty, strPOE, strSPNO, strloadidNO, strpModel, strGS1, strSSCC, strLocation);
            NonEDIShpmrk nonEDIShpmrk = new NonEDIShpmrk("N");

            dic.Add("EDISHPMRK", ediShpmrk);
            dic.Add("NONEDISHPMRK", nonEDIShpmrk);

            //GS1
            switch (dt.Rows[0]["REFNM3"].ToString())
            {
                case "1.Basic":
                    string strSSCCNO = dt.Rows[0]["SSCCNO"].ToString();
                    string strCLIMAT = dt.Rows[0]["CLIMAT"].ToString();
                    string strCUNQTY = dt.Rows[0]["CUNQTY"].ToString();
                    string strGTINNO = dt.Rows[0]["GTINNO"].ToString();
                    string strMODNUM = dt.Rows[0]["MODNUM"].ToString();
                    GS1 GS1 = new GS1("Y", strMODNUM, strCLIMAT, strSSCCNO, strCUNQTY, strGTINNO);
                    GS1MIX GS1MIX = new GS1MIX("N");
                    PalletID PalletID = new PalletID("N");
                    dic.Add("GS1", GS1);
                    dic.Add("GS1MIX", GS1MIX);
                    dic.Add("PalletID", PalletID);
                    break;
                case "2.Mix":
                    string strSSCCNOMIX = dt.Rows[0]["SSCCNO"].ToString();
                    GS1MIX GS1MIXA = new GS1MIX("Y", strSSCCNOMIX);
                    GS1 GS1A = new GS1("N");
                    PalletID PalletIDA = new PalletID("N");
                    dic.Add("GS1", GS1A);
                    dic.Add("GS1MIX", GS1MIXA);
                    dic.Add("PalletID", PalletIDA);
                    break;
                default:
                    GS1 GS1B = new GS1("N");
                    GS1MIX GS1MIXB = new GS1MIX("N");
                    PalletID PalletIDB = new PalletID("N");
                    dic.Add("GS1", GS1B);
                    dic.Add("GS1MIX", GS1MIXB);
                    dic.Add("PalletID", PalletIDB);
                    break;
            }
            //FireLabel&OverPack EMEA GEN Model除外，如M4A、U4、L4、U3等model不需要打印，其他都需要打印，如果有新增的model需要调整此SP
            strSql = "exec " + strRegion + ".dbo.checkFireLabelOverPackPrint '" + strLoadid + "' ,'" + strPid + "' ";
            dt = clsGlobalVar.GetDataTable(strSql);
            if (dt.Rows.Count > 0)
            {
                FireLabel FL = new FireLabel("N");
                OverPack OP = new OverPack("N");
                dic.Add("FireLabel", FL);
                dic.Add("OverPack", OP);
            }
            else
            {
                FireLabel FL = new FireLabel("Y");
                OverPack OP = new OverPack("Y");
                dic.Add("FireLabel", FL);
                dic.Add("OverPack", OP);
            }

            return "_OK_";
        }

        private DataTable ReturnPPQty(string strType, string strModel, string strQty)
        {
            DataTable dt = new DataTable();
            strSql = "select top 1 TYPE from PAL_QSMC_USA.dbo.Plant_Model where CLIMOD='" + strModel + "' ";
            string strMod = clsGlobalVar.GetFieldValue(strSql).ToString().ToUpper();
            if (strMod == "PB")
            {
                strSql = "select top 1 PPqty,PPtype from PAL_QSMC_USA.dbo.[AutoPPSTapeQty] where type='" + strType + "' and model='" + strModel + "' and qty='" + strQty + "'";
                dt = clsGlobalVar.GetDataTable(strSql);
                return dt;
            }
            else
            {
                return dt;
            }
        }
        private string NONEDIReturnLableZPL(Dictionary<string, object> dic, string strSN, string flag)
        {

            string strSQL = "";
            string strCustomerID = "";
            string strShipmentNo = "";
            string strShipMark = "";
            string strDate = String.Format("{0:yyyy-MM-dd HH:mm}", DateTime.Now);
            DataTable dtPalletList = new DataTable();
            DataTable dtShipmarkList = new DataTable();
            if (flag == "SCAN")//非提前称重
            {
                strSQL = string.Format(@"select SU.CustomerID,SU.REFNM2 as ShipmentNo,SP.Shipmark from SDS_NONEDI.dbo.Shipment_Unit SU 
                                        inner join SDS_NONEDI.dbo.Shipment_Package SP on SP.ShipmentNo = SU.REFNM2 and SP.PalletID = SU.PalletID 
                                        where SU.SerialNo =  '{0}'  or SU.QMS_BoxID = '{0}'", strSN);
                DataTable dt = new DataTable();
                dt = clsGlobalVar.GetDataTable(strSQL);
                strCustomerID = dt.Rows[0][0].ToString();
                strShipmentNo = dt.Rows[0][1].ToString();
                strShipMark = dt.Rows[0][2].ToString();
            }
            else//提前称重
            {
                strCustomerID = flag;
            }
            PalletID palletID = new PalletID();
            if (strCustomerID == "PAL")
            {
                palletID.labelName = "PalletID.txt";
                palletID.print = "N";
            }
            else
            {
                strSQL = string.Format(@"exec SDS_NONEDI.dbo.usp_GetQMSData_WeighingInAdvance_H2AT '{0}','{1}','B'", strCustomerID, strSN);
                dtPalletList = clsGlobalVar.GetDataTable(strSQL);

                palletID.labelName = "PalletID.txt";
                palletID.print = "Y";
                palletID.PALLET_ID = dtPalletList.Rows[0]["Pallet_ID"].ToString();
                palletID.QPN = dtPalletList.Rows[0]["Quanta_PN"].ToString();
                palletID.CPN = dtPalletList.Rows[0]["Cust_PN"].ToString();
                palletID.LOCATION = dtPalletList.Rows[0]["Pallet_Type"].ToString();
                palletID.QTY = dtPalletList.Rows[0]["Pallet_Qty"].ToString();
                palletID.DATE = strDate;
            }


            EDIShpmrk ediShpmrk = new EDIShpmrk("N");

            NonEDIShpmrk nonEDIShpmrk = new NonEDIShpmrk();
            if (flag == "SCAN")//非提前称重
            {
                strSQL = string.Format(@"exec SDS_NONEDI.dbo.usp_Label_Get_Shipmark_byShipment_H2_ATPPS '{0}','{1}','{2}','1'", strCustomerID, strShipmentNo, strShipMark);
                dtShipmarkList = clsGlobalVar.GetDataTable(strSQL);

                nonEDIShpmrk.labelName = "NONEDI_SHIPMARK.txt";
                nonEDIShpmrk.print = "Y";
                nonEDIShpmrk.CUST = dtShipmarkList.Rows[0]["LabelFeild8"].ToString();
                nonEDIShpmrk.SHIPTO = dtShipmarkList.Rows[0]["LabelFeild4"].ToString();
                nonEDIShpmrk.MADEIN = dtShipmarkList.Rows[0]["LabelFeild5"].ToString();
                nonEDIShpmrk.PLT = dtShipmarkList.Rows[0]["LabelFeild9"].ToString();
                nonEDIShpmrk.MODE = dtShipmarkList.Rows[0]["LabelFeildE"].ToString();
                nonEDIShpmrk.STRQTY = dtShipmarkList.Rows[0]["LabelFeildA"].ToString();
                nonEDIShpmrk.STRMARK = dtShipmarkList.Rows[0]["LabelFeild3"].ToString();
                nonEDIShpmrk.STRSHIPMARK = dtShipmarkList.Rows[0]["LabelFeild6"].ToString();
                nonEDIShpmrk.SINO = dtShipmarkList.Rows[0]["LabelFeild1"].ToString();
                nonEDIShpmrk.PALLID = dtShipmarkList.Rows[0]["LabelFeildD"].ToString();
                nonEDIShpmrk.PTYPE = dtShipmarkList.Rows[0]["LabelFeildT"].ToString();
                nonEDIShpmrk.DATE = dtShipmarkList.Rows[0]["LabelFeild7"].ToString();

                strSQL = "select SDS_NONEDI.dbo.Label_CheckAddShipMark('" + dtShipmarkList.Rows[0]["CustomerID"].ToString() + "','" + dtShipmarkList.Rows[0]["LabelFeild1"].ToString() + "')";
                string overpack_flag = clsGlobalVar.GetFieldValue(strSQL);
                if (overpack_flag == "Y")
                {
                    nonEDIShpmrk.OVERPACK = "OVERPACK";
                }
                else
                {
                    nonEDIShpmrk.OVERPACK = "";
                }
            }
            else
            {
                nonEDIShpmrk.print = "N";
            }
            dic.Add("PPqty", "2");
            dic.Add("PPTYPE", "21");
            dic.Add("EDISHPMRK", ediShpmrk);
            dic.Add("NONEDISHPMRK", nonEDIShpmrk);
            GS1 GS1 = new GS1("N");
            GS1MIX GS1MIX = new GS1MIX("N");
            FireLabel FL = new FireLabel("N");
            OverPack OP = new OverPack("N");
            dic.Add("GS1", GS1);
            dic.Add("GS1MIX", GS1MIX);
            dic.Add("PalletID", palletID);
            dic.Add("FireLabel", FL);
            dic.Add("OverPack", OP);

            return "_OK_";
        }

    }
}
