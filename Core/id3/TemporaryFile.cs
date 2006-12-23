#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.IO;

namespace Roger.ID3
{
	/// <summary>
	/// Utility class to encapsulate handling temporary files.
	/// </summary>
	class TemporaryFile
	{
		string path;
		string original;

		/// <summary>
		/// Create a temporary file with a name based on the name of an original file.
		/// </summary>
		/// <param name="original">The template filename, essentially</param>
		public TemporaryFile(string original)
		{
			string temp_suffix = ".new";

			this.original = original;
			this.path = original + temp_suffix;
		}

		public void Swap()
		{
			// Then we can swap the files over.  It's a three step process:
			// foo -> foo.old
			// foo.new -> foo
			// delete foo.old
			string old = original + ".old";
			File.Move(original, old);
			File.Move(path, original);
			File.Delete(old);
		}

		public string Path
		{
			get
			{
				return this.path;
			}
		}

		public string OriginalPath
		{
			get
			{
				return this.original;
			}
		}
	}
}
