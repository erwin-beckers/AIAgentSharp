using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace AIAgentSharp.Tests;

[TestClass]
public class ReasoningTreeTests
{
	[TestMethod]
	public void CreateRoot_SetsRootAndNodeProperties()
	{
		var tree = new ReasoningTree { Goal = "goal" };
		var root = tree.CreateRoot("root-thought", ThoughtType.Hypothesis);
		Assert.IsNotNull(root);
		Assert.IsNotNull(tree.RootId);
		Assert.AreEqual(root.NodeId, tree.RootId);
		Assert.AreEqual(0, root.Depth);
		Assert.AreEqual("root-thought", root.Thought);
		Assert.AreEqual(ThoughtType.Hypothesis, root.ThoughtType);
		Assert.AreEqual(root.NodeId, tree.GetPathToNode(root.NodeId)[0]);
	}

	[TestMethod]
	public void CreateRoot_WithExistingRoot_ThrowsInvalidOperationException()
	{
		var tree = new ReasoningTree();
		tree.CreateRoot("first");
		Assert.ThrowsException<InvalidOperationException>(() => tree.CreateRoot("second"));
	}

	[TestMethod]
	public void AddChild_AddsNodeAndLinksToParent()
	{
		var tree = new ReasoningTree();
		var root = tree.CreateRoot("root");
		var child = tree.AddChild(root.NodeId, "child", ThoughtType.Analysis);
		Assert.AreEqual(root.NodeId, child.ParentId);
		Assert.AreEqual(1, child.Depth);
		CollectionAssert.Contains(root.ChildIds, child.NodeId);
	}

	[TestMethod]
	public void AddChild_WithInvalidParentId_ThrowsArgumentException()
	{
		var tree = new ReasoningTree();
		Assert.ThrowsException<ArgumentException>(() => tree.AddChild("invalid-id", "child"));
	}

	[TestMethod]
	public void AddChild_AtMaxDepth_ThrowsInvalidOperationException()
	{
		var tree = new ReasoningTree { MaxDepth = 1 };
		var root = tree.CreateRoot("root");
		tree.AddChild(root.NodeId, "child");
		Assert.ThrowsException<ArgumentException>(() => tree.AddChild("child", "grandchild"));
	}

	[TestMethod]
	public void AddChild_AtMaxCapacity_ThrowsInvalidOperationException()
	{
		var tree = new ReasoningTree { MaxNodes = 2 };
		var root = tree.CreateRoot("root");
		tree.AddChild(root.NodeId, "child");
		Assert.ThrowsException<InvalidOperationException>(() => tree.AddChild(root.NodeId, "child2"));
	}

	[TestMethod]
	public void EvaluateNode_UpdatesScoreStateAndTimestamp()
	{
		var tree = new ReasoningTree();
		var root = tree.CreateRoot("root");
		tree.EvaluateNode(root.NodeId, 0.9);
		var updated = tree.Nodes[root.NodeId];
		Assert.AreEqual(0.9, updated.Score, 1e-9);
		Assert.AreEqual(ThoughtNodeState.Evaluated, updated.State);
		Assert.IsNotNull(updated.EvaluatedUtc);
	}

	[TestMethod]
	public void EvaluateNode_ClampsScoreToValidRange()
	{
		var tree = new ReasoningTree();
		var root = tree.CreateRoot("root");
		tree.EvaluateNode(root.NodeId, 1.5); // Above 1.0
		Assert.AreEqual(1.0, tree.Nodes[root.NodeId].Score, 1e-9);
		
		tree.EvaluateNode(root.NodeId, -0.5); // Below 0.0
		Assert.AreEqual(0.0, tree.Nodes[root.NodeId].Score, 1e-9);
	}

	[TestMethod]
	public void EvaluateNode_WithInvalidNodeId_ThrowsArgumentException()
	{
		var tree = new ReasoningTree();
		Assert.ThrowsException<ArgumentException>(() => tree.EvaluateNode("invalid-id", 0.5));
	}

	[TestMethod]
	public void PruneNode_MarksNodeAndDescendantsAsPruned()
	{
		var tree = new ReasoningTree();
		var root = tree.CreateRoot("root");
		var c1 = tree.AddChild(root.NodeId, "c1");
		var c2 = tree.AddChild(c1.NodeId, "c2");
		tree.PruneNode(c1.NodeId);
		Assert.AreEqual(ThoughtNodeState.Pruned, tree.Nodes[c1.NodeId].State);
		Assert.AreEqual(ThoughtNodeState.Pruned, tree.Nodes[c2.NodeId].State);
	}

	[TestMethod]
	public void PruneNode_WithInvalidNodeId_ThrowsArgumentException()
	{
		var tree = new ReasoningTree();
		Assert.ThrowsException<ArgumentException>(() => tree.PruneNode("invalid-id"));
	}

	[TestMethod]
	public void GetDescendants_ReturnsAllChildrenRecursively()
	{
		var tree = new ReasoningTree();
		var root = tree.CreateRoot("root");
		var c1 = tree.AddChild(root.NodeId, "c1");
		var c2 = tree.AddChild(c1.NodeId, "c2");
		var list = tree.GetDescendants(root.NodeId);
		CollectionAssert.AreEquivalent(new List<string> { c1.NodeId, c2.NodeId }, list);
	}

	[TestMethod]
	public void GetDescendants_WithInvalidNodeId_ReturnsEmptyList()
	{
		var tree = new ReasoningTree();
		var descendants = tree.GetDescendants("invalid-id");
		Assert.AreEqual(0, descendants.Count);
	}

