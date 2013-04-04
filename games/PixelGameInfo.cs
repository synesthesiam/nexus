using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using Nexus.Shared;

namespace Nexus
{
	public class PixelGameInfo : IGameInfo	
	{
		public int MaxSize { get; set; }
		public string PlayerSort { get; set; }
		public string InitialState { get; set; }
		public IList<Point> FixedPixels { get; protected set; }
		
		public PixelGameInfo()
		{
			FixedPixels = new List<Point>();
		}
		
		public PixelGameInfo(XElement element) : this()
		{
			Load(element);
		}
	
		#region IGameInfo Members
		
		public string GameName
		{
			get { return ("Pixel"); }
		}

		public string GameDescription { get; set; }
		
		public IDictionary<string, string> GetArguments()
		{
			var arguments = new Dictionary<string, string>();
			
			arguments["max-size"] = MaxSize.ToString() ;
			arguments["player-sort"] = PlayerSort ;
			arguments["initial-state"] = InitialState ;
			arguments["fixed-pixels"] = string.Join(string.Empty, FixedPixels.Select(p =>
				CLIObject.ToString(p, "X", "Y")).ToArray()
			);
			
			return (arguments);
		}
		
		public void Load(XElement element)
		{
			MaxSize = Convert.ToInt32(element.Element("maxSize").Value);
			PlayerSort = element.Element("playerSort").Value;
			InitialState = element.Element("initialState").Value;

			var fixedPixelsElement = element.Element("fixedPixels");
			FixedPixels.Clear();

			if (fixedPixelsElement != null)
			{
				foreach (var pixelElement in fixedPixelsElement.Elements("pixel"))
				{
					FixedPixels.Add(new Point(Convert.ToInt32(pixelElement.Element("x").Value),
						Convert.ToInt32(pixelElement.Element("y").Value))
					);
				}
			}
		}
		
		public void Save(XmlTextWriter writer)
		{
			writer.WriteElementString("maxSize", MaxSize.ToString());
			writer.WriteElementString("playerSort", PlayerSort);
			writer.WriteElementString("initialState", InitialState);

			if (FixedPixels.Count > 0)
			{
				writer.WriteStartElement("fixedPixels");

				foreach (var point in FixedPixels)
				{
					writer.WriteStartElement("pixel");
					writer.WriteElementString("x", point.X.ToString());
					writer.WriteElementString("y", point.Y.ToString());
					writer.WriteEndElement();
				}
				
				writer.WriteEndElement();
			}
		}
		
		#endregion
	}
}
