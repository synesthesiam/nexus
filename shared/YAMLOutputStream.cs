using System;
using System.Linq;
using System.IO;

namespace Nexus.Shared
{
	public class YAMLOutputStream
	{
		public string IndentString { get; set; }
		public int IndentLevel { get; set; }
		
		protected TextWriter writer = null;
		protected bool ownsWriter = false;
		protected int listLevel = 0;
		protected string itemPrefix = "";
		
		protected YAMLOutputStream()
		{
			IndentString = "  ";
			IndentLevel = 0;
		}

		public YAMLOutputStream(string filePath) : this()
		{
			writer = new StreamWriter(filePath)
			{
				AutoFlush = true
			};

			ownsWriter = true;
		}
		
		public YAMLOutputStream(TextWriter writer) : this()
		{
			this.writer = writer;
		}

		public void Close()
		{
			if (ownsWriter)
			{
				writer.Close();
			}
		}

		public void WriteLine()
		{
			writer.WriteLine();
		}

		public void WriteStartDocument()
		{
			writer.WriteLine("---");
		}

		public void WriteEndDocument()
		{
			writer.WriteLine("...");
		}

		public void WriteHashSingle(object key, object value)
		{
			WriteIndent();
			writer.WriteLine("{0}{1}: {2}", itemPrefix, key, value);
		}

		public void WriteHashMultiple(object key, object firstValue, params object[] otherValues)
		{
			WriteIndent();
			writer.WriteLine("{0}{1}:", itemPrefix, key);

			IndentLevel++;
			WriteIndent();

			foreach (var value in Enumerable.Concat(new object[] { firstValue }, otherValues))
			{
				writer.WriteLine(value);
			}

			IndentLevel--;
		}

		public void WriteStartList(object header)
		{
			WriteIndent();
			writer.WriteLine("{0}{1}:", itemPrefix, header);
			itemPrefix = "- ";
			listLevel++;
			IndentLevel++;
		}

		public void WriteText(object item)
		{
			WriteIndent();
			writer.WriteLine("{0}{1}", itemPrefix, item);
		}

		public void WriteEndList()
		{
			if (listLevel <= 0)
			{
				return;
			}
			
			IndentLevel--;
			listLevel--;

			if (listLevel < 1)
			{
				itemPrefix = "";
			}
		}

		public void WriteComment(object commentText)
		{
			WriteIndent();
			writer.WriteLine("# {0}", commentText);
		}

		protected void WriteIndent()
		{
			if (IndentLevel < 1)
			{
				return;
			}

			writer.Write(string.Join(string.Empty, Enumerable.Range(0, IndentLevel)
				.Select(i => IndentString).ToArray())
			);
		}
	}
}
