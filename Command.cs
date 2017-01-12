//
// Authors:
//  Rolf Bjarne Kvinge (rolfbjarne@gmail.com)
//
// Copyright (C) 2012 Rolf Bjarne Kvinge
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gui_diff
{
	public delegate void CommandAction (string cmd);

	public class Commands : List<Command>
	{
		public void Add (string Words, string Help, CommandAction Action)
		{
			Command c = new Command ();
			c.Words = Words.Split ('|');
			c.Action = Action;
			c.Help = Help;
			base.Add (c);
		}

		public void Help ()
		{
			int max_length = 0;
			foreach (Command c in this)
				max_length = Math.Max (max_length, string.Join ("|", c.Words).Length);

			foreach (Command c in this) {
				Console.WriteLine (" {0,-" + max_length.ToString () + "}: {1}", string.Join ("|", c.Words), c.Help);
			}
		}

		public bool Execute (string cmd)
		{
			foreach (Command c in this) {
				if (c.Execute (cmd))
					return true;
			}
			return false;
		}
	}

	public class Command
	{
		public string [] Words;
		public CommandAction Action;
		public string Help;

		public bool Execute (string Word)
		{
			foreach (string w in Words) {
				if (w == Word) {
					try {
						Action (Word);
					} catch (DiffException ex) {
						Console.WriteLine (ex.Message);
					} catch (Exception ex) {
						Console.WriteLine (ex.ToString ());
					}
					return true;
				}
			}
			return false;
		}
	}
}
