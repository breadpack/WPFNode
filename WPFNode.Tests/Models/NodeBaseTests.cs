using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using WPFNode.Core.Attributes;
using WPFNode.Core.Models;

namespace WPFNode.Tests.Models
{
    [TestClass]
    public class NodeBaseTests
    {
        [TestMethod]
        public async Task ProcessAsync_AdditionNode_CalculatesCorrectly()
        {
            // Arrange
            var node = new TestAdditionNode();
            node.Initialize();
            
            var inputA = node.InputPorts.First(p => p.Name == "A") as InputPort<double>;
            var inputB = node.InputPorts.First(p => p.Name == "B") as InputPort<double>;
            var output = node.OutputPorts.First(p => p.Name == "결과") as OutputPort<double>;
            
            // Act
            inputA.Value = 5;
            inputB.Value = 3;
            await node.ProcessAsync();
            
            // Assert
            Assert.AreEqual(8, output.Value);
        }

        [TestMethod]
        public void Initialize_RegistersPorts_CorrectlyInitialized()
        {
            // Arrange
            var node = new TestAdditionNode();
            
            // Act
            node.Initialize();
            
            // Assert
            Assert.AreEqual(2, node.InputPorts.Count);
            Assert.AreEqual(1, node.OutputPorts.Count);
            Assert.IsTrue(node.InputPorts.Any(p => p.Name == "A"));
            Assert.IsTrue(node.InputPorts.Any(p => p.Name == "B"));
            Assert.IsTrue(node.OutputPorts.Any(p => p.Name == "결과"));
        }

        [TestMethod]
        public void NodeMetadata_ReturnsCorrectValues()
        {
            // Arrange
            var node = new TestAdditionNode();
            
            // Assert
            Assert.AreEqual("테스트 덧셈", node.Name);
            Assert.AreEqual("테스트", node.Category);
            Assert.AreEqual("테스트용 덧셈 노드", node.Description);
        }

        [TestMethod]
        public async Task ProcessAsync_DefaultValues_UsesZero()
        {
            // Arrange
            var node = new TestAdditionNode();
            node.Initialize();
            
            var inputA = node.InputPorts.First(p => p.Name == "A") as InputPort<double>;
            var output = node.OutputPorts.First(p => p.Name == "결과") as OutputPort<double>;
            
            // Act
            inputA.Value = 5; // B는 설정하지 않음
            await node.ProcessAsync();
            
            // Assert
            Assert.AreEqual(5, output.Value); // B는 기본값 0을 사용
        }
    }

    [NodeName("테스트 덧셈")]
    [NodeCategory("테스트")]
    [NodeDescription("테스트용 덧셈 노드")]
    public class TestAdditionNode : NodeBase
    {
        private readonly InputPort<double> _inputA;
        private readonly InputPort<double> _inputB;
        private readonly OutputPort<double> _output;

        public TestAdditionNode()
        {
            _inputA = new InputPort<double>("A", this);
            _inputB = new InputPort<double>("B", this);
            _output = new OutputPort<double>("결과", this);
        }

        protected override void InitializePorts()
        {
            RegisterInputPort(_inputA);
            RegisterInputPort(_inputB);
            RegisterOutputPort(_output);
        }

        public override async Task ProcessAsync()
        {
            var a = _inputA.GetValueOrDefault(0.0);
            var b = _inputB.GetValueOrDefault(0.0);
            _output.Value = a + b;
            await Task.CompletedTask;
        }
    }
} 