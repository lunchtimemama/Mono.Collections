// 
// ContiguousSplayedRadixTree.cs
//  
// Author:
//       Scott Peterson <lunchtimemama@gmail.com>
// 
// Copyright (c) 2009 Scott Peterson
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using Mono.Collections.Internal;

namespace Mono.Collections.DataStructures
{
	// Genus: Trie (a.k.a. Prefix Tree)
	//
	// Species: Radix Tree (a.k.a. Crit Bit Trie a.k.a. Patricia Trie)
	//
	// Unique Features: Child nodes are stored in a binary tree
	//
	// Capabilities: Value lookup by string key
	//
	// Pros: Fast lookup, low memory overhead, good locality of reference
	//
	// Cons: Readonly, 65,536 maximum capacity, 2,147,483,648 maximum structure size
	//
	// Biography:
	// The contiguous splayed radix tree is a memory-efficient data structure to store values
	// by unique key strings. It is based on a radix tree (also know as a crit bit trie) which
	// is a kind of trie. The radix tree is stored in a single char array, hense "contigugous."
	// By storing the entire structure in a single array, we improve the locality of memory and
	// therefore increase the likelihood of CPU cache hits. As with all tries, the lookup time
	// for a key is constant with respect to the number of elements in the structure. Tries
	// almost always out-perform hash tables for lookup when the trie fits into system memory.
	// The radix tree is also highly compact. The amount of memory consumed by the entire tree
	// is often less than the amount of memory required to contain all of the constituent keys.
	// The children of each node in the tree are stored in balanced binary trees, hense
	// "splayed." This allows for O(log n) search time among the children of a given node. The
	// added complexity of a binary search is only warented if the average number of children
	// per node is high (4 or higher). When the system constructs the radix tree, it analyses
	// the structure of the tree and chooses between ContiguousSplayedRadixTree and
	// ContiguousRadixTree (a similar structure, but with an ordered list of children as
	// opposed to a binary tree) as appropriate. The use of a char array limits the number of
	// total items the tree can store to 65,536 (the number of values representable with 16
	// bits). To store more than 65,536 items, use a pointer-based trie.
	
	public sealed class ContiguousSplayedRadixTree<T>
	{
		// The children of a node in the prefix tree are
		// arranged into a balanced binary tree which
		// branches first to the left. Trees are stored
		// contiguously in the array as follows: a node
		// in the prefix tree is immediately followed in
		// memory by its entire subtree, starting with
		// the root node of its children's binary tree.
		// Nodes in a binary tree are immediately
		// followed in memory first by the entire left
		// subtree, then the entire right subtree.
		//
		// The first element of a node is the length of
		// the node's string. The node's string
		// follows. The element after the string
		// contains the number of child nodes in the radix
		// tree. If this number is 0, the next element
		// indexes into the value array for the value of
		// the key string at that location in the tree. If
		// the node's string has no length (meaning that a
		// key is stored in the tree which is the prefix
		// of another key), then the first element of the
		// node is 0 and the node immediately afterward
		// indexes into the value array. If the node has
		// children to the left in the binary tree, the
		// element after the radix tree child count is the
		// relative index of the child to the left in the
		// binary tree. If the node has children to the
		// right in the binary tree, the element after the
		// left binary tree child relative index is the
		// relative index of the child to the right in the
		// binary tree. (A node has a right binary tree
		// child only if it has a left binary tree child).
		// The first element of the array is the number of
		// children in the prefix tree under the root node.
		
		readonly char[] tree;
		readonly T[] values;
		
		public ContiguousSplayedRadixTree (IList<KeyValuePair<string, T>> keyValuePairs)
		{
			// The CSRT has a very compact, very specific layout in memory.
			// The construction of this layout is non-trivial. We start with
			// a sorted list of key-value pairs.
			
			var children = new List<SinglyLinkedList<char>> ();
			var values = new List<T> (keyValuePairs.Count);
			var index = 0;
			
			// We collect the root nodes of the radix tree
			do {
				children.Add (Probe (keyValuePairs, values, ref index, 0, keyValuePairs.Count));
			} while (index < keyValuePairs.Count);
			
			// The radix tree starts with the number of children under
			// the root, followed by the binary tree of those children.
			var root = new SinglyLinkedList<char> ();
			root.AddFirst ((char)children.Count);
			root.AddLast (CreateBinaryTree (children));
			
			// We render the value and tree arrays to their final form.
			this.values = values.ToArray ();
			tree = new char[root.Count];
			var node = root.First;
			for (var i = 0; i < root.Count; i++) {
				tree[i] = node.Value;
				node = node.Next;
			}
		}
		
