using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;

namespace ExpressSharp
{
	public delegate Task NextHandler();

	public delegate void ErrorHandler(Exception err);
	public delegate Task ErrorHandlerAsync(Exception err);

	public delegate void RequestHandler(Request req, Response res);
	public delegate Task RequestHandlerAsync(Request req, Response res);

	public delegate void RequestNextHandler(Request req, Response res, NextHandler next);
	public delegate Task RequestNextHandlerAsync(Request req, Response res, NextHandler next);

	public delegate void ErrorRequestNextHandler(Exception err, Request req, Response res, NextHandler next);
	public delegate Task ErrorRequestNextHandlerAsync(Exception err, Request req, Response res, NextHandler next);

	public class Express
	{
		public bool Running { get; set; }
		public int Port { get; private set; }

		private HttpListener httpListener;
		private Task runnerTask;
		private Dictionary<String, List<Delegate>> handlers = new Dictionary<String, List<Delegate>>();

		public void Listen(int port)
		{
			Running = true;
			Port = port;
			httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://*:" + port + "/");

			runnerTask = Task.Run((Action)MainListenLoop);
		}

		public void Wait() => Task.WaitAll(runnerTask);

		public void Use(RequestHandler handler) => RegisterHandler("/", handler);

		public void Use(RequestHandlerAsync handler) => RegisterHandler("/", handler);

		public void Use(ErrorHandler handler) => RegisterHandler("/", handler);

		public void Use(ErrorHandlerAsync handler) => RegisterHandler("/", handler);

		public void Use(RequestNextHandler handler) => RegisterHandler("/", handler);

		public void Use(RequestNextHandlerAsync handler) => RegisterHandler("/", handler);

		public void Use(ErrorRequestNextHandler handler) => RegisterHandler("/", handler);

		public void Use(ErrorRequestNextHandlerAsync handler) => RegisterHandler("/", handler);

		private void RegisterHandler(string baseRoute, Delegate handler)
		{
			if(!handlers.ContainsKey(baseRoute))
				handlers.Add(baseRoute, new List<Delegate>());
			handlers[baseRoute].Add(handler);
		}

		private void MainListenLoop()
		{
			httpListener.Start();
			Console.WriteLine("Listening on port " + Port);
			while (Running)
			{
				var context = httpListener.GetContext();
				HandleSocketAsync(context);
			}
		}

		private async void HandleSocketAsync(HttpListenerContext context)
		{
			var request = new Request
			{
				HTTPVersion = context.Request.ProtocolVersion.ToString(),
				HTTPVersionMajor = context.Request.ProtocolVersion.Major,
				HTTPVersionMinor = context.Request.ProtocolVersion.Minor,

				Headers = context.Request.Headers,
				Query = context.Request.QueryString,
				// TODO Body and Params (in that order),

				URL = context.Request.RawUrl,
				Protocol = context.Request.Url.Scheme,
				Domain = context.Request.Url.Host,
				Port = context.Request.Url.Port,
				Route = context.Request.Url.AbsolutePath,

				RawMethod = context.Request.HttpMethod,
				ContentType = context.Request.ContentType,
			};
			foreach (Cookie cookie in context.Request.Cookies)
				request.Cookies.Add(cookie);

			var handlers = GetRouteHandlers(request.Route);
			try
			{
				await HandleRouteAsync(request, new Response(context.Response), handlers, 0);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
			}
		}

		private List<Delegate> GetRouteHandlers(string route)
		{
			List<Delegate> handlers = new List<Delegate>();
			foreach(var handlerRoute in this.handlers.Keys)
			{
				if(route.StartsWith(handlerRoute))
				{
					handlers.AddRange(this.handlers[handlerRoute]);
				}
			}
			return handlers;
		}

