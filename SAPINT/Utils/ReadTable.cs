﻿namespace SAPINT.Utils
{
    using SAPINT;
    using System;
    using System.Collections;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using SAP.Middleware.Connector;
    using System.Collections.Generic;
    using System.Linq;

    public class ReadTable
    {
        private string _Delimiter;
        private int _FetchedRows;
        //读出的字段列表,接口调用后，就是读出表的字段列表
        private ReadTableFieldCollection _Fields;
        private string _FunctionName;
        private string _LastPrimaryKey;
        private int _PackageSize;
        private ReadTableFieldCollection _PrimaryKeys;
        private bool _RaiseIncomingPackageEvent;
        private int _RowCount;
        private int _RowSkip;
        private string _TableName;
        private bool _UsePrimaryKeyPackaging;
        private string _WhereClause;
        private int anzahlaufrufe;
        private bool BackgroundExtraction;
        private string BackgroundRequestID;
        private string BufferLocation;
        private RfcDestination des;
        private ArrayList fields;//读入的字段列表
        private OnIncomingPackage IncomingPackage;
        private bool OHSExtraction;
        private int OHSLastPackageNr;
        private int OHSRequestID;
        private ArrayList options;
        private OnPackageProgress PackageProgress;
        private DataTable t;
        public event OnIncomingPackage onIncomingPackage
        {
            add
            {
                OnIncomingPackage package2;
                OnIncomingPackage incomingPackage = this.IncomingPackage;
                do
                {
                    package2 = incomingPackage;
                    OnIncomingPackage package3 = (OnIncomingPackage)Delegate.Combine(package2, value);
                    incomingPackage = Interlocked.CompareExchange<OnIncomingPackage>(ref this.IncomingPackage, package3, package2);
                }
                while (incomingPackage != package2);
            }
            remove
            {
                OnIncomingPackage package2;
                OnIncomingPackage incomingPackage = this.IncomingPackage;
                do
                {
                    package2 = incomingPackage;
                    OnIncomingPackage package3 = (OnIncomingPackage)Delegate.Remove(package2, value);
                    incomingPackage = Interlocked.CompareExchange<OnIncomingPackage>(ref this.IncomingPackage, package3, package2);
                }
                while (incomingPackage != package2);
            }
        }
        public event OnPackageProgress onPackageProgress
        {
            add
            {
                OnPackageProgress progress2;
                OnPackageProgress packageProgress = this.PackageProgress;
                do
                {
                    progress2 = packageProgress;
                    OnPackageProgress progress3 = (OnPackageProgress)Delegate.Combine(progress2, value);
                    packageProgress = Interlocked.CompareExchange<OnPackageProgress>(ref this.PackageProgress, progress3, progress2);
                }
                while (packageProgress != progress2);
            }
            remove
            {
                OnPackageProgress progress2;
                OnPackageProgress packageProgress = this.PackageProgress;
                do
                {
                    progress2 = packageProgress;
                    OnPackageProgress progress3 = (OnPackageProgress)Delegate.Remove(progress2, value);
                    packageProgress = Interlocked.CompareExchange<OnPackageProgress>(ref this.PackageProgress, progress3, progress2);
                }
                while (packageProgress != progress2);
            }
        }
        //public ReadTable()
        //{
        //    this.fields = new ArrayList();
        //    this.options = new ArrayList();
        //    this._PrimaryKeys = new ReadTableFieldCollection();
        //    this._TableName = "";
        //    this._Fields = new ReadTableFieldCollection();
        //    this._WhereClause = "";
        //    this._Delimiter = "";
        //    this._FunctionName = "";
        //    this.BackgroundRequestID = "";
        //    this.BufferLocation = "";
        //    this._LastPrimaryKey = "";
        //}
        public ReadTable(string sysName)
        {
            this.fields = new ArrayList();
            this.options = new ArrayList();
            this._PrimaryKeys = new ReadTableFieldCollection();
            this._TableName = "";
            this._Fields = new ReadTableFieldCollection();
            this._WhereClause = "";
            this._Delimiter = "";
            this._FunctionName = "";
            this.BackgroundRequestID = "";
            this.BufferLocation = "";
            this._LastPrimaryKey = "";
            this.des = SAPDestination.GetDesByName(sysName);
            //if (Connection.Codepage.Equals("8000"))
            //{
            //    this._Delimiter = "|";
            //}
            //Connection.Log("ReadTable() Connection.Codepage = " + Connection.Codepage);
        }
        public void ActivateBackgroundExtraction(string BackgroundRequestID, string BufferLocation)
        {
            this.BackgroundRequestID = BackgroundRequestID;
            this.BufferLocation = BufferLocation;
            this.BackgroundExtraction = true;
        }
        public void ActivateOHSExtraction(int RequestID)
        {
            this.OHSExtraction = true;
            this.OHSRequestID = RequestID;
            this.PackageSize = 0xc350;
        }
        public void AddCriteria(string SQL)
        {
            if (SQL.Length > 0x47)
            {
                throw new SAPException(Messages.SQLtoolong);
            }
            this.options.Add(SQL);
        }
        //填加字段列表
        public void AddField(string FieldName)
        {
            this.fields.Add(FieldName.ToUpper().Trim());
        }
        private void AddWhereLine(IRfcTable toptions, string whereline)
        {
            //  toptions.AddRow()["TEXT"] = whereline;
            // IRfcStructure row = toptions.Metadata.LineType.CreateStructure();
            toptions.Append();
            toptions.CurrentRow.SetValue("TEXT", whereline);
        }
        private void ExecuteRFC_READ_TABLE(ref IRfcFunction f)
        {
            if (this.BackgroundExtraction)
            {
                f["ACTIONID"].SetValue("D");
                f["REQUESTID"].SetValue(this.BackgroundRequestID);
                if (!this.BufferLocation.Equals(""))
                {
                    f["LCTN"].SetValue(this.BufferLocation);
                }
            }
            IRfcTable toptions = f.GetTable("OPTIONS");
            this.InitWhereClause(ref toptions);
            this.anzahlaufrufe++;
            if ((this._UsePrimaryKeyPackaging && (this.anzahlaufrufe > 1)) && (((int)f["ROWCOUNT"].GetValue()) > 0))
            {
                f["ROWCOUNT"].SetValue(((int)f["ROWCOUNT"].GetValue()) + 1);
            }
            f["DELIMITER"].SetValue(this.Delimiter);
            //if (this.con.Logging)
            //{
            //    try
            //    {
            //        f.SaveToXML("ReadTableBeforeCall_" + this.anzahlaufrufe.ToString() + ".xml");
            //    }
            //    catch
            //    {
            //    }
            //}
            if (this._UsePrimaryKeyPackaging)
            {
                f["ROWSKIPS"].SetValue("0");
            }
            try
            {
                f.Invoke(des);
            }
            catch (RfcAbapException e)
            {
                this.fields.Clear();
                this.options.Clear();
                throw new SAPException(e.Key + e.Message);
            }
            catch (RfcAbapRuntimeException ee)
            {
                throw new SAPException(ee.Key + ee.Message);
            }
        }
        private void InitWhereClause(ref IRfcTable toptions)
        {
            toptions.Clear();
            if (this._WhereClause.Equals(""))
            {
                for (int i = 0; i < this.options.Count; i++)
                {
                    toptions.Append();
                    toptions.CurrentRow.SetValue("TEXT", this.options[i].ToString());
                    //toptions.AddRow()["TEXT"] = this.options[i].ToString();
                }
            }
            else
            {
                for (int j = 0; j < this._WhereClause.Length; j += 0x48)
                {
                    if ((this._WhereClause.Length - j) > 0x48)
                    {
                        IRfcStructure structure2 = toptions.Metadata.LineType.CreateStructure();
                        bool flag = false;
                        for (int k = 0; k < 0x47; k++)
                        {
                            if (this._WhereClause.Substring((j + 0x47) - k, 1).Equals(" "))
                            {
                                structure2["TEXT"].SetValue(this._WhereClause.Substring(j, 0x48 - k));
                                toptions.Append(structure2);
                                j -= k;
                                flag = true;
                                k = 0x48;
                            }
                        }
                        if (!flag)
                        {
                            structure2["TEXT"].SetValue(this._WhereClause.Substring(j, 0x48));
                            toptions.Append(structure2);
                        }
                    }
                    else
                    {
                        // toptions.AddRow()["TEXT"] = this._WhereClause.Substring(j);
                        toptions.Append();
                        toptions.CurrentRow["TEXT"].SetValue(this._WhereClause.Substring(j));
                    }
                }
                string str = "";
                foreach (IRfcStructure structure4 in toptions.ToList())
                {
                    str = str + structure4["TEXT"].ToString() + "\r\n";
                }
            }
            if (this._UsePrimaryKeyPackaging)
            {
                if (toptions.ToList().Count == 0)
                {
                    this.AddWhereLine(toptions, "(");
                }
                else
                {
                    this.AddWhereLine(toptions, "AND (");
                }
                for (int m = 0; m < this._PrimaryKeys.Count; m++)
                {
                    string whereline = this._PrimaryKeys[m].FieldName + " >= '" + this._PrimaryKeys[m].LastKeyValue + "'";
                    if (m == 0)
                    {
                        whereline = "( " + whereline;
                    }
                    else
                    {
                        whereline = "AND " + whereline;
                    }
                    if (m == (this._PrimaryKeys.Count - 1))
                    {
                        whereline = whereline + " )";
                    }
                    this.AddWhereLine(toptions, whereline);
                }
                for (int n = 0; n < this._PrimaryKeys.Count; n++)
                {
                    for (int num6 = n; num6 >= 0; num6--)
                    {
                        string str3;
                        if (num6 == n)
                        {
                            str3 = "OR ( " + this._PrimaryKeys[num6].FieldName + " > '" + this._PrimaryKeys[num6].LastKeyValue + "'";
                        }
                        else
                        {
                            str3 = "AND " + this._PrimaryKeys[num6].FieldName + " >= '" + this._PrimaryKeys[num6].LastKeyValue + "'";
                        }
                        if (num6 == 0)
                        {
                            str3 = str3 + " )";
                        }
                        this.AddWhereLine(toptions, str3);
                    }
                }
                //toptions.AddRow()["TEXT"] = ")";
                toptions.Append();
                toptions.CurrentRow.SetValue("TEXT", ")");
            }
            //if (!this.con.LogDir.Equals(""))
            //{
            //    this.con.Log("Building where: ");
            //    foreach (RFCStructure structure6 in toptions.Rows)
            //    {
            //        this.con.Log(structure6["TEXT"].ToString());
            //    }
            //}
        }
        private void ProcessRetrievdData(ref DataTable t, IRfcFunction f)
        {
            //this.con.Log("Enter ProcessRetrievdData; LastPrimaryKey=" + this._LastPrimaryKey);
            string str = "";
            IRfcTable table = f.GetTable("FIELDS");
            for (int i = 0; i < f.GetTable("DATA").RowCount; i++)
            {
                string str2 = (string)f.GetTable("DATA")[i].GetValue(0);
                str2 = str2.PadRight(f.GetTable("DATA").Metadata.LineType[0].NucLength);
                string[] strArray = null;
                if (this.UsePrimaryKeyPackaging)
                {
                    str = str2.Substring(0, this._PrimaryKeys.GetOverallLength());
                    if (i >= (f.GetTable("DATA").RowCount - 1))
                    {
                        int startIndex = 0;
                        foreach (ReadTableField field in this._PrimaryKeys)
                        {
                            field.LastKeyValue = str.Substring(startIndex, field.Length);
                            startIndex += field.Length;
                        }
                    }
                }
                if (!this.UsePrimaryKeyPackaging || (this._LastPrimaryKey != str))
                {
                    DataRow row = t.NewRow();
                    if (!this._Delimiter.Equals(""))
                    {
                        strArray = str2.Split(this._Delimiter.ToCharArray());
                    }
                    if (this._UsePrimaryKeyPackaging)
                    {
                        for (int j = this._PrimaryKeys.Count; j < table.RowCount; j++)
                        {
                            if (this._Delimiter.Equals(""))
                            {
                                String value = str2.Substring(Convert.ToInt32(table[j].GetValue("OFFSET").ToString()), Convert.ToInt32(table[j].GetValue("LENGTH").ToString()));
                                value = value.TrimEnd(' ');
                                String type = table[j].GetValue("TYPE").ToString();
                                row[table[j].GetValue(0).ToString()] = Converts.RfcValueToObject(value, type);
                            }
                            else if (strArray.Length > j)
                            {
                                String value = strArray[j];
                               // value = value.TrimStart(' ').TrimEnd(' ');//去除前导零。
                                value = value.TrimEnd(' ');
                                row[table[j].GetValue(0).ToString()] = value;
                            }
                        }
                    }
                    else
                    {
                        for (int k = 0; k < table.RowCount; k++)
                        {
                            if (this._Delimiter.Equals(""))
                            {
                               //// Console.WriteLine(str2.Length);
                               // Console.WriteLine("OFFSET:" + table[k]["OFFSET"].GetValue().ToString());
                               // Console.WriteLine("LENGTH:" + table[k]["LENGTH"].GetValue().ToString());
                                String value = str2.Substring(Convert.ToInt32(table[k]["OFFSET"].GetValue().ToString()), Convert.ToInt32(table[k]["LENGTH"].GetValue().ToString()));
                                value = value.TrimEnd(' ');
                                //Console.WriteLine(value);
                                String type = table[k].GetValue("TYPE").ToString();
                                //Console.WriteLine("TYPE:" + type);
                                
                                row[table[k][0].GetValue().ToString()] = Converts.RfcValueToObject(value, type);
                            }
                            else if (strArray.Length > k)
                            {
                                String value = strArray[k];
                                //value = value.TrimStart(' ').TrimEnd(' ');//去除前导零。
                                //value.Trim();
                                value = value.TrimEnd(' ');
                                row[table[k][0].GetValue().ToString()] = value;
                            }
                        }
                    }
                    t.Rows.Add(row);
                }
                this._LastPrimaryKey = str;
            }
            this._FetchedRows += f.GetTable("DATA").RowCount;
            if (this._RaiseIncomingPackageEvent)
            {
                if (this.IncomingPackage != null)
                {
                    this.IncomingPackage(this, t);
                    t.Rows.Clear();
                    t.AcceptChanges();
                }
            }
            else if (this.PackageProgress != null)
            {
                this.PackageProgress(this, t.Rows.Count);
            }
        }

        public ReadTableFieldCollection GetAllFieldsOfTable()
        {
            ReadTableFieldCollection fields = new ReadTableFieldCollection();
            if (string.IsNullOrEmpty(this.TableName))
            {
                return null;
            }
            IRfcFunction function = des.Repository.CreateFunction("DDIF_FIELDINFO_GET");
            function["LANGU"].SetValue(Converts.languageIsotoSap(this.des.Language));
            function["TABNAME"].SetValue(this.TableName);
            try
            {
                function.Invoke(des);
                for (int k = 0; k < function.GetTable("DFIES_TAB").RowCount; k++)
                {
                    IRfcStructure structure = function.GetTable("DFIES_TAB")[k];
                    fields.Add(new ReadTableField(structure["FIELDNAME"].GetValue().ToString(), Convert.ToInt32(structure["OUTPUTLEN"].GetValue()), structure["INTTYPE"].GetValue().ToString(), structure["FIELDTEXT"].GetValue().ToString(), Convert.ToInt32(structure["DECIMALS"].GetValue()), structure["CHECKTABLE"].GetValue().ToString()));
                }
                return fields;
            }
            catch (RfcAbapException ee)
            {
                throw new SAPException(ee.Key + ee.Message);
            }
        }
        private void RetrieveAllFieldsOfTable()
        {
            this._Fields.Clear();
            if (string.IsNullOrEmpty(this.TableName))
            {
                return;
            }
            IRfcFunction function = des.Repository.CreateFunction("DDIF_FIELDINFO_GET");
            function["LANGU"].SetValue(Converts.languageIsotoSap(this.des.Language));
            function["TABNAME"].SetValue(this.TableName);
            try
            {
                function.Invoke(des);
                for (int k = 0; k < function.GetTable("DFIES_TAB").RowCount; k++)
                {
                    IRfcStructure structure = function.GetTable("DFIES_TAB")[k];
                    this._Fields.Add(new ReadTableField(structure["FIELDNAME"].GetValue().ToString(), Convert.ToInt32(structure["OUTPUTLEN"].GetValue()), structure["INTTYPE"].GetValue().ToString(), structure["FIELDTEXT"].GetValue().ToString(), Convert.ToInt32(structure["DECIMALS"].GetValue())));
                }
            }
            catch (RfcAbapException ee)
            {
                throw new SAPException(ee.Key + ee.Message);
            }
        }
        public void Run()
        {
            IRfcFunction function;
            IRfcTable table2;
            this._LastPrimaryKey = "";
            this._FetchedRows = 0;
            if ((this._RowCount == 0) && (this._PackageSize > 0))
            {
                this._RowCount = 0x11d260c0;
            }
            this.anzahlaufrufe = 0;
            if (this._FunctionName.Equals(""))
            {
                bool isUnicode = this.des.Repository.UnicodeEnabled;
                // if (this.des.Codepage.ToString().Equals(""))
                // {
                //this.des.Repository.UnicodeEnabled = true;
                // }
                if (this._UsePrimaryKeyPackaging)
                {
                    throw new SAPException(Messages.Donotuseprimaykeypackagingwithoutacustomfunctionmodule);
                    //throw new Exception("Donot use primaykey packaging without a custom function module");
                }
                // function = new RFCFunction(this.con, "RFC_READ_TABLE");
                function = des.Repository.CreateFunction("RFC_READ_TABLE");
                //function.Exports.Add("QUERY_TABLE", RfcDataType.CHAR, this.con.IsUnicode ? 60 : 30);
                //function.Exports.Add("DELIMITER", RfcDataType.CHAR, this.con.IsUnicode ? 2 : 1);
                //function.Exports.Add("ROWSKIPS", RfcDataType.INT, 4);
                //function.Exports.Add("ROWCOUNT", RfcDataType.INT, 4);
                //function.Tables.Add("DATA").Columns.Add("WA", this.con.IsUnicode ? 0x400 : 0x200, RfcDataType.CHAR);
                //table2 = function.Tables.Add("FIELDS");
                //table2.Columns.Add("FIELDNAME", this.con.IsUnicode ? 60 : 30, RfcDataType.CHAR);
                //table2.Columns.Add("OFFSET", this.con.IsUnicode ? 12 : 6, RfcDataType.NUM);
                //table2.Columns.Add("LENGTH", this.con.IsUnicode ? 12 : 6, RfcDataType.NUM);
                //table2.Columns.Add("TYPE", this.con.IsUnicode ? 2 : 1, RfcDataType.CHAR);
                //table2.Columns.Add("FIELDTEXT", this.con.IsUnicode ? 120 : 60, RfcDataType.CHAR);
                //function.Tables.Add("OPTIONS").Columns.Add("TEXT", this.con.IsUnicode ? 0x90 : 0x48, RfcDataType.CHAR);
                //this.con.IsUnicode = isUnicode;
            }
            else
            {
                try
                {
                    function = this.des.Repository.CreateFunction(this._FunctionName);
                }
                catch (RfcAbapException ee)
                {
                    throw new SAPException(ee.Key + ee.Message);
                }
                IRfcTable table3 = function.GetTable("OPTIONS");
                table2 = function.GetTable("FIELDS");
                IRfcTable table = function.GetTable("DATA");
            }
            if (this._UsePrimaryKeyPackaging)
            {
                this._PrimaryKeys.Clear();
                IRfcFunction function2 = des.Repository.CreateFunction("DDIF_FIELDINFO_GET");//.GenerateFunctionObjectForDDIF_FIELDINFO_GET(this.con.IsUnicode);
                //function2.Connection = this.con;
                function2["TABNAME"].SetValue(this.TableName);
                function2.Invoke(des);
                foreach (IRfcStructure structure in function2.GetTable("DFIES_TAB").ToList())
                {
                    if (structure["KEYFLAG"].GetValue().ToString().Equals("X"))
                    {
                        ReadTableField newParameter = new ReadTableField(structure["FIELDNAME"].GetValue().ToString(), Convert.ToInt32(structure["OUTPUTLEN"].GetValue()), "C", "", 0);
                        this._PrimaryKeys.Add(newParameter);
                        // this.con.Log("Primary key Add " + newParameter);
                    }
                }
            }
            if (this.UsePrimaryKeyPackaging)
            {
                for (int j = 0; j < this._PrimaryKeys.Count; j++)
                {
                    // table2.AddRow()["FIELDNAME"] = this._PrimaryKeys[j].FieldName;
                    table2 = function.GetTable("FIELDS");
                    table2.Append();
                    table2.CurrentRow["FIELDNAME"].SetValue(this._PrimaryKeys[j].FieldName);
                }
                if (this.fields.Count == 0)
                {
                    this.GetAllFieldsOfTable();
                    for (int k = 0; k < this._Fields.Count; k++)
                    {
                        this.AddField(this._Fields[k].FieldName);
                    }
                }
            }
            for (int i = 0; i < this.fields.Count; i++)
            {
                //table2.AddRow()["FIELDNAME"] = this.fields[i].ToString();
                table2 = function.GetTable("FIELDS");
                table2.Append();
                table2.CurrentRow["FIELDNAME"].SetValue(this.fields[i].ToString());
            }
            function["QUERY_TABLE"].SetValue(this.TableName);
            if ((this._PackageSize > 0) && ((this._RowCount > this._PackageSize) || (this._RowCount == 0)))
            {
                function["ROWCOUNT"].SetValue(this._PackageSize);
                function["ROWSKIPS"].SetValue(0);
            }
            else
            {
                function["ROWCOUNT"].SetValue(this._RowCount);
                function["ROWSKIPS"].SetValue(this._RowSkip);
            }
            if (this.OHSExtraction)
            {
                function["ROWCOUNT"].SetValue(0);
                function["ROWSKIPS"].SetValue(0);
                this.OHSLastPackageNr = 1;
                function["REQUESTID"].SetValue(this.OHSRequestID);
                function["PACKETID"].SetValue(this.OHSLastPackageNr);
            }
            this.ExecuteRFC_READ_TABLE(ref function);
            this.fields.Clear();
            this.t = new DataTable();
            this.t.BeginInit();
            table2 = function.GetTable("FIELDS");
            if (this.UsePrimaryKeyPackaging)
            {
                this._Fields.Clear();
                for (int m = this._PrimaryKeys.Count; m < table2.RowCount; m++)
                {
                    IRfcStructure structure4 = table2[m];
                    this.t.Columns.Add(table2[m][0].GetValue().ToString().Trim(), Type.GetType("System.String"));
                    this._Fields.Add(new ReadTableField(structure4["FIELDNAME"].GetValue().ToString(), Convert.ToInt32(structure4["LENGTH"].GetValue()), structure4["TYPE"].GetValue().ToString(), structure4["FIELDTEXT"].GetValue().ToString(), 0));
                }
            }
            else
            {
                this._Fields.Clear();
                for (int n = 0; n < table2.RowCount; n++)
                {
                    IRfcStructure structure5 = table2[n];
                    this.t.Columns.Add(table2[n][0].GetValue().ToString().Trim(), Type.GetType("System.String"));
                    this._Fields.Add(new ReadTableField(structure5["FIELDNAME"].GetValue().ToString(), Convert.ToInt32(structure5["LENGTH"].GetValue()), structure5["TYPE"].GetValue().ToString(), structure5["FIELDTEXT"].GetValue().ToString(), 0));
                }
            }
            this.t.EndInit();
            this.ProcessRetrievdData(ref this.t, function);
            bool flag2 = false;
            if ((this._PackageSize > 0) && (this._FetchedRows < this._RowCount))
            {
                flag2 = true;
            }
            while (flag2)
            {
                if (this.OHSExtraction)
                {
                    function["ROWCOUNT"].SetValue(0);
                    function["ROWSKIPS"].SetValue(0);
                    this.OHSLastPackageNr++;
                    function["REQUESTID"].SetValue(this.OHSRequestID);
                    function["PACKETID"].SetValue(this.OHSLastPackageNr);
                }
                else
                {
                    function["ROWCOUNT"].SetValue(this.PackageSize);
                    function["ROWSKIPS"].SetValue(this._FetchedRows);
                    if ((this.RowCount > 0) && ((this._FetchedRows + this.PackageSize) > this.RowCount))
                    {
                        function["ROWCOUNT"].SetValue(this.RowCount - this._FetchedRows);
                    }
                }
                function.GetTable("DATA").Clear();
                this.ExecuteRFC_READ_TABLE(ref function);
                this.ProcessRetrievdData(ref this.t, function);
                if ((this._FetchedRows >= this._RowCount) && (this._RowCount > 0))
                {
                    flag2 = false;
                }
                if (function.GetTable("DATA").RowCount < this.PackageSize)
                {
                    flag2 = false;
                }
                if (this.OHSExtraction && (function.GetTable("DATA").RowCount > 0))
                {
                    flag2 = true;
                }
            }
            //function.Tables["DATA"].Dispose();
        }
        public void SetCustomFunctionName(string FunctionName)
        {
            this._FunctionName = FunctionName;
        }
        public RfcDestination Destination
        {
            get
            {
                return this.des;
            }
            set
            {
                this.des = value;
            }
        }
        public string Delimiter
        {
            get
            {
                return this._Delimiter;
            }
            set
            {
                this._Delimiter = value;
            }
        }
        public ReadTableFieldCollection Fields
        {
            get
            {
                return this._Fields;
            }
            set
            {
                this._Fields = value;
            }
        }
        public int PackageSize
        {
            get
            {
                return this._PackageSize;
            }
            set
            {
                this._PackageSize = value;
            }
        }
        public bool RaiseIncomingPackageEvent
        {
            get
            {
                return this._RaiseIncomingPackageEvent;
            }
            set
            {
                this._RaiseIncomingPackageEvent = value;
            }
        }
        public DataTable Result
        {
            get
            {
                return this.t;
            }
        }
        public int RowCount
        {
            get
            {
                return this._RowCount;
            }
            set
            {
                this._RowCount = value;
            }
        }
        public int RowSkip
        {
            get
            {
                return this._RowSkip;
            }
            set
            {
                this._RowSkip = value;
            }
        }
        public string TableName
        {
            get
            {
                return this._TableName;
            }
            set
            {
                this._TableName = value.ToUpper().Trim();
            }
        }
        public bool UsePrimaryKeyPackaging
        {
            get
            {
                return this._UsePrimaryKeyPackaging;
            }
            set
            {
                this._UsePrimaryKeyPackaging = value;
            }
        }
        //public ArrayList Options
        //{
        //    get
        //    {
        //        return this.options;
        //    }
        //    set
        //    {
        //        this.options = value;
        //    }
        //}
        public string WhereClause
        {
            get
            {
                return this._WhereClause;
            }
            set
            {
                this._WhereClause = value;
            }
        }
        public delegate void OnIncomingPackage(ReadTable Sender, DataTable PackageResult);
        public delegate void OnPackageProgress(ReadTable Sender, int FetchedRows);
    }
}