		static SinglyLinkedList<char> Probe (IList<KeyValuePair<string, T>> keyValuePairs, List<T> values, ref int index, int keyIndex, int probeLimit)
		{
			var pair = keyValuePairs[index];
			var key = pair.Key;
			var key_length = key.Length;
			
			if (keyIndex == key_length) {
				
				// We are trying to probe a subtree which doesn't exist.
				// This will only happen if there are duplicate keys.
				throw new ArgumentException ("There is a duplicate key in the set.");
			}
			
			// We probe ahead to see how many other keys share
			// the first character (the prefix).
			var probe_depth = GetProbeDepth (keyValuePairs, index, keyIndex, probeLimit);
			if (probe_depth - 1 == index) {
				
				// Nothing else has this prefix, so we are at a leaf node.
				// Leaf nodes begin with the length of the node's string,
				// followed by the string, followed by 0, followed by
				// the index of the matching value in the value array.
				var leaf_node = new SinglyLinkedList<char> ();
				leaf_node.AddLast ((char)(key_length - keyIndex));
				for (var i = keyIndex; i < key_length; i++) {
					leaf_node.AddLast (key[i]);
				}
				leaf_node.AddLast ((char)0);
				leaf_node.AddLast ((char)values.Count);
				values.Add (pair.Value);
				index++;
				
				// A leaf node has no children, so we return it.
				return leaf_node;
			}
			
			// We know that there is a shared prefix but so far we have
			// only looked at the first character. Now we extend the
			// character we are looking at as long as the same depth
			// of keys match. This will give us the full prefix.
			var key_probe_index = keyIndex;
			do {
				key_probe_index++;
			} while (key_probe_index < key_length && probe_depth == GetProbeDepth (keyValuePairs, index, key_probe_index, probeLimit));
			
			// We now know the full prefix string for this node.
			
			// Non-leaf nodes start with the length of the prefix
			// string, followed by the prefix string, followed
			// by the number of child nodes in the readix tree,
			// followed by the relative offset of the left sub-
			// node in this node's binary tree, followed by the
			// relative offset of the right sub-node in this
			// node's binary tree (if it exists).
			var node = new SinglyLinkedList<char> ();
			node.AddLast ((char)(key_probe_index - keyIndex));
			for (var i = keyIndex; i < key_probe_index; i++) {
				node.AddLast (key[i]);
			}
			
			var children = new List<SinglyLinkedList<char>> ();
			
			if (key_probe_index == key_length) {
				
				// The node's prefix string matches one of the
				// keys, so we add a leaf node to this node's
				// children.
				var leaf_node = new SinglyLinkedList<char> ();
				leaf_node.AddLast ((char)0);
				leaf_node.AddLast ((char)values.Count);
				values.Add (pair.Value);
				index++;
				children.Add (leaf_node);
			}
			
			// We recurse through the radix tree and collect
			// the children of this node.
			do {
				children.Add (Probe (keyValuePairs, values, ref index, key_probe_index, probe_depth));
			} while (index < probe_depth);
			
			node.AddLast ((char)children.Count);
			
			// We organize the children into a binary tree.
			node.AddLast (CreateBinaryTree (children));
			
			return node;
		}
		
		static int GetProbeDepth (IList<KeyValuePair<string, T>> keyValuePairs, int index, int keyIndex, int probeLimit)
		{
			var character = keyValuePairs[index].Key[keyIndex];
			do {
				index++;
			} while (index < probeLimit &&
			         keyIndex < keyValuePairs[index].Key.Length &&
			         character == keyValuePairs[index].Key[keyIndex]);
			return index;
		}
		
		static SinglyLinkedList<char> CreateBinaryTree (List<SinglyLinkedList<char>> children)
		{
			var midpoint = children.Count >> 0x1;
			return CreateBinaryTree (children, midpoint, midpoint, children.Count - midpoint - 1);
		}
		
		static SinglyLinkedList<char> CreateBinaryTree (List<SinglyLinkedList<char>> nodes, int index, int left, int right)
		{
			var node = nodes[index];
			var insertion_point = node.First;
			var length = insertion_point.Value;
			for (var i = 0; i <= length; i++) {
				insertion_point = insertion_point.Next;
			}
			if (insertion_point.Value == 0) {
				insertion_point = insertion_point.Next;
			}
			if (left > 0) {
				var new_left = left >> 0x1;
				var new_index = (index - left) + new_left;
				var left_node = CreateBinaryTree (nodes, new_index, new_left, left - new_left - 1);
				var relative_index = node.Count - length;
				if (right > 0) {
					relative_index++;
				}
				node.AddAfter ((char)relative_index, insertion_point);
				insertion_point = insertion_point.Next;
				node.AddLast (left_node);
			}
			if (right > 0) {
				var new_right = right >> 0x1;
				var new_index = (index + right) - new_right;
				var right_node = CreateBinaryTree (nodes, new_index, right - new_right - 1, new_right);
				var relative_index = node.Count - length;
				node.AddAfter ((char)relative_index, insertion_point);
				node.AddLast (right_node);
			}
			return node;
		}
		