	[TestMethod]
	public void GetPathToNode_ReturnsRootToNodePath()
	{
		var tree = new ReasoningTree();
		var root = tree.CreateRoot("root");
		var c1 = tree.AddChild(root.NodeId, "c1");
		var c2 = tree.AddChild(c1.NodeId, "c2");
		var path = tree.GetPathToNode(c2.NodeId);
		CollectionAssert.AreEqual(new List<string> { root.NodeId, c1.NodeId, c2.NodeId }, path);
	}

	[TestMethod]
	public void GetPathToNode_WithInvalidNodeId_ReturnsEmptyPath()
	{
		var tree = new ReasoningTree();
		var path = tree.GetPathToNode("invalid-id");
		Assert.AreEqual(0, path.Count);
	}

	[TestMethod]
	public void Complete_SetsBestPathAndMarksNodes()
	{
		var tree = new ReasoningTree();
		var root = tree.CreateRoot("root");
		var c1 = tree.AddChild(root.NodeId, "c1");
		var path = new List<string> { root.NodeId, c1.NodeId };
		tree.Complete(path);
		Assert.IsTrue(tree.IsComplete);
		Assert.AreEqual(ThoughtNodeState.BestPath, tree.Nodes[root.NodeId].State);
		Assert.AreEqual(ThoughtNodeState.BestPath, tree.Nodes[c1.NodeId].State);
	}

	[TestMethod]
	public void Complete_WithEmptyPath_StillMarksAsComplete()
	{
		var tree = new ReasoningTree();
		tree.CreateRoot("root");
		tree.Complete(new List<string>());
		Assert.IsTrue(tree.IsComplete);
	}

	[TestMethod]
	public void Complete_WithInvalidNodeIds_IgnoresInvalidIds()
	{
		var tree = new ReasoningTree();
		var root = tree.CreateRoot("root");
		var path = new List<string> { root.NodeId, "invalid-id" };
		tree.Complete(path);
		Assert.IsTrue(tree.IsComplete);
		Assert.AreEqual(ThoughtNodeState.BestPath, tree.Nodes[root.NodeId].State);
	}

	[TestMethod]
	public void Properties_ReturnCorrectValues()
	{
		var tree = new ReasoningTree
		{
			Goal = "test goal",
			MaxDepth = 5,
			MaxNodes = 100,
			ExplorationStrategy = ExplorationStrategy.BestFirst
		};

		Assert.AreEqual("test goal", tree.Goal);
		Assert.AreEqual(5, tree.MaxDepth);
		Assert.AreEqual(100, tree.MaxNodes);
		Assert.AreEqual(ExplorationStrategy.BestFirst, tree.ExplorationStrategy);
		Assert.AreEqual(0, tree.NodeCount);
		Assert.AreEqual(0, tree.CurrentMaxDepth);
		Assert.IsFalse(tree.IsComplete);
		Assert.IsFalse(tree.IsAtCapacity);
	}

	[TestMethod]
	public void NodeCount_UpdatesCorrectly()
	{
		var tree = new ReasoningTree();
		Assert.AreEqual(0, tree.NodeCount);
		
		var root = tree.CreateRoot("root");
		Assert.AreEqual(1, tree.NodeCount);
		
		tree.AddChild(root.NodeId, "child");
		Assert.AreEqual(2, tree.NodeCount);
	}

	[TestMethod]
	public void CurrentMaxDepth_UpdatesCorrectly()
	{
		var tree = new ReasoningTree();
		Assert.AreEqual(0, tree.CurrentMaxDepth);
		
		var root = tree.CreateRoot("root");
		Assert.AreEqual(0, tree.CurrentMaxDepth);
		
		tree.AddChild(root.NodeId, "child");
		Assert.AreEqual(1, tree.CurrentMaxDepth);
		
		var child = tree.AddChild(root.NodeId, "child2");
		tree.AddChild(child.NodeId, "grandchild");
		Assert.AreEqual(2, tree.CurrentMaxDepth);
	}

	[TestMethod]
	public void IsAtCapacity_ReturnsTrueWhenMaxNodesReached()
	{
		var tree = new ReasoningTree { MaxNodes = 2 };
		Assert.IsFalse(tree.IsAtCapacity);
		
		var root = tree.CreateRoot("root");
		Assert.IsFalse(tree.IsAtCapacity);
		
		tree.AddChild(root.NodeId, "child");
		Assert.IsTrue(tree.IsAtCapacity);
	}

	[TestMethod]
	public void ThoughtNode_Properties_WorkCorrectly()
	{
		var node = new ThoughtNode
		{
			NodeId = "test-id",
			ParentId = "parent-id",
			Depth = 2,
			Thought = "test thought",
			Score = 0.8,
			ThoughtType = ThoughtType.Analysis,
			State = ThoughtNodeState.Evaluated
		};

		Assert.AreEqual("test-id", node.NodeId);
		Assert.AreEqual("parent-id", node.ParentId);
		Assert.AreEqual(2, node.Depth);
		Assert.AreEqual("test thought", node.Thought);
		Assert.AreEqual(0.8, node.Score);
		Assert.AreEqual(ThoughtType.Analysis, node.ThoughtType);
		Assert.AreEqual(ThoughtNodeState.Evaluated, node.State);
		Assert.IsTrue(node.IsLeaf);
		Assert.IsFalse(node.IsRoot);
	}

	[TestMethod]
	public void ThoughtNode_IsLeaf_ReturnsTrueWhenNoChildren()
	{
		var node = new ThoughtNode();
		Assert.IsTrue(node.IsLeaf);
		
		node.ChildIds.Add("child");
		Assert.IsFalse(node.IsLeaf);
	}

	[TestMethod]
	public void ThoughtNode_IsRoot_ReturnsTrueWhenNoParent()
	{
		var node = new ThoughtNode();
		Assert.IsTrue(node.IsRoot);
		
		node.ParentId = "parent";
		Assert.IsFalse(node.IsRoot);
	}
}
