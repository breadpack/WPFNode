using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using WPFNode.Models;
using WPFNode.Demo.Models;
using WPFNode.Attributes;
using WPFNode.Interfaces;

namespace WPFNode.Demo.Nodes
{
    [NodeName("Excel Input")]
    [NodeCategory("Data")]
    [NodeDescription("엑셀 테이블 데이터를 입력으로 사용하는 노드")]
    public class ExcelInputNode : NodeBase
    {
        private ExcelTableData _tableData;
        private int _currentRowIndex;
        private List<string> _headers;
        private string _identifier;

        public string Identifier
        {
            get => _identifier;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("식별자는 비어있을 수 없습니다.", nameof(value));
                _identifier = value;
            }
        }

        [JsonIgnore]
        public IReadOnlyList<string> Headers => _headers;

        public ExcelInputNode(INodeCanvas canvas, Guid guid, string identifier) : base(canvas, guid)
        {
            _currentRowIndex = 0;
            _headers = new List<string>();
            _tableData = new ExcelTableData();
            Identifier = identifier;
        }

        public void SetHeaders(IEnumerable<string> headers)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            _headers.Clear();
            _headers.AddRange(headers);
            
            // 헤더가 변경되면 출력 포트도 다시 생성
            RecreateOutputPorts();
            
            // 테이블 구조 업데이트
            UpdateTableStructure();
        }

        private void UpdateTableStructure()
        {
            // 새로운 테이블 생성
            _tableData = new ExcelTableData
            {
                TableName = "DataTable",
                Headers = new List<string>(_headers)
            };

            // 데이터 테이블 컬럼 생성
            foreach (var header in _headers)
            {
                _tableData.Data.Columns.Add(header);
            }

            _currentRowIndex = 0;
        }

        private void RecreateOutputPorts()
        {
            // 기존 포트 제거
            ClearPorts();
            
            // 헤더 기반으로 출력 포트 생성
            foreach (var header in _headers)
            {
                CreateOutputPort<string>(header);
            }
        }

        protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            if (_currentRowIndex >= _tableData.Data.Rows.Count)
            {
                _currentRowIndex = 0;
                return;
            }

            // 현재 행의 데이터를 출력 포트로 전달
            var currentRow = _tableData.Data.Rows[_currentRowIndex];
            for (int i = 0; i < OutputPorts.Count; i++)
            {
                if (OutputPorts[i] is OutputPort<string> outputPort)
                {
                    outputPort.Value = currentRow[i].ToString();
                }
            }

            _currentRowIndex++;
            await Task.CompletedTask;
        }

        public void SetTableData(ExcelTableData tableData)
        {
            if (tableData == null)
                throw new ArgumentNullException(nameof(tableData));

            _tableData = tableData;
            SetHeaders(tableData.Headers);
        }

        public void AddRow(IEnumerable<string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var row = _tableData.Data.NewRow();
            var valueArray = values.ToArray();

            if (valueArray.Length != _headers.Count)
                throw new ArgumentException($"값의 개수({valueArray.Length})가 헤더 개수({_headers.Count})와 일치하지 않습니다.");

            for (int i = 0; i < valueArray.Length; i++)
            {
                row[i] = valueArray[i];
            }

            _tableData.Data.Rows.Add(row);
        }

        public class SaveData
        {
            public string Identifier { get; set; }
            public List<string> Headers { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        public SaveData GetSaveData()
        {
            return new SaveData
            {
                Identifier = Identifier,
                Headers = new List<string>(_headers),
                X = X,
                Y = Y
            };
        }

        public void LoadFromSaveData(SaveData saveData)
        {
            if (saveData == null)
                throw new ArgumentNullException(nameof(saveData));

            if (saveData.Identifier != Identifier)
                throw new ArgumentException($"저장된 식별자({saveData.Identifier})가 현재 노드의 식별자({Identifier})와 일치하지 않습니다.");

            X = saveData.X;
            Y = saveData.Y;
            SetHeaders(saveData.Headers);
        }

        public override string ToString()
        {
            return $"Excel Input Node ({Identifier})";
        }
    }
} 