		// Time Complexity:
		//		O(1) worst case WRT to number of values in the structure
		//		O(n) worst case WRT the length of 'key'
		public T this[string key] {
			get {
				var value_index = GetValueIndex (key);
				if (value_index == -1) {
					throw new KeyNotFoundException ();
				}
				return values[value_index];
			}
		}
		
		// Time Complexity:
		//		O(1) worst case WRT to number of values in the structure
		//		O(n) worst case WRT the length of 'key'
		public bool ContainsKey (string key)
		{
			return GetValueIndex (key) != -1;
		}
		
		int GetValueIndex (string key)
		{
			var key_length = key.Length;
			var key_index = 0;
			var tree_index = 1;
			
			// The first element of the tree has the
			// number of children under the root.
			// These children are in a balanced binary
			// tree. We split this number into the left
			// and right space of the binary tree.
			var left = tree[0] >> 1;
			var right = tree[0] - left - 1;
			
			while (true) {
				
				// The first element of a node is the length
				// of the node's string. 
				var length = tree[tree_index];
				
				// If the node has a string, we step into it.
				
				// TODO if we really wanted to, we could use
				// a goto in an else to this if and jump
				// over a comparison or two, but goto is EVIL.
				// What to do, what to do, what to do!
				if (length != 0) {
					tree_index++;
					
					if (key_index == key_length || key[key_index] < tree[tree_index]) {
						
						// The key would be to our left
						// in the binary tree.
						
						if (left == 0) {
							
							// There is nothing to our left in the
							// binary tree, so the key does not exist.
							return -1;
						}
						
						// We move left in the binary tree.
						
						tree_index += length;
						if (tree[tree_index] == 0) {
						
							// If the node has no children, the relative
							// address of the node to our left in the
							// binary tree is stored two after 'end'
							tree_index += tree[tree_index + 2];
						} else {
							
							// If the node has children, the relative
							// address of the node to our left in the
							// binary tree is stored directly after 'end'
							tree_index += tree[tree_index + 1];
						}
						
						// We split the left and right values
						
						// If we get strapped for registers, we can do:
						// right = (left >> 0x1) + (left & 0x1) - 1;
						var new_left = left >> 0x1;
						right = left - new_left - 1;
						left = new_left;
						
						// Step ahead
						continue;
					} else if (key[key_index] > tree[tree_index]) {
						
						// The key would be to our right
						// in the binary tree.
						
						if (right == 0) {
							
							// There is nothing to our right in the
							// binary tree, so the key does not exist.
							return -1;
						}
						
						// We move right in the binary tree.
						
						tree_index += length;
						if (tree[tree_index] == 0) {
						
							// If the node has no children, the relative
							// address of the node to our right in the
							// binary tree is stored three after 'end'
							tree_index += tree[tree_index + 3];
						} else {
							
							// If the node has children, the relative
							// address of the node to our left in the
							// binary tree is stored two after 'end'
							tree_index += tree[tree_index + 2];
						}
						
						// We split the left and right values
						
						// If we get strapped for registers, we can do:
						// left = (right >> 0x1) + (right & 0x1) - 1;
						var new_right = right >> 0x1;
						left = right - new_right - 1;
						right = new_right;
						
						// Step ahead
						continue;
					} else {
						
						// The key must match this node, so we
						// check the whole thing.
						tree_index++;
						key_index++;
						length--;
						
						while (length != 0) {
							if (key_index == key_length) {
							
								// We have consumed the input string
								// and no such key exists.
								return -1;
							}
							if (tree[tree_index] != key[key_index]) {
							
								// The input string does not match
								// the rest of the prefix so the
								// key does not exist.
								return -1;
							}
							
							tree_index++;
							key_index++;
							length--;
						}
					}
				}
				
				// The node matches the input and
				// tree_index is positioned on the
				// number of children under this
				// node in the radix tree.
				var children = tree[tree_index];
				
				if (children == 0) {
					
					// We have reached the end of the tree.
					
					if (key_index == key_length) {
					
						// And we have consumed all of the
						// input string, so we have a match!
						// The next element in the tree indexes
						// into the value array.
						return tree[tree_index + 1];
					} else {
					
						// There is more input string, but no
						// more tree! The key does not exist.
						return -1;
					}
				} else {
				
					// We are at a non-key prefix node in the radix
					// tree. Children of this node are stored in a
					// balanced binary tree.
					
					// The root node of that binary tree is next up
					// in the array.
					
					tree_index++;
					
					if (left != 0) {
						
						// If there is something to our left, we need
						// to skip over its relative index.
						tree_index++;
					}
					
					if (right != 0) {
						
						// If there is something to our right, we need
						// to skip over its relative index.
						tree_index++;
					}
					
					// We split the child count into the left and
					// right space of the new binary tree.
					left = children >> 1;
					right = children - left - 1;
				}
			}
		}
	}
}