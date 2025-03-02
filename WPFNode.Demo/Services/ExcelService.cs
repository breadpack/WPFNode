using System;
using System.IO;
using System.Data;
using ClosedXML.Excel;
using WPFNode.Demo.Models;

namespace WPFNode.Demo.Services
{
    public class ExcelService
    {
        public ExcelTableData LoadExcelTable(string filePath, string sheetName = null)
        {
            var tableData = new ExcelTableData();
            
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = sheetName != null 
                    ? workbook.Worksheet(sheetName) 
                    : workbook.Worksheet(1);
                
                tableData.TableName = worksheet.Name;
                
                // Get headers
                var headerRow = worksheet.Row(1);
                foreach (var cell in headerRow.CellsUsed())
                {
                    tableData.Headers.Add(cell.Value.ToString());
                    tableData.Data.Columns.Add(cell.Value.ToString());
                }
                
                // Get data
                var rows = worksheet.RowsUsed().Skip(1); // Skip header
                foreach (var row in rows)
                {
                    var dataRow = tableData.Data.NewRow();
                    for (int i = 0; i < tableData.Headers.Count; i++)
                    {
                        dataRow[i] = row.Cell(i + 1).Value.ToString();
                    }
                    tableData.Data.Rows.Add(dataRow);
                }
            }
            
            return tableData;
        }
        
        public void SaveExcelTable(string filePath, ExcelTableData tableData)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(tableData.TableName);
                
                // Write headers
                for (int i = 0; i < tableData.Headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = tableData.Headers[i];
                }
                
                // Write data
                for (int row = 0; row < tableData.Data.Rows.Count; row++)
                {
                    for (int col = 0; col < tableData.Headers.Count; col++)
                    {
                        worksheet.Cell(row + 2, col + 1).Value = tableData.Data.Rows[row][col].ToString();
                    }
                }
                
                workbook.SaveAs(filePath);
            }
        }
    }
} 