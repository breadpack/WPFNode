using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using WPFNode.Abstractions;
using WPFNode.Core.Models;

namespace WPFNode.Tests.Models
{
    [TestClass]
    public class NodeCanvasTests
    {
        private NodeCanvas _canvas = null!;
        private TestAdditionNode _node1 = null!;
        private TestAdditionNode _node2 = null!;

        [TestInitialize]
        public void Setup()
        {
            _canvas = new NodeCanvas();
            _node1 = new TestAdditionNode();
            _node2 = new TestAdditionNode();
            
            _node1.Initialize();
            _node2.Initialize();
        }

        [TestMethod]
        public void AddNode_ValidNode_AddsToCanvas()
        {
            // Act
            _canvas.AddNode(_node1);
            
            // Assert
            Assert.AreEqual(1, _canvas.Nodes.Count);
            Assert.AreEqual(_node1, _canvas.Nodes.First());
        }

        [TestMethod]
        public void RemoveNode_ExistingNode_RemovesFromCanvas()
        {
            // Arrange
            _canvas.AddNode(_node1);
            
            // Act
            _canvas.RemoveNode(_node1);
            
            // Assert
            Assert.AreEqual(0, _canvas.Nodes.Count);
        }

        [TestMethod]
        public void Connect_ValidPorts_AddsConnection()
        {
            // Arrange
            _canvas.AddNode(_node1);
            _canvas.AddNode(_node2);
            
            var sourcePort = _node1.OutputPorts.First(p => p.Name == "결과");
            var targetPort = _node2.InputPorts.First(p => p.Name == "A");
            
            // Act
            var connection = _canvas.Connect(sourcePort, targetPort);
            
            // Assert
            Assert.IsNotNull(connection);
            Assert.AreEqual(1, _canvas.Connections.Count);
            Assert.AreEqual(connection, _canvas.Connections.First());
        }

        [TestMethod]
        public void Connect_DuplicateConnection_ReturnsNull()
        {
            // Arrange
            _canvas.AddNode(_node1);
            _canvas.AddNode(_node2);
            
            var sourcePort = _node1.OutputPorts.First(p => p.Name == "결과");
            var targetPort = _node2.InputPorts.First(p => p.Name == "A");
            
            _canvas.Connect(sourcePort, targetPort);
            
            // Act
            var duplicateConnection = _canvas.Connect(sourcePort, targetPort);
            
            // Assert
            Assert.IsNull(duplicateConnection);
            Assert.AreEqual(1, _canvas.Connections.Count);
        }

        [TestMethod]
        public void Disconnect_ExistingConnection_RemovesConnection()
        {
            // Arrange
            _canvas.AddNode(_node1);
            _canvas.AddNode(_node2);
            var connection = _canvas.Connect(_node1.OutputPorts.First(), _node2.InputPorts.First());
            
            // Act
            _canvas.Disconnect(connection!);
            
            // Assert
            Assert.AreEqual(0, _canvas.Connections.Count);
        }

        [TestMethod]
        public void RemoveNode_WithConnections_RemovesConnections()
        {
            // Arrange
            _canvas.AddNode(_node1);
            _canvas.AddNode(_node2);
            _canvas.Connect(_node1.OutputPorts.First(), _node2.InputPorts.First());
            
            // Act
            _canvas.RemoveNode(_node1);
            
            // Assert
            Assert.AreEqual(1, _canvas.Nodes.Count);
            Assert.AreEqual(0, _canvas.Connections.Count);
        }

        [TestMethod]
        public void AddNode_NullNode_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => _canvas.AddNode(null!));
        }

        [TestMethod]
        public void RemoveNode_NullNode_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => _canvas.RemoveNode(null!));
        }

        [TestMethod]
        public void Connect_NullPorts_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => _canvas.Connect(null!, _node2.InputPorts.First()));
            Assert.ThrowsException<ArgumentNullException>(() => _canvas.Connect(_node1.OutputPorts.First(), null!));
        }

        [TestMethod]
        public void Disconnect_NullConnection_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => _canvas.Disconnect(null!));
        }
    }
} 