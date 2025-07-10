using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using WPFNode.Models;
using WPFNode.Interfaces;

namespace WPFNode.Tests;

public class SingleElementToContainerTests
{
    private class TestNode : NodeBase
    {
        public TestNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }
        
        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, CancellationToken cancellationToken)
        {
            yield break;
        }
    }

    [Fact]
    public void IntToListInt_ShouldConvertSuccessfully()
    {
        // Arrange
        var canvas = NodeCanvas.Create();
        var sourceNode = new TestNode(canvas, Guid.NewGuid()) { Name = "Source" };
        var targetNode = new TestNode(canvas, Guid.NewGuid()) { Name = "Target" };
        
        var intOutput = new OutputPort<int>("IntOutput", sourceNode, 0);
        var listInput = new InputPort<List<int>>("ListInput", targetNode, 0);
        
        intOutput.Value = 42;
        
        // Act
        var canAccept = listInput.CanAcceptType(typeof(int));
        
        // Assert
        Assert.True(canAccept);
        
        if (canAccept)
        {
            var connection = canvas.Connect(intOutput, listInput);
            var result = listInput.GetValueOrDefault();
            
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(42, result[0]);
        }
    }

    [Fact]
    public void StringToStringArray_ShouldConvertSuccessfully()
    {
        // Arrange
        var canvas = NodeCanvas.Create();
        var sourceNode = new TestNode(canvas, Guid.NewGuid()) { Name = "Source" };
        var targetNode = new TestNode(canvas, Guid.NewGuid()) { Name = "Target" };
        
        var stringOutput = new OutputPort<string>("StringOutput", sourceNode, 0);
        var arrayInput = new InputPort<string[]>("ArrayInput", targetNode, 0);
        
        stringOutput.Value = "Hello";
        
        // Act
        var canAccept = arrayInput.CanAcceptType(typeof(string));
        
        // Assert
        Assert.True(canAccept);
        
        if (canAccept)
        {
            var connection = canvas.Connect(stringOutput, arrayInput);
            var result = arrayInput.GetValueOrDefault();
            
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Hello", result[0]);
        }
    }

    [Fact]
    public void IntToListString_ShouldConvertWithTypeConversion()
    {
        // Arrange
        var canvas = NodeCanvas.Create();
        var sourceNode = new TestNode(canvas, Guid.NewGuid()) { Name = "Source" };
        var targetNode = new TestNode(canvas, Guid.NewGuid()) { Name = "Target" };
        
        var intOutput = new OutputPort<int>("IntOutput", sourceNode, 0);
        var listInput = new InputPort<List<string>>("ListInput", targetNode, 0);
        
        intOutput.Value = 123;
        
        // Act
        var canAccept = listInput.CanAcceptType(typeof(int));
        
        // Assert
        Assert.True(canAccept);
        
        if (canAccept)
        {
            var connection = canvas.Connect(intOutput, listInput);
            var result = listInput.GetValueOrDefault();
            
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("123", result[0]);
        }
    }

    [Fact]
    public void HashSetConversion_ShouldWork()
    {
        // Arrange
        var canvas = NodeCanvas.Create();
        var sourceNode = new TestNode(canvas, Guid.NewGuid()) { Name = "Source" };
        var targetNode = new TestNode(canvas, Guid.NewGuid()) { Name = "Target" };
        
        var intOutput = new OutputPort<int>("IntOutput", sourceNode, 0);
        var hashSetInput = new InputPort<HashSet<int>>("HashSetInput", targetNode, 0);
        
        intOutput.Value = 42;
        
        // Act
        var canAccept = hashSetInput.CanAcceptType(typeof(int));
        
        // Assert
        Assert.True(canAccept);
        
        if (canAccept)
        {
            var connection = canvas.Connect(intOutput, hashSetInput);
            var result = hashSetInput.GetValueOrDefault();
            
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(42, result);
        }
    }
} 