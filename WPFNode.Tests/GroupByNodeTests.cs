using System;
using System; // Added for HashCode
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPFNode.Models;
using WPFNode.Plugins.Basic.Constants; // Ensure StartNode is here
using WPFNode.Plugins.Basic.Object; // For GroupByNode
using WPFNode.Tests.Helpers; // For TrackingNode
using Xunit;
// Add potentially missing using for ConstantNode if not covered by others
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Flow;


namespace WPFNode.Tests;

// Helper class for test data
public class GroupByTestData
{
    public string Category { get; set; }
    public int Value { get; set; }
    public bool Flag { get; set; }

    public GroupByTestData(string category, int value, bool flag = true)
    {
        Category = category;
        Value = value;
        Flag = flag;
    }

    // Override Equals and GetHashCode if comparing instances directly in asserts
    public override bool Equals(object? obj) => obj is GroupByTestData other && Category == other.Category && Value == other.Value && Flag == other.Flag;
    public override int GetHashCode() => HashCode.Combine(Category, Value, Flag);
}


public class GroupByNodeTests
{
    [Fact]
    public async Task GroupByNode_GroupsByStringProperty_LoopsCorrectly()
    {
        // Arrange
        var canvas = NodeCanvas.Create();

        // Create nodes
        var startNode = canvas.CreateNode<StartNode>(0, 0);
        var groupByNode = canvas.CreateNode<GroupByNode>(100, 50);
        var keyTracker = canvas.CreateNode<TrackingNode<object>>(200, 0); // Key type is dynamic
        var itemsTracker = canvas.CreateNode<TrackingNode<IList>>(200, 100); // Items type is List<T>, use IList
        var completeTracker = canvas.CreateNode<TrackingNode<int>>(200, 200); // Track completion

        // Prepare test data
        var testData = new List<GroupByTestData>
        {
            new("A", 1),
            new("B", 10),
            new("A", 2),
            new("C", 100),
            new("B", 20),
            new("A", 3)
        };

        // Configure GroupByNode
        groupByNode.ItemType.Value = typeof(GroupByTestData);
        groupByNode.SelectedKeyMember.Value = nameof(GroupByTestData.Category); // Group by Category property
        groupByNode.InputCollection.Value = testData;

        // Connect nodes
        startNode.FlowOut.Connect(groupByNode.FlowIn);
        groupByNode.LoopBody?.Connect(keyTracker.FlowIn); // Connect LoopBody to both trackers
        groupByNode.LoopBody?.Connect(itemsTracker.FlowIn);
        groupByNode.FlowComplete?.Connect(completeTracker.FlowIn);

        // Connect dynamic outputs to trackers
        // Find ports using the correct OutputPorts collection from NodeBase
        var currentKeyPort = groupByNode.OutputPorts.FirstOrDefault(p => p.Name == "Current Key");
        var currentItemsPort = groupByNode.OutputPorts.FirstOrDefault(p => p.Name == "Current Items");

        Assert.NotNull(currentKeyPort); // Ensure port was found
        Assert.NotNull(currentItemsPort); // Ensure port was found

        // Cast is not needed as Connect should accept IInputPort which TrackingNode.InputValue implements
        currentKeyPort.Connect(keyTracker.InputValue);
        currentItemsPort.Connect(itemsTracker.InputValue);


        // Setup completion tracker input (optional, just to confirm flow)
        // Ensure ConstantNode<T> is accessible
        var completeValue = canvas.CreateNode<ConstantNode<int>>(150, 250);
        completeValue.Value.Value = 1; // Assuming ConstantNode has Value property of type NodeProperty<T>
        completeValue.Result.Connect(completeTracker.InputValue); // Assuming ConstantNode has Result OutputPort


        // Act
        await canvas.ExecuteAsync();

        // Assert
        // 1. Check number of loops (should match number of unique categories)
        Assert.Equal(3, keyTracker.ReceivedValues.Count);
        Assert.Equal(3, itemsTracker.ReceivedValues.Count);

        // 2. Check completion tracker
        Assert.Single(completeTracker.ReceivedValues);
        Assert.Equal(1, completeTracker.ReceivedValues[0]);

        // 3. Verify the content of each group
        var receivedGroups = new Dictionary<object, List<GroupByTestData>>();
        for (int i = 0; i < keyTracker.ReceivedValues.Count; i++)
        {
            var key = keyTracker.ReceivedValues[i];
            var items = itemsTracker.ReceivedValues[i].Cast<GroupByTestData>().ToList(); // Cast IList back to List<TestData>
            receivedGroups[key] = items;
        }

        // Check group "A"
        Assert.True(receivedGroups.ContainsKey("A"));
        var groupA = receivedGroups["A"];
        Assert.Equal(3, groupA.Count);
        Assert.Contains(groupA, item => item.Value == 1);
        Assert.Contains(groupA, item => item.Value == 2);
        Assert.Contains(groupA, item => item.Value == 3);

        // Check group "B"
        Assert.True(receivedGroups.ContainsKey("B"));
        var groupB = receivedGroups["B"];
        Assert.Equal(2, groupB.Count);
        Assert.Contains(groupB, item => item.Value == 10);
        Assert.Contains(groupB, item => item.Value == 20);

        // Check group "C"
        Assert.True(receivedGroups.ContainsKey("C"));
        var groupC = receivedGroups["C"];
        Assert.Single(groupC);
        Assert.Contains(groupC, item => item.Value == 100);
    }

    // TODO: Add more tests:
    // - Grouping by an integer property
    // - Grouping by a boolean property
    // - Handling empty input collection
    // - Handling collection with null items
    // - Handling invalid KeyMember name
    // - Handling null keys (if applicable, e.g., grouping by a nullable property)
}
