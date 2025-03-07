using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WPFNode.Demo.Models;
using WPFNode.Demo.Models.Serialization;
using WPFNode.Demo.Nodes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Demo.Services
{
    public class MigrationService
    {
        private readonly INodePluginService _pluginService;
        private readonly string _saveFolderPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public MigrationService(INodePluginService pluginService)
        {
            _pluginService = pluginService;
            _saveFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WPFNode",
                "MigrationPlans"
            );

            // 저장 디렉토리가 없으면 생성
            if (!Directory.Exists(_saveFolderPath))
            {
                Directory.CreateDirectory(_saveFolderPath);
            }

            // JSON 설정
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null
            };
            _jsonOptions.Converters.Add(new NodeCanvasMigrationConverter());
        }

        /// <summary>
        /// 테이블 이름에 해당하는 마이그레이션 플랜 파일 경로를 반환합니다.
        /// </summary>
        public string GetMigrationPlanPath(string tableName)
        {
            return Path.Combine(_saveFolderPath, $"{tableName}.json");
        }

        /// <summary>
        /// 테이블 이름에 해당하는 마이그레이션 플랜이 존재하는지 확인합니다.
        /// </summary>
        public bool MigrationPlanExists(string tableName)
        {
            return File.Exists(GetMigrationPlanPath(tableName));
        }

        /// <summary>
        /// 마이그레이션 플랜을 저장합니다.
        /// </summary>
        public void SaveMigrationPlan(NodeCanvas canvas, string tableName)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("테이블 이름은 비어있을 수 없습니다.", nameof(tableName));

            var filePath = GetMigrationPlanPath(tableName);
            var json = canvas.ToJson();
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 마이그레이션 플랜을 로드합니다. 저장된 내역이 없으면 새로 생성합니다.
        /// </summary>
        public NodeCanvas LoadMigrationPlan(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) throw new ArgumentException("테이블 이름은 비어있을 수 없습니다.", nameof(tableName));

            var filePath = GetMigrationPlanPath(tableName);
            NodeCanvas canvas;
            
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                canvas = NodeCanvas.FromJson(json);
                
                // ExcelInputNode가 직렬화/역직렬화 과정에서 포트를 재생성하므로
                // 여기서는 추가 설정이 필요 없음
                return canvas;
            }

            // 저장된 내역이 없으면 새로운 마이그레이션 플랜 생성
            canvas = new NodeCanvas();
                
            // ExcelInputNode 생성
            var nodeType = typeof(ExcelInputNode);
            var node     = canvas.CreateNode(nodeType, 100, 100);
            if (node is ExcelInputNode excelNode)
            {
                excelNode.Id = tableName;
                    
                // 테이블 데이터 설정
                var tableData = GetTableDataForName(tableName);
                if (tableData != null)
                {
                    excelNode.SetTableData(tableData);
                }
            }

            return canvas;
        }
        
        /// <summary>
        /// 테이블 이름에 해당하는 테이블 데이터를 가져옵니다.
        /// </summary>
        private TableData GetTableDataForName(string tableName)
        {
            // 테이블 이름에 따라 적절한 샘플 데이터 반환
            if (tableName.Equals("Employees", StringComparison.OrdinalIgnoreCase))
            {
                return TableDataGenerator.CreateSampleEmployeeData();
            }
            else if (tableName.Equals("Products", StringComparison.OrdinalIgnoreCase))
            {
                return TableDataGenerator.CreateSampleProductData();
            }
            else if (tableName.Equals("MultipleEmployees", StringComparison.OrdinalIgnoreCase)) {
                return TableDataGenerator.CreateMultipleEmployeeData();
            }
            else
            {
                // 기본값으로 직원 데이터 반환
                var tableData = TableDataGenerator.CreateSampleEmployeeData();
                tableData.TableName = tableName; // 테이블 이름 변경
                return tableData;
            }
        }

        /// <summary>
        /// 마이그레이션 플랜을 로드하거나 생성하고, 필요한 노드들이 있는지 확인합니다.
        /// </summary>
        private (NodeCanvas canvas, ExcelInputNode excelInputNode, TableOutputNode tableOutputNode) LoadOrCreateMigrationPlanWithNodes(TableData sourceData)
        {
            if (sourceData == null) throw new ArgumentNullException(nameof(sourceData));

            // 테이블 이름에 해당하는 마이그레이션 플랜 로드
            var canvas = LoadMigrationPlan(sourceData.TableName);
            
            // ExcelInputNode 찾기
            var excelInputNode = canvas.Nodes.OfType<ExcelInputNode>().FirstOrDefault();
            if (excelInputNode == null)
            {
                throw new InvalidOperationException($"마이그레이션 플랜에 ExcelInputNode가 없습니다.");
            }

            // TableOutputNode 찾기
            var tableOutputNode = canvas.Nodes.OfType<TableOutputNode>().FirstOrDefault();
            if (tableOutputNode == null)
            {
                // 새로 생성된 마이그레이션 플랜인 경우 TableOutputNode 생성
                var nodeType = typeof(TableOutputNode);
                var node = canvas.CreateNode(nodeType, 300, 100);
                tableOutputNode = node as TableOutputNode;
                
                if (tableOutputNode == null)
                {
                    throw new InvalidOperationException($"TableOutputNode를 생성할 수 없습니다.");
                }
                
                // ExcelInputNode와 TableOutputNode 연결
                var excelOutputPort = excelInputNode.OutputPorts.FirstOrDefault();
                var tableInputPort = tableOutputNode.InputPorts.FirstOrDefault();
                
                if (excelOutputPort != null && tableInputPort != null)
                {
                    canvas.Connect(excelOutputPort, tableInputPort);
                }
            }

            // 실제 마이그레이션에 사용할 소스 데이터 설정
            // (LoadMigrationPlan에서 설정한 것은 포트 초기화용이었음)
            excelInputNode.SetTableData(sourceData);
            
            return (canvas, excelInputNode, tableOutputNode);
        }

        /// <summary>
        /// 테이블 데이터를 마이그레이션하고 JSON 결과를 반환합니다.
        /// </summary>
        public async Task<string> MigrateTableDataToJsonAsync(TableData sourceData)
        {
            if (sourceData == null) throw new ArgumentNullException(nameof(sourceData));

            var (canvas, _, tableOutputNode) = LoadOrCreateMigrationPlanWithNodes(sourceData);

            // 마이그레이션 실행
            var parameters = new Dictionary<Guid, object>();
            await canvas.ExecuteAsync(parameters);

            // JSON 결과 반환
            return tableOutputNode.ResultJson;
        }
        
        /// <summary>
        /// 테이블 데이터를 마이그레이션하고 결과 객체를 반환합니다.
        /// </summary>
        public async Task<object> MigrateTableDataToObjectAsync(TableData sourceData)
        {
            if (sourceData == null) throw new ArgumentNullException(nameof(sourceData));

            var (canvas, _, tableOutputNode) = LoadOrCreateMigrationPlanWithNodes(sourceData);

            // 마이그레이션 실행
            var parameters = new Dictionary<Guid, object>();
            await canvas.ExecuteAsync(parameters);

            // 결과 객체 반환
            return tableOutputNode.Result ?? 
                   throw new InvalidOperationException("마이그레이션 결과가 없습니다.");
        }
    }
} 