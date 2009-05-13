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

namespace Mono.Collections
{
	// Genus: Trie (a.k.a. Prefix Tree)
	//
	// Species: Radix Tree (a.k.a. Crit Bit Trie a.k.a. Patricia Trie)
	//
	// Unique Features: Child nodes are stored in a binary tree
	//
	// Capabilities: Value lookup by string key
	//
	// Pros: Fast lookup, low memory overhead
	//
	// Cons: Readonly, 65,536 maximum capacity, 2,147,483,648 maximum structure size
	//
	// Implementation: The structure is implemented in a single char array
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
	
	sealed class ContiguousSplayedRadixTree<T>
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
		// the node's string plus 1. The node's string
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
		
		public ContiguousSplayedRadixTree (IEnumerable<KeyValuePair<string, T>> keyValuePairs)
		{
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
				
				// The first element of a node is one
				// more than the length of the node's
				// string, or 0 if the node does not
				// have a string.
				var end = tree_index + tree[tree_index];
				
				// If the node has a string, we step into it.
				if (end != 0) {
					tree_index++;
					
					// We compare the first character of the node
					// with the current character in the key.
					
					// If we are strapped for registers, we can
					// forgo the element value caching.
					var tree_char = tree[tree_index];
					var key_char = key[key_index];
					
					if (tree_char == key_char) {
						
						// They are equal, so we check the entire node.
						key_index++;
						
						while (tree_index < end) {
							if (key_index == key_length) {
								
								// We have consumed the input string
								// so the key does not exist.
								return -1;
							}
							
							if (tree[tree_index] != key[key_index]) {
							
								// The input string does not match
								// the rest of the prefix so the
								// key does not exist.
								return -1;
							}
							
							key_index++;
						}
					} else if (key_char < tree_char) {
						
						// The key would be to our left
						// in the binary tree.
						
						if (left == 0) {
							
							// There is nothing to our left in the
							// binary tree, so the key does not exist.
							return -1;
						}
						
						// We move left in the binary tree. The
						// relative index of the node to our left
						// in the binary tree is stored in the
						// element two after the node's string.
						tree_index = end + tree[end + 1];
						
						// We split the left and right values
						
						// If we get strapped for registers, we can do:
						// right = (left >> 0x1) + (left & 0x1) - 1;
						var new_left = left >> 0x1;
						right = left - new_left - 1;
						left = new_left;
						
						// Step ahead
						continue;
					} else {
						
						// The key would be to our right
						// in the binary tree.
						
						if (right == 0) {
							
							// There is nothing to our right in the
							// binary tree, so the key does not exist.
							return -1;
						}
						
						// We move right in the binary tree. The
						// relative index of the node to our right
						// in the binary tree is stored in the
						// element three after the node's string.
						tree_index = end + tree[end + 2];
						
						// We split the left and right values
						
						// If we get strapped for registers, we can do:
						// left = (right >> 0x1) + (right & 0x1) - 1;
						var new_right = right >> 0x1;
						left = right - new_right - 1;
						right = new_right;
						
						// Step ahead
						continue;
					}
				}
				
				// The node matches the input and
				// tree_index is positioned on the
				// number of children under this
				// node in the radix tree.
				var children = tree[tree_index];
				
				if (children == 0) {
					
					// We are at a key in the tree
					
					if (key_index == key_length) {
					
						// And we have consumed all of the
						// input string, so we have a match!
						// The next element in the tree indexes
						// into the value array.
						return tree[tree_index + 1];
					} else {
					
						// The input string is longer than this
						// key, meaning it would be to our right
						// in the binary tree.
						
						if (right == 0) {
							
							// There is nothing to our right in the
							// binary tree, so the key does not exist.
							return -1;
						}
						
						// We move right in the binary tree. The
						// relative index of the node to our right
						// in the binary tree is stored in the
						// element two after the radix tree child count.
						tree_index += tree[tree_index + 2];
						
						// We split the left and right values.
						
						// If we get strapped for registers, we can do:
						// left = (right >> 0x1) + (right & 0x1) - 1;
						var new_right = right >> 0x1;
						left = right - new_right - 1;
						right = new_right;
					}
				} else {
				
					// We are at a non-key prefix node in the radix
					// tree. Children of this node are stored in a
					// balanced binary tree.
					
					// The root node of that binary tree is next up
					// in the array.
					if (right == 0) {
						
						// If there are no nodes to the right in this node's
						// binary tree, we need to skip ahead 2 elements
						// to reach the root of the next binary tree.
						tree_index += 2;
					} else {
						
						// Otherwise we skip ahead 3.
						tree_index += 3;
					}
					
					// We split the child count into the left and
					// right space of the new binary tree.
					left = children >> 1;
					right = children - left - 1;
				}
			}
			
			// We will never hit this.
			return -1;
		}
	}
}