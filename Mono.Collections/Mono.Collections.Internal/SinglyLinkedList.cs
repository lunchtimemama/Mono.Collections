// 
// SinglyLinkedList.cs
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

namespace Mono.Collections.Internal
{
	// This is an internal utility linked list. It is intended purely
	// for use by code under our control. It does not have a safe API
	// and can be made inconsistant (WRT the Count property). It
	// provides a fast, small, simple singly linked list for code
	// which knows what it's doing.
	
	sealed class SinglyLinkedList<T>
	{
		public void AddFirst (T value)
		{
			First = new SinglyLinkedListNode<T> (value, First);
			if (Last == null) {
				Last = First;
			}
			Count++;
		}
		
		public void AddLast (T value)
		{
			if (First == null) {
				AddFirst (value);
			} else {
				Last.Next = new SinglyLinkedListNode<T> (value);
				Last = Last.Next;
				Count++;
			}
		}
		
		public void AddFirst (SinglyLinkedList<T> list)
		{
			list.Last.Next = First;
			First = list.First;
			Count += list.Count;
			list.First = null;
			list.Last = null;
			list.Count = 0;
		}
		
		public void AddLast (SinglyLinkedList<T> list)
		{
			Last.Next = list.First;
			Last = list.Last;
			Count += list.Count;
			list.First = null;
			list.Last = null;
			list.Count = 0;
		}
		
		public void AddAfter (T value, SinglyLinkedListNode<T> node)
		{
			node.Next = new SinglyLinkedListNode<T> (value, node.Next);
			Count++;
			if (node == Last) {
				Last = node.Next;
			}
		}
		
		public SinglyLinkedListNode<T> First { get; private set; }
		
		public SinglyLinkedListNode<T> Last { get; private set; }
		
		public int Count { get; private set; }
	}
}
