using System;
using System.Collections.Generic;

namespace WPFNode.Demo.Models;

public static class TableDataGenerator
{
    public static TableData CreateSampleEmployeeData()
    {
        var tableData = new TableData
        {
            TableName = "Employees",
            Headers = new List<string> { "Id", "Name", "Age", "Department", "Salary" },
            Columns = new List<ColumnDefinition>
            {
                new() { Name = "Id", Type = "System.Int32", IsNullable = false },
                new() { Name = "Name", Type = "System.String", IsNullable = false },
                new() { Name = "Age", Type = "System.Int32", IsNullable = false },
                new() { Name = "Department", Type = "System.String", IsNullable = false },
                new() { Name = "Salary", Type = "System.Decimal", IsNullable = false }
            },
            Rows = new List<RowData>
            {
                new() { Values = new List<string> { "1", "김철수", "30", "개발팀", "5000000" } },
                new() { Values = new List<string> { "2", "이영희", "28", "디자인팀", "4500000" } },
                new() { Values = new List<string> { "3", "박민수", "35", "기획팀", "6000000" } },
                new() { Values = new List<string> { "4", "정지원", "32", "개발팀", "5500000" } },
                new() { Values = new List<string> { "5", "한미영", "29", "디자인팀", "4800000" } }
            }
        };

        return tableData;
    }

    public static TableData CreateSampleProductData()
    {
        var tableData = new TableData
        {
            TableName = "Products",
            Headers = new List<string> { "ProductId", "ProductName", "Category", "Price", "Stock" },
            Columns = new List<ColumnDefinition>
            {
                new() { Name = "ProductId", Type = "System.Int32", IsNullable = false },
                new() { Name = "ProductName", Type = "System.String", IsNullable = false },
                new() { Name = "Category", Type = "System.String", IsNullable = false },
                new() { Name = "Price", Type = "System.Decimal", IsNullable = false },
                new() { Name = "Stock", Type = "System.Int32", IsNullable = false }
            },
            Rows = new List<RowData>
            {
                new() { Values = new List<string> { "1", "노트북", "전자기기", "1500000", "50" } },
                new() { Values = new List<string> { "2", "스마트폰", "전자기기", "1000000", "100" } },
                new() { Values = new List<string> { "3", "마우스", "주변기기", "50000", "200" } },
                new() { Values = new List<string> { "4", "키보드", "주변기기", "150000", "150" } },
                new() { Values = new List<string> { "5", "모니터", "전자기기", "500000", "80" } }
            }
        };

        return tableData;
    }
} 