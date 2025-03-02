using System.Data;
using WPFNode.Demo.Models;

namespace WPFNode.Demo.Services
{
    public class TestDataService
    {
        public ExcelTableData CreateTestTableData()
        {
            var tableData = new ExcelTableData
            {
                TableName = "TestMonsterTable",
                Headers = new List<string> { "ID", "Name", "Level", "HP", "Attack" }
            };

            // 데이터 테이블 컬럼 생성
            foreach (var header in tableData.Headers)
            {
                tableData.Data.Columns.Add(header);
            }

            // 테스트 데이터 추가
            var testData = new[]
            {
                new[] { "1001", "골렘", "10", "1000", "50" },
                new[] { "1002", "오크", "5", "500", "30" },
                new[] { "1003", "고블린", "3", "200", "15" },
                new[] { "1004", "드래곤", "20", "5000", "200" }
            };

            foreach (var row in testData)
            {
                var dataRow = tableData.Data.NewRow();
                for (int i = 0; i < row.Length; i++)
                {
                    dataRow[i] = row[i];
                }
                tableData.Data.Rows.Add(dataRow);
            }

            return tableData;
        }
    }
} 