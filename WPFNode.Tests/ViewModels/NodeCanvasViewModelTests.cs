using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using WPFNode.Models;
using WPFNode.Services;
using WPFNode.ViewModels.Nodes;
using WPFNode.Interfaces;
using WPFNode.Attributes;
using Xunit;

namespace WPFNode.Tests.ViewModels
{
    public class NodeCanvasViewModelTests : IDisposable
    {
        private readonly NodeCanvasViewModel _viewModel;
        private readonly NodeCanvas _canvas;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly object _lockObject = new object();

        public NodeCanvasViewModelTests()
        {
            _synchronizationContext = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

            NodeServices.Initialize("TestPlugins");
            _canvas = new NodeCanvas();
            _viewModel = new NodeCanvasViewModel(_canvas);

            // 컬렉션 동기화 설정
            BindingOperations.EnableCollectionSynchronization(_viewModel.Nodes, _lockObject);
            BindingOperations.EnableCollectionSynchronization(_viewModel.Connections, _lockObject);
            BindingOperations.EnableCollectionSynchronization(_viewModel.Groups, _lockObject);
        }

        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(null);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [Fact]
        public async Task LoadFromJson_EmptyCanvas_Success()
        {
            // Arrange
            var json = @"{
                ""Nodes"": [],
                ""Connections"": [],
                ""Groups"": []
            }";

            // Act
            await _viewModel.LoadFromJsonAsync(json);

            // Assert
            Assert.Empty(_viewModel.Nodes);
            Assert.Empty(_viewModel.Connections);
            Assert.Empty(_viewModel.Groups);
        }

        [Fact]
        public async Task LoadFromJson_WithNodes_Success()
        {
            // Arrange
            var json = @"{
                ""Nodes"": [
                    {
                        ""Guid"": ""00000000-0000-0000-0000-000000000001"",
                        ""Type"": ""WPFNode.Tests.ViewModels.NodeCanvasViewModelTests+TestNode, WPFNode.Tests"",
                        ""Name"": ""TestNode"",
                        ""Category"": ""Basic"",
                        ""Description"": """",
                        ""X"": 100,
                        ""Y"": 100,
                        ""IsVisible"": true,
                        ""Properties"": []
                    }
                ],
                ""Connections"": [],
                ""Groups"": []
            }";

            // Act
            await _viewModel.LoadFromJsonAsync(json);

            // Assert
            Assert.Single(_viewModel.Nodes);
            var node = _viewModel.Nodes[0];
            Assert.Equal("TestNode", node.Name);
            Assert.Equal(new Point(100, 100), node.Position);
        }

        [Fact]
        public void Scale_PropertyChanged()
        {
            // Arrange
            var propertyChanged = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.Scale))
                {
                    propertyChanged = true;
                }
            };

            // Act
            _viewModel.Scale = 2.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(2.0, _viewModel.Scale);
        }

        [Fact]
        public void Offset_PropertyChanged()
        {
            // Arrange
            var propertyChanged = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.OffsetX) || e.PropertyName == nameof(_viewModel.OffsetY))
                {
                    propertyChanged = true;
                }
            };

            // Act
            _viewModel.OffsetX = 100;
            _viewModel.OffsetY = 100;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(100, _viewModel.OffsetX);
            Assert.Equal(100, _viewModel.OffsetY);
        }

        [Fact]
        public void Constructor_InitializesCollections()
        {
            // Assert
            Assert.NotNull(_viewModel.Nodes);
            Assert.NotNull(_viewModel.Connections);
            Assert.NotNull(_viewModel.Groups);
            Assert.Empty(_viewModel.Nodes);
            Assert.Empty(_viewModel.Connections);
            Assert.Empty(_viewModel.Groups);
        }

        [Fact]
        public void Constructor_InitializesCommands()
        {
            // Assert
            Assert.NotNull(_viewModel.AddNodeCommand);
            Assert.NotNull(_viewModel.RemoveNodeCommand);
            Assert.NotNull(_viewModel.ConnectCommand);
            Assert.NotNull(_viewModel.DisconnectCommand);
            Assert.NotNull(_viewModel.AddGroupCommand);
            Assert.NotNull(_viewModel.RemoveGroupCommand);
            Assert.NotNull(_viewModel.UndoCommand);
            Assert.NotNull(_viewModel.RedoCommand);
            Assert.NotNull(_viewModel.ExecuteCommand);
            Assert.NotNull(_viewModel.CopyCommand);
            Assert.NotNull(_viewModel.PasteCommand);
            Assert.NotNull(_viewModel.SaveCommand);
            Assert.NotNull(_viewModel.LoadCommand);
        }

        [Fact]
        public async Task ToJson_EmptyCanvas_Success()
        {
            // Act
            var json = await _viewModel.ToJsonAsync();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"Nodes\"", json);
            Assert.Contains("\"Connections\"", json);
            Assert.Contains("\"Groups\"", json);
        }

        [Fact]
        public async Task ToJson_WithNodes_Success()
        {
            // Arrange
            _canvas.CreateNode(typeof(TestNode), 100, 100);

            // Act
            var json = await _viewModel.ToJsonAsync();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"Nodes\"", json);
            Assert.Contains("\"Id\"", json);
            Assert.Contains("\"Type\"", json);
            Assert.Contains("\"X\"", json);
            Assert.Contains("\"Y\"", json);
        }

        [Fact]
        public async Task SaveAndLoad_RoundTrip_Success()
        {
            // Arrange
            var json = @"{
                ""Nodes"": [
                    {
                        ""Guid"": ""00000000-0000-0000-0000-000000000001"",
                        ""Type"": ""WPFNode.Tests.ViewModels.NodeCanvasViewModelTests+TestNode, WPFNode.Tests"",
                        ""Name"": ""TestNode"",
                        ""Category"": ""Basic"",
                        ""Description"": """",
                        ""X"": 100,
                        ""Y"": 100,
                        ""IsVisible"": true,
                        ""Properties"": []
                    }
                ],
                ""Connections"": [],
                ""Groups"": []
            }";

