using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ArcaeaCoverMaker.Logging
{
	public enum LogLevel
	{
		None,
		Message,
		Debug,
		Warning,
		Error
	}
	public class Logger : StreamWriter
	{
		public Logger(FileStream fileStream, bool printLog) : base(fileStream)
		{
			m_PrintLog = printLog;
		}

		private readonly bool m_PrintLog;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Logger CreateLogger(string filePath, bool printLog = true)
		{
			return new Logger(File.Open(filePath, FileMode.OpenOrCreate), printLog);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task LogTask(LogLevel logLevel, string? msg, params object[] args)
		{
			var newLine = Environment.NewLine;
			var sb = new StringBuilder($"{newLine}[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
			sb.Append(newLine);
			if (!string.IsNullOrWhiteSpace(msg))
			{
				sb.Append($"[{logLevel}]");
				sb.Append(newLine);
				sb.Append(msg);
				sb.Append(newLine);
			}
			if (args != null && args.Length != 0)
			{
				sb.Append("[Objects]");
				sb.Append(newLine);
				sb.AppendJoin(newLine, args);
				sb.Append(newLine);
				sb.Append(newLine);
			}

			string log = sb.ToString();

			if (m_PrintLog)
			{
				await Task.Run(() => Trace.WriteLine(log));
			}

			await WriteLineAsync(log);
			await FlushAsync();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async void Log(LogLevel logLevel, string? msg, params object[] args)
		{
			await LogTask(logLevel, msg, args);
		}

		#region Fast logging

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async void LogMessage(string? msg, params object[] args)
		{
			await LogTask(LogLevel.Message, msg, args);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async void LogWarning(string? msg, params object[] args)
		{
			await LogTask(LogLevel.Warning, msg, args);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async void LogError(string? msg, params object[] args)
		{
			await LogTask(LogLevel.Error, msg, args);
		}

		#endregion
	}
}
