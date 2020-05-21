import java.util.*;

public class KruskalsAlgorithm {
	private class Node 
	{ 
		public int Index; 
		public Node(int index) {
			this.Index = index;
		}
	}
	
	public class Edge {
		private Node nodeA;
		private Node nodeB;
		private int weight;
		
		public Edge(Node nodeA, Node nodeB, int weight) {
			this.nodeA = nodeA;
			this.nodeB = nodeB;
			this.weight = weight;
		}
	}
	
	private Node[] nodes;
	private ArrayList<Edge> edges = new ArrayList<Edge>();
	
	// Creates all nodes and edges, and gives them random weights. 
	public KruskalsAlgorithm(int n) {
		nodes = new Node[n];
		for (int i = 0; i < nodes.length; i++) {
			nodes[i] = new Node(i);
		}
		Random random = new Random();
		for (int i = 0; i < nodes.length; i++) {
			for (int j = i + 1; j < nodes.length; j++) {
				Edge edge = new Edge(nodes[i], nodes[j], random.nextInt(n * (n / 3)));
				edges.add(edge);
			}
		}
		
		edges.sort((Edge e, Edge f) -> {return e.weight - f.weight;});
	}
	
	public Edge[] Solve() {
		// A spanning tree is always n - 1 long. 
		Edge[] solution = new Edge[nodes.length - 1];
		
		// Initializing the different trees. 
		int[] trees = new int[nodes.length];
		for(int i = 0; i < trees.length; i++) {
			trees[i] = i;
		}
		
		// The provided list is sorted, thus can be iterated through linearly. 
		int j = 0;
		for (int i = 0; i < nodes.length - 1; i++) {
			for (; j < edges.size(); j++) {				
				Edge current = edges.get(j);
				int a = trees[current.nodeA.Index];
				int b = trees[current.nodeB.Index];
				
				// if the selected edge is from a different tree, they are merged.
				if (a != b) {
					for (int k = 0; k < trees.length; k++) {
						if (trees[k] == b) {
							trees[k] = a;
						}
					}
					
					solution[i] = current;
					break;
				}

			}
		}
		return solution;
	}
	
	public static void main(String[] args) {
		KruskalsAlgorithm krus = new KruskalsAlgorithm(16);
		Edge[] a = krus.Solve();
	}
}
