﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections;

namespace ExcelAddIn1
{

    /// <summary>
    /// 保存上运行过程中的字段与条件
    /// </summary>
   public class TableInfo
    {
       public TableInfo()
       {
           Fields = new List<SAPINT.Utils.ReadTableField>();
           Options = new List<string>();
       }
       //表名
       public string Name { get; set; }
       //字段列表
       public List<SAPINT.Utils.ReadTableField> Fields { get; set; }
       //条件
       public List<string> Options { get; set; }
    }
}
