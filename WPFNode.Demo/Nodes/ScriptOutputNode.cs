using System;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using WPFNode.Models;
using WPFNode.Attributes;
using WPFNode.Interfaces;

namespace WPFNode.Demo.Nodes
{
    [NodeName("Script Output")]
    [NodeCategory("Data")]
    [NodeDescription("게임 데이터 스크립트를 출력하는 노드")]
    [OutputNode]
    public class ScriptOutputNode : NodeBase
    {
        private string _identifier;
        private string _scriptType;

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

        public string ScriptType
        {
            get => _scriptType;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("스크립트 타입은 비어있을 수 없습니다.", nameof(value));
                _scriptType = value;
            }
        }
        

        public InputPort<int> _AttackPort { get; set; }

        public InputPort<int> _HPPort { get; set; }

        public InputPort<int> _LevelPort { get; set; }

        public InputPort<string> _NamePort { get; set; }

        public InputPort<string> _IdPort { get; set; }

        public ScriptOutputNode(INodeCanvas canvas, Guid guid, string identifier, string scriptType) : base(canvas, guid)
        {
            Identifier = identifier;
            ScriptType = scriptType;
            
            // 기본 입력 포트 설정
            _IdPort     = CreateInputPort<string>("ID");
            _NamePort   = CreateInputPort<string>("Name");
            _LevelPort  = CreateInputPort<int>("Level");
            _HPPort     = CreateInputPort<int>("HP");
            _AttackPort = CreateInputPort<int>("Attack");
        }

        protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            var id = GetPortValue<string>("ID");
            var name = GetPortValue<string>("Name");
            var level = GetPortValue<int>("Level");
            var hp = GetPortValue<int>("HP");
            var attack = GetPortValue<int>("Attack");

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
            {
                Console.WriteLine("필수 입력값이 누락되었습니다.");
                return;
            }

            // 여기서는 간단히 콘솔에 출력하는 것으로 테스트
            Console.WriteLine($"스크립트 출력 ({ScriptType}) - ID: {id}, Name: {name}, Level: {level}, HP: {hp}, Attack: {attack}");
            await Task.CompletedTask;
        }

        private T? GetPortValue<T>(string portName)
        {
            var port = InputPorts.FirstOrDefault(p => p.Name == portName);
            if (port is InputPort<T> inputPort)
            {
                return inputPort.GetValueOrDefault();
            }
            return default;
        }

        public class SaveData
        {
            public string Identifier { get; set; }
            public string ScriptType { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        public SaveData GetSaveData()
        {
            return new SaveData
            {
                Identifier = Identifier,
                ScriptType = ScriptType,
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
            ScriptType = saveData.ScriptType;
        }

        public override string ToString()
        {
            return $"Script Output Node ({Identifier} - {ScriptType})";
        }
    }
} 