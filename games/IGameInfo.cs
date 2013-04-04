using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Nexus
{
	public interface IGameInfo
	{
		string GameName { get; }
		string GameDescription { get; set; }
		
		IDictionary<string, string> GetArguments();
		void Load(XElement element);
		void Save(XmlTextWriter writer);
	}
}
