using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using WPFNode.Models;
using WPFNode.Demo.Models;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models.Execution;

namespace WPFNode.Demo.Nodes
{
    [NodeName("Excel Input")]
    [NodeCategory("Data")]
    [NodeDescription("엑셀 테이블 데이터를 입력으로 사용하는 노드")]
    public class ExcelInputNode : NodeBase, IFlowEntry
    {
        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }
        
        private TableData _tableData;
        private int _currentRowIndex;
        private string _identifier;

        public bool IsLoopCompleted { get; private set; }

        [JsonIgnore]
        public IReadOnlyList<ColumnDefinition> Headers => _tableData.Columns;

        public ExcelInputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            _currentRowIndex = 0;
            _tableData = new();
        }

        public void SetTableData(TableData tableData)
        {
            if (tableData == null)
                throw new ArgumentNullException(nameof(tableData));

            // 헤더에 변경이 있는지 확인
            bool headersChanged = IsHeadersChanged(tableData.Columns);

            _tableData = tableData;

            // 헤더에 변경이 있는 경우에만 포트 재생성
            if (headersChanged)
            {
                RecreateOutputPorts(tableData.Columns);
            }
        }

        // 헤더 변경 여부 확인
        private bool IsHeadersChanged(IEnumerable<ColumnDefinition> newHeaders)
        {
            // 현재 헤더가 없으면 변경됨
            if (Headers == null || Headers.Count == 0)
                return true;

            var currentHeaders = Headers.ToList();
            var newHeaderList = newHeaders.ToList();

            // 헤더 수가 다르면 변경됨
            if (currentHeaders.Count != newHeaderList.Count)
                return true;

            // 헤더 이름과 타입 비교
            for (int i = 0; i < currentHeaders.Count; i++)
            {
                var current = currentHeaders[i];
                var newHeader = newHeaderList[i];

                // 이름이나 타입이 다르면 변경됨
                if (current.Name != newHeader.Name || 
                    current.TypeName != newHeader.TypeName)
                    return true;
            }

            // 모든 검사를 통과하면 변경 없음
            return false;
        }

        private void RecreateOutputPorts(IEnumerable<ColumnDefinition> headers)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            var newHeaders = headers.ToList();
            var existingPorts = OutputPorts.ToList();
            var portsToKeep = new Dictionary<string, (Type Type, IOutputPort Port)>();
            var connectionsToRestore = new List<(string PortName, IConnection Connection)>();

            // 기존과 동일한 헤더를 가진 포트는 유지
            foreach (var header in newHeaders)
            {
                var headerType = Type.GetType(header.TypeName);
                var existingPort = existingPorts.FirstOrDefault(p => p.Name == header.Name && p.DataType == headerType);
                
                if (existingPort != null)
                {
                    // 이름과 타입이 같은 포트가 있으면 유지
                    portsToKeep[existingPort.Name] = (headerType, existingPort);
                    
                    // 연결 정보 저장
                    foreach (var connection in existingPort.Connections)
                    {
                        connectionsToRestore.Add((existingPort.Name, connection));
                    }
                }
            }

            // 모든 포트 제거 (내부 구현상 필요)
            ClearPorts();

            // 새로운 포트들 추가
            foreach (var header in newHeaders)
            {
                var headerType = Type.GetType(header.TypeName);
                
                // 포트 다시 생성
                AddOutputPort(header.Name, headerType!);
            }
            
            // 연결 복원
            foreach (var (portName, connection) in connectionsToRestore)
            {
                var newPort = OutputPorts.FirstOrDefault(p => p.Name == portName);
                var targetPort = connection.Target;
                
                if (newPort != null && targetPort is IInputPort inputPort)
                {
                    try
                    {
                        // 새로운 포트와 타겟 포트 연결
                        newPort.Connect(inputPort);
                    }
                    catch (Exception ex)
                    {
                        // 연결 복원 중 오류가 발생한 경우 로깅
                        Console.WriteLine($"포트 연결 복원 중 오류 발생: {ex.Message}");
                    }
                }
            }
        }

        public void Reset()
        {
            IsLoopCompleted = false;
            _currentRowIndex = 0;
        }

        public Task<bool> ShouldContinueAsync(CancellationToken cancellationToken = default)
        {
            if (_currentRowIndex >= _tableData.Rows.Count)
            {
                IsLoopCompleted = true;
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, CancellationToken cancellationToken) {
            if (_currentRowIndex >= _tableData.Rows.Count)
            {
                _currentRowIndex = 0;
                yield return FlowOut;
            }

            // 현재 행의 데이터를 출력 포트로 전달
            var currentRow = _tableData.Rows[_currentRowIndex];
            for (int i = 0; i < OutputPorts.Count; i++)
            {
                if (i < currentRow.Values.Count && i < Headers.Count)
                {
                    var port  = OutputPorts[i];
                    var value = 123123;

                    // 리플렉션을 사용하여 포트의 Value 속성에 값 설정
                    var portType = port.GetType();
                    var valueProperty = portType.GetProperty("Value");
                    if (valueProperty != null)
                    {
                        valueProperty.SetValue(port, value);
                    }
                }

                yield return FlowOut;
            }

            _currentRowIndex++;
            yield return FlowOut;
        }

        public override string ToString()
        {
            return $"Excel Input Node ({Id})";
        }
    }
}