            // Act
            await _viewModel.LoadFromJsonAsync(json);
            var savedJson = await _viewModel.ToJsonAsync();
            await _viewModel.LoadFromJsonAsync(savedJson);

            // Assert
            Assert.Single(_viewModel.Nodes);
            var node = _viewModel.Nodes[0];
            Assert.Equal("TestNode", node.Name);
            Assert.Equal(new Point(100, 100), node.Position);
        }

        private class TestNode : NodeBase
        {
            private readonly InputPort<int> _input;
            private readonly OutputPort<int> _output;

            [JsonConstructor]
            public TestNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
            {
                Name = "TestNode";
                _input = CreateInputPort<int>("Input");
                _output = CreateOutputPort<int>("Output");
            }

            protected override Task ProcessAsync(CancellationToken cancellationToken = default)
            {
                _output.Value = _input.GetValueOrDefault();
                return Task.CompletedTask;
            }
        }

        private class NumberNode : NodeBase
        {
            private double _value;
            private readonly OutputPort<double> _output;

            [JsonConstructor]
            public NumberNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
            {
                Name = "Number";
                _output = CreateOutputPort<double>("Value");
            }

            public double Value
            {
                get => _value;
                set => _value = value;
            }

            protected override Task ProcessAsync(CancellationToken cancellationToken = default)
            {
                _output.Value = _value;
                return Task.CompletedTask;
            }
        }

        private class AdditionNode : NodeBase
        {
            private readonly InputPort<double> _inputA;
            private readonly InputPort<double> _inputB;
            private readonly OutputPort<double> _output;

            [JsonConstructor]
            public AdditionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
            {
                Name = "Addition";
                
                _inputA = CreateInputPort<double>("A");
                _inputB = CreateInputPort<double>("B");
                _output = CreateOutputPort<double>("Result");
            }

            protected override Task ProcessAsync(CancellationToken cancellationToken = default)
            {
                var a = _inputA.GetValueOrDefault();
                var b = _inputB.GetValueOrDefault();
                _output.Value = a + b;
                return Task.CompletedTask;
            }
        }

        private class MultiplicationNode : NodeBase
        {
            private readonly InputPort<double> _inputA;
            private readonly InputPort<double> _inputB;
            private readonly OutputPort<double> _output;

            [JsonConstructor]
            public MultiplicationNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
            {
                Name = "Multiplication";
                _inputA = CreateInputPort<double>("A");
                _inputB = CreateInputPort<double>("B");
                _output = CreateOutputPort<double>("Result");
            }

            protected override Task ProcessAsync(CancellationToken cancellationToken = default)
            {
                var a = _inputA.GetValueOrDefault();
                var b = _inputB.GetValueOrDefault();
                _output.Value = a * b;
                return Task.CompletedTask;
            }
        }

        [OutputNodeAttribute]
        private class OutputNode : NodeBase
        {
            private readonly InputPort<double> _input;

            [JsonConstructor]
            public OutputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
            {
                Name = "Output";
                _input = CreateInputPort<double>("Input");
            }

            protected override Task ProcessAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task ComplexNodeConfiguration_Success()
        {
            // Arrange
            var json = @"{
                ""Nodes"": [
                    {
                        ""Guid"": ""00000000-0000-0000-0000-000000000001"",
                        ""Type"": ""WPFNode.Tests.ViewModels.NodeCanvasViewModelTests+NumberNode, WPFNode.Tests"",
                        ""Name"": ""Number1"",
                        ""Category"": ""Math"",
                        ""Description"": """",
                        ""X"": 100,
                        ""Y"": 100,
                        ""IsVisible"": true,
                        ""Properties"": [],
                        ""InputPorts"": [],
                        ""OutputPorts"": [
                            {
                                ""Guid"": ""00000000-0000-0000-0000-000000000001:out[0]"",
                                ""Name"": ""Value"",
                                ""Type"": ""System.Double""
                            }
                        ]
                    },
                    {
                        ""Guid"": ""00000000-0000-0000-0000-000000000002"",
                        ""Type"": ""WPFNode.Tests.ViewModels.NodeCanvasViewModelTests+NumberNode, WPFNode.Tests"",
                        ""Name"": ""Number2"",
                        ""Category"": ""Math"",
                        ""Description"": """",
                        ""X"": 100,
                        ""Y"": 200,
                        ""IsVisible"": true,
                        ""Properties"": [],
                        ""InputPorts"": [],
                        ""OutputPorts"": [
                            {
                                ""Guid"": ""00000000-0000-0000-0000-000000000002:out[0]"",
                                ""Name"": ""Value"",
                                ""Type"": ""System.Double""
                            }
                        ]
                    },
                    {
                        ""Guid"": ""00000000-0000-0000-0000-000000000003"",
                        ""Type"": ""WPFNode.Tests.ViewModels.NodeCanvasViewModelTests+AdditionNode, WPFNode.Tests"",
                        ""Name"": ""Add"",
                        ""Category"": ""Math"",
                        ""Description"": """",
                        ""X"": 300,
                        ""Y"": 150,
                        ""IsVisible"": true,
                        ""Properties"": [],
                        ""InputPorts"": [
                            {
                                ""Guid"": ""00000000-0000-0000-0000-000000000003:in[0]"",
                                ""Name"": ""A"",
                                ""Type"": ""System.Double""
                            },
                            {
                                ""Guid"": ""00000000-0000-0000-0000-000000000003:in[1]"",
                                ""Name"": ""B"",
                                ""Type"": ""System.Double""
                            }
                        ],
                        ""OutputPorts"": [
                            {
                                ""Guid"": ""00000000-0000-0000-0000-000000000003:out[0]"",
                                ""Name"": ""Result"",
                                ""Type"": ""System.Double""
                            }
                        ]
                    }
                ],
                ""Connections"": [
                    {
                        ""Guid"": ""00000000-0000-0000-0000-000000000004"",
                        ""SourcePortId"": ""00000000-0000-0000-0000-000000000001:out[0]"",
                        ""TargetPortId"": ""00000000-0000-0000-0000-000000000003:in[0]""
                    },
                    {
                        ""Guid"": ""00000000-0000-0000-0000-000000000007"",
                        ""SourcePortId"": ""00000000-0000-0000-0000-000000000002:out[0]"",
                        ""TargetPortId"": ""00000000-0000-0000-0000-000000000003:in[1]""
                    }
                ],
                ""Groups"": []
            }";

            // Act
            await _viewModel.LoadFromJsonAsync(json);

            // Assert
            Assert.Equal(3, _viewModel.Nodes.Count);
            Assert.Equal(2, _viewModel.Connections.Count);
            Assert.Empty(_viewModel.Groups);

            var number1 = _viewModel.Nodes[0];
            var number2 = _viewModel.Nodes[1];
            var add = _viewModel.Nodes[2];

            Assert.Equal("Number", number1.Name);
            Assert.Equal("Number", number2.Name);
            Assert.Equal("Addition", add.Name);
        }

        [Fact]
        public async Task NodeExecution_DataFlow_Success()
        {
            // Arrange
            var number1 = _canvas.CreateNode(typeof(NumberNode), 100, 100) as NumberNode;
            var number2 = _canvas.CreateNode(typeof(NumberNode), 100, 200) as NumberNode;
            var add = _canvas.CreateNode(typeof(AdditionNode), 300, 150) as AdditionNode;
            var output = _canvas.CreateNode(typeof(OutputNode), 500, 150) as OutputNode;

            // 포트 연결
            _canvas.Connect(number1.OutputPorts[0], add.InputPorts[0]);
            _canvas.Connect(number2.OutputPorts[0], add.InputPorts[1]);
            _canvas.Connect(add.OutputPorts[0], output.InputPorts[0]);

            // 초기값 설정
            number1.Value = 5;
            number2.Value = 3;

            // Act
            await _canvas.ExecuteAsync();

            // Assert
            Assert.Equal(8.0, ((IInputPort<double>)output.InputPorts[0]).GetValueOrDefault());
        }

        [Fact]
        public async Task GroupedNodes_Execution_Success()
        {
            // Arrange
            var number1 = _canvas.CreateNode(typeof(NumberNode), 100, 100) as NumberNode;
            var number2 = _canvas.CreateNode(typeof(NumberNode), 100, 200) as NumberNode;
            var add     = _canvas.CreateNode(typeof(AdditionNode), 300, 150) as AdditionNode;
            var mult    = _canvas.CreateNode(typeof(MultiplicationNode), 500, 150) as MultiplicationNode;
            var output  = _canvas.CreateNode(typeof(OutputNode), 700, 150) as OutputNode;

            _canvas.Connect(number1.OutputPorts[0], add.InputPorts[0]);
            _canvas.Connect(number2.OutputPorts[0], add.InputPorts[1]);
            _canvas.Connect(add.OutputPorts[0], mult.InputPorts[0]);
            _canvas.Connect(number1.OutputPorts[0], mult.InputPorts[1]);
            _canvas.Connect(mult.OutputPorts[0], output.InputPorts[0]);

            var group = _canvas.CreateGroup(new NodeBase[] { number1, number2, add }, "Math Group");

            number1.Value = 5;
            number2.Value = 3;

            // Act
            try
            {
                await _canvas.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Execution failed: {ex.Message}");
            }

            // Assert
            Assert.Equal(8.0, add.OutputPorts[0].Value);
            Assert.Equal(40.0, ((IInputPort<double>)output.InputPorts[0]).GetValueOrDefault());
            Assert.Equal(3, group.Nodes.Count);
        }

        [Fact]
        public void CircularReference_Detection()
        {
            // Arrange
            var node1 = _canvas.CreateNode(typeof(TestNode), 100, 100) as TestNode;
            var node2 = _canvas.CreateNode(typeof(TestNode), 300, 100) as TestNode;
            var node3 = _canvas.CreateNode(typeof(TestNode), 200, 300) as TestNode;

            // 순환 참조 생성
            _canvas.Connect(node1.OutputPorts[0], node2.InputPorts[0]);
            _canvas.Connect(node2.OutputPorts[0], node3.InputPorts[0]);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                _canvas.Connect(node3.OutputPorts[0], node1.InputPorts[0]);
                await _canvas.ExecuteAsync();
            });
        }

        [Fact]
        public void FindNodeById_ExistingNode_ReturnsNode()
        {
            // Arrange
            var node = _canvas.CreateNode(typeof(TestNode), 100, 100);

            // Act
            var foundNode = _viewModel.FindNodeById(node.Guid);

            // Assert
            Assert.NotNull(foundNode);
            Assert.Equal(node.Guid, foundNode.Model.Guid);
        }

        [Fact]
        public void FindNodesByName_ExistingNodes_ReturnsMatchingNodes()
        {
            // Arrange
            var node1 = _canvas.CreateNode(typeof(TestNode), 100, 100) as NodeBase;
            var node2 = _canvas.CreateNode(typeof(TestNode), 200, 200) as NodeBase;
            node1.Name = "TestNode1";
            node2.Name = "TestNode2";

            // Act
            var foundNodes = _viewModel.FindNodesByName("TestNode").ToList();

            // Assert
            Assert.Equal(2, foundNodes.Count);
            Assert.Contains(foundNodes, n => n.Name == "TestNode1");
            Assert.Contains(foundNodes, n => n.Name == "TestNode2");
        }

        [Fact]
        public void FindNodesByType_ExistingNodes_ReturnsMatchingNodes()
        {
            // Arrange
            _canvas.CreateNode(typeof(TestNode), 100, 100);
            _canvas.CreateNode(typeof(NumberNode), 200, 200);
            _canvas.CreateNode(typeof(TestNode), 300, 300);

            // Act
            var foundNodes = _viewModel.FindNodesByType(typeof(TestNode)).ToList();

            // Assert
            Assert.Equal(2, foundNodes.Count);
            Assert.All(foundNodes, n => Assert.IsType<TestNode>(n.Model));
        }

        [Fact]
        public void GetNodePorts_ExistingNode_ReturnsPortInformation()
        {
            // Arrange
            var node = _canvas.CreateNode(typeof(TestNode), 100, 100);
            var nodeViewModel = _viewModel.FindNodeById(node.Guid);
            Assert.NotNull(nodeViewModel);

            // Act
            var (inputPorts, outputPorts) = nodeViewModel.GetPorts();

            // Assert
            Assert.Single(inputPorts);
            Assert.Single(outputPorts);
            Assert.Equal("Input", inputPorts.First().Name);
            Assert.Equal("Output", outputPorts.First().Name);
        }

        [Fact]
        public void GetNodeType_ExistingNode_ReturnsCorrectType()
        {
            // Arrange
            var node = _canvas.CreateNode(typeof(TestNode), 100, 100);
            var nodeViewModel = _viewModel.FindNodeById(node.Guid);
            Assert.NotNull(nodeViewModel);

            // Act
            var nodeType = nodeViewModel.GetNodeType();

            // Assert
            Assert.NotNull(nodeType);
            Assert.Equal(typeof(TestNode), nodeType);
        }
    }
} 