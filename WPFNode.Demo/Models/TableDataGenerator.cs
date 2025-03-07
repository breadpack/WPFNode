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
            Columns = new List<ColumnDefinition>
            {
                new() { Name = "Id", TypeName = "System.Int32", IsNullable = false },
                new() { Name = "Name", TypeName = "System.String", IsNullable = false },
                new() { Name = "Age", TypeName = "System.Int32", IsNullable = false },
                new() { Name = "Department", TypeName = "System.String", IsNullable = false },
                new() { Name = "Salary", TypeName = "System.Decimal", IsNullable = false }
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
            Columns = new List<ColumnDefinition>
            {
                new() { Name = "ProductId", TypeName = "System.Int32", IsNullable = false },
                new() { Name = "ProductName", TypeName = "System.String", IsNullable = false },
                new() { Name = "Category", TypeName = "System.String", IsNullable = false },
                new() { Name = "Price", TypeName = "System.Decimal", IsNullable = false },
                new() { Name = "Stock", TypeName = "System.Int32", IsNullable = false }
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
    
    public static TableData CreateMultipleEmployeeData()
    {
        var tableData = new TableData
        {
            TableName = "MultipleEmployees",
            Columns = new List<ColumnDefinition>
            {
                new() { Name = "Name1", TypeName = "System.String", IsNullable = true },
                new() { Name = "Age1", TypeName = "System.Int32", IsNullable = false },
                new() { Name = "Name2", TypeName = "System.String", IsNullable = true },
                new() { Name = "Age2", TypeName = "System.Int32", IsNullable = false },
                new() { Name = "Name3", TypeName = "System.String", IsNullable = true },
                new() { Name = "Age3", TypeName = "System.Int32", IsNullable = false }
            },
            Rows = new List<RowData>
            {
                new() { Values = new List<string> { "김철수", "30", "이영희", "28", "박민수", "35" } },
                new() { Values = new List<string> { "정지원", "32", "", "0", "한미영", "29" } },
                new() { Values = new List<string> { "최준호", "27", "강지현", "31", "", "0" } }
            }
        };

        return tableData;
    }
}
