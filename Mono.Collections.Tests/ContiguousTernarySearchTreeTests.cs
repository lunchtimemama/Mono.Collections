// 
// ContiguousTernarySearchTreeTests.cs
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
using System.IO;

using Mono.Collections.DataStructures;

using NUnit.Framework;

namespace Mono.Collections.Tests
{
	[TestFixture]
	public class ContiguousTernarySearchTreeTests
	{
		[Test]
		public void SortedInputTest ()
		{
			var pairs = new List<KeyValuePair<string, string>> ();
			pairs.Add (new KeyValuePair<string, string> ("ARG_Browse", "string"));
			pairs.Add (new KeyValuePair<string, string> ("ARG_Browse_Flags", "int"));
			pairs.Add (new KeyValuePair<string, string> ("ARG_Browse_Limit", "int"));
			pairs.Add (new KeyValuePair<string, string> ("ARG_Browse_Offset", "int"));
			pairs.Add (new KeyValuePair<string, string> ("ARG_Search_Flags", "int"));
			pairs.Add (new KeyValuePair<string, string> ("System_Id", "uuid"));
			pairs.Add (new KeyValuePair<string, string> ("System_Update_Id", "uuid"));
			
			var csrt = new ContiguousTernarySearchTree<string> (pairs);
			foreach (var pair in pairs) {
				Assert.AreEqual (pair.Value, csrt[pair.Key]);
			}
			
			Assert.IsFalse (csrt.ContainsKey ("Foo"));
			Assert.IsFalse (csrt.ContainsKey ("AR"));
			Assert.IsFalse (csrt.ContainsKey ("ARG_"));
			Assert.IsFalse (csrt.ContainsKey ("ARG_Browse_Foo"));
		}
		
		[Test]
		public void SortedWorldInputTest ()
		{
			using (var reader = new StreamReader (Path.Combine ("data", "world"))) {
				var pairs = new List<KeyValuePair<string, bool>> ();
				while (!reader.EndOfStream) {
					pairs.Add (new KeyValuePair<string, bool> (reader.ReadLine (), true));
				}
				var csrt = new ContiguousTernarySearchTree<bool> (pairs);
				foreach (var pair in pairs) {
					Assert.IsTrue (csrt[pair.Key]);
				}
			}
		}
	}
}
