using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using WPFNode.Abstractions;
using WPFNode.Core.Models;
using System.Threading.Tasks;

namespace WPFNode.Tests.Models
{
    [TestClass]
    public class ConnectionTests
    {
        private TestNode _sourceNode;
        private TestNode _targetNode;
        private IOutputPort _sourcePort;
        private IInputPort _targetPort;

        [TestInitialize]
        public void Setup()
        {
            _sourceNode = new TestNode();
            _targetNode = new TestNode();
            _sourceNode.Initialize();
            _targetNode.Initialize();
            _sourcePort = _sourceNode.DoubleOutput;
            _targetPort = _targetNode.DoubleInput;
        }

        [TestMethod]
        public void Constructor_ValidPorts_CreatesConnection()
        {
            // Arrange & Act
            var connection = new Connection(_sourcePort, _targetPort);
            
            // Assert
            Assert.IsNotNull(connection);
            Assert.AreEqual(_sourcePort, connection.Source);
            Assert.AreEqual(_targetPort, connection.Target);
            Assert.AreNotEqual(Guid.Empty, connection.Id);
        }

        [TestMethod]
        public void Constructor_NullSource_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new Connection(null, _targetPort));
        }

        [TestMethod]
        public void Constructor_NullTarget_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new Connection(_sourcePort, null));
        }

        [TestMethod]
        public void IsValid_CompatibleTypes_ReturnsTrue()
        {
            // Arrange
            var connection = new Connection(_sourcePort, _targetPort);
            
            // Act & Assert
            Assert.IsTrue(connection.IsValid);
        }

        [TestMethod]
        public void IsValid_IncompatibleTypes_ReturnsFalse()
        {
            // Arrange
            var connection = new Connection(_sourcePort, _targetNode.StringInput);
            
            // Act & Assert
            Assert.IsFalse(connection.IsValid);
        }

        [TestMethod]
        public void IsEnabled_DefaultTrue_CanBeDisabled()
        {
            // Arrange
            var connection = new Connection(_sourcePort, _targetPort);
            
            // Assert
            Assert.IsTrue(connection.IsEnabled);
            
            // Act
            connection.IsEnabled = false;
            
            // Assert
            Assert.IsFalse(connection.IsEnabled);
        }

        [TestMethod]
        public async Task ValuePropagation_ConnectedPorts_PropagatesValue()
        {
            // Arrange
            var connection = new Connection(_sourcePort, _targetPort);
            _sourceNode.DoubleOutput.Value = 42.0;
            
            // Act
            await _sourceNode.ProcessAsync();
            await _targetNode.ProcessAsync();

            // Assert
            Assert.AreEqual(42.0, _targetNode.DoubleOutput.Value);
        }
    }
} 