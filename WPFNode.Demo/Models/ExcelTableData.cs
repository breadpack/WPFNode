using System.Collections.Generic;
using System.Data;

namespace WPFNode.Demo.Models
{
    public class ExcelTableData
    {
        public string TableName { get; set; }
        public List<string> Headers { get; set; }
        public DataTable Data { get; set; }
        
        public ExcelTableData()
        {
            Headers = new List<string>();
            Data = new DataTable();
        }
    }
} 