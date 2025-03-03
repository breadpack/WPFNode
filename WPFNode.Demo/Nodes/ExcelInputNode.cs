using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using WPFNode.Models;
using WPFNode.Demo.Models;
using WPFNode.Attributes;
using WPFNode.Interfaces;

namespace WPFNode.Demo.Nodes {
    [NodeName("Excel Input")]
    [NodeCategory("Data")]
    [NodeDescription("엑셀 테이블 데이터를 입력으로 사용하는 노드")]
    public class ExcelInputNode : DynamicNode, ILoopNode {
        private TableData              _tableData;
        private int                    _currentRowIndex;
        private List<ColumnDefinition> _headers;
        private string                 _identifier;

        public bool IsLoopCompleted { get; private set; }

        [JsonIgnore]
        public IReadOnlyList<ColumnDefinition> Headers => _headers;

        public ExcelInputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
            _currentRowIndex = 0;
            _headers         = [];
            _tableData       = new();
        }

        public void SetTableData(TableData tableData) {
            _tableData = tableData ?? throw new ArgumentNullException(nameof(tableData));
            RecreateOutputPorts(tableData.Columns);
        }

        private void RecreateOutputPorts(IEnumerable<ColumnDefinition> headers) {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            // 헤더 정보 업데이트
            _headers.Clear();
            _headers.AddRange(headers);

            // 기존 포트 제거
            ClearPorts();

            // 헤더 기반으로 출력 포트 생성
            foreach (var header in _headers) {
                AddOutputPort(header.Name, Type.GetType(header.TypeName)!);
            }
        }

        public void Reset() {
            IsLoopCompleted  = false;
            _currentRowIndex = 0;
        }

        public Task<bool> ShouldContinueAsync(CancellationToken cancellationToken = default) {
            if (_currentRowIndex >= _tableData.Rows.Count) {
                IsLoopCompleted = true;
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override async Task ProcessAsync(CancellationToken cancellationToken = default) {
            if (_currentRowIndex >= _tableData.Rows.Count) {
                _currentRowIndex = 0;
                return;
            }

            // 현재 행의 데이터를 출력 포트로 전달
            var currentRow = _tableData.Rows[_currentRowIndex];
            for (int i = 0; i < OutputPorts.Count; i++) {
                if (i < currentRow.Values.Count && i < _headers.Count) {
                    var port = OutputPorts[i];
                    var value = JsonSerializer.Deserialize(currentRow.Values[i], _headers[i].Type);
                    
                    // 리플렉션을 사용하여 포트의 Value 속성에 값 설정
                    var portType = port.GetType();
                    var valueProperty = portType.GetProperty("Value");
                    if (valueProperty != null) {
                        valueProperty.SetValue(port, value);
                    }
                }
            }

            _currentRowIndex++;
            await Task.CompletedTask;
        }

        public override string ToString() {
            return $"Excel Input Node ({Id})";
        }
    }
}