		private async Task HandleRouteAsync(Request req, Response res, List<Delegate> handlers, int itemToHandle)
		{
			try
			{
				if(itemToHandle >= handlers.Count)
				{
					await res.Close();
					return;
				}
				if(handlers[itemToHandle] is RequestHandler)
				{
					(handlers[itemToHandle] as RequestHandler).Invoke(req, res);
					await res.Close();
				}
				else if(handlers[itemToHandle] is RequestHandlerAsync)
				{
					await (handlers[itemToHandle] as RequestHandlerAsync).Invoke(req, res);
					await res.Close();
				}
				else if(handlers[itemToHandle] is RequestNextHandler)
				{
					var handledNext = false;
					(handlers[itemToHandle] as RequestNextHandler).Invoke
					(
						req,
						res,
						async () =>
						{
							handledNext = true;
							await HandleRouteAsync(req, res, handlers, ++itemToHandle);
						}
					);
					if(!handledNext)
						await res.Close();
				}
				else if(handlers[itemToHandle] is RequestNextHandlerAsync)
				{
					var handledNext = false;
					await (handlers[itemToHandle] as RequestNextHandlerAsync).Invoke
					(
						req,
						res,
						async () =>
						{
							handledNext = true;
							await HandleRouteAsync(req, res, handlers, ++itemToHandle);
						}
					);
					if(!handledNext)
						await res.Close();
				}
				else if(handlers[itemToHandle] is ErrorRequestNextHandler)
				{
					var handledNext = false;
					(handlers[itemToHandle] as ErrorRequestNextHandler).Invoke
					(
						null,
						req,
						res,
						async () =>
						{
							handledNext = true;
							await HandleRouteAsync(req, res, handlers, ++itemToHandle);
						}
					);
					if(!handledNext)
						await res.Close();
				}
				else if(handlers[itemToHandle] is ErrorRequestNextHandlerAsync)
				{
					var handledNext = false;
					await (handlers[itemToHandle] as ErrorRequestNextHandlerAsync).Invoke
					(
						null,
						req,
						res,
						async () =>
						{
							handledNext = true;
							await HandleRouteAsync(req, res, handlers, ++itemToHandle);
						}
					);
					if(!handledNext)
						await res.Close();
				}
			}
			catch(Exception ex)
			{
				List<Delegate> errorHandlers = new List<Delegate>();
				foreach(var handler in handlers)
				{
					if
					(
						handler is ErrorHandler ||
						handler is ErrorHandlerAsync ||
						handler is ErrorRequestNextHandler ||
						handler is ErrorRequestNextHandlerAsync
					)
					{
						errorHandlers.Add(handler);
					}
				}
				await HandleErrorAsync(ex, req, res, errorHandlers, 0);
			}
		}

		private async Task HandleErrorAsync(Exception err, Request req, Response res, List<Delegate> handlers, int itemToHandle)
		{
			if(itemToHandle >= handlers.Count)
			{
				await res.Close();
				return;
			}
			if(handlers[itemToHandle] is ErrorHandler)
			{
				(handlers[itemToHandle] as ErrorHandler).Invoke(err);
				await res.Close();
			}
			else if(handlers[itemToHandle] is ErrorHandlerAsync)
			{
				await (handlers[itemToHandle] as ErrorHandlerAsync).Invoke(err);
				await res.Close();
			}
			else if(handlers[itemToHandle] is ErrorRequestNextHandler)
			{
				var handledNext = false;
				(handlers[itemToHandle] as ErrorRequestNextHandler).Invoke
				(
					err,
					req,
					res,
					async () =>
					{
						handledNext = true;
						await HandleRouteAsync(req, res, handlers, ++itemToHandle);
					}
				);
				if(!handledNext)
					await res.Close();
			}
			else if(handlers[itemToHandle] is ErrorRequestNextHandlerAsync)
			{
				var handledNext = false;
				await (handlers[itemToHandle] as ErrorRequestNextHandlerAsync).Invoke
				(
					err,
					req,
					res,
					async () =>
					{
						handledNext = true;
						await HandleRouteAsync(req, res, handlers, ++itemToHandle);
					}
				);
				if(!handledNext)
					await res.Close();
			}
		}
	}
}