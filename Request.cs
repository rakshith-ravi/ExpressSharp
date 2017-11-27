using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ExpressSharp
{
	public class Request
	{
		public Socket Socket { get; internal set; }

		public string HTTPVersion { get; internal set; }
		public int HTTPVersionMinor { get; internal set; }
		public int HTTPVersionMajor { get; internal set; }

		public NameValueCollection Headers { get; internal set; } = new NameValueCollection();
		public Dictionary<string, string> Params { get; internal set; } = new Dictionary<string, string>();
		public NameValueCollection Query { get; internal set; } = new NameValueCollection();
		public Dictionary<string, string> BodyMap { get; internal set; } = new Dictionary<string, string>();
		public dynamic Body;

		public string URL { get; internal set; }
		public string Protocol { get; internal set; }
		public string Domain { get; internal set; }
		public int Port { get; internal set; }
		public string Route { get; internal set; }

		public string RawMethod { get; internal set; }
		public HttpMethod Method
		{ 
			get
			{
				switch (RawMethod)
				{
					case "GET":
						return HttpMethod.Get;
					case "HEAD":
						return HttpMethod.Head;
					case "POST":
						return HttpMethod.Post;
					case "PUT":
						return HttpMethod.Put;
					case "DELETE":
						return HttpMethod.Delete;
					case "OPTIONS":
						return HttpMethod.Options;
					case "TRACE":
						return HttpMethod.Trace;
					default:
						return new HttpMethod(RawMethod);
				}
			}
		}
		public string ContentType { get; internal set; }

		public List<Cookie> Cookies { get; internal set; } = new List<Cookie>();
	}
}