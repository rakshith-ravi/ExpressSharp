/*
app
charset
clearCookie
connection
contenttype
cookie
cork
destory
detachSocket
download
flushHeaders
getHeaders
headersSent
pipe
redirect
render
sendDate
sendFile
sendStatus
status
statusCode
statusMessage
type (.html, .mp3)...includes setting content type
writable
write
writeHead
 */

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ExpressSharp
{
	public class Response
	{
		private HttpListenerResponse response;

		internal Response(HttpListenerResponse response)
		{
			this.response = response;
		}

		public async Task<Response> Send(string data)
		{
			var bytes = Encoding.UTF8.GetBytes(data);
			await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
			return this;
		}

		public async Task<Response> SendLine(string data)
		{
			var bytes = Encoding.UTF8.GetBytes(data + "\n");
			await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
			return this;
		}

		public async Task<Response> Json(object data)
		{
			var output = "{";
			var properties = data.GetType().GetProperties();
			for(var i = 0; i < properties.Length; i++)
			{
				var property = properties[i];
				if(!property.GetMethod.IsPublic)
					continue;

				var returnType = property.GetMethod.ReturnType;
				if
				(
					returnType == typeof(byte) ||
					returnType == typeof(short) ||
					returnType == typeof(int) ||
					returnType == typeof(long) ||
					returnType == typeof(float) ||
					returnType == typeof(double) ||
					returnType == typeof(bool)
				)
				{
					output += "\"" + property.Name + "\":" + property.GetValue(data).ToString();
				}
				else
				{
					var value = property.GetValue(data);
					if(value == null)
						output += "\"" + property.Name + "\":null";
					else
						output += "\"" + property.Name + "\":\"" + value.ToString() + "\"";
				}
				if(i < properties.Length - 1)
					output += ",";
			}
			output += "}";

			var bytes = Encoding.UTF8.GetBytes(output);
			await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);

			return this;
		}

		public async Task Close()
		{
			await response.OutputStream.FlushAsync();
			response.OutputStream.Close();
		}
	}
}