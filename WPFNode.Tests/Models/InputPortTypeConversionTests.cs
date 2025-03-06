using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
using WPFNode.Models;
using WPFNode.Interfaces;
using Xunit;

namespace WPFNode.Tests.Models;

public class InputPortTypeConversionTests
{
    private class TestNode : NodeBase
    {
        public TestNode() : base(new NodeCanvas(), Guid.NewGuid()) { }
        
        protected override Task ProcessAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
    
    [Fact]
    public void CanAcceptType_StringTarget_AcceptsAllTypes()
    {
        // Arrange
        var node = new TestNode();
        var port = new InputPort<string>("Test", node, 0);
        
        // Act & Assert
        Assert.True(port.CanAcceptType(typeof(int)));
        Assert.True(port.CanAcceptType(typeof(double)));
        Assert.True(port.CanAcceptType(typeof(DateTime)));
        Assert.True(port.CanAcceptType(typeof(Guid)));
    }
    
    [Fact]
    public void CanAcceptType_NumericTarget_AcceptsCompatibleNumericTypes()
    {
        // Arrange
        var node = new TestNode();
        var intPort = new InputPort<int>("Int", node, 0);
        var doublePort = new InputPort<double>("Double", node, 1);
        
        // Act & Assert - Integer port
        Assert.True(intPort.CanAcceptType(typeof(byte)));
        Assert.True(intPort.CanAcceptType(typeof(short)));
        
        // Act & Assert - Double port
        Assert.True(doublePort.CanAcceptType(typeof(int)));
        Assert.True(doublePort.CanAcceptType(typeof(float)));
    }
    
    [Fact]
    public void CanAcceptType_ParseableTarget_AcceptsStringSource()
    {
        // Arrange
        var node = new TestNode();
        var guidPort = new InputPort<Guid>("Guid", node, 0);
        var dateTimePort = new InputPort<DateTime>("DateTime", node, 1);
        var intPort = new InputPort<int>("Int", node, 2);
        
        // Act & Assert
        Assert.True(guidPort.CanAcceptType(typeof(string)));
        Assert.True(dateTimePort.CanAcceptType(typeof(string)));
        Assert.True(intPort.CanAcceptType(typeof(string)));
    }
    
    [Fact]
    public void CanAcceptType_TypeConverterTarget_AcceptsConvertibleSources()
    {
        // Arrange
        var node = new TestNode();
        var colorPort = new InputPort<Color>("Color", node, 0);
        
        // Act & Assert - Color can be converted from string
        Assert.True(colorPort.CanAcceptType(typeof(string)));
    }
    
    [Fact]
    public void RegisterConverter_CustomTypeConversion_WorksAsExpected()
    {
        // Arrange
        var node = new TestNode();
        var port = new InputPort<int>("Test", node, 0);
        port.RegisterConverter<string>(s => int.Parse(s));
        
        // Act & Assert
        Assert.True(port.CanAcceptType(typeof(string)));
    }